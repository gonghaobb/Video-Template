Shader "PicoURP_PBR(Weather)"
{
    Properties
    {
        [Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
    	[KWEnum(_SurfaceOptions, BlinnPhong, USE_BLINN_PHONG, PBR, _)]_LightingModel("Lighting Model ", float) = 1.0
	    [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.SurfaceType,1,_Blend)]_Surface("Surface Type", Float) = 0
        [HideInInspector][ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.BlendMode)] _Blend("Blending Mode", Float) = 0.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.RenderFace)]_Cull("Render Face", Float) = 2
        [SubToggle(_SurfaceOptions, _ALPHATEST_ON)] _AlphaClip("Alpha Clipping", Float) = 0.0
	    [Sub(_SurfaceOptions_ALPHATEST_ON)]_Cutoff("Threshold", Range(0.0, 1.0)) = 0.5
	    [SubToggle(_SurfaceOptions, _)]_ReceiveShadows("Receive Shadows", Float) = 1.0
	    
    	[Main(_SurfaceInputs, _, on, off)]_SurfaceInputs("Surface Inputs", Float) = 0
	    [Title(_SurfaceInputs, Base)]
	    [Sub(_SurfaceInputs)][HDR][MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
	    [SubToggle(_SurfaceInputs)] _ApplyVertexColor("Apply Vertex Color", float) = 0
	    [Sub(_SurfaceInputs)][MainTexture] _BaseMap("Base Color(RGB),AO(A)", 2D) = "white" {}
    	[Sub(_SurfaceInputs)]_BaseMapBias("BaseMap Bias", Range(-1.2, 0.5)) = 0
    	[Sub(_SurfaceInputs)]_OcclusionStrength("Occlusion Map", Range(0.0, 1.0)) = 1.0
        [ExtendSub(_SurfaceInputs, _ALPHAMAP_ON)]_AlphaMap("Alpha Map" , 2D) = "white" {}
    	[Title(_SurfaceInputs, NMR)]
	    [ExtendSub(_SurfaceInputs, _NORMALMAP)]_BumpMap("Normal(RG),Metallic(B),Smoothness(A)", 2D) = "bump" {}
    	[Sub(_SurfaceInputs)]_BumpMapBias("NormalMap Bias", Range(-1.2, 1)) = 0
	    [Sub(_SurfaceInputs)]_BumpScale("Normal Scale", Float) = 1.0
	    [Sub(_SurfaceInputs)]_Metallic("Metallic Scale", Range(0.0, 1.0)) = 0.0
	    [Sub(_SurfaceInputs)]_Smoothness("Smoothness Scale", Range(0.0, 1.0)) = 0.5
	    [Title(_SurfaceInputs, Fresnel)]
	    [Sub(_SurfaceInputs)]_FresnelBias("Fresnel Bias", float) = 0
        [Sub(_SurfaceInputs)]_FresnelScale("Fresnel Scale", float) = 1
        [Sub(_SurfaceInputs)]_FresnelPower("Fresnel Power", float) = 4
    	
    	[Main(_Emission, _EMISSION)]_Emission("Emission", Float) = 0
		[Sub(_Emission)][HDR] _EmissionColor("Color", Color) = (0,0,0)
        [Sub(_Emission)]_EmissionMap("Emission", 2D) = "white" {}
	    [ExtendTex(_Emission, _, _EMISSION_CUBEMAP)]_EmssionCubemap("Emssion Cubemap", Cube) = "" {}
        [Sub(_Emission)]_EmissionCubemapIntensity("Emission Cubemap Intensity", Range(0.01, 10)) = 1
        [Sub(_Emission)]_EmissionCubemapLod("Emission Cubemap Lod", Range(0, 10)) = 0

    	[Main(_DetailInputs, _, on, off)]_DetailInputs("Detail Inputs", Float) = 0
        [Sub(_DetailInputs)]_DetailMask("Mask", 2D) = "white" {}
        [ExtendSub(_DetailInputs)]_DetailAlbedoMap("Detail Albedo x2", 2D) = "linearGrey" {}
        [Sub(_DetailInputs)]_DetailAlbedoMapScale("Scale", Range(0.0, 2.0)) = 1.0
        [Sub(_DetailInputs)][Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}
        [Sub(_DetailInputs)]_DetailNormalMapScale("Scale", Range(0.0, 2.0)) = 1.0
        
		[Main(_LayerBlend, _ENABLE_LAYER_BLEND)]_EnableLayerBlend("Layer Blend", Float) = 0
        [Sub(_LayerBlend)]_LayerBlendGroup("Layer Blends", Float) = 0
        [Sub(_LayerBlend)]_LayerBlendStrength("Blend Strength", Range(0.0, 1.0)) = 0.5
        [Sub(_LayerBlend)]_LayerBlendSmoothness("Blend Smoothness", Range(0.01, 1)) = 1
        [Sub(_LayerBlend)]_LayerBaseMap("Base Map (RGB), AO(A)", 2D) = "white" {}
        [Sub(_LayerBlend)][HDR]_LayerBaseColor("Base Color", Color) = (1,1,1,1)
        [Sub(_LayerBlend)]_LayerEnableAlphaMap("Enable Alpha Map", float) = 0
        [ExtendSub(_LayerBlend, _LAYER_ALPHAMAP_ON)]_LayerAlphaMap("Alpha Map" , 2D) = "white" {}
        [ExtendSub(_LayerBlend, _LAYER_NORMALMAP)]_LayerBumpMap("Bump Map(RG), Metallic(B), Smoothness(A)" , 2D) = "white" {}
        [Sub(_LayerBlend)]_LayerBumpScale("Bump Scale", Float) = 1.0
        [Sub(_LayerBlend)]_LayerMetallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Sub(_LayerBlend)]_LayerSmoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Sub(_LayerBlend)]_LayerOcclusionStrength("Occlusion", Range(0.0, 1.0)) = 1.0
    	
        [Main(_AdvancedOptions, _, on, off)]_Advanced("Advanced Options", Float) = 0
	    [SubToggle(_AdvancedOptions, _)] _SpecularHighlights("Specular Highlights", Float) = 1.0
	    [SubToggle(_AdvancedOptions, _CUSTOM_SPEC_LIGHT_DIRECTION)]_EnableCustomSpecular("Enable Custom Specular", Float) = 0
        [Sub(_AdvancedOptions_CUSTOM_SPEC_LIGHT_DIRECTION)]_CustomSpecularIntensity("Custom Specular Intensity", Range(0,1)) = 1
	    [HideInInspector][Sub(_AdvancedOptions)]_LightmapSpecular("LightmapSpecular", Range(0.0, 1.0)) = 1.0
	    [SubToggle(_AdvancedOptions, _)] _EnvironmentReflections("Environment Reflections", Float) = 1.0
        [SubToggle(_AdvancedOptions, _REFLECTION_PROBE_BOX_PROJECTION)]_EnableBoxProjection("Enable Box Projection", float) = 0
	    [KWEnum(_AdvancedOptions, None, _, Rain, _GLOBAL_RAIN_SURFACE, Snow, _GLOBAL_SNOW_SURFACE)]_WeatherMode("Weather Mode", float) = 0
    	[SubToggle(_AdvancedOptions, _DISABLE_RENDER_HEIGHT)]_DisableHeightmapRenderer("Disable Heightmap Renderer", float) = 0
	    [SubToggle(_AdvancedOptions, CUSTOM_FOG_FRAGMENT)]_CustomFogFragment("Custom Fog Use Fragment", float) = 0
        [SubToggle(_AdvancedOptions, _GLOBAL_CLOUD_SHADOW)]_GlobalCloudShadow("GlobalCloudShadow", float) = 0
        [Sub(_AdvancedOptions_GLOBAL_CLOUD_SHADOW)]_CloudShadowIntensity("Global Cloud Shadow Intensity", Range(0, 1)) = 1
	    [Sub(_AdvancedOptions)]_AdjustColorIntensity("Global Adjust Color Intensity", Range(0, 1)) = 1
	    [SubIntRange(_AdvancedOptions)] _QueueOffset("Priority", Range(-50, 50)) = 0

       // ObsoleteProperties
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
            #pragma shader_feature_local_fragment _EMISSION_CUBEMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local_fragment _OCCLUSIONMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            //#pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _ALPHAMAP_ON

            // #pragma multi_compile _ _MATRIX_SPC_LIGHTINGMAP
            #define _MATRIX_MIX_NORMAL_MR
            #pragma shader_feature_local_fragment _ _CUSTOM_SPEC_LIGHT_DIRECTION

			#pragma shader_feature_local_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            
            #pragma shader_feature_local _ USE_BLINN_PHONG
			#if !defined(_SPECULARHIGHLIGHTS_OFF) && defined(USE_BLINN_PHONG)
			#define _SPECULAR_COLOR
			#endif
            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ /*_ADDITIONAL_LIGHTS_VERTEX*/ _ADDITIONAL_LIGHTS
            //#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT
            //#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            //#pragma multi_compile _ SHADOWS_SHADOWMASK

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            
            #pragma multi_compile_fog
            #pragma multi_compile _ CUSTOM_FOG
            #pragma shader_feature _ CUSTOM_FOG_FRAGMENT
            #pragma multi_compile _ _GLOBAL_RAIN_SURFACE _GLOBAL_SNOW_SURFACE

            #ifdef CUSTOM_FOG
			#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/FogSimulate/Resource/Shaders/Fog.hlsl"
            #endif

            #pragma shader_feature_local_fragment _ENABLE_LAYER_BLEND
            #pragma shader_feature_local_fragment _LAYER_ALPHAMAP_ON
            #pragma shader_feature_local _LAYER_NORMALMAP

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "PBRInput.hlsl"
            #include "PBRForwardPass.hlsl"
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
            #pragma target 4.5
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _ALPHAMAP_ON
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "PBRInput.hlsl"
            #include "PBRShadowCasterPass.hlsl"
            ENDHLSL
        }
        
    	UsePass "PicoURP_PBR/DepthOnly"
        UsePass "PicoURP_PBR/DepthNormals"
    	UsePass "PicoURP_PBR/MotionVectors"
    	UsePass "PicoURP_PBR/Meta"
        UsePass "PicoVideo/EcosystemSimulate/HeightMapSimulate/HeightMap"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Matrix.CommonShader.PBRShaderGUI"
}
