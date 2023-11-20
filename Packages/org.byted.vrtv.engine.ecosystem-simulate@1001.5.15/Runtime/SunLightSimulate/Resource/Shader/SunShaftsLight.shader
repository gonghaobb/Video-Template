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

Shader "PicoVideo/EcosystemSimulate/SunLightSimulate/SunShaftsLight"
{
	Properties 
	{
	    [HideInInspector]_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	SubShader 
	{
		ZTest Always Cull Off ZWrite Off

		// 0 depth
		Pass
		{
            HLSLPROGRAM
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
			
            TEXTURE2D_X(_GlobalMainTex);
            SAMPLER(sampler_GlobalMainTex);

			TEXTURE2D_X(_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthTexture);

			float4 _SunPosition;
			uniform half4 _SunThreshold;

            struct Attributes
            {
                float4 positionOS       : POSITION;
                uint   vertexID			: SV_VertexID;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv				: TEXCOORD0;
                float4 vertex			: SV_POSITION;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };
           
			half TransformColor (half4 skyboxValue)
			{
				return dot(max(skyboxValue.rgb - _SunThreshold.rgb, half3(0,0,0)), half3(1,1,1)); // threshold and convert to greyscale
			}

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				
                output.vertex = GetQuadVertexPosition(input.vertexID);
				output.vertex.xy = output.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
				output.uv = GetQuadTexCoord(input.vertexID);
                return output;
            }
            
            float4 frag (Varyings i) : SV_Target 
            {
            	UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            	
				float4 mainColor = SAMPLE_TEXTURE2D_X(_GlobalMainTex, sampler_GlobalMainTex, i.uv.xy);
				float depthSample = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, i.uv.xy).x;

				depthSample = Linear01Depth(depthSample, _ZBufferParams);

				// 计算在太阳光半径内的颜色
				float2 vec = _SunPosition.xy - i.uv.xy;
				float dist = saturate(_SunPosition.w - length(vec.xy));	
				
				// 将深度图和颜色图(深度很远的地方)混合，形成黑白图，然后扩散黑白图
				// 越白的地方散射越强，如果是半透的通常也会漏光
				float4 outColor = 0;
				if(depthSample > 0.99)
				{
					outColor = TransformColor (mainColor) * dist;
				}
                return outColor;
            }
            
			#pragma vertex vert
			#pragma fragment frag
			
			ENDHLSL
		}

		// 1 blur
		Pass
		{
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            
			#define SAMPLES_FLOAT 6.0f
			#define SAMPLES_INT 6

            TEXTURE2D_X(_GlobalMainTex);
            SAMPLER(sampler_GlobalMainTex);
           
			uniform float4 _SunPosition;
			uniform half4 _BlurRadius4;

            struct Attributes
            {
                float4 positionOS   : POSITION;
                uint   vertexID		: SV_VertexID;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 vertex	  : SV_POSITION;
            	float2 uv         : TEXCOORD0;
				float2 blurVector : TEXCOORD1;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };
          
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
            	UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.vertex = GetQuadVertexPosition(input.vertexID);
				output.vertex.xy = output.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
				output.uv = GetQuadTexCoord(input.vertexID);
				output.blurVector = (_SunPosition.xy - output.uv.xy) * _BlurRadius4.xy;
                return output;
            }
            
            float4 frag (Varyings i) : SV_Target 
            {
            	UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            	
				float4 color = float4(0,0,0,0);
				for(int j = 0; j < SAMPLES_INT; j++)   
				{	
					float4 tmpColor = SAMPLE_TEXTURE2D_X(_GlobalMainTex, sampler_GlobalMainTex, i.uv.xy);
					color += tmpColor;
					i.uv.xy += i.blurVector; 	
				}
				return color / SAMPLES_FLOAT;
            }
            
			#pragma vertex vert
			#pragma fragment frag
			
			ENDHLSL
		}

		// 2 add mode
		Pass
		{
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D_X(_GlobalMainTex);
            SAMPLER(sampler_GlobalMainTex);

			TEXTURE2D_X(_SunShaftsTex);
            SAMPLER(sampler_SunShaftsTex);
			uniform half4 _SunColor;

            struct Attributes
            {
                float4 positionOS   : POSITION;
				uint   vertexID		: SV_VertexID;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
            	float4 vertex	 : SV_POSITION;
                float2 uv        : TEXCOORD0;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
            	UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.vertex = GetQuadVertexPosition(input.vertexID);
				output.vertex.xy = output.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
				output.uv = GetQuadTexCoord(input.vertexID);
                return output;
            }
            
            float4 frag (Varyings i) : SV_Target 
            {
            	UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            	
				float4 ori = SAMPLE_TEXTURE2D_X(_GlobalMainTex,sampler_GlobalMainTex, i.uv);
				float4 sunShafts = SAMPLE_TEXTURE2D_X(_SunShaftsTex, sampler_SunShaftsTex, i.uv);
		
				float4 depthMask = saturate (sunShafts.r * _SunColor.rgba);

				return ori + depthMask;
            }
            
			#pragma vertex vert
			#pragma fragment frag
			
			ENDHLSL
		}
		
		// 3 Screen mode
		Pass
		{
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D_X(_GlobalMainTex);
            SAMPLER(sampler_GlobalMainTex);

			TEXTURE2D_X(_SunShaftsTex);
            SAMPLER(sampler_SunShaftsTex);
			uniform half4 _SunColor;

            struct Attributes
            {
                float4 positionOS   : POSITION;
				uint   vertexID		: SV_VertexID;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
            	float4 vertex	 : SV_POSITION;
                float2 uv        : TEXCOORD0;
            	UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
            	UNITY_SETUP_INSTANCE_ID(input);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.vertex = GetQuadVertexPosition(input.vertexID);
				output.vertex.xy = output.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
				output.uv = GetQuadTexCoord(input.vertexID);
                return output;
            }
            
            float4 frag (Varyings i) : SV_Target
            {
            	UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
            	
				float4 ori = SAMPLE_TEXTURE2D_X(_GlobalMainTex,sampler_GlobalMainTex, i.uv);
				float4 sunShafts = SAMPLE_TEXTURE2D_X(_SunShaftsTex, sampler_SunShaftsTex, i.uv);
		
				float4 depthMask = saturate (sunShafts.r * _SunColor);

				return 1.0f - (1.0f-ori) * (1.0f-depthMask);	
            }
            
			#pragma vertex vert
			#pragma fragment frag
			
			ENDHLSL
		}
	} 
}
