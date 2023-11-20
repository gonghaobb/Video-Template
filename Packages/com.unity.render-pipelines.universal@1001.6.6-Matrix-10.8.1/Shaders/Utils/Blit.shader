Shader "Hidden/Universal Render Pipeline/Blit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "Blit"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION _SRGB_TO_LINEAR_CONVERSION //PicoVideo;EditorUIColorAdjustment;WuJunLin
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            //PicoVideo;FoveatedFeature;YangFan;Begin
            #pragma multi_compile_fragment _ USE_FOVEATED_SUBSAMPLED_LAYOUT
            #if defined(USE_FOVEATED_SUBSAMPLED_LAYOUT)
                TEXTURE2D_X(_SubsampledLayoutSourceTex);
                SAMPLER(sampler_SubsampledLayoutSourceTex);
            #else
                TEXTURE2D_X(_SourceTex);
                SAMPLER(sampler_SourceTex);
            #endif
            //PicoVideo;FoveatedFeature;YangFan;End
            
            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

             //PicoVideo;FoveatedFeature;YangFan;Begin
             #if defined(USE_FOVEATED_SUBSAMPLED_LAYOUT)
                half4 col = SAMPLE_TEXTURE2D_X(_SubsampledLayoutSourceTex, sampler_SubsampledLayoutSourceTex, input.uv);
             #else
                half4 col = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv);
             #endif
             //PicoVideo;FoveatedFeature;YangFan;End

             #ifdef _LINEAR_TO_SRGB_CONVERSION
                col = LinearToSRGB(col);
             #endif

             //PicoVideo;EditorUIColorAdjustment;WuJunLin;Begin
             #ifdef _SRGB_TO_LINEAR_CONVERSION
                col = SRGBToLinear(col);
             #endif
             //PicoVideo;EditorUIColorAdjustment;WuJunLin;End   

                return col;
            }
            ENDHLSL
        }
        
//        Pass
//        {
//            Name "BlitWithDepth"
//            ZTest Always
//            ZWrite On
//            Cull Off
//
//            HLSLPROGRAM
//            #pragma vertex FullscreenVert
//            #pragma fragment Fragment
//            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION _SRGB_TO_LINEAR_CONVERSION //PicoVideo;EditorUIColorAdjustment;WuJunLin
//            #pragma multi_compile_fragment _ _SubPassMSAA2 _SubPassMSAA4
//            #pragma multi_compile _ _USE_DRAW_PROCEDURAL
//            
//            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
//            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
//
//            #if defined(_SubPassMSAA2)
//                #define MSAA_SAMPLES 2
//            #elif defined(_SubPassMSAA4)
//                #define MSAA_SAMPLES 4
//            #else
//                #define MSAA_SAMPLES 1
//            #endif
//
//            #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
//            #define DEPTH_TEXTURE_MS(name, samples) Texture2DMSArray<float, samples> name
//            #define DEPTH_TEXTURE(name) TEXTURE2D_ARRAY_FLOAT(name)
//            #define LOAD(uv, sampleIndex) LOAD_TEXTURE2D_ARRAY_MSAA(_CameraDepthTexture, uv, unity_StereoEyeIndex, sampleIndex)
//            #define SAMPLE(uv) SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, uv, unity_StereoEyeIndex).r
//            #else
//            #define DEPTH_TEXTURE_MS(name, samples) Texture2DMS<float, samples> name
//            #define DEPTH_TEXTURE(name) TEXTURE2D_FLOAT(name)
//            #define LOAD(uv, sampleIndex) LOAD_TEXTURE2D_MSAA(_CameraDepthTexture, uv, sampleIndex)
//            #define SAMPLE(uv) SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv)
//            #endif
//
//            #if MSAA_SAMPLES == 1
//                DEPTH_TEXTURE(_CameraDepthTexture);
//                SAMPLER(sampler_CameraDepthTexture);
//            #else
//                DEPTH_TEXTURE_MS(_CameraDepthTexture, MSAA_SAMPLES);
//                float4 _CameraDepthTexture_TexelSize;
//            #endif
//
//            #if UNITY_REVERSED_Z
//                #define DEPTH_DEFAULT_VALUE 1.0
//                #define DEPTH_OP min
//            #else
//                #define DEPTH_DEFAULT_VALUE 0.0
//                #define DEPTH_OP max
//            #endif
//
//            float SampleDepth(float2 uv)
//            {
//            #if MSAA_SAMPLES == 1
//                return SAMPLE(uv);
//            #else
//                int2 coord = int2(uv * _CameraDepthTexture_TexelSize.zw);
//                float outDepth = DEPTH_DEFAULT_VALUE;
//
//                UNITY_UNROLL
//                for (int i = 0; i < MSAA_SAMPLES; ++i)
//                    outDepth = DEPTH_OP(LOAD(coord, i), outDepth);
//                return outDepth;
//            #endif
//            }
//
//            //PicoVideo;FoveatedFeature;YangFan;Begin
//            #pragma multi_compile_fragment _ USE_FOVEATED_SUBSAMPLED_LAYOUT
//            #if defined(USE_FOVEATED_SUBSAMPLED_LAYOUT)
//                TEXTURE2D_X(_SubsampledLayoutSourceTex);
//                SAMPLER(sampler_SubsampledLayoutSourceTex);
//            #else
//                TEXTURE2D_X(_SourceTex);
//                SAMPLER(sampler_SourceTex);
//            #endif
//            //PicoVideo;FoveatedFeature;YangFan;End
//            
//            void Fragment(Varyings input, out half4 color : SV_Target, out float depth : SV_Depth)
//            {
//                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
//
//             //PicoVideo;FoveatedFeature;YangFan;Begin
//             #if defined(USE_FOVEATED_SUBSAMPLED_LAYOUT)
//                half4 col = SAMPLE_TEXTURE2D_X(_SubsampledLayoutSourceTex, sampler_SubsampledLayoutSourceTex, input.uv);
//             #else
//                half4 col = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_SourceTex, input.uv);
//             #endif
//             //PicoVideo;FoveatedFeature;YangFan;End
//
//             #ifdef _LINEAR_TO_SRGB_CONVERSION
//                col = LinearToSRGB(col);
//             #endif
//
//             //PicoVideo;EditorUIColorAdjustment;WuJunLin;Begin
//             #ifdef _SRGB_TO_LINEAR_CONVERSION
//                col = SRGBToLinear(col);
//             #endif
//             //PicoVideo;EditorUIColorAdjustment;WuJunLin;End   
//
//                color = col;
//                depth = SampleDepth(input.uv);
//            }
//            ENDHLSL
//        }
    }
}
