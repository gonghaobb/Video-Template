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

Shader "PicoVideo/Scene/BillboardCloud"
{
    Properties 
	{
		[Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.SurfaceType,1,_Blend)]_Surface("Surface Type", Float) = 0
        [HideInInspector][ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.BlendMode)] _Blend("Blending Mode", Float) = 0.0
        [ExtendEnum(_SurfaceOptions, Matrix.ShaderGUI.RenderFace)]_Cull("Render Face", Float) = 2
        [ExtendToggle(_SurfaceOptions, _BLING_PHONG, _BlingPhongSpecColor)]_EnableBlingPhongLight("BlingPhong LightModel", Float) = 0
        [HideInInspector][Sub(_SurfaceOptions)] [HDR]_BlingPhongSpecColor("SpecColor", Color) = (0,0,0,1)
        [ExtendToggle(_SurfaceOptions, _CUSTOM_LIGHT, _CustomSpecularIntensity)]_EnableCustomLight("Enable Custom Light", Float) = 0
        [HideInInspector]_CustomSpecularIntensity("Custom Specular Intensity", Range(0,1)) = 1
        [SubToggle(_SurfaceOptions, _BILLBOARD)]_EnableBillboard("Enable Billboard", float) = 0
        [ExtendToggle(_SurfaceOptions, _DEPTH_FADE, _DepthFade)]_EnableDepthFade("Enable Depth Fade", float) = 0
        [HideInInspector]_DepthFade("DepthFade", Range(0.0001, 20)) = 5
        
        [Title(_SurfaceOptions, Stencil)]
		[Sub(_SurfaceOptions)]_StencilRef("StencilRef", Range(0,255)) = 127
		[ExtendEnum(_SurfaceOptions, UnityEngine.Rendering.CompareFunction)]_StencilComp("StencilComp", int) = 8
        [ExtendEnum(_SurfaceOptions, UnityEngine.Rendering.StencilOp)]_StencilPass("Stencil Pass", Float) = 0
        [ExtendEnum(_SurfaceOptions, UnityEngine.Rendering.StencilOp)]_StencilFail("Stencil Fail", Float) = 0

        [Main(_SurfaceInputs, _, on, off)]_SurfaceInputs("Surface Inputs", Float) = 0
        [Sub(_SurfaceInputs)][MainTexture] _BaseMap("Base Map (RGBA)", 2D) = "white" {}
        [Sub(_SurfaceInputs)]_BaseMapBias("BaseMap Bias", Range(-5, 5)) = 0
        [Sub(_SurfaceInputs)][MainColor][HDR]_BaseColor("Base Color", Color) = (1,1,1,1)
        [SubToggle(_SurfaceInputs)] _ApplyVertexColor("Apply Vertex Color", Float) = 0
        [ExtendSub(_SurfaceInputs, _NORMAL)]_NormalMap("Normal Map (RG), AO(B), Smoothness(A)", 2D) = "white" {}
        [Sub(_SurfaceInputs)]_NormalScale("Normal Scale", Float) = 1.0
        [Sub(_SurfaceInputs)]_NormalMapBias("NormalMap Bias", Range(-5, 5)) = 0
        [Sub(_SurfaceInputs)]_OcclusionStrength("Occlusion", Range(0.0, 1.0)) = 1.0
        [Sub(_SurfaceInputs)]_Smoothness("Smoothness", Range(0.0, 1.0)) = 0.2

        [Main(_CloudInputs, _CLOUD_FRAMES)]_CloudFrames("Cloud Frames", Float) = 0
        [Sub(_CloudInputs)]_CloudFrameAndSpeed("Cloud Frame(xy) Speed(z)", Vector) = (1,1,1,1)
        [ExtendSub(_CloudInputs, _MOTIONMAP)]_CloudMotionMap("Cloud Motion Map", 2D) = "white" {}
        [Sub(_CloudInputs)]_CloudMotionIntensity("Cloud Motion Intensity", Range(0, 0.1)) = 0.001

        [Main(_FlowMapOptions, _FLOWMAP)]_FlowMapOptions("FlowMap Options", Float) = 0
        [Sub(_FlowMapOptions)]_FlowMap("Flow Map (RG)", 2D) = "white" {}
        [Sub(_FlowMapOptions)] _FlowDirSpeed("Flow Direction Speed", Range(-0.1, 0.1)) = 0.001
        [Sub(_FlowMapOptions)] _FlowTimeSpeed("Flow Time Speed", Float) = 1

        [Main(_DistortionOptions, _DISTORTION_MAP)]_DistortionOptions("Distortion Options", Float) = 0
		[Sub(_DistortionOptions)] _DistortionMap("Distortion Map (RG)", 2D) = "white" {}
        [Sub(_DistortionOptions)] _DistortionUSpeed("Distortion U Speed", Float) = 0
		[Sub(_DistortionOptions)] _DistortionVSpeed("Distortion V Speed", Float) = 0
        [Sub(_DistortionOptions)] _DistortionIntensity("Distortion Intensity", Range(-0.1, 0.1)) = 0.001
        [ExtendSub(_DistortionOptions, _DISTORTION_MASK_MAP)] _DistortionMaskMap("Distortion Mask Map (R)", 2D) = "white" {}
        [Channel(_DistortionOptions)]_DistortionMaskChannelMask("Distortion Mask Channel Mask (Default R)", Vector) = (1,0,0,0)

        [Main(_DissolveOptions, _DISSOLVE_MAP)]_DissolveOptions("Dissolve Options", Float) = 0
        [Sub(_DissolveOptions)] _DissolveMap ("Dissolve Map (R)", 2D) = "white" {}
        [Channel(_DissolveOptions)] _DissolveChannelMask("Dissolve Channel Mask (Default R)", Vector) = (1,0,0,0)
	    [Sub(_DissolveOptions)] _DissolveUSpeed("Dissolve U Speed", Float) = 0
	    [Sub(_DissolveOptions)] _DissolveVSpeed("Dissolve V Speed", Float) = 0
        [Sub(_DissolveOptions)] _DissolveIntensity ("Dissolve Intensity", Range(0, 1)) = 0
        [Sub(_DissolveOptions)] _DissolveWidth ("Dissolve Width", Range(0, 1)) = 0.001
        [Sub(_DissolveOptions)][HDR]_DissolveEdgeColor("Dissolve Edge Color", Color) = (1, 1, 1, 1)
        [Sub(_DissolveOptions)] _DissolveEdgeIntensity("Dissolve Edge Intensity", Float) = 1

        [Main(_EmissionOptions, _EMISSION)]_EmissionOptions("Emission Options", Float) = 0
		[Sub(_EmissionOptions)]_EmissionMap("Emission Map", 2D) = "white" {}
        [Sub(_EmissionOptions)]_EmissionMapBias("Emission Map Bias", Range(-5, 5)) = 0
        [Sub(_EmissionOptions)][HDR]_EmissionColor("Emission Color", Color) = (0,0,0,1)
        [Sub(_EmissionOptions)]_EmissionIntensity("Emission Intensity", Range(0.01, 10)) = 1

        [Main(_AdvancedOptions, _, on, off)]_AdvancedOptions("Advanced Options", Float) = 0
        [SubToggle(_AdvancedOptions, _)] _SpecularHighlights("Specular Highlights", Float) = 0.0
        [SubToggle(_AdvancedOptions, _)] _EnvironmentReflections("Environment Reflections", Float) = 1.0
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
        Tags{"RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True"}

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            Blend[_SrcBlend][_DstBlend], [_SrcBlendAlpha] [_DstBlendAlpha]
            ZWrite[_ZWrite]
            Cull[_Cull]

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

            #pragma shader_feature_local _ _BLING_PHONG
            #pragma shader_feature_local _CUSTOM_LIGHT
            #pragma shader_feature_local_vertex _BILLBOARD
            #pragma shader_feature_local _DEPTH_FADE
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _CLOUD_FRAMES
            #pragma shader_feature_local _MOTIONMAP
            #pragma shader_feature_local _FLOWMAP
            #pragma shader_feature_local _DISTORTION_MAP
            #pragma shader_feature_local _DISTORTION_MASK_MAP
            #pragma shader_feature_local _DISSOLVE_MAP
            #pragma shader_feature_local _EMISSION
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF

			#pragma multi_compile_instancing

            #pragma vertex BillboardCloudVert
            #pragma fragment BillboardFrag

            #include "BillboardCloudInput.hlsl"
            #include "BillboardCloudForwardPass.hlsl"

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

            #include "BillboardCloudInput.hlsl"
            #include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/PBR/PBRDepthOnlyPass.hlsl"

            ENDHLSL
        }
	}

	FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Matrix.ShaderGUI.BaseShaderGUI"
}