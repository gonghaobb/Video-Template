//
//      __  __       _______ _____  _______   __
//      |  \/  |   /\|__   __|  __ \|_   _\ \ / /
//      | \  / |  /  \  | |  | |__) | | |  \ V / 
//      | |\/| | / /\ \ | |  |  _  /  | |   > <  
//      | |  | |/ ____ \| |  | | \ \ _| |_ / . \ 
//      |_|  |_/_/    \_\_|  |_|  \_\_____/_/ \_\                        
//									   (ByteDance)
//
//      Created by Matrix team.
//      Procedural LOGO:https://www.shadertoy.com/view/ftKBRW
//
//      The team was set up on September 4, 2019.
//

Shader "PicoVideo/EcosystemSimulate/RainScreenEffectSimulate/RainScreenEffect"
{
    Properties
    {
    }
    SubShader
    {
		ZTest Always 
    	Cull Off 
    	ZWrite Off

    	//index: 0   rainDrop
        Pass
        {
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin
        	
            HLSLPROGRAM
            #define BLUR
            
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
			uniform float4 _RainScreenEffectOverlayColor;
			uniform sampler2D _RainScreenEffectNormalMap;
			uniform sampler2D _RainScreenEffectReliefMap;
            // uniform sampler2D _RainScreenEffectCameraColorTexture;
            uniform float _RainScreenEffectStrength;
            
            TEXTURE2D_X(_CameraTransparentTexture);
			SAMPLER(sampler_CameraTransparentTexture);

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(half, _RainScreenEffectDistortions)
            UNITY_DEFINE_INSTANCED_PROP(half, _RainScreenEffectBlurs)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
				uint vertexID     : SV_VertexID;

				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

            static inline float4 GetPixel (float2 uv, half weight, half kern, half size)
            {
            	return SAMPLE_TEXTURE2D_X(_CameraTransparentTexture, sampler_CameraTransparentTexture, float2(uv.x + kern * size, uv.y)) * weight;
            	// return tex2D(_RainScreenEffectCameraColorTexture, float2(uv.x + kern * size, uv.y)) * weight;
			}

            static inline float4 ComputeBlur (float2 uv, float alpha, half blur)
            {
				half intensity = alpha * blur * 1000 / _ScreenParams.x;

				half4 sum = half4(0,0,0,0);
				sum += GetPixel(uv, 0.05, -4.0, intensity);
				sum += GetPixel(uv, 0.09, -3.0, intensity);
				sum += GetPixel(uv, 0.12, -2.0, intensity);
				sum += GetPixel(uv, 0.15, -1.0, intensity);
				sum += GetPixel(uv, 0.18,  0.0, intensity);
				sum += GetPixel(uv, 0.15, +1.0, intensity);
				sum += GetPixel(uv, 0.12, +2.0, intensity);
				sum += GetPixel(uv, 0.09, +3.0, intensity);
				sum += GetPixel(uv, 0.05, +4.0, intensity);
				return sum;
			}

            Varyings vert (Attributes v)
            {
                Varyings o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            	
                o.positionCS = GetQuadVertexPosition(v.vertexID);
				o.positionCS.xy = o.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
				o.uv = GetQuadTexCoord(v.vertexID);
            	
            	return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            	
				float relf = tex2D(_RainScreenEffectReliefMap, i.uv).r;
                
				float2 norm = UnpackNormal(tex2D(_RainScreenEffectNormalMap, i.uv)).rg;
            	clip(pow(norm.r, 2) + pow(norm.g, 2) - 0.0001);

            	float2 uv = i.positionCS.xy / _ScreenParams.xy;
				uv -= UNITY_ACCESS_INSTANCED_PROP(Props, _RainScreenEffectDistortions) * norm.rg / _ScreenParams.xy;
#ifdef BLUR
				float4 color = float4 (ComputeBlur(uv, norm.r, UNITY_ACCESS_INSTANCED_PROP(Props, _RainScreenEffectBlurs)));
#else
            	// float4 color = tex2D(_RainScreenEffectCameraColorTexture, uv);
            	float4 color = SAMPLE_TEXTURE2D_X(_CameraTransparentTexture, sampler_CameraTransparentTexture, uv);
#endif

				color.rgb += (relf * _RainScreenEffectOverlayColor.rgb) * _RainScreenEffectOverlayColor.a * _RainScreenEffectStrength;
				relf = saturate(pow(relf, 3));
				color.rgb *= saturate (1 - 2.5 * _RainScreenEffectOverlayColor.a * relf);
				// return 0;
            	return color;
            }
            ENDHLSL
        }
    	
    	//index: 1   staticScreenEffect
    	Pass
    	{
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin
        	
            HLSLPROGRAM
            #define BLUR
            
            #pragma vertex vert
			#pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
			float4 _RainScreenEffectOverlayColor;
			uniform sampler2D _RainScreenEffectNormalMap;
			uniform sampler2D _RainScreenEffectReliefMap;
			uniform sampler2D _RainScreenEffectTrailMap;
			// uniform sampler2D _RainScreenEffectCameraColorTexture;
            uniform float _RainScreenEffectStrength;
            
            TEXTURE2D_X(_CameraTransparentTexture);
			SAMPLER(sampler_CameraTransparentTexture);
            
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_DEFINE_INSTANCED_PROP(half, _RainScreenEffectDistortion)
            UNITY_DEFINE_INSTANCED_PROP(half, _RainScreenEffectBlur)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
				uint vertexID     : SV_VertexID;

				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };
            
            static inline float4 GetPixel (float2 uv, half weight, half kern, half size)
            {
            	return SAMPLE_TEXTURE2D_X(_CameraTransparentTexture, sampler_CameraTransparentTexture,  float2(uv.x + kern * size, uv.y)) * weight;
				// return tex2D(_RainScreenEffectCameraColorTexture, float2(uv.x + kern * size, uv.y)) * weight;
            }

            static inline float4 ComputeBlur (float2 uv, float alpha, half blur)
            {
				half intensity = alpha * blur * 1000 / _ScreenParams.x;

				half4 sum = half4(0,0,0,0);

            	//Blur1
				// sum += GetPixel(uv, 0.05, -4.0, intensity);
				// sum += GetPixel(uv, 0.09, -3.0, intensity);
				// sum += GetPixel(uv, 0.12, -2.0, intensity);
				// sum += GetPixel(uv, 0.15, -1.0, intensity);
				// sum += GetPixel(uv, 0.18,  0.0, intensity);
				// sum += GetPixel(uv, 0.15, +1.0, intensity);
				// sum += GetPixel(uv, 0.12, +2.0, intensity);
				// sum += GetPixel(uv, 0.09, +3.0, intensity);
				// sum += GetPixel(uv, 0.05, +4.0, intensity);

            	//Blur2
				// sum += GetPixel(uv, 0.09615, -4.0, intensity);
				// sum += GetPixel(uv, 0.23076, -2.0, intensity);
				// sum += GetPixel(uv, 0.31615,  0.0, intensity);
				// sum += GetPixel(uv, 0.23076, +2.0, intensity);
				// sum += GetPixel(uv, 0.09615, +4.0, intensity);

            	//Blur3
				sum += GetPixel(uv, 0.25, -2.0, intensity);
				sum += GetPixel(uv, 0.5,  0.0, intensity);
				sum += GetPixel(uv, 0.25, +2.0, intensity);
            	
				return sum;
			}

            Varyings vert (Attributes v)
            {
                Varyings o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
            	
                o.positionCS = GetQuadVertexPosition(v.vertexID);
				o.positionCS.xy = o.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
				o.uv = GetQuadTexCoord(v.vertexID);
                return o;
            }
            
            float4 frag (Varyings i) : SV_Target
            {
            	float2 uv = i.uv;
				float relf = tex2D(_RainScreenEffectReliefMap, uv).r;
				float2 norm = UnpackNormal(tex2D(_RainScreenEffectNormalMap, uv)).rg;
				float4 trailNorm = tex2D(_RainScreenEffectTrailMap, uv);
				trailNorm = float4(trailNorm.rgb * 2 - 1, trailNorm.a);
            	norm.rg = lerp(norm.rg, trailNorm.rg * trailNorm.a, step(pow(norm.r, 2) + pow(norm.g, 2), pow(trailNorm.r * trailNorm.a, 2) + pow(trailNorm.g * trailNorm.a, 2)));
            	float powRG = pow(norm.r, 2) + pow(norm.g, 2);
            	clip(powRG - 0.001);
            	
				uv -= UNITY_ACCESS_INSTANCED_PROP(Props, _RainScreenEffectDistortion) * norm.rg / _ScreenParams.xy;
#ifdef BLUR
				float4 color = float4 (ComputeBlur(uv, norm.r, UNITY_ACCESS_INSTANCED_PROP(Props, _RainScreenEffectBlur)));
#else
            	// float4 color = tex2D(_RainScreenEffectCameraColorTexture, uv);
            	float4 color = SAMPLE_TEXTURE2D_X(_CameraTransparentTexture, sampler_CameraTransparentTexture, uv);
#endif
            	
				color.rgb += (relf * _RainScreenEffectOverlayColor.rgb) * _RainScreenEffectOverlayColor.a * _RainScreenEffectStrength + min(0.05,  0.25 * powRG * abs(_RainScreenEffectBlur));
				relf = saturate(pow(relf, 3));
				color.rgb *= saturate (1 - 2.5 * _RainScreenEffectOverlayColor.a * relf);
            	return color;
            }
            ENDHLSL
    	}

    	//index: 2   AddNewTrail
        Pass
        {
        	
            HLSLPROGRAM
            #define INSTANCING_ON
            #pragma target 4.5
            
            #pragma vertex vert
            #pragma fragment frag
			#pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
			uniform sampler2D _RainScreenEffectNormalMap;

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes v)
            {
                Varyings o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
				o.positionCS = TransformObjectToHClip(v.vertex.xyz);
				o.uv = v.uv;
                return o;
            }

            float4 frag (Varyings i) : SV_Target
            {
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            	
				float4 rawNorm = tex2D(_RainScreenEffectNormalMap, i.uv);
				float3 norm = UnpackNormal(rawNorm);
            	clip(pow(norm.r, 2) + pow(norm.g, 2) - 0.001);

				return float4((norm + 1) / 2, 1);
            }
            ENDHLSL
        }
    	
    	//index: 3   updateOldTrail
        Pass
        {
        	
            HLSLPROGRAM
            
            #pragma vertex vert
			#pragma fragment frag

			uniform sampler2D _RainScreenEffectTrailMap;
            uniform float _RainScreenEffectTrailLifeTime;

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
				uint vertexID     : SV_VertexID;

				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert (Attributes v)
            {
                Varyings o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.positionCS = GetQuadVertexPosition(v.vertexID);
				o.positionCS.xy = o.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
				o.uv = GetQuadTexCoord(v.vertexID);
                return o;
            }
            
            float4 frag (Varyings i) : SV_Target
            {
            	float2 uv = i.uv;
				float4 normUp = tex2D(_RainScreenEffectTrailMap, (uv + float2(0, 1) / _ScreenParams.xy));
				float4 norm = tex2D(_RainScreenEffectTrailMap, uv);
				float4 normDown = tex2D(_RainScreenEffectTrailMap, (uv - float2(0, 1) / _ScreenParams.xy));
            	norm.rgb = normUp.rgb * 0.3 + norm.rgb * 0.4 + normDown.rgb * 0.3;
            	norm.a = saturate(norm.a - unity_DeltaTime.x / _RainScreenEffectTrailLifeTime);
				half var = any(norm.a);
            	norm.rgb = norm.rgb * var + float3(0.5, 0.5, 0.5) * (1 - var);

            	return norm;
            }
            ENDHLSL
        }
    	
    	//index: 4   DownSample
        Pass
        {
            HLSLPROGRAM
            
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_CameraTransparentTexture);
			SAMPLER(sampler_CameraTransparentTexture);
            
            struct Attributes
            {
				uint vertexID     : SV_VertexID;

				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert (Attributes v)
            {
                Varyings o;

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.positionCS = GetQuadVertexPosition(v.vertexID);
				o.positionCS.xy = o.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
				o.uv = GetQuadTexCoord(v.vertexID);
                return o;
            }
            
            float4 frag (Varyings i) : SV_Target
            {
            	return SAMPLE_TEXTURE2D_X(_CameraTransparentTexture, sampler_CameraTransparentTexture, i.uv);
            }
            ENDHLSL
        }
    }
}
