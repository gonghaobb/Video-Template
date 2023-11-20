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

Shader "PicoVideo/Scene/CartoonScene"
{
    Properties
    {
	    [Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
	    [KWEnum(_SurfaceOptions, BlinnPhong, USE_BLINN_PHONG, PBR, _)]_LightingModel("Lighting Model ", float) = 1.0
	    [Sub(_SurfaceOptionsUSE_BLINN_PHONG)][HDR]_BlingPhongSpecColor("SpecColor", Color) = (1,1,1,1)
	    [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.SurfaceType,1,_Blend)]_Surface("Surface Type", Float) = 0
        [HideInInspector][ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.BlendMode)] _Blend("Blending Mode", Float) = 0.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.RenderFace)]_Cull("Render Face", Float) = 2
        [SubToggle(_SurfaceOptions, _ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0.0
	    [Sub(_SurfaceOptions_ALPHATEST_ON)]_Cutoff("Threshold", Range(0.0, 1.0)) = 0.5
	    [SubToggle(_SurfaceOptions)]_ReceiveShadows("Receive Shadows", Float) = 1.0
	    
    	[Main(_SurfaceInputs, _, on, off)]_SurfaceInputs("Surface Inputs", Float) = 0
    	[Title(_SurfaceInputs, Base)]
        [Sub(_SurfaceInputs)][HDR][MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
	    [SubToggle(_SurfaceInputs)] _ApplyVertexColor("Apply Vertex Color", float) = 0
	    [Sub(_SurfaceInputs)][MainTexture] _BaseMap("Base Color(RGB),AO(A)", 2D) = "white" {}
    	[Sub(_SurfaceInputs)]_BaseMapBias("BaseMap Bias", Range(-1.2, 0.5)) = 0
        [ExtendSub(_SurfaceInputs, _ALPHAMAP_ON)]_AlphaMap("Alpha Map" , 2D) = "white" {}
    	[Title(_SurfaceInputs, NMR)]
	    [ExtendSub(_SurfaceInputs, _NORMALMAP)]_NormalMap("Normal(RG),Metallic(B),Smoothness(A)", 2D) = "bump" {}
    	[Sub(_SurfaceInputs)]_BumpMapBias("NormalMap Bias", Range(-1.2, 1)) = 0
	    [Sub(_SurfaceInputs)]_BumpScale("Normal Scale", Float) = 1.0
	    [Sub(_SurfaceInputs)]_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
	    [Sub(_SurfaceInputs)]_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
	    [Sub(_SurfaceInputs)]_OcclusionStrength("Occlusion", Range(0.0, 1.0)) = 1.0

	    [Main(_LayerShadows, _, on, off)]_LayerShadows("Cartoon Shadows", Float) = 0
        [SubToggle(_LayerShadows, _CUSTOM_LIGHT)]_EnableCustomLight("Enable Custom Light", Float) = 0
        [Sub(_LayerShadows_CUSTOM_LIGHT)]_CustomSpecularIntensity("Custom Specular Intensity", Range(0,1)) = 1
        [SubToggle(_LayerShadows)]_ShadowBoundaryFirst("FirstShadowBoundary", Float) = 0
	    [Sub(_LayerShadows)][Gamma][HDR]_ShadowColorFirst("FirstShadowColor", Color) = (1,1,1,1)
        [Sub(_LayerShadows)]_ShadowAreaFirst("FirstShadowArea", Range(0.0, 1.0)) = 0.5
        [Sub(_LayerShadows)]_ShadowSmoothFirst("FirstShadowSmooth", Range(0.0, 1.0)) = 0.05
        [SubToggle(_LayerShadows)]_ShadowBoundarySecond("SecondShadowBoundary", Float) = 0
        [Sub(_LayerShadows)][Gamma][HDR]_ShadowColorSecond("SecondShadowColor", Color) = (1,1,1,1)
        [Sub(_LayerShadows)]_ShadowAreaSecond("SecondShadowArea", Range(0.0, 1.0)) = 0.5
        [Sub(_LayerShadows)]_ShadowSmoothSecond("SecondShadowSmooth", Range(0.0, 1.0)) = 0.05
        [SubToggle(_LayerShadows)]_ShadowBoundaryThird("ThirdShadowBoundary", Float) = 0
        [Sub(_LayerShadows)][Gamma][HDR]_ShadowColorThird("ThirdShadowColor", Color) = (1,1,1,1)
        [Sub(_LayerShadows)]_ShadowAreaThird("ThirdShadowArea", Range(0.0, 1.0)) = 0.35
        [Sub(_LayerShadows)]_ShadowSmoothThird("ThirdShadowSmooth", Range(0.0, 1.0)) = 0.05
    	
	    [Main(_EmissionOptions, _EMISSION)]_EmissionOptions("Emission Options", Float) = 0
        [Sub(_EmissionOptions)]_EmissionMap("Emission Map", 2D) = "white" {}
		[Sub(_EmissionOptions)][HDR] _EmissionColor("Emission Color", Color) = (0,0,0)
        [Sub(_EmissionOptions)]_EmissionIntensity("Emission Intensity", Range(0.01, 10)) = 1
        [Title(_EmissionOptions, Cubemap)]
        [ExtendTex(_EmissionOptions, _EmissionCubemapColor, _EMISSION_CUBEMAP)]_EmssionCubemap("Emssion Cubemap", Cube) = "" {}
        [HideInInspector][HDR]_EmissionCubemapColor("EmissionCubemapColor", Color) = (1,1,1,1)
        [Sub(_EmissionOptions)]_EmissionCubemapIntensity("Emission Cubemap Intensity", Range(0.01, 10)) = 1
        [Sub(_EmissionOptions)]_EmissionCubemapLod("Emission Cubemap Lod", Range(0, 10)) = 0
    	
	    [Main(_AdvancedOptions, _, on, off)]_Advanced("Advanced Options", Float) = 0
	    [SubToggle(_AdvancedOptions)] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [SubToggle(_AdvancedOptions)] _EnvironmentReflections("Environment Reflections", Float) = 1.0
	    [SubToggle(_AdvancedOptions, _REFLECTION_PROBE_BOX_PROJECTION)]_EnableBoxProjection("Enable Box Projection", float) = 0
	    [KWEnum(_AdvancedOptions, None, _, Rain, _GLOBAL_RAIN_SURFACE, Snow, _GLOBAL_SNOW_SURFACE)]_WeatherMode("Weather Mode", float) = 0
	    [SubToggle(_AdvancedOptions, CUSTOM_FOG_FRAGMENT)]_CustomFogFragment("Custom Fog Use Fragment", float) = 0
        [SubToggle(_AdvancedOptions, _GLOBAL_CLOUD_SHADOW)]_GlobalCloudShadow("GlobalCloudShadow", float) = 0
        [Sub(_AdvancedOptions_GLOBAL_CLOUD_SHADOW)]_CloudShadowIntensity("Global Cloud Shadow Intensity", Range(0, 1)) = 1
	    [Sub(_AdvancedOptions)]_AdjustColorIntensity("Global Adjust Color Intensity", Range(0, 1)) = 1
	    [SubIntRange(_AdvancedOptions)] _QueueOffset("Priority", Range(-50, 50)) = 0

        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
    	//PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;Start
		[HideInInspector] _SrcBlendAlpha("__srcAlpha", Float) = 1.0
		[HideInInspector] _DstBlendAlpha("__dstAlpha", Float) = 0.0
    	//PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;End
		[HideInInspector] _ZWrite("__zw", Float) = 1.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True"}

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha] //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local _EMISSION_CUBEMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _ALPHAMAP_ON

            #pragma shader_feature_local_fragment _ _CUSTOM_LIGHT
			#pragma shader_feature_local_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            // #pragma multi_compile _ /*_ADDITIONAL_LIGHTS_VERTEX*/ _ADDITIONAL_LIGHTS
            //#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT
            //#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            //#pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma shader_feature_local _ USE_BLINN_PHONG

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON

            #pragma shader_feature_local _ _GLOBAL_RAIN_SURFACE _GLOBAL_SNOW_SURFACE
            
            #pragma multi_compile_fog
            #pragma multi_compile _ CUSTOM_FOG
            #pragma shader_feature _ CUSTOM_FOG_FRAGMENT
            #ifdef CUSTOM_FOG
			#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/FogSimulate/Resource/Shaders/Fog.hlsl"
            #endif
            
            #pragma shader_feature_local_fragment _GLOBAL_CLOUD_SHADOW

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "CartoonInput.hlsl"
            #include "CartoonForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _ _ALPHAMAP_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "CartoonInput.hlsl"
            #include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/PBR/PBRShadowCasterPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _ _ALPHAMAP_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #include "CartoonInput.hlsl"
            #include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/PBR/PBRDepthOnlyPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "MotionVectors"
            Tags{ "LightMode" = "MotionVectors"}
            Tags { "RenderType" = "Opaque" }

            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5

            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MotionVectorCore.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMeta

            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED

            #pragma shader_feature_local_fragment _SPECGLOSSMAP

            #include "CartoonInput.hlsl"
            #include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/PBR/PBRMetaPass.hlsl"

            ENDHLSL
        }
    	
        UsePass "PicoVideo/EcosystemSimulate/HeightMapSimulate/HeightMap"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
//    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.SLitShader"
    CustomEditor "Matrix.CommonShader.CartoonSceneShaderGUI"
}
