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

Shader "PicoVideo/EcosystemSimulate/CloudSimulate/ParallaxCloud"
{
	Properties {
        [Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
		[ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.SurfaceType,1,_Blend)]_Surface("Surface Type", Float) = 1
        [HideInInspector][ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.BlendMode)] _Blend("Blending Mode", Float) = 0.0
		
		[Main(_SurfaceInputs, _, on, off)]_SurfaceInputs("Surface Inputs", Float) = 0
        [Sub(_SurfaceInputs)]_Color("Color 云颜色",Color) = (1,1,1,1)
		[Sub(_SurfaceInputs)]_MaskTex("Mask Tex(R) 遮罩图(R通道)",2D)="white"{}
		[Sub(_SurfaceInputs)]_MainTex("Main Tex 云贴图(A通道为视差图)",2D)="white"{}
		[Sub(_SurfaceInputs)]_HeightTileSpeed("Turbulence Tile 云边缘扰动Tile",Vector) = (1.0,1.0,0,0)
		[Sub(_SurfaceInputs)]_CloudSpeed("Cloud Speed 云移动速度(xy:整体 zw:边缘)",Vector) = (1.0,1.0,0.05,0.0)
		[Sub(_SurfaceInputs)]_Alpha("Alpha Threshold Alpha阈值", Range(0,1)) = 0.5
        [SubToggle(_SurfaceInputs, _ENABLE_DEPTH_FADE)]_EnableDepthFade("Enable Depth Fade 开启深度渐隐", Float) = 0
		[Sub(_SurfaceInputs)]_DepthFadeMultiplier("Depth Fade Multiplier 深度渐隐强度", Float) = 1
		
		[Main(_FlowOptions, _, on, off)]_FlowOptions("Flow Options", Float) = 0
		[SubToggle(_FlowOptions, _ENABLE_FLOW)]_EnableFlow("Enable Flow 开启流速图", Float) = 0
		[Sub(_FlowOptions)]_FlowTex("Flow Tex(R) 流速图(R通道)", 2D) = "gray"{}
		[Sub(_FlowOptions)]_FlowIntensity("Flow Intensity 流速影响强度", Range(0,1)) = 1
        [Sub(_FlowOptions)]_FlowSpeed("Flow Speed 流动速度", Float) = 1
		
		[Main(_ShapeOptions, _, on, off)]_ShapeOptions("Shape Options", Float) = 0
		[Sub(_ShapeOptions)]_Height("Displacement Amount 视差深度",range(0,1)) = 0.15
		[Sub(_ShapeOptions)]_HeightAmount("Turbulence Amount 视差强度",range(0,2)) = 1
		[Sub(_ShapeOptions)]_VertexHeightIntensity("Vertexd Height Intensity 顶点高度偏移强度", Range(0,1)) = 1
		
		[Main(_LightOptions, _, on, off)]_LightOptions("Light Options", Float) = 0
		[SubToggle(_LightOptions)] _UseFixedLight("Use Fixed Light 使用自定义光照方向", Int) = 1
		[Sub(_LightOptions)]_FixedLightDir("Fixed Light Direction 自定义光照方向", Vector) = (0.981, 0.122, -0.148, 0.0)
		[KWEnum(_LightOptions, Simple, _REDDERMODE_SIMPLE, POM, _RENDERMODE_POM)]_RenderMode("Render Mode 渲染模式", Float) = 0
	}

	CGINCLUDE

	ENDCG

	SubShader 
	{
		LOD 300		
        Tags 
		{
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

		Pass
		{
		    Name "FORWARD"
            Tags 
			{
                "LightMode"="UniversalForward"
            }
			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin
			ZWrite Off
			Cull Back

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "AutoLight.cginc"
			#include "Lighting.cginc"

			#pragma  shader_feature_local _REDDERMODE_SIMPLE _RENDERMODE_POM
			#pragma  shader_feature_local _ENABLE_FLOW
			#pragma  shader_feature_local _ENABLE_DEPTH_FADE
            #pragma target 3.0

			sampler2D _MaskTex;
			sampler2D _MainTex;
			sampler2D _FlowTex;
			half4 _MainTex_ST;
			half4 _FlowTex_ST;
			half4 _MaskTex_ST;
			half _Height;
			half _VertexHeightIntensity;
			half2 _HeightTileSpeed;
			half4 _CloudSpeed;
			half _HeightAmount;
			half4 _Color;
			half _Alpha;
			half _FlowSpeed;
			half _FlowIntensity;
			
			half4 _LightingColor;
			half4 _FixedLightDir;
			half _UseFixedLight;
			half _DepthFadeMultiplier;
			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			
			struct v2f 
			{
				half4 pos : SV_POSITION;
				half4 uv : TEXCOORD0;			//zw : origin uv
				half3 normalDir : TEXCOORD1;
				half3 viewDir : TEXCOORD2;
				half4 posWorld : TEXCOORD3;
				half4 uv2 : TEXCOORD4;
				half4 color : TEXCOORD5;
				half4 projPos : TEXCOORD6;
			};

			v2f vert (appdata_full v) 
			{
				v2f o;
				
				o.uv.xy = TRANSFORM_TEX(v.texcoord,_MainTex);
				o.uv.zw = TRANSFORM_TEX(v.texcoord,_FlowTex);
				o.uv2.xy = v.texcoord * _HeightTileSpeed.xy;
				o.uv2.zw = TRANSFORM_TEX(v.texcoord,_MaskTex);
				half test = 1 - tex2Dlod(_MainTex, half4(o.uv2.xy,0,0));
				v.vertex -= test * _VertexHeightIntensity;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.posWorld = mul(unity_ObjectToWorld, v.vertex);
				o.normalDir = UnityObjectToWorldNormal(v.normal);
				TANGENT_SPACE_ROTATION;
				o.viewDir = mul(rotation, ObjSpaceViewDir(v.vertex));
				o.color = v.color;
				o.projPos = ComputeScreenPos (v.vertex);
                COMPUTE_EYEDEPTH(o.projPos.z);
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
			#if defined(_REDDERMODE_SIMPLE)
				half3 viewRay=normalize(i.viewDir*-1);
				viewRay.z=abs(viewRay.z)+0.42;
				viewRay.xy *= _Height;
				i.uv.xy += frac(_Time.x * _CloudSpeed.zw * 5);
				i.uv2.xy += frac(_Time.x * _CloudSpeed.xy);
				#ifdef _ENABLE_FLOW
				half4 flowMap = tex2D(_FlowTex, i.uv.zw);
				// flowMap = flowMap * 2 - 1;
				half phase0 = frac(_Time.x * _FlowSpeed);
				half phase1 = frac(_Time.x * _FlowSpeed + 0.5);
				half lerpFlow = abs(phase0 * 2 - 1);
				phase0 *= _FlowIntensity;
				phase1 *= _FlowIntensity;
				half3 shadeP = half3(i.uv.xy + phase0 * _CloudSpeed.zw * flowMap.x, 0);
				half3 shadeP2 = half3(i.uv2.xy+ phase0 * _CloudSpeed.xy * flowMap.x, 0);
   	
				half4 T = tex2D(_MainTex,shadeP2.xy);
				
				half h2 = T.a * _HeightAmount;
				
				half3 sioffset = viewRay / viewRay.z;
				half d = 1.0 - tex2Dlod(_MainTex, half4(shadeP.xy,0,0)).a * h2;
				shadeP += sioffset * d;
   	
				half4 c = tex2D(_MainTex, shadeP.xy) * T * _Color;
				
				half3 shadePFlow = half3(i.uv.xy + phase1 * _CloudSpeed.zw * flowMap.x, 0);
				half3 shadeP2Flow = half3(i.uv2.xy + phase1 * _CloudSpeed.xy * flowMap.x, 0);
   
				half4 TFlow = tex2D(_MainTex,shadeP2Flow.xy);
				half h2Flow = TFlow.a * _HeightAmount;
   
				half3 sioffsetFlow = viewRay / viewRay.z;
				half dFlow = 1.0 - tex2Dlod(_MainTex, half4(shadePFlow.xy,0,0)).a * h2Flow;
				shadePFlow += sioffsetFlow * dFlow;
				
				half4 cFlow = tex2D(_MainTex, shadePFlow.xy) * TFlow * _Color;

				c = lerp(c, cFlow, lerpFlow);
				shadeP = lerp(shadeP, shadePFlow, lerpFlow);
				shadeP2 = lerp(shadeP2, shadeP2Flow, lerpFlow);
				T = lerp(T, TFlow, lerpFlow);
				#else
				half3 shadeP = half3(i.uv.xy, 0);
				half3 shadeP2 = half3(i.uv2.xy, 0);
				half4 T = tex2D(_MainTex,shadeP2.xy);
				half h2 = T.a * _HeightAmount;
				half3 sioffset = viewRay / viewRay.z;
				half d = 1.0 - tex2Dlod(_MainTex, half4(shadeP.xy,0,0)).a * h2;
				shadeP += sioffset * d;
				half4 c = tex2D(_MainTex, shadeP.xy) * T * _Color;
				#endif

				half mask = tex2Dlod(_MaskTex, half4(i.uv2.zw,0,0));
				half alpha = saturate(T.a * c.a - _Alpha) / (1.0001 - _Alpha);
				#ifdef _ENABLE_DEPTH_FADE
                half sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, (i.pos.xy/ _ScreenParams.xy)));
				half partZ = i.projPos.z;
				half offset = (shadeP - shadeP2).z;
				half depthFade = saturate((sceneZ - partZ - offset) / _DepthFadeMultiplier);
				alpha *= depthFade;
				#endif
				
				half3 normal = normalize(i.normalDir);
				half3 lightDir1 = normalize(_FixedLightDir.xyz);
				half3 lightDir2 = UnityWorldSpaceLightDir(i.posWorld);
				half3 lightDir = lerp(lightDir2, lightDir1, _UseFixedLight);
				half nDotL = max(0,dot(normal,lightDir));
				half3 lightColor = _LightColor0.rgb;
                fixed3 finalColor = c.rgb*(nDotL*lightColor + 1);
                return half4(finalColor, alpha * mask);
			#elif defined(_RENDERMODE_POM)
				i.uv.xy += frac(_Time.x * _CloudSpeed.zw * 5);
				i.uv2.xy += frac(_Time.x * _CloudSpeed.xy);
				half3 viewRay=normalize(i.viewDir*-1);
				viewRay.z=abs(viewRay.z)+0.2;
				viewRay.xy *= _Height;
				
				half3 shadeP = half3(i.uv.xy,0);
				half3 shadeP2 = half3(i.uv2.xy,0);
				
				half linearStep = 8;
   
				half4 T = tex2D(_MainTex, shadeP2.xy);
				half h2 = T.a * _HeightAmount;
   
				half3 lioffset = viewRay / (viewRay.z * linearStep);
				half d = 1.0 - tex2Dlod(_MainTex, half4(shadeP.xy,0,0)).a * h2;
				half3 prev_d = d;
				half3 prev_shadeP = shadeP;
				while(d > shadeP.z)
				{
					prev_shadeP = shadeP;
					shadeP += lioffset;
					prev_d = d;
					d = 1.0 - tex2Dlod(_MainTex, half4(shadeP.xy,0,0)).a * h2;
				}
				half d1 = d - shadeP.z;
				half d2 = prev_d - prev_shadeP.z;
				half w = d1 / (d1 - d2 + 0.000001);
				shadeP = lerp(shadeP, prev_shadeP, w);
				
                half sceneZ = LinearEyeDepth (SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, (i.pos.xy/ _ScreenParams.xy)));
				half partZ = i.projPos.z;
				half offset = (shadeP - shadeP2).z;
				half depthFade = saturate((sceneZ - partZ - offset) / _DepthFadeMultiplier);
   
				half mask = tex2Dlod(_MaskTex, half4(i.uv2.zw,0,0));
				half4 c = tex2D(_MainTex,shadeP.xy) * T * _Color;
				half alpha = saturate(T.a * c.a - _Alpha) / (1.0001 - _Alpha) * depthFade;
				
				half3 normal = normalize(i.normalDir);
				half3 lightDir1 = normalize(_FixedLightDir.xyz);
				half3 lightDir2 = UnityWorldSpaceLightDir(i.posWorld);
				half3 lightDir = lerp(lightDir2, lightDir1, _UseFixedLight);
				half nDotL = max(0,dot(normal,lightDir));
				half3 lightColor = _LightColor0.rgb;
                fixed3 finalColor = c.rgb*(nDotL*lightColor + 1.0);
                return half4(finalColor.rgb, alpha * mask);
			#endif
			}
		ENDCG
		}
	}
	FallBack "Diffuse"
	CustomEditor "Matrix.EcosystemSimulate.ModelCloudShaderGUi"
}
