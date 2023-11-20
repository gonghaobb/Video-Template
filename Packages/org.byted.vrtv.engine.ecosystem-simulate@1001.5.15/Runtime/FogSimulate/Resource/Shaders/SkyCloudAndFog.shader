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

Shader "PicoVideo/EcosystemSimulate/FogSimulate/SkyCloudAndFog" 
{
	Properties
	{
		[NoScaleOffset] _Tex("Cubemap   (HDR)", Cube) = "black" {}
		[NoScaleOffset]_CloudSampler("Cloud Cubemap   (HDR)", Cube) = "black" {}
		
		[NoScaleOffset]_GalaxyMap("Galaxy Cubemap", Cube) = "black" {}
		[NoScaleOffset]_StarsMap("Stars Cubemap", Cube) = "black" {}
		[NoScaleOffset]_StarsTwinklingNoiseMap("StarsTwinklingNoise Cubemap", Cube) = "black" {}
		[NoScaleOffset]_MoonTex("Moon Tex", 2D) = "black" {}
		
		[HDR]_SkyColor("SkyColor",Color) = (0.58,0.7,0.86,1)
		[HDR]_CloudColor("CloudColor",Color) = (0.58,0.7,0.86,1)
		
		_Rotation("_Rotation", Range(0, 360)) = 0
		_CloudRotation("_CloudRotation", Range(0, 360)) = 0
		
		// Fog
		[HideInInspector]_FogOffsetHeight("fogOffset", Range(0.01, 1)) = 0
		[Gamma] _CustomExposure("Exposure", Range(0, 8)) = 1.0

		_LightningExposureMultiplier ("Lightning Exposure Multiplier" , Range(0 , 2)) = 0
	}
	SubShader
	{
		Tags
		{ 
			"Queue" = "Background" 
			"RenderType" = "Background" 
			"PreviewType" = "Skybox" 
		}
		Cull Off ZWrite Off
		ZClip false
		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 4.5

			#pragma shader_feature_local _ SKY_CLOUD_DIRECTIONAL_MOTION
			#pragma shader_feature_local_fragment _ _CLOUD_MAP
			#pragma multi_compile_local_fragment _ _SUN_ENABLE
			#pragma multi_compile _ CUSTOM_FOG

			#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/FogSimulate/Resource/Shaders/Fog.hlsl"

			samplerCUBE _Tex;
			samplerCUBE	_CloudSampler;
			samplerCUBE	_StarsMap;
			samplerCUBE	_GalaxyMap;
			samplerCUBE	_StarsTwinklingNoiseMap;
			sampler2D _MoonTex;

			half _SunDistance;
			half _SunPower;
			half3 _SunDirection;
			
			float _Rotation;			// 天空盒子本身的角度
			float _CloudRotation;		// 云天空盒子本身的角度
			float _RotationCloudSpeed;  // 云盒子本身的角度 旋转速度*Time
			float _FogOffsetHeight;		// 雾高度偏移

            float3 _SunColor;
            float3 _SkyColor;
            float3 _CloudColor;
			sampler2D _FogGradHeight;
			float4 _Tex_HDR;
			
			float _CustomExposure;
			float4 _SkyBoxCloudMotionParameters;	//xy scrollDirection, z scrollFactor, w intensity

			struct Attributes 
			{
				float3 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings 
			{
				float4 vertex : SV_POSITION;
				float4 texcoord : TEXCOORD0;   // 天空盒纹理采样
				float4 texcoord1 : TEXCOORD1;  // 用于旋转云的采样
				half3 dirWS : TEXCOORD2;
    			UNITY_VERTEX_OUTPUT_STEREO
			};
			
			float4 GetSkyColor(float3 dir)
			{
				float scrollFactor = _SkyBoxCloudMotionParameters.z;
				float2 scrollDirection = _SkyBoxCloudMotionParameters.xy;
				
			    if (dir.y >= 0)
			    {
			        float2 alpha = frac(float2(scrollFactor, scrollFactor + 0.5)) - 0.5;
			        float3 windDir = float3(scrollDirection.x, 0.0f, scrollDirection.y);
			        float3 dd = windDir*sin(dir.y*PI*0.5);
			        // Sample twice
			        float4 color1 = texCUBE(_CloudSampler, dir + alpha.x*dd);
			        float4 color2 = texCUBE(_CloudSampler, dir + alpha.y*dd);
			        // Blend color samples
			        return lerp(color1, color2, abs(2.0 * alpha.x));
			    }
			    else
			    {
					return texCUBE(_CloudSampler, dir);
			    }
			}
			
			Varyings vert(Attributes v)
			{
				Varyings o = (Varyings)0;
			    UNITY_SETUP_INSTANCE_ID(v);
    			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				float3 rotated = RotateAroundYInDegrees(v.vertex.xyz, _Rotation);
				//trick SinglePassInstance技术会导致HLSL下天空盒缩放不正确，需要缩小天空盒以防止被远裁剪面裁掉
				o.vertex = TransformObjectToHClip(v.vertex.xyz / 15.0); 
				o.texcoord.xyz = rotated;
#ifdef SKY_CLOUD_DIRECTIONAL_MOTION
				rotated = RotateAroundYInDegrees(v.vertex.xyz, _CloudRotation);
#else
				rotated = RotateAroundYInDegrees(v.vertex.xyz, _CloudRotation + _Time.y * _RotationCloudSpeed);
#endif
				o.texcoord1.xyz = rotated;
				o.dirWS = v.vertex; // 中心点在世界坐标原点
				
			#if defined(CUSTOM_FOG)
				o.texcoord.w = lerp(v.vertex.y, 1, _FogOffsetHeight);
			#endif

				return o;
			}

			float4 frag(Varyings i) : SV_Target
			{
				// 一个常规的天空盒采样和HDR解码和颜色相乘
				float3 c = FogAndSkyDecodeHDR(texCUBE(_Tex, i.texcoord.xyz), _Tex_HDR) * _SkyColor.xyz;
				// 云立方体贴图的采样
			#ifdef _CLOUD_MAP
			#ifdef SKY_CLOUD_DIRECTIONAL_MOTION
				float4 opacity = GetSkyColor(i.texcoord1.xyz);
			#else
				float4 opacity = texCUBE(_CloudSampler, i.texcoord1.xyz);
			#endif
				// RGB的平均色
				float a = saturate(FogAndSkyLuminance(opacity.xyz));
				// 先将云和太阳进行融合 2.通过RGB的均色将1的结果与原天空色融合
				
				c = lerp(c, lerp(c, opacity.rgb, _CloudColor.xyz), a);
			#endif

				#if _SUN_ENABLE
				//太阳
				half3 dir = normalize(i.dirWS);
                half3 sunDir = normalize(_SunDirection);
                half dis = length(sunDir - dir) * _SunDistance;
				c += (saturate(max(1 / dis, 0.01)) * _SunColor) * _SunPower;
				#endif
			#if defined(CUSTOM_FOG)
				c.rgb = lerp(BlendDirection(i.dirWS, lerp(_FogColorEnd.xyz, c, i.texcoord1.w)).xyz, c.rgb, saturate(tex2D(_FogGradHeight, float2(i.texcoord.w, 0.25))).xyz);
			#endif
				
				c = FogAndSkyEncodeHDR(float4(c, 1)).xyz;

		   		c *= _CustomExposure;
				return float4(c, 1);
			}
			ENDHLSL
		}
	}
	Fallback Off
}
