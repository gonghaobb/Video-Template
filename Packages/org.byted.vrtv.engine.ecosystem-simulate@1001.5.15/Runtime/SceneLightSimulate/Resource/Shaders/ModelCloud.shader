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

Shader "PicoVideo/EcosystemSimulate/CloudSimulate/ModelCloud"
{
    Properties
    {
        [Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.SurfaceType,1,_Blend)]_Surface("Surface Type", Float) = 1
        [HideInInspector][ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.BlendMode)] _Blend("Blending Mode 混合模式", Float) = 0.0
        [Ramp(_SurfaceOptions)]_LightColorGradient("Lihgt Color Gradient 光照色带图", 2D) = "white"{}
        [SubToggle(_SurfaceOptions, _CLOUD_CUSTOM_LIGHT)]_CustomLight("Enable Custom Light", float) = 0
        [Sub(_SurfaceOptions)]_LightIntensity("Light Intensity 方向光强度", Range(0, 1)) = 1
        [Sub(_SurfaceOptions)]_AdditionLightIntensity("Addition Light Intensity 点光源强度", Range(0, 1)) = 1
        [Sub(_SurfaceOptions)]_AOStrength("AO Strength AO强度(需要R通道顶点色)", Range(0,1)) = 1
        
        [Main(_SurfaceInputs, _, on, off)]_SurfaceInputs("Surface Inputs", Float) = 0
        [Sub(_SurfaceInputs)]_BaseMap("Base Map 云基础贴图", 2D) = "white" {}
        [Sub(_SurfaceInputs)][ExtendTex(_, _, _NORMAL_MAP, false)]
        [ExtendSub(_SurfaceInputs, _NORMAL_MAP)][NoScaleOffset]_NormalMap("Normal Map 法线图", 2D) = "gray"{}
        [Sub(_SurfaceInputs)]_BumpScale("Normal Scale 法线强度", Float) = 1
        [Sub(_SurfaceInputs)]_UVAnimation("UV Animation uv动画", Vector) = (0, 0, 0, 0)
        [Sub(_SurfaceInputs)]_Alpha("Alpha 整体透明度", Range(0,1)) = 1
        [Sub(_SurfaceInputs)]_NoiseTexture("Noise Texture 噪声贴图", 2D) = "gray"{}
        [Sub(_SurfaceInputs)]_NoiseIntensity("Noise Intensity 噪声强度", Float) = 1
        [Sub(_SurfaceInputs)]_NoiseTiling("Noise Tiling 噪声Tiling", Float) = 1
        [Sub(_SurfaceInputs)]_NoiseSpeed("Noise Speed 噪声移动速度", Vector) = (1,1,1,0)

        [Main(_OutlineOption,  _, on, off)]_OutlineOption("Outline Option", Float) = 0
        [Sub(_OutlineOption)]_FuzzyPower("Edge Power 边缘效果指数", Float) = 1
        [Sub(_OutlineOption)]_EdgeBrightness("Edge Brightness 边缘亮度增强", Float) = 1
        [Sub(_OutlineOption)]_CoreDarkness("Core Darkness 中心暗部增强", Float) = 1
        [SubToggle(_OutlineOption, _EDGE_FADE)] _EdgeFade("Edge Fade 开启边缘透明度渐变", Float) = 0
        
        [Main(_FadeOptions,  _, on, off)]_FadeOptions("Fade Options", Float) = 0
        [Sub(_FadeOptions)]_AlphaFade("Alpha Fade 底部透明度渐隐强度", Range(0,1)) = 0
        [Sub(_FadeOptions)]_AlphaFadePosition("Alpha Fade Position 渐隐位置", Range(-1,1)) = 0
        [Sub(_FadeOptions)]_AlphaFadeRate("Alpha Fade Rate 渐隐过度", Range(0,1)) = 0
        [Sub(_FadeOptions)]_AlphaFadeNoiseIntensity("Alpha Fade Noise Intensity 渐隐噪声强度", Range(0,1)) = 0
        [SubToggle(_FadeOptions, _ENABLE_DEPTH_FADE)]_EnableDepthFade("Enable Depth Fade", float) = 0
        [Sub(_FadeOptions)]_DepthFade("Depth Fade", Range(0.001,2)) = 1
        
        [Main(_InsidePowerOptions,  _, on, off)]_InsidePowerOptions("Inside Power Options", Float) = 0
        [SubToggle(_InsidePowerOptions)]_InsidePowerEnable("Inside Enable 开启内部强度渐变", Float) = 0
        [SubToggle(_InsidePowerOptions)]_InsidePowerDebug("Inside Debug Debug显示渐变值", Float) = 0
        [Sub(_InsidePowerOptions)]_InsidePowerStrength("Inside Strength 渐变强度", Float) = 1
        [Sub(_InsidePowerOptions)]_InsidePowerScale("Inside Scale 渐变范围缩放", Vector) = (0,0,0,0)
        [Sub(_InsidePowerOptions)]_InsidePowerOffset("Inside Offset 渐变位置偏移", Vector) = (0,0,0,0) 
        
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
    }
    SubShader
    {
        Pass
        {
            Tags { "RenderType"="Transparent" "LightMode" = "SRPDefaultUnlit" "Queue"="Transparent"}
            LOD 100
            Blend zero one
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/SceneLightSimulate/Resource/Shaders/CloudCommon.hlsl"
            
 			CBUFFER_START(UnityPerMaterial)
            uniform half4 _NoiseSpeed;
            uniform half4 _UVAnimation;
            uniform half4 _InsidePowerScale;
            uniform half4 _InsidePowerOffset;
            half4 _BaseMap_ST;
            half4 _NormalMap_ST;
            uniform half _AOStrength;
            uniform half _LightIntensity;
            uniform half _NoiseIntensity;
            uniform half _NoiseTiling;
            uniform half _Alpha;
            uniform half _FuzzyPower;
            uniform half _EdgeBrightness;
            uniform half _CoreDarkness;
            uniform half _AlphaFade;
            uniform half _AlphaFadePosition;
            uniform half _AlphaFadeRate;
            uniform half _AdditionLightIntensity;
            uniform half _BumpScale;
            uniform half _InsidePowerStrength;
            uniform half _AlphaFadeNoiseIntensity;
            uniform half _DepthFade;
			CBUFFER_END
            sampler2D _LightColorGradient;
            sampler2D _NormalMap;
            sampler2D _BaseMap;
            sampler2D _NoiseTexture;
            struct appdata
            {
                half4 vertex : POSITION;
                half2 uv : TEXCOORD0;
                half3 normal : NORMAL;
                half4 tangent : TANGENT;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                half4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };


            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                half3 positionWSOrigin = TransformObjectToWorld(v.vertex.xyz);
                half3 positionUv = positionWSOrigin * _NoiseTiling + _NoiseSpeed.xyz * _Time.x;
                half4 noiseUV = half4(positionUv.x + positionUv.y * 0.5, positionUv.z + positionUv.y * 0.5,0,0);
                half noise = tex2Dlod(_NoiseTexture, noiseUV).x;
                noise = noise * 2 - 1;
                v.vertex.xyz = TransformWorldToObject(positionWSOrigin);
                v.vertex.xyz += noise * _NoiseIntensity * normalize(v.normal) * 0.1;
                half3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                #if UNITY_REVERSED_Z
                o.vertex.z -= 0.0001;
                #else
                o.vertex.z += 0.0001;
                #endif
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                return half4(0,0,0,1);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "RenderType"="Transparent"  "LightMode" = "UniversalForward" "Queue"="Transparent"}
            LOD 100
            Blend[_SrcBlend][_DstBlend]
            ZWrite On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local_fragment _ _EDGE_FADE
            #pragma shader_feature_local_fragment _ _NORMAL_MAP
            #pragma shader_feature_local _ _INSIDEPOWERDEBUG_ON
            #pragma shader_feature_local _ _INSIDEPOWERENABLE_ON
            #pragma shader_feature_local_fragment _ _CLOUD_CUSTOM_LIGHT
            #pragma shader_feature_local_fragment _ _ENABLE_DEPTH_FADE
            
            // make fog work
            #pragma multi_compile_fog
            #pragma multi_compile _ CUSTOM_FOG
            #pragma shader_feature _ CUSTOM_FOG_FRAGMENT
            #ifdef CUSTOM_FOG
            #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/FogSimulate/Resource/Shaders/Fog.hlsl"
            #endif

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/SceneLightSimulate/Resource/Shaders/CloudCommon.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
 			CBUFFER_START(UnityPerMaterial)
            uniform half4 _NoiseSpeed;
            uniform half4 _UVAnimation;
            uniform half4 _InsidePowerScale;
            uniform half4 _InsidePowerOffset;
            half4 _BaseMap_ST;
            half4 _NormalMap_ST;
            uniform half _AOStrength;
            uniform half _LightIntensity;
            uniform half _NoiseIntensity;
            uniform half _NoiseTiling;
            uniform half _Alpha;
            uniform half _FuzzyPower;
            uniform half _EdgeBrightness;
            uniform half _CoreDarkness;
            uniform half _AlphaFade;
            uniform half _AlphaFadePosition;
            uniform half _AlphaFadeRate;
            uniform half _AdditionLightIntensity;
            uniform half _BumpScale;
            uniform half _InsidePowerStrength;
            uniform half _AlphaFadeNoiseIntensity;
            uniform half _DepthFade;
			CBUFFER_END
            sampler2D _LightColorGradient;
            sampler2D _NormalMap;
            sampler2D _BaseMap;
            sampler2D _NoiseTexture;

            half SphereSDF(half3 samplePoint)
            {
                half3 pointOS = TransformWorldToObject(samplePoint);
                pointOS /= _InsidePowerScale.xyz;
                pointOS += _InsidePowerOffset.xyz / 10;
                return saturate(1 - length(pointOS));
            }
            
            half ShortestDistanceToSurface(half3 eye, half3 marchingDirection, half start, half end)
            {
                half depth = start;
                half sum = 0;
                // half step = 20;
                half offset = (end - start) / 20;
                int i = 0;
                half dist = 0;
                [unroll(20)]
                for ( ;i < 20; i++) {
                    dist = SphereSDF(eye + depth * marchingDirection);
                    sum += dist;
                    depth += offset;
                }
                return sum / 20;
            }
            
            struct appdata
            {
                half4 vertex : POSITION;
                half2 uv : TEXCOORD0;
                half3 normal : NORMAL;
                half4 tangent : TANGENT;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                half4 uv : TEXCOORD0;
                half4 vertex : SV_POSITION;
                half3 positionWS : TEXCOORD1;
                half4 normalWS : TEXCOORD2;                    // w : height Alpha
                half4 tangentWS : TEXCOORD3;
                half4 vertexColor : TEXCOORD4;
                half4 viewDirectionWSAndFogFactor : TEXCOORD5;
                half4 center : TEXCOORD6;                      //w : insidePower
                float4 screenUV : TEXCOORD7;
                half3 test : TEXCOORD8;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert (appdata v)
            {
                v2f o;
                            
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                half3 center = unity_ObjectToWorld._m03_m13_m23;
                half3 positionWSOrigin = TransformObjectToWorld(v.vertex.xyz);
                half3 positionUv = positionWSOrigin * _NoiseTiling + _NoiseSpeed.xyz * _Time.x;
                half4 noiseUV = half4(positionUv.x + positionUv.y * 0.5, positionUv.z + positionUv.y * 0.5,0,0);
                half noise = tex2Dlod(_NoiseTexture, noiseUV).r;
                noise = noise * 2 - 1;
                half alphaFade = 1 - (saturate((_AlphaFadePosition * 5 - positionWSOrigin.y + center.y  + noise * _AlphaFadeNoiseIntensity) * _AlphaFadeRate * 5) * _AlphaFade);
                
                v.vertex.xyz = TransformWorldToObject(positionWSOrigin);
                v.vertex.xyz += noise * _NoiseIntensity * normalize(v.normal) * 0.1;
                half3 positionWS = TransformObjectToWorld(v.vertex.xyz);
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                o.viewDirectionWSAndFogFactor.xyz = normalize(GetWorldSpaceViewDir(positionWS));
                #if defined(CUSTOM_FOG)
                    half fogFactor = FogVert(positionWS);
                #else
                    half fogFactor = ComputeFogFactor(o.vertex.z);
                #endif
                o.viewDirectionWSAndFogFactor.w = fogFactor;
                o.positionWS = positionWS;
                // mikkts space compliant. only normalize when extracting normal at frag.
                real sign = v.tangent.w * GetOddNegativeScale();
                o.tangentWS = half4(TransformObjectToWorldDir(v.tangent.xyz), sign);

                o.normalWS.xyz = TransformObjectToWorldNormal(v.normal);
                o.normalWS.w = alphaFade;
                o.vertexColor = v.color;
                o.uv.xy = TRANSFORM_TEX(v.uv, _BaseMap);
                o.uv.zw = o.uv.xy + _UVAnimation.xy * _Time.x;
                o.center.xyz = center;
                o.center.w = 0;
                half depth = UNITY_Z_0_FAR_FROM_CLIPSPACE(o.vertex.z);
                o.screenUV = ComputeScreenPos(o.vertex);
                o.test = TransformWorldToView(positionWSOrigin);
                #ifdef _INSIDEPOWERENABLE_ON
                half insidePower = ShortestDistanceToSurface(_WorldSpaceCameraPos, -o.viewDirectionWSAndFogFactor.xyz, depth, depth + 5);
                insidePower = saturate(insidePower * _InsidePowerStrength);
                o.center.w = insidePower;
                #endif
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                half4 baseColor = tex2D(_BaseMap, i.uv.zw);
                #ifdef _NORMAL_MAP
                half4 normalColor = tex2D(_NormalMap, i.uv.zw);
                half3 normal;
                normal.xy = normalColor.rg * 2.0 - 1.0;
                normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));
                normal.xy *= _BumpScale;
                normal = normalize(normal);
                half sgn = i.tangentWS.w;      // should be either +1 or -1
                half3 bitangent = sgn * cross(i.normalWS.xyz, i.tangentWS.xyz);
                i.normalWS.xyz = TransformTangentToWorld(normal, half3x3(i.tangentWS.xyz, bitangent.xyz, i.normalWS.xyz));
                #endif
                
                half ao = _AOStrength * i.vertexColor.r;
                half mainLightIntensity = 0;
                half3 mainLightColor;
                half3 mainLightDirection;
                GetMainLight(i.positionWS, i.normalWS.xyz, mainLightIntensity, mainLightColor, mainLightDirection);
                half lightIntensity = saturate(mainLightIntensity) * (1 - ao) * _LightIntensity;
                half mainLightDir = saturate((dot(i.normalWS.xyz, mainLightDirection.xyz) + 1) / 2);

                half additionLightIntensity = 0;
                half3 additionLightColor;
                GetAdditionLight(i.positionWS, i.normalWS.xyz, i.viewDirectionWSAndFogFactor.xyz, additionLightIntensity, additionLightColor);
                additionLightIntensity = saturate(additionLightIntensity) * (1 - ao) * _AdditionLightIntensity;
                half lightDir = mainLightDir * _LightIntensity + additionLightIntensity * _AdditionLightIntensity;
          
                half4 gradient = tex2D(_LightColorGradient, half2(saturate(mainLightDir * lightIntensity + additionLightIntensity + 0.01) / 1.01, 0));
                half4 color = baseColor * gradient;

                color.rgba = FuzzyShading(color, _FuzzyPower, _EdgeBrightness, _CoreDarkness, i.normalWS.xyz, i.viewDirectionWSAndFogFactor.xyz, lightDir);
                color.rgb  *= lerp(1, saturate(mainLightColor), _LightIntensity);
                #ifdef _INSIDEPOWERENABLE_ON
                #if _INSIDEPOWERDEBUG_ON
                return half4(i.center.w,0,0,1);
                #endif
                color.a = lerp(color.a,1, i.center.w);
                #endif
                #ifdef _CLOUD_CUSTOM_LIGHT
                #endif
                #if defined(CUSTOM_FOG)
                color.rgb = FogFrag(color.rgb, i.viewDirectionWSAndFogFactor.xyz, i.positionWS, i.viewDirectionWSAndFogFactor.w);
                #else
                color.rgb = MixFog(color.rgb, i.viewDirectionWSAndFogFactor.w);
                #endif
                
                #ifdef _EDGE_FADE
                color.a *= i.normalWS.w;
                #else
                color.a = i.normalWS.w;
                #endif
                #ifdef _ENABLE_DEPTH_FADE
                half2 screenPos = i.screenUV.xy / i.screenUV.w;
                half rawDepth = SampleSceneDepth(screenPos);
                half sceneZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                half thisZ = LinearEyeDepth((i.screenUV.z / i.screenUV.w) * 0.5 + 0.5, _ZBufferParams);
                color.a *= saturate((sceneZ - thisZ) / _DepthFade);
                #endif
                color.a *= _Alpha;
                return color;
            }
            ENDHLSL
        }
    }
    CustomEditor "Matrix.EcosystemSimulate.ModelCloudShaderGUi"
}
