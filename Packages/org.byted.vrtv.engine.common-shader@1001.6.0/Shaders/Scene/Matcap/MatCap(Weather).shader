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

Shader "PicoVideo/Scene/MatCap(Weather)"
{
    Properties 
	{
        [Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.SurfaceType,1,_Blend)]_Surface("Surface Type", Float) = 0
        [HideInInspector][ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.BlendMode)] _Blend("Blending Mode", Float) = 0.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.RenderFace)]_Cull("Render Face", Float) = 2
        [ExtendToggle(_SurfaceOptions, _, _ZWrite)]_CustomZWrite("Custom ZWrite", Float) = 0
        [HideInInspector][SubToggle(_SurfaceOptions)]_ZWrite("ZWrite", Float) = 0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.ColorWriteMask)]_ColorMask ("ColorMask", Float) = 15
        [ExtendToggle(_SurfaceOptions, _ALPHATEST_ON, _Cutoff)] _AlphaClip("Alpha Clipping", Float) = 0.0
		[HideInInspector]_Cutoff("Threshold", Range(0.0, 1.0)) = 0.5
        [Title(_SurfaceOptions, Stencil)]
		[Sub(_SurfaceOptions)]_StencilRef("StencilRef", Range(0,255)) = 127
		[ExtendEnum(_SurfaceOptions, UnityEngine.Rendering.CompareFunction)]_StencilComp("StencilComp", int) = 8
        [ExtendEnum(_SurfaceOptions, UnityEngine.Rendering.StencilOp)]_StencilPass("Stencil Pass", Float) = 0
        [ExtendEnum(_SurfaceOptions, UnityEngine.Rendering.StencilOp)]_StencilFail("Stencil Fail", Float) = 0
        
        [Main(_SurfaceInputs, _, on, off)]_SurfaceInputs("Surface Inputs", Float) = 0
        [ExtendTex(_SurfaceInputs, _BaseColor, _, true)][MainTexture] _BaseMap("Albedo (Alpha)", 2D) = "white" {}
        [HideInInspector][MainColor][HDR]_BaseColor("Base Color", Color) = (1,1,1,1)
        [SubToggle(_SurfaceInputs)] _ApplyVertexColor("Apply Vertex Color", Float) = 1
        [ExtendToggle(_SurfaceInputs, _ALPHAMAP_ON, _AlphaScale)] _AlphaMap("Is Albedo A Alpha Map ?", Float) = 0.0
        [HideInInspector][Sub(_SurfaceInputs)]_AlphaScale("AlphaScale", range(0.01, 1)) = 1
        [ExtendTex(_SurfaceInputs, _NormalScale, _, true)]_NormalMap("Normal Map (RG)", 2D) = "white" {}
        [HideInInspector]_NormalScale("Normal Scale", Float) = 1.0
        [Sub(_SurfaceInputs)] _NormalUSpeed("Normal U Speed", Float) = 0
		[Sub(_SurfaceInputs)] _NormalVSpeed("Normal V Speed", Float) = 0

        [Main(_ReceiveLightOptions, _ENABLE_LIGHT_AFFECT)]_ReceiveLightOptions("RealTime Light Options", Float) = 0
        [Sub(_ReceiveLightOptions)]_RealTimeLightStrength("Real Time Light Strength", range(0, 1)) = 1
        [Sub(_ReceiveLightOptions)]_RealTimeShadowStrength("Real Time Shadow Strength", range(0, 1)) = 1
        [Sub(_ReceiveLightOptions)]_RealTimeShadowColor("Real Time Shadow Color", Color) = (0,0,0,1)
        [Sub(_ReceiveLightOptions)]_RealTimeShadowColorStrength("Real Time Shadow Color Strength", range(0, 1)) = 0

        [Main(_MatCapGroup, _MATCAP)]_MatCapGroup("MatCap Options", Float) = 0
        [Tex(_MatCapGroup)]_Matcap("Matcap", 2D) = "white" {}
        [Sub(_MatCapGroup)]_MatcapUVScale("Matcap UVScale", range(0.01, 1)) = 1
        [Sub(_MatCapGroup)]_MatcapStrength("Matcap Strength", range(0.01, 3)) = 1
        [SubToggle(_MatCapGroup, _MATCAP_FIX_EDGE_FLAW)] _FixEdgeFlaw("Fix Edge Flaw", Float) = 0.0
        [SubToggle(_MatCapGroup, _MATCAP_HIGHLIGHTS)] _MatCapHighLights("MatCap HighLights", Float) = 0.0
        [Sub(_MatCapGroup)][HDR]_MatCapHighLightsColor("HighLights Color", Color) = (1,1,1,1)
        [Sub(_MatCapGroup)]_MatCapHighLightsThreshold("HighLights Threshold", Range(0.0, 1.0)) = 0.0
        [Sub(_MatCapGroup)]_MatCapHighLightsStrength("HighLights Strength", Range(0.01, 10.0)) = 1

        [Main(_CartoonOptions, _CARTOON)]_CartoonOptions("Cartoon Options", Float) = 0
        [SubToggle(_CartoonOptions, _CUSTOM_CARTOON_LIGHT)]_CustomCartoonLight("Custom Cartoon Light", Float) = 0
        [SubToggle(_CartoonOptions)]_ShadowBoundaryFirst("FirstShadowBoundary", Float) = 0
	    [Sub(_CartoonOptions)][Gamma][HDR]_ShadowColorFirst("FirstShadowColor", Color) = (1,1,1,1)
        [Sub(_CartoonOptions)]_ShadowAreaFirst("FirstShadowArea", Range(0.0, 1.0)) = 0.5
        [Sub(_CartoonOptions)]_ShadowSmoothFirst("FirstShadowSmooth", Range(0.0, 1.0)) = 0.05
        [SubToggle(_CartoonOptions)]_ShadowBoundarySecond("SecondShadowBoundary", Float) = 0
        [Sub(_CartoonOptions)][Gamma][HDR]_ShadowColorSecond("SecondShadowColor", Color) = (1,1,1,1)
        [Sub(_CartoonOptions)]_ShadowAreaSecond("SecondShadowArea", Range(0.0, 1.0)) = 0.5
        [Sub(_CartoonOptions)]_ShadowSmoothSecond("SecondShadowSmooth", Range(0.0, 1.0)) = 0.05
        [SubToggle(_CartoonOptions)]_ShadowBoundaryThird("ThirdShadowBoundary", Float) = 0
        [Sub(_CartoonOptions)][Gamma][HDR]_ShadowColorThird("ThirdShadowColor", Color) = (1,1,1,1)
        [Sub(_CartoonOptions)]_ShadowAreaThird("ThirdShadowArea", Range(0.0, 1.0)) = 0.35
        [Sub(_CartoonOptions)]_ShadowSmoothThird("ThirdShadowSmooth", Range(0.0, 1.0)) = 0.05
        
        [Main(_RimLightOptions, _RIM_LIGHT)] _RimLightOptions ("RimLight Options", Float) = 0
        [Sub(_RimLightOptions)] _RimLightColor("Rim Light Color", Color) = (1, 1, 1, 1)
        [Sub(_RimLightOptions)] _RimLightWidth("Rim Light Width", Range(0.01, 30)) = 1
        [Sub(_RimLightOptions)] _RimLightSmoothness("Rim Light Smoothness", Range(1, 100)) = 1
        [Sub(_RimLightOptions)] _RimLightIntensity("Rim Light Intensity", Float) = 1
        [Sub(_RimLightOptions)] _RimLightMinValue ("Rim Light Min", Range(0, 1)) = 0
        [Sub(_RimLightOptions)] _RimLightMaxValue ("Rim Light Max", Range(1, 100)) = 1
        [SubToggle(_RimLightOptions)] _RimLightReverse("Rim Light Reverse", Float) = 0
        [SubToggle(_RimLightOptions)] _EnableRimLightVertexColorMask("Enable RimLight Vertex Color Mask", Float) = 0
        [Channel(_RimLightOptions)] _RimLightVertexColorMask("RimLight Vertex Color Mask (Default R)", Vector) = (1,0,0,0)
        [Title(_RimLightOptions, Transparency)]
        [SubToggle(_RimLightOptions, _RIM_TRANSPARENCY)] _RimTransparency("Rim Transparency", float) = 0
        [Sub(_RimLightOptions_RIM_TRANSPARENCY)] _RimTransparencyWidth("Rim Transparency Width", Range(0.01, 30)) = 1
        [Sub(_RimLightOptions_RIM_TRANSPARENCY)] _RimTransparencySmoothness("Rim Transparency Smoothness", Range(1, 100)) = 1
        [Sub(_RimLightOptions_RIM_TRANSPARENCY)] _RimTransparencyIntensity("Rim Transparency Intensity", Float) = 1
        [Sub(_RimLightOptions_RIM_TRANSPARENCY)] _RimTransparencyMinValue ("Rim Transparency Min", Range(0, 1)) = 0
        [Sub(_RimLightOptions_RIM_TRANSPARENCY)] _RimTransparencyMaxValue ("Rim Transparency Max", Range(1, 100)) = 1
        [SubToggle(_RimLightOptions_RIM_TRANSPARENCY)] _RimTransparencyReverse("Rim Transparency Reverse", Float) = 0
        [Sub(_RimLightOptions_RIM_TRANSPARENCY)] _RimTransparencyBaseAlpha("Rim Transparency Base Alpha", Range(0, 1)) = 1
        [Title(_RimLightOptions, Refraction)]
        [SubToggle(_RimLightOptions, _RIM_REFRACTIONY)] _RimRefraction("Rim Refraction", float) = 0
        [Sub(_RimLightOptions_RIM_REFRACTIONY)] _RimRefractionWidth("Rim Refraction Width", Range(0.01, 30)) = 1
        [Sub(_RimLightOptions_RIM_REFRACTIONY)] _RimRefractionSmoothness("Rim Refraction Smoothness", Range(1, 100)) = 1
        [Sub(_RimLightOptions_RIM_REFRACTIONY)] _RimRefractionIntensity("Rim Refraction Intensity", Float) = 1
        [Sub(_RimLightOptions_RIM_REFRACTIONY)] _RimRefractionMinValue ("Rim Refraction Min", Range(0, 1)) = 0
        [Sub(_RimLightOptions_RIM_REFRACTIONY)] _RimRefractionMaxValue ("Rim Refraction Max", Range(1, 100)) = 1
        [SubToggle(_RimLightOptions_RIM_REFRACTIONY)] _RimRefractionReverse("Rim Refraction Reverse", Float) = 0
        [Sub(_RimLightOptions_RIM_REFRACTIONY)]_RimRefractionMap("Rim Refraction Map", 2D) = "white" {}
        [SubToggle(_RimLightOptions_RIM_REFRACTIONY)] _RimRefractionWorldPosUV("Use WorldPos As UV", Float) = 0

        [Main(_EmissionOptions, _EMISSION)]_EmissionOptions("Emission Options", Float) = 0
		[Sub(_EmissionOptions)]_EmissionMap("Emission Map", 2D) = "white" {}
        [Sub(_EmissionOptions)][HDR]_EmissionColor("Emission Color", Color) = (0,0,0,1)
        [SubToggle(_EmissionOptions)] _EmissionWorldPosUV("Use WorldPos As UV", Float) = 0
        [Sub(_EmissionOptions)] _EmissioUSpeed("Emission U Speed", Float) = 0
	    [Sub(_EmissionOptions)] _EmissioVSpeed("Emission V Speed", Float) = 0
        [Sub(_EmissionOptions)]_EmissionIntensity("Emission Intensity", Range(0.01, 10)) = 1
        [Title(_EmissionOptions, Cubemap)]
        [Tex(_EmissionOptions, _EmissionCubemapColor)]_EmssionCubemap("Emssion Cubemap", Cube) = "" {}
        [HideInInspector][HDR]_EmissionCubemapColor("EmissionCubemapColor", Color) = (1,1,1,1)
        [Sub(_EmissionOptions)]_EmissionCubemapIntensity("Emission Cubemap Intensity", Range(0.01, 10)) = 1
        [Sub(_EmissionOptions)]_EmissionCubemapBias("Emission Cubemap Bias", Range(-1.2, 1)) = 0
        [ExtendToggle(_EmissionOptions, _EMISSION_CUBEMAP_LOD, _EmissionCubemapLod)] _EnableEmissionCubemapLod("Enable Emission Cubemap Lod", Float) = 1
        [HideInInspector][Sub(_EmissionOptions)]_EmissionCubemapLod("Emission Cubemap Lod", Range(0, 10)) = 0
        [SubToggle(_EmissionOptions, _BOX_PROJECTION_CUBEMAP)] _BoxProjectionCubemapReflection("Box Projection Cubemap Reflection", Float) = 0.0
        [Sub(_EmissionOptions_BOX_PROJECTION_CUBEMAP)]_EmissionCubemapBoxPostion("Emission Cubemap Box Position", Vector) = (0, 0, 0, 0)
        [Sub(_EmissionOptions_BOX_PROJECTION_CUBEMAP)]_EmissionCubemapBoxMin("Emission Cubemap Box Min", Vector) = (-1, -1, -1, 0)
        [Sub(_EmissionOptions_BOX_PROJECTION_CUBEMAP)]_EmissionCubemapBoxMax("Emission Cubemap Box Max", Vector) = (1, 1, 1, 0)

        [Main(_AdvancedOptions, _, on, off)]_AdvancedOptions("Advanced Options", Float) = 0
        [SubToggle(_AdvancedOptions, CUSTOM_FOG_FRAGMENT)] _CustomFogFragment("Custom Fog Use Fragment", float) = 0
        [ExtendToggle(_AdvancedOptions, _GLOBAL_CLOUD_SHADOW, _CloudShadowIntensity)] _GlobalCloudShadow("Global Cloud Shadow", float) = 0
        [HideInInspector][Sub(_AdvancedOptions)] _CloudShadowIntensity("Global Cloud Shadow Intensity", Range(0, 1)) = 1
        [Sub(_AdvancedOptions)] _AdjustColorIntensity("Global Adjust Color Intensity", Range(0, 1)) = 1
        [SubIntRange(_AdvancedOptions)] _QueueOffset("Priority", Range(-50, 50)) = 0
        
        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (0.5, 0.5, 0.5, 1)

        [HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		//PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;Start
        [HideInInspector] _SrcBlendAlpha("__srcAlpha", Float) = 1.0
        [HideInInspector] _DstBlendAlpha("__dstAlpha", Float) = 0.0
        //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;End
		//[HideInInspector] _ZWrite("__zw", Float) = 1.0
    }

    SubShader
	{
        Tags 
		{
            "IgnoreProjector"="True"
            "Queue"="Transparent"
            "RenderType"="Transparent"
			"RenderPipeline" = "UniversalPipeline"
        }

        Pass
		{
            Name "ForwardLit"

            Blend[_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha] //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin
            ZWrite[_ZWrite]
            Cull [_Cull]
            ColorMask [_ColorMask]

            Stencil
		    {
			    Ref [_StencilRef]
			    Comp [_StencilComp]
			    Pass [_StencilPass]
                Fail [_StencilFail]
		    }

			Tags
            {
                "LightMode" = "UniversalForward"
            }
		    
            HLSLPROGRAM
            
            // Required to compile gles 2.0 with standard SRP library
			// All shaders must be compiled with HLSLcc and currently only gles is not using HLSLcc by default
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAMAP_ON
            #pragma shader_feature_local _ENABLE_LIGHT_AFFECT
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _MATCAP
            #pragma shader_feature_local _MATCAP_FIX_EDGE_FLAW
            #pragma shader_feature_local _MATCAP_HIGHLIGHTS
            #pragma shader_feature_local _CARTOON
            #pragma shader_feature_local _CUSTOM_CARTOON_LIGHT
            #pragma shader_feature_local _RIM_LIGHT
            #pragma shader_feature_local _RIM_TRANSPARENCY
            #pragma shader_feature_local _RIM_REFRACTIONY
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local _EMISSION_CUBEMAP
            #pragma shader_feature_local _EMISSION_CUBEMAP_LOD
            #pragma shader_feature_local _BOX_PROJECTION_CUBEMAP
            #pragma shader_feature_local _SWEEP_LIGHT

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ CUSTOM_FOG
            #pragma shader_feature _ CUSTOM_FOG_FRAGMENT
            #pragma shader_feature_local_fragment _GLOBAL_CLOUD_SHADOW
            #pragma multi_compile _ _GLOBAL_RAIN_SURFACE _GLOBAL_SNOW_SURFACE

            #pragma vertex MatcapVert
            #pragma fragment MatcapFrag
            
            #include "MatCapInput.hlsl"
            #include "MatCapForwardPass.hlsl"
            
            ENDHLSL
        }

        //ShadowCaster可能会被剔除,UsePass真机丢失Shader
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            
            #include "MatCapInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            ENDHLSL
        }

        UsePass "PicoVideo/Scene/MatCap/DepthOnly"
        UsePass "PicoVideo/Scene/MatCap/DepthNormals"
        UsePass "PicoVideo/Scene/MatCap/Meta"
	}

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Matrix.CommonShader.MatCapShaderGUI"
}