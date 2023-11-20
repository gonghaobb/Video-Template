Shader "Hidden/Universal Render Pipeline/Bloom"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma multi_compile_local _ _USE_RGBM
        #pragma multi_compile _ _USE_DRAW_PROCEDURAL

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
    
        //PicoVideo;FoveatedFeature;YangFan;Begin
        #ifdef USE_FOVEATED_SUBSAMPLED_LAYOUT
            #define _SourceTex                          _SubsampledLayoutSourceTex
            #define _SubsampledLayoutSourceTexLowMip    _SourceTexLowMip
        #endif
        //PicoVideo;FoveatedFeature;YangFan;End
    
        TEXTURE2D_X(_SourceTex);
        float4 _SourceTex_TexelSize;
        
        TEXTURE2D_X(_SourceTexLowMip);
        float4 _SourceTexLowMip_TexelSize;

        float4 _Params; // x: scatter, y: clamp, z: threshold (linear), w: threshold knee

        #define Scatter             _Params.x
        #define ClampMax            _Params.y
        #define Threshold           _Params.z
        #define ThresholdKnee       _Params.w

        //PicoVideo;CustomBloom;ZhouShaoyang;Begin
        struct DownVaryings
        {
            float4 positionCS  : SV_POSITION;
            float2 uv          : TEXCOORD0;
            float4 uvs[2]      : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };

        struct UpVaryings
        {
            float4 positionCS  : SV_POSITION;
            float4 uvs[4]      : TEXCOORD0;
            UNITY_VERTEX_OUTPUT_STEREO
        };
        //PicoVideo;CustomBloom;ZhouShaoyang;End


        half4 EncodeHDR(half3 color)
        {
        #if _USE_RGBM
            half4 outColor = EncodeRGBM(color);
        #else
            half4 outColor = half4(color, 1.0);
        #endif

        #if UNITY_COLORSPACE_GAMMA
            return half4(sqrt(outColor.xyz), outColor.w); // linear to γ
        #else
            return outColor;
        #endif
        }

        half3 DecodeHDR(half4 color)
        {
        #if UNITY_COLORSPACE_GAMMA
            color.xyz *= color.xyz; // γ to linear
        #endif

        #if _USE_RGBM
            return DecodeRGBM(color);
        #else
            return color.xyz;
        #endif
        }

        half4 FragPrefilter(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);

        #if _BLOOM_HQ
            float texelSize = _SourceTex_TexelSize.x;
            half4 A = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, -1.0));
            half4 B = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.0, -1.0));
            half4 C = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, -1.0));
            half4 D = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-0.5, -0.5));
            half4 E = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.5, -0.5));
            half4 F = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, 0.0));
            half4 G = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv);
            half4 H = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, 0.0));
            half4 I = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-0.5, 0.5));
            half4 J = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.5, 0.5));
            half4 K = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, 1.0));
            half4 L = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.0, 1.0));
            half4 M = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, 1.0));

            half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

            half4 o = (D + E + I + J) * div.x;
            o += (A + B + G + F) * div.y;
            o += (B + C + H + G) * div.y;
            o += (F + G + L + K) * div.y;
            o += (G + H + M + L) * div.y;

            half3 color = o.xyz;
        #else
            half3 color = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv).xyz;
        #endif

            // User controlled clamp to limit crazy high broken spec
            color = min(ClampMax, color);

            // Thresholding
            half brightness = Max3(color.r, color.g, color.b);
            half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
            softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
            half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
            color *= multiplier;

            // Clamp colors to positive once in prefilter. Encode can have a sqrt, and sqrt(-x) == NaN. Up/Downsample passes would then spread the NaN.
            color = max(color, 0);
            return EncodeHDR(color);
        }

        half4 FragBlurH(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float texelSize = _SourceTex_TexelSize.x * 2.0;
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);

            // 9-tap gaussian blur on the downsampled source
            half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv - float2(texelSize * 4.0, 0.0)));
            half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv - float2(texelSize * 3.0, 0.0)));
            half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv - float2(texelSize * 2.0, 0.0)));
            half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv - float2(texelSize * 1.0, 0.0)));
            half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv                               ));
            half3 c5 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + float2(texelSize * 1.0, 0.0)));
            half3 c6 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + float2(texelSize * 2.0, 0.0)));
            half3 c7 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + float2(texelSize * 3.0, 0.0)));
            half3 c8 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + float2(texelSize * 4.0, 0.0)));

            half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
                        + c4 * 0.22702703
                        + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;

            return EncodeHDR(color);
        }

        half4 FragBlurV(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float texelSize = _SourceTex_TexelSize.y;
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);

            // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
            half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv - float2(0.0, texelSize * 3.23076923)));
            half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv - float2(0.0, texelSize * 1.38461538)));
            half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv                                      ));
            half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + float2(0.0, texelSize * 1.38461538)));
            half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + float2(0.0, texelSize * 3.23076923)));

            half3 color = c0 * 0.07027027 + c1 * 0.31621622
                        + c2 * 0.22702703
                        + c3 * 0.31621622 + c4 * 0.07027027;

            return EncodeHDR(color);
        }

        half3 Upsample(float2 uv)
        {
            half3 highMip = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv));

        #if _BLOOM_HQ && !defined(SHADER_API_GLES)
            half3 lowMip = DecodeHDR(SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_SourceTexLowMip, sampler_LinearClamp), uv, _SourceTexLowMip_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex));
        #else
            half3 lowMip = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTexLowMip, sampler_LinearClamp, uv));
        #endif

            return lerp(highMip, lowMip, Scatter);
        }

        half4 FragUpsample(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half3 color = Upsample(UnityStereoTransformScreenSpaceTex(input.uv));
            return EncodeHDR(color);
        }

        //PicoVideo;CustomBloom;ZhouShaoyang;Begin
        DownVaryings DownVert(Attributes input)
        {
            DownVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float2 texelSize = _SourceTex_TexelSize * 2.0;
            #define BLURRANGEIN 1
            
            #if _USE_DRAW_PROCEDURAL
            output.positionCS = GetQuadVertexPosition(input.vertexID);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            output.uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
            #else
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            #endif
            output.uvs[0].xy = output.uv + float2(-1,-1) * texelSize * BLURRANGEIN; //↖
            output.uvs[0].zw = output.uv + float2(-1, 1) * texelSize * BLURRANGEIN; //↙
            output.uvs[1].xy = output.uv + float2( 1,-1) * texelSize * BLURRANGEIN; //↗
            output.uvs[1].zw = output.uv + float2( 1, 1) * texelSize * BLURRANGEIN; //↘

            return output;
        }

        UpVaryings UpVert(Attributes input)
        {
            UpVaryings output;
            UNITY_SETUP_INSTANCE_ID(input);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
            float2 texelSize = _SourceTex_TexelSize * 0.5;
            
            #if _USE_DRAW_PROCEDURAL
            output.positionCS = GetQuadVertexPosition(input.vertexID);
            output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f); //convert to -1..1
            float2 uv = GetQuadTexCoord(input.vertexID) * _ScaleBias.xy + _ScaleBias.zw;
            #else
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            float2 uv = input.uv;
            #endif
            output.uvs[0].xy = uv + float2(-1,-1) * texelSize * BLURRANGEIN; //↖
            output.uvs[0].zw = uv + float2(-1, 1) * texelSize * BLURRANGEIN; //↙
            output.uvs[1].xy = uv + float2( 1,-1) * texelSize * BLURRANGEIN; //↗
            output.uvs[1].zw = uv + float2( 1, 1) * texelSize * BLURRANGEIN; //↘
            output.uvs[2].xy = uv + float2(-2, 0) * texelSize * BLURRANGEIN; //←
            output.uvs[2].zw = uv + float2( 0,-2) * texelSize * BLURRANGEIN; //↓
            output.uvs[3].xy = uv + float2( 2, 0) * texelSize * BLURRANGEIN; //→
            output.uvs[3].zw = uv + float2( 0, 2) * texelSize * BLURRANGEIN; //↑
            #undef BLURRANGEIN

            return output;
        }

        half4 FragCustomDownsample(DownVaryings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            half3 color =  0;
            color += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uv))) * 4;
            color += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uvs[0].xy)));
            color += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uvs[0].zw)));
            color += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uvs[1].xy)));
            color += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uvs[1].zw)));
            return EncodeHDR(color * 0.125); // sum / 8.0f;
        }

        half3 DualBlurUpsample(float2 uv)
        {
            half3 highMip = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv));

        #if _BLOOM_HQ && !defined(SHADER_API_GLES)
            half3 lowMip = DecodeHDR(SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_SourceTexLowMip, sampler_LinearClamp), uv, _SourceTexLowMip_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex));
        #else
            half3 lowMip = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTexLowMip, sampler_LinearClamp, uv));
        #endif

            return lerp(highMip, lowMip, Scatter);
        }

        half3 FragCustomUpsample(UpVaryings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uvs0 = UnityStereoTransformScreenSpaceTex(input.uvs[0].xy);
            float2 uvs1 = UnityStereoTransformScreenSpaceTex(input.uvs[0].zw);
            float2 uvs2 = UnityStereoTransformScreenSpaceTex(input.uvs[1].xy);
            float2 uvs3 = UnityStereoTransformScreenSpaceTex(input.uvs[1].zw);
            float2 uv = (uvs0 + uvs1 + uvs2 + uvs3) * 0.25;
            half3 highMip = 0;
            highMip += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uvs0) * 2);
            highMip += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uvs1) * 2);
            highMip += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uvs2) * 2);
            highMip += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uvs3) * 2);
            highMip += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uvs[2].xy)));
            highMip += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uvs[2].zw)));
            highMip += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uvs[3].xy)));
            highMip += DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, UnityStereoTransformScreenSpaceTex(input.uvs[3].zw)));
            highMip *= 0.0833; //sum /12.0f

            #if _BLOOM_HQ && !defined(SHADER_API_GLES)
                half3 lowMip = DecodeHDR(SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_SourceTexLowMip, sampler_LinearClamp), uv, _SourceTexLowMip_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex));
            #else
                half3 lowMip = DecodeHDR(SAMPLE_TEXTURE2D_X(_SourceTexLowMip, sampler_LinearClamp, uv));
            #endif
            return lerp(highMip, lowMip, Scatter);
        }

        half4 FragAlphaPrefilter(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);

        #if _BLOOM_HQ
            float texelSize = _SourceTex_TexelSize.x;
            half4 A = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, -1.0));
            half4 B = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.0, -1.0));
            half4 C = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, -1.0));
            half4 D = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-0.5, -0.5));
            half4 E = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.5, -0.5));
            half4 F = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, 0.0));
            half4 G = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv);
            half4 H = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, 0.0));
            half4 I = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-0.5, 0.5));
            half4 J = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.5, 0.5));
            half4 K = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, 1.0));
            half4 L = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.0, 1.0));
            half4 M = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, 1.0));

            half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

            half4 o = (D + E + I + J) * div.x;
            o += (A + B + G + F) * div.y;
            o += (B + C + H + G) * div.y;
            o += (F + G + L + K) * div.y;
            o += (G + H + M + L) * div.y;

            half3 color = o.xyz * o.a;
        #else
            half4 tmp = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv);
            half3 color = tmp.xyz * (tmp.a);
        #endif

            // User controlled clamp to limit crazy high broken spec
            color = min(ClampMax, color);

            // Thresholding
            half brightness = Max3(color.r, color.g, color.b);
            half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
            softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
            half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
            color *= multiplier;

            // Clamp colors to positive once in prefilter. Encode can have a sqrt, and sqrt(-x) == NaN. Up/Downsample passes would then spread the NaN.
            color = max(color, 0);
            return EncodeHDR(color);
        }

        half4 FragAlphaPrefilter2(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);

        #if _BLOOM_HQ
            float texelSize = _SourceTex_TexelSize.x;
            half4 A = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, -1.0));
            half4 B = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.0, -1.0));
            half4 C = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, -1.0));
            half4 D = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-0.5, -0.5));
            half4 E = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.5, -0.5));
            half4 F = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, 0.0));
            half4 G = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv);
            half4 H = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, 0.0));
            half4 I = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-0.5, 0.5));
            half4 J = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.5, 0.5));
            half4 K = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(-1.0, 1.0));
            half4 L = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(0.0, 1.0));
            half4 M = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv + texelSize * float2(1.0, 1.0));

            half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

            half4 o = (D + E + I + J) * div.x;
            o += (A + B + G + F) * div.y;
            o += (B + C + H + G) * div.y;
            o += (F + G + L + K) * div.y;
            o += (G + H + M + L) * div.y;

            half3 color = o.xyz * o.a;
        #else
            half4 tmp = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv);
            half3 color = tmp.xyz * (1 - tmp.a);
        #endif

            // User controlled clamp to limit crazy high broken spec
            color = min(ClampMax, color);

            // Thresholding
            half brightness = Max3(color.r, color.g, color.b);
            half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
            softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
            half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
            color *= multiplier;

            // Clamp colors to positive once in prefilter. Encode can have a sqrt, and sqrt(-x) == NaN. Up/Downsample passes would then spread the NaN.
            color = max(color, 0);
            return EncodeHDR(color);
        }
        //PicoVideo;CustomBloom;ZhouShaoyang;End
    
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Bloom Prefilter"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragPrefilter
                #pragma multi_compile_local _ _BLOOM_HQ
                //PicoVideo;FoveatedFeature;YangFan;Begin
                #pragma multi_compile_local _ USE_FOVEATED_SUBSAMPLED_LAYOUT
                //PicoVideo;FoveatedFeature;YangFan;End
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Blur Horizontal"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragBlurH
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Blur Vertical"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragBlurV
            ENDHLSL
        }

        Pass
        {
            Name "Bloom Upsample"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragUpsample
                #pragma multi_compile_local _ _BLOOM_HQ
            ENDHLSL
        }

        //PicoVideo;CustomBloom;ZhouShaoyang;Begin
        Pass
        {
            Name "Custom Bloom Downsample"

            HLSLPROGRAM
                #pragma vertex DownVert
                #pragma fragment FragCustomDownsample
            ENDHLSL
        }
        Pass
        {
            Name "Custom Bloom Upsample"
            HLSLPROGRAM
                #pragma vertex UpVert
                #pragma fragment FragCustomUpsample
            ENDHLSL
        }
        
        Pass
        {
            Name "Mask Bloom Prefilter Forward"
            Stencil
            {
	            Ref 2
	            ReadMask 2
	            Comp NotEqual
            }

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragPrefilter
                #pragma multi_compile_local _ _BLOOM_HQ
                //PicoVideo;FoveatedFeature;YangFan;Begin
                #pragma multi_compile_local _ USE_FOVEATED_SUBSAMPLED_LAYOUT
                //PicoVideo;FoveatedFeature;YangFan;End
            ENDHLSL
        }
        
        Pass
        {
            Name "Mask Bloom Prefilter Reverse"
            Stencil
            {
        	    Ref 2
        	    ReadMask 2
        	    Comp Equal
            }

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragPrefilter
                #pragma multi_compile_local _ _BLOOM_HQ
                //PicoVideo;FoveatedFeature;YangFan;Begin
                #pragma multi_compile_local _ USE_FOVEATED_SUBSAMPLED_LAYOUT
                //PicoVideo;FoveatedFeature;YangFan;End
            ENDHLSL
        }
        
        Pass
        {
            Name "Alpha Mask Bloom Prefilter Forward"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragAlphaPrefilter
                #pragma multi_compile_local _ _BLOOM_HQ
                //PicoVideo;FoveatedFeature;YangFan;Begin
                #pragma multi_compile_local _ USE_FOVEATED_SUBSAMPLED_LAYOUT
                //PicoVideo;FoveatedFeature;YangFan;End 
            ENDHLSL
        }
        
        Pass
        {
            Name "Alpha Mask Bloom Prefilter Reverse"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment FragAlphaPrefilter2
                #pragma multi_compile_local _ _BLOOM_HQ
                //PicoVideo;FoveatedFeature;YangFan;Begin
                #pragma multi_compile_local _ USE_FOVEATED_SUBSAMPLED_LAYOUT
                //PicoVideo;FoveatedFeature;YangFan;End
            ENDHLSL
        }
        //PicoVideo;CustomBloom;ZhouShaoyang;End
    }
}
