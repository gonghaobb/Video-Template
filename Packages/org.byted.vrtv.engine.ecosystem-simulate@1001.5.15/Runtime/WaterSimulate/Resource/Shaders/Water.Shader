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

Shader "PicoVideo/Scene/Water"
{
    Properties
    {
        [Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
        [HideInInspector]_Cutoff("Threshold", Range(0.0, 1.0)) = 0.5
        [SubToggle(_SurfaceOptions)] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.LightingMode,1,_BlingPhongSpecColor)]_LightingMode("Lighting Mode", Float) = 0
        [HideInInspector][Sub(_SurfaceOptions)] [HDR]_BlingPhongSpecColor("SpecColor", Color) = (1,1,1,1)

        [Main(_SurfaceInputs, _, on, off)]_SurfaceInputs("Surface Inputs", Float) = 0
        [Title(_SurfaceInputs, Color)]
		[Sub(_SurfaceInputs)][HDR][MainColor] _ShallowBaseColor("Shallow Base Color", Color) = (1,1,1,1)
        [Sub(_SurfaceInputs)][HDR] _DeepBaseColor("Deep Base Color", Color) = (1,1,1,1)
        [Sub(_SurfaceInputs)] _WaterDepthMultiple("Water Depth Multiple", Range(1, 10)) = 1
        [Sub(_SurfaceInputs)] _WaterDepthAdd("Water Depth Add", Range(0, 0.1)) = 0.0
        [Sub(_SurfaceInputs)] _ShallowDepthCutOff("Shallow Depth Cut Off", Range(0.0, 1)) = 0.1
        [Sub(_SurfaceInputs)] _TransparentDepthCutOff("Transparent Depth Cut Off", Range(0.001, 0.2)) = 0.1
        [Sub(_SurfaceInputs)] _TransparentAdd("Transparent Add", Range(0.00, 1)) = 0.1
        [Sub(_SurfaceInputs)] _OutputScale("Output Scale", Range(0.01, 1)) = 1
        [Title(_SurfaceInputs, PBR)]
        [Sub(_SurfaceInputs)]_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Sub(_SurfaceInputs)]_Smoothness("Smoothness", Range(0.0, 1.0)) = 1
        [Title(_SurfaceInputs, Normal)]
        [ExtendSub(_SurfaceInputs, _NORMALMAP)]_NormalMap("Normal Map (RG), Metallic(B), Smoothness(A)", 2D) = "white" {}
        [Sub(_SurfaceInputs)]_NormalScaleSpeed1("Normal Scale1(x) Speed1(yz)", Vector) = (1,1,1,1)
        [Sub(_SurfaceInputs)]_NormalScaleSpeed2("Normal Scale2(x) Speed2(yz)", Vector) = (1,1,1,1)

        [Main(_FlowMapOptions, _FLOWMAP)]_FlowMapOptions("FlowMap Options", Float) = 0
        [ExtendTex(_FlowMapOptions)]_FlowMap("Flow Map (RG)", 2D) = "white" {}
        [Sub(_FlowMapOptions)] _FlowDirSpeed("Flow Direction Speed", Float) = 1
        [Sub(_FlowMapOptions)] _FlowTimeSpeed("Flow Time Speed", Float) = 1
        [Sub(_FlowMapOptions)] _FlowNormalScale("Flow Normal Scale", Float) = 1

        [Main(_DepthOptions, _, on, off)]_DepthOptions("Depth Options", Float) = 0
        [SubToggle(_DepthOptions, _REALTIME_DEPTH_MAP)] _RealtimeDepth("Realtime Depth", Float) = 0.0
        [Sub(_DepthOptions)] _MaxWaterDepthDiff("Max Water Depth", Range(1, 100)) = 10
        [Tex(_DepthOptions)]_OfflineDepthMap("Offline Depth Map", 2D) = "white" {}
        [Sub(_DepthOptions)]_OfflineTextureWorldOrgin("Camera World Origin(x,y,z)", Vector) = (0, 0, 0, 0)
        [Sub(_DepthOptions)]_OfflineTextureCameraSettings("Camera Size(x) Near(y) Far(z)", Vector) = (100, 0, 100, 0)

        [Main(_RefractionOptions,  _, on, off)]_RefractionOptions("Refraction Options", Float) = 0
        [SubToggle(_RefractionOptions, _REALTIME_REFRACTION)] _RealtimeRefraction("Realtime Refraction", Float) = 0.0
        [Tex(_RefractionOptions)]_OfflineRefractionMap("Offline Refraction Map", 2D) = "white" {}
        [Sub(_RefractionOptions)]_OfflineRefractionLodMultiple("Offline Refraction Lod Multiple", Range(0, 10)) = 0
        [Sub(_RefractionOptions)][HDR]_RefractionColor("Refraction Color", Color) = (1,1,1,1)
        [Sub(_RefractionOptions)]_RefractionNoiseIntensity("Refraction Noise Intensity", Range(0, 1)) = 1
        [Sub(_RefractionOptions)]_RefractionDepthCutOff("Refraction Depth Cut Off", Range(0.01, 1)) = 1
        [Sub(_RefractionOptions)]_RefractionIntensity("Refraction Intensity", Range(0.01, 1)) = 1

        [Main(_CausticsOptions,  _, on, off)]_CausticsOptions("Caustics Options", Float) = 0
        [ExtendSub(_CausticsOptions, _CAUSTICS)]_CausticsMaskMap("Caustics Mask Map", 2D) = "white" {}
        [Sub(_CausticsOptions)][HDR]_CausticsColor("Caustics Color", Color) = (1,1,1,1)
		[Sub(_CausticsOptions)]_CausticsSpeed("Caustics Speed", Float) = 1
        [Sub(_CausticsOptions)]_CausticsDepthCutOff("Caustics Depth Cut Off", Range(0.01, 1)) = 1
        [Sub(_CausticsOptions)]_CausticsIntensity("Caustics Intensity", Range(0.01, 2)) = 1

        [Main(_ReflectionOptions,  _, on, off)]_ReflectionOptions("Reflection Options", Float) = 0
        [Title(_ReflectionOptions, Cubemap Reflection)]
		[ExtendTex(_ReflectionOptions, _, _CUBEMAP_REFLECTION)]_ReflectionCubeMap("Reflection CubeMap", Cube) = "white" {}
        [Sub(_ReflectionOptions)]_ReflectionCubemapLod("Reflection Cubemap Lod", Range(0, 10)) = 0
        [Sub(_ReflectionOptions)][HDR]_ReflectionCubemapColor("Reflection Cubemap Color", Color) = (1,1,1,1)
        [Sub(_ReflectionOptions)]_ReflectionCubemapIntensity("Reflection Cubemap Intensity", Range(0.01, 5)) = 1
        [Title(_ReflectionOptions, Probe Reflection)]
        [SubToggle(_ReflectionOptions, _PROBE_REFLECTION)] _ProbeReflection("Probe Reflection", Float) = 1.0
        [Sub(_ReflectionOptions)][HDR]_ProbeReflectionColor("Probe Reflection Color", Color) = (1,1,1,1)
        [Sub(_ReflectionOptions)]_ProbeReflectionIntensity("Probe Reflection Intensity", Range(0.01, 2)) = 1

        [Main(_FoamOptions, _, on, off)] _FoamOptions("Foam Options", Float) = 0
        [ExtendSub(_FoamOptions, _FOAM)]_FoamMap("Foam Map", 2D) = "white" {}
        [Sub(_FoamOptions)][HDR]_FoamColor("Foam Color", Color) = (1,1,1,1)
        [Sub(_FoamOptions)]_FoamSpeed1("Foam Base Speed", Float) = 1
        [Sub(_FoamOptions)]_FoamSpeed2("Foam Roll Speed", Float) = 1
        [Sub(_FoamOptions)]_FoamThickness("Foam Thickness", Range(0.001, 1)) = 1
        [Sub(_FoamOptions)]_FoamIntensity("Foam Intensity", Range(0.001, 2)) = 1

        [Main(_SSSOptions, _SSS)] _SSSOptions("SSS Options", Float) = 0
        [Sub(_SSSOptions)][HDR]_SSSColor("SSS Color", Color) = (1,1,1,1)
        [Sub(_SSSOptions)]_SSSLightDistortion("SSS Light Distortion", Range(0, 3)) = 1
        [Sub(_SSSOptions)]_SSSLightPower("SSS Light Power", Range(1, 20)) = 10
        [Sub(_SSSOptions)]_SSSLighDepthCutOff("SSS Ligh Depth Cut Off", Range(0.01, 1)) = 1
        [Sub(_SSSOptions)]_SSSLightIntensity("SSS Light Intensity", Range(0.01, 1)) = 1

        [Main(_AdvancedOptions, _, on, off)]_AdvancedOptions("Advanced Options", Float) = 0
        [SubToggle(_AdvancedOptions)] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [SubToggle(_AdvancedOptions, _CUSTOM_SPEC_LIGHT_DIRECTION)]_EnableCustomSpecular("Enable Custom Specular", Float) = 0
        [Sub(_AdvancedOptions_CUSTOM_SPEC_LIGHT_DIRECTION)]_CustomSpecularIntensity("Custom Specular Intensity", Range(0,1)) = 1
        [KWEnum(_AdvancedOptions, None, _, Rain, _GLOBAL_RAIN_SURFACE, Snow, _GLOBAL_SNOW_SURFACE)] _WeatherType("WeatherType", float) = 0
	    [SubToggle(_AdvancedOptions, CUSTOM_FOG_FRAGMENT)] _CustomFogFragment("Custom Fog Use Fragment", float) = 0
        [ExtendToggle(_AdvancedOptions, _GLOBAL_CLOUD_SHADOW, _CloudShadowIntensity)] _GlobalCloudShadow("Global Cloud Shadow", float) = 0
        [HideInInspector][Sub(_AdvancedOptions)] _CloudShadowIntensity("Global Cloud Shadow Intensity", Range(0, 1)) = 1
        [Sub(_AdvancedOptions)] _AdjustColorIntensity("Global Adjust Color Intensity", Range(0, 1)) = 1
        [Sub(_AdvancedOptions)] _QueueOffset("Priority", Range(-50, 50)) = 0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (0.5, 0.5, 0.5, 1)

        [HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
    }

    SubShader
	{
        Tags 
		{
            "RenderType" = "Transparent"
            "Queue"="Transparent"
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        HLSLINCLUDE

        //天气系统影响
        #pragma shader_feature_local _ _GLOBAL_RAIN_SURFACE _GLOBAL_SNOW_SURFACE

        ENDHLSL

        Pass
		{
            Name "UniversalForward"

            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _PBR _BLING_PHONG
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _FLOWMAP
            #pragma shader_feature_local _REALTIME_DEPTH_MAP
            #pragma shader_feature_local _REALTIME_REFRACTION
            #pragma shader_feature_local_fragment _CUBEMAP_REFLECTION
            #pragma shader_feature_local_fragment _PROBE_REFLECTION
            #pragma shader_feature_local_fragment _REFLECTION_PROBE_BOX_PROJECTION
            #define _ENVIRONMENTREFLECTIONS_OFF 1 //关闭默认的环境反射
            #pragma shader_feature_local_fragment _CAUSTICS
            #pragma shader_feature_local_fragment _FOAM
            #pragma shader_feature_local_fragment _SSS

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS //_MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ /*_ADDITIONAL_LIGHTS_VERTEX*/ _ADDITIONAL_LIGHTS
            //#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT
            //#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // -------------------------------------
            // Unity defined keywords
            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            //#pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma shader_feature_local_fragment _ _CUSTOM_SPEC_LIGHT_DIRECTION

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ CUSTOM_FOG
            #pragma shader_feature _ CUSTOM_FOG_FRAGMENT
            #pragma shader_feature_local_fragment _GLOBAL_CLOUD_SHADOW

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Includes/WaterInput.hlsl"
            #include "Includes/WaterForwardPass.hlsl"

            ENDHLSL
        }

        Pass
		{
            Name "ForwardWater"

            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin
            ZTest Always

            Tags
            {
                "LightMode" = "ForwardWater"
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _PBR _BLING_PHONG
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local _REALTIME_DEPTH_MAP
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _FLOWMAP
            #pragma shader_feature_local _REALTIME_REFRACTION
            #pragma shader_feature_local_fragment _CUBEMAP_REFLECTION
            #pragma shader_feature_local_fragment _PROBE_REFLECTION
            #pragma shader_feature_local_fragment _REFLECTION_PROBE_BOX_PROJECTION
            #define _ENVIRONMENTREFLECTIONS_OFF 1 //关闭默认的环境反射
            #pragma shader_feature_local_fragment _CAUSTICS
            #pragma shader_feature_local_fragment _FOAM
            #pragma shader_feature_local_fragment _SSS

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS //_MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ /*_ADDITIONAL_LIGHTS_VERTEX*/ _ADDITIONAL_LIGHTS
            //#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT
            //#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // -------------------------------------
            // Unity defined keywords
            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            //#pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ CUSTOM_FOG
            #pragma shader_feature _ CUSTOM_FOG_FRAGMENT
            #pragma shader_feature_local_fragment _GLOBAL_CLOUD_SHADOW

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Includes/WaterInput.hlsl"
            #include "Includes/WaterForwardPass.hlsl"

            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMeta

            #pragma shader_feature_local_fragment _EMISSION

            #include "Includes/WaterInput.hlsl"
            #include "Includes/WaterBase.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv0          : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                float2 uv2          : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 positionWS   : TEXCOORD1;

                #if _REALTIME_DEPTH_MAP || _REALTIME_REFRACTION
                    float4 screenPos                : TEXCOORD2;
                #endif
            };

            Varyings UniversalVertexMeta(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
                output.uv = input.uv0;

                #if _REALTIME_DEPTH_MAP || _REALTIME_REFRACTION
                    output.screenPos = ComputeScreenPos(output.positionCS);
                #endif

                return output;
            }

            half4 UniversalFragmentMeta(Varyings input) : SV_Target
            {
                #if _REALTIME_DEPTH_MAP || _REALTIME_REFRACTION
                    float2 screenUV = input.screenPos.xy / input.screenPos.w;
                #endif

                float2 offlineWorldUV = GetOfflineWorldUV(input.positionWS);

                #if _REALTIME_DEPTH_MAP
                    float waterDepth = GetRealtimeWaterEyeDepth(input.screenPos);
                    float sceneDepth = GetSceneEyeDepth(screenUV);
                #else
                    float waterDepth = GetOfflineWaterEyeDepth(input.positionWS);
                    float sceneDepth = GetSceneEyeDepth(offlineWorldUV);
                #endif

                float sceneDeltaDepth = max((sceneDepth - waterDepth) / _MaxWaterDepthDiff, 0.0);

                float2 uv = input.uv;
                #if _OFFLINE_REFRACTION
                    uv = offlineWorldUV;
                #endif

                float2 UVPanner1;
                float2 UVPanner2;
                float flowLerp;
                SurfaceData surfaceData;
                InitializeWaterSurfaceData(uv, sceneDeltaDepth, UVPanner1, UVPanner2, flowLerp, surfaceData);

                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

                MetaInput metaInput;
                metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
                metaInput.SpecularColor = surfaceData.specular;
                metaInput.Emission = surfaceData.emission;

                return MetaFragment(metaInput);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Matrix.EcosystemSimulate.WaterShaderGUI"
}