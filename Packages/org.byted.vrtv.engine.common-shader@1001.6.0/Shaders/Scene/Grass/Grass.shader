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

Shader "PicoVideo/Scene/Grass"
{
    Properties
    {
	    [Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
	    [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.SurfaceType,1,_Blend)]_Surface("Surface Type", Float) = 0
        [HideInInspector][ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.BlendMode)] _Blend("Blending Mode", Float) = 0.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.RenderFace)]_Cull("Render Face", Float) = 2
        [ExtendToggle(_SurfaceOptions, _ALPHATEST_ON, _Cutoff)] _AlphaClip("Alpha Clipping", Float) = 1.0
	    [HideInInspector]_Cutoff("Threshold", Range(0.0, 1.0)) = 0.5
	    [SubToggle(_SurfaceOptions)] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.LightingMode,1,_BlingPhongSpecColor)]_LightingMode("Lighting Mode", Float) = 0
        [HideInInspector][Sub(_SurfaceOptions)] [HDR]_BlingPhongSpecColor("SpecColor", Color) = (1,1,1,1)
        [ExtendToggle(_SurfaceOptions, _CUSTOM_LIGHT, _CustomSpecularIntensity)]_EnableCustomLight("Enable Custom Light", Float) = 0
        [HideInInspector]_CustomSpecularIntensity("Custom Specular Intensity", Range(0,1)) = 1
    	
	    [Main(_SurfaceInputs, _, on, off)]_SurfaceInputs("Surface Inputs", Float) = 0
        [Sub(_SurfaceInputs)][HDR][MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [HideInInspector] _ApplyVertexColor("Apply Vertex Color", Float) = 0
	    [Sub(_SurfaceInputs)][MainTexture] _BaseMap("Base Color(RGB),A(Alpha)", 2D) = "white" {}
	    [Sub(_SurfaceInputs)]_BaseMapBias("BaseMap Bias", Range(-1.2, 0.5)) = 0
        [SubToggle(_SurfaceInputs)] _BackfaceNormalFlip("Backface Normal Flip", Float) = 1.0
        [SubToggle(_SurfaceInputs)] _NormalAlwaysUp("Normal Always Up", Float) = 0.0
	    [Sub(_SurfaceInputs)]_Metallic("Metallic Scale", Range(0.0, 1.0)) = 0.0
	    [Sub(_SurfaceInputs)]_Smoothness("Smoothness Scale", Range(0.0, 1.0)) = 0.5
    	[Sub(_SurfaceInputs)]_OcclusionStrength("Occlusion Map", Range(0.0, 1.0)) = 1.0

        [Main(_EmissionOptions, _EMISSION)]_EmissionOptions("Emission Options", Float) = 0
        [Sub(_EmissionOptions)]_EmissionMap("Emission", 2D) = "white" {}
		[Sub(_EmissionOptions)][HDR] _EmissionColor("Color", Color) = (0,0,0)
        [Sub(_EmissionOptions)]_EmissionIntensity("Emission Intensity", Range(0.01, 10)) = 1

        [Main(_GrassInputs, _, on, off)]_GrassInputs("Grass Inputs", Float) = 0
        [MinMaxSlider(_GrassInputs, _SpecularMinV, _SpecularMaxV)] _SpecularMinMaxV("Specular Min Max V", Range(0.0, 1.0)) = 1.0
        [HideInInspector]_SpecularMinV("Specular Min V", Range(0, 1)) = 0.0
        [HideInInspector]_SpecularMaxV("Specular Max V", Range(0, 1)) = 1.0
        [SubToggle(_GrassInputs, _VERTEX_WIND)] _VertexWind("Vertex Wind", Float) = 0.0
        [Sub(_GrassInputs_VERTEX_WIND)]_WindMultiplier("BaseWind Multiplier", Float) = 0
        [SubToggle(_GrassInputs, _GRASS_COLOR)] _GrassColor("Grass Color", Float) = 0.0
    	[Sub(_GrassInputs_GRASS_COLOR)][HDR]_GrassHeightColor("Height Color", Color) = (1,1,1,1) 
        [Sub(_GrassInputs_GRASS_COLOR)]_GrassHeightFalloff("Height Falloff", Range(-1, 1)) = 1
		[Sub(_GrassInputs_GRASS_COLOR)]_GrassColorNoise("Noise", 2D) = "white" {}
        [Sub(_GrassInputs_GRASS_COLOR)][HDR]_GrassGradientColor("Gradient Color", Color) = (1,1,1,1)
        [Sub(_GrassInputs_GRASS_COLOR)]_GrassColorVariationPower("Color Variation Power", Range( 0 , 1)) = 0
        [SubToggle(_GrassInputs, _CAPTURE_GROUND_COLOR)] _CaptureGroundColor("Capture Ground Color", Float) = 1.0
    	
        [Main(_Advanced, _, on, off)]_Advanced("Advanced", Float) = 0
	    [SubToggle(_Advanced, _)] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [SubToggle(_Advanced, _)] _EnvironmentReflections("Environment Reflections", Float) = 1.0
	    [KWEnum(_Advanced, None, _, Rain, _GLOBAL_RAIN_SURFACE, Snow, _GLOBAL_SNOW_SURFACE)]_WeatherMode("Weather Mode", float) = 0
    	[SubToggle(_Advanced, _DISABLE_RENDER_HEIGHT)]_DisableHeightmapRenderer("Disable Heightmap Renderer", float) = 0
	    [SubToggle(_Advanced, CUSTOM_FOG_FRAGMENT)]_CustomFogFragment("Custom Fog Use Fragment", float) = 0
        [SubToggle(_Advanced, _GLOBAL_CLOUD_SHADOW)]_GlobalCloudShadow("GlobalCloudShadow", float) = 0
        [Sub(_Advanced_GLOBAL_CLOUD_SHADOW)]_CloudShadowIntensity("Global Cloud Shadow Intensity", Range(0, 1)) = 1
	    [Sub(_Advanced)]_AdjustColorIntensity("Global Adjust Color Intensity", Range(0, 1)) = 1
	    [SubIntRange(_Advanced)] _QueueOffset("Priority", Range(-50, 50)) = 0
	    
        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
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

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 4.5
            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _ _CUSTOM_LIGHT
            #pragma shader_feature_local _ _PBR _BLING_PHONG
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_vertex _VERTEX_WIND
            #pragma shader_feature_local _GRASS_COLOR
            #pragma shader_feature_local_fragment _CAPTURE_GROUND_COLOR
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            //#pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            //#pragma multi_compile _ /*_ADDITIONAL_LIGHTS_VERTEX*/ _ADDITIONAL_LIGHTS
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
            #pragma shader_feature_local_fragment _GLOBAL_CLOUD_SHADOW
            #pragma shader_feature_local _ _GLOBAL_RAIN_SURFACE _GLOBAL_SNOW_SURFACE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "GrassInput.hlsl"
            #include "GrassForwardPass.hlsl"
            
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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "GrassInput.hlsl"
            #include "GrassShadowCasterPass.hlsl"
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

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            //#pragma multi_compile _ DOTS_INSTANCING_ON

            #include "GrassInput.hlsl"
            #include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/PBR/PBRDepthOnlyPass.hlsl"
            
            ENDHLSL
        }

        //PicoVideo;AppSW;Ernst;Begin
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
        //PicoVideo;AppSW;Ernst;End

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

            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF

            #include "GrassInput.hlsl"
            #include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/PBR/PBRMetaPass.hlsl"

            ENDHLSL
        }
    	
        UsePass "PicoVideo/EcosystemSimulate/HeightMapSimulate/HeightMap"
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Matrix.EcosystemSimulate.VegetationShaderGUI"
}