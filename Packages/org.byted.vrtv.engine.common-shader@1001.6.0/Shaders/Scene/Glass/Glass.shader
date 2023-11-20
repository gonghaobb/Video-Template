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

Shader "PicoVideo/Scene/Glass"
{
    Properties
    {
        [Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.SurfaceType,1,_Blend)]_Surface("Surface Type", Float) = 0
        [HideInInspector][ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.BlendMode)] _Blend("Blending Mode", Float) = 0.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.RenderFace)]_Cull("Render Face", Float) = 2
        [ExtendToggle(_SurfaceOptions, _ALPHATEST_ON, _Cutoff)] _AlphaClip("Alpha Clipping", Float) = 0.0
		[HideInInspector]_Cutoff("Threshold", Range(0.0, 1.0)) = 0.5
        [SubToggle(_SurfaceOptions)] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.LightingMode,1,_BlingPhongSpecColor)]_LightingMode("Lighting Mode", Float) = 0
        [HideInInspector][Sub(_SurfaceOptions)] [HDR]_BlingPhongSpecColor("SpecColor", Color) = (0,0,0,1)
        [Title(_SurfaceOptions, Glass Fresnel)]
        [Sub(_SurfaceOptions)]_GlassFresnelIntensity("Glass Fresnel Intensity", Range(0, 10)) = 1
        [MinMaxSlider(_SurfaceOptions, _GlassMinFresnel, _GlassMaxFresnel)] _GlassMinMaxFresnelSlider("Glass Min Max Fresnel", Range(0.0, 10.0)) = 1.0
        [HideInInspector]_GlassMinFresnel("Glass Min Fresnel", Range(0.0, 1)) = 0.0
        [HideInInspector]_GlassMaxFresnel("Glass Max Fresnel", Range(1, 10.0)) = 1.0

        [Main(_SurfaceInputs, _, on, off)]_SurfaceInputs("Surface Inputs", Float) = 0
		[Sub(_SurfaceInputs)][MainTexture] _BaseMap("Base Map (RGB), AO(A)", 2D) = "white" {}
		[Sub(_SurfaceInputs)][HDR][MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [SubToggle(_SurfaceInputs)] _ApplyVertexColor("Apply Vertex Color", Float) = 0
        [Sub(_SurfaceInputs)]_AlphaMap("Alpha Map" , 2D) = "white" {}
        [Sub(_SurfaceInputs)]_NormalMap("Normal Map (RG), Metallic(B), Smoothness(A)", 2D) = "white" {}
        [Sub(_SurfaceInputs)]_NormalScale("Normal Scale", Float) = 1.0
        [Sub(_SurfaceInputs)]_Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Sub(_SurfaceInputs)]_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Sub(_SurfaceInputs)]_OcclusionStrength("Occlusion", Range(0.0, 1.0)) = 1.0

        [Main(_InteriorOptions, _, on, off)]_InteriorOptions("Interior Options", Float) = 0
        [Sub(_InteriorOptions)]_InteriorCubemap("Interior Cubemap", Cube) = "white" {}
        [Sub(_InteriorOptions)]_InteriorDepthScale("Interior Depth Scale", Range(0.0001, 0.9999)) = 0.5
        [Sub(_InteriorOptions)]_InteriorIntensity("Interior Intensity", Range(0.01, 3)) = 1
        [SubToggle(_InteriorOptions, _INTERIOR_TANGENT)] _InteriorTangentSpace("Interior Tangent Space", Float) = 1.0

        [Main(_EnvironmentOptions,  _, on, off)]_EnvironmentOptions("Environment Options", Float) = 0
		[Tex(_EnvironmentOptions)]_EnvironmentCubeMap("Environment CubeMap", Cube) = "white" {}
        [Sub(_EnvironmentOptions)]_EnvironmentCubemapLod("Environment Cubemap Lod", Range(0, 10)) = 0
        [Sub(_EnvironmentOptions)][HDR]_EnvironmentReflectionColor("Environment Reflection Color", Color) = (1,1,1,1)
        [Sub(_EnvironmentOptions)]_EnvironmentReflectionIntensity("Environment Reflection Intensity", Range(0.01, 3)) = 1
        [Sub(_EnvironmentOptions)][HDR]_EnvironmentRefractionColor("Environment Refraction Color", Color) = (1,1,1,1)
        [Sub(_EnvironmentOptions)]_EnvironmentRefractionIntensity("Environment Refraction Intensity", Range(0.01, 3)) = 1

        [Main(_FrostOptions, _FROST)]_FrostOptions("Frost Options", Float) = 0
        [Sub(_FrostOptions)][HDR]_FrostColor("Frost Color", Color) = (1,1,1,1)
        [Sub(_FrostOptions)]_FrostCenter("Frost Center", Vector) = (0.5,0.5,0,0)
        [Sub(_FrostOptions)]_FrostDistance("Frost Distance", Range(0.0, 1)) = 0
        [SubToggle(_FrostOptions)]_FrostReverse("Frost Reverse", Float) = 0
        [Sub(_FrostOptions)]_FrostMaskMap("Frost Mask Map", 2D) = "white" {}
        [Sub(_FrostOptions)]_FrostBlendFactor("Frost Blend Factor (Color And Mask)", Range(0.0, 1)) = 0
        [Sub(_FrostOptions)]_FrostIntensity("Frost Intensity", Range(0.01, 5)) = 1
        [Title(_FrostOptions, Frost Noise)]
        [Sub(_FrostOptions)]_FrostNoiseMap("Frost Noise Map", 2D) = "white" {}
        [Sub(_FrostOptions)]_FrostNoiseDistance("Frost Noise Distance", Range(0.0, 1)) = 1
        [Sub(_FrostOptions)]_FrostNoiseIntensity("Frost Noise Intensity", Range(0.0, 1)) = 1
        
        [Main(_EmissionOptions, _EMISSION)]_EmissionOptions("Emission Options", Float) = 0
		[Sub(_EmissionOptions)]_EmissionMap("Emission Map", 2D) = "white" {}
        [Sub(_EmissionOptions)][HDR]_EmissionColor("EmissionColor", Color) = (0,0,0,1)
        [Sub(_EmissionOptions)]_EmissionIntensity("Emission Intensity", Range(0.01, 10)) = 1

        [Main(_AdvancedOptions, _, on, off)]_AdvancedOptions("Advanced Options", Float) = 0
        [SubToggle(_AdvancedOptions)] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [SubToggle(_AdvancedOptions, _CUSTOM_SPEC_LIGHT_DIRECTION)]_EnableCustomSpecular("Enable Custom Specular", Float) = 0
        [Sub(_AdvancedOptions_CUSTOM_SPEC_LIGHT_DIRECTION)]_CustomSpecularIntensity("Custom Specular Intensity", Range(0,1)) = 1
        [SubToggle(_AdvancedOptions)] _EnvironmentReflections("Environment Reflections", Float) = 1.0
        [SubToggle(_AdvancedOptions, _REFLECTION_PROBE_BOX_PROJECTION)] _EnableBoxProjection("Enable Box Projection", float) = 0
        [KWEnum(_AdvancedOptions, None, _, Rain, _GLOBAL_RAIN_SURFACE, Snow, _GLOBAL_SNOW_SURFACE)] _WeatherType("WeatherType", float) = 0
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
		[HideInInspector] _ZWrite("__zw", Float) = 1.0
    }

    SubShader
	{
        Tags 
		{
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        Pass
		{
            Name "ForwardGlass"

            Blend[_SrcBlend] [_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha] //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin
            ZWrite[_ZWrite]
            Cull [_Cull]

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local _ _PBR _BLING_PHONG
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _ALPHAMAP_ON
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _INTERIOR_CUBEMAP
            #pragma shader_feature_local _INTERIOR_TANGENT
            #pragma shader_feature_local_fragment _ENVIRONMENT_CUBEMAP
            #pragma shader_feature_local_fragment _FROST
            #pragma shader_feature_local_fragment _FROST_NOISE
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _ _CUSTOM_SPEC_LIGHT_DIRECTION
			#pragma shader_feature_local_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS// _MAIN_LIGHT_SHADOWS_CASCADE
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
            #pragma shader_feature_local _ _GLOBAL_RAIN_SURFACE _GLOBAL_SNOW_SURFACE

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "GlassInput.hlsl"
            #include "GlassForwardPass.hlsl"

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
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "GlassInput.hlsl"
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
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "GlassInput.hlsl"
            #include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/PBR/PBRDepthOnlyPass.hlsl"

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

            #include "GlassInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv0          : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                float2 uv2          : TEXCOORD2;
                half4 color         : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                half4 color         : COLOR;
            };

            Varyings UniversalVertexMeta(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
                output.uv = input.uv0;
                output.color = 1 - _ApplyVertexColor + input.color * _ApplyVertexColor;
                return output;
            }

            half4 UniversalFragmentMeta(Varyings input) : SV_Target
            {
                SurfaceData surfaceData;
                InitializeGlassSurfaceData(input.uv, input.color, surfaceData);

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
    CustomEditor "Matrix.CommonShader.GlassShaderGUI"
}