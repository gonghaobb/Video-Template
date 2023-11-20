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

Shader "PicoVideo/Effects/UberPostEffect"
{
    Properties 
	{
        [Main(_ScreenDistortion, _SCREEN_DISTORTION)]_ScreenDistortion("Screen Distortion", Float) = 0
        [Sub(_ScreenDistortion)]_ScreenDistortionTexture ("Screen Distortion Texture", 2D) = "white" {}
        [Sub(_ScreenDistortion)]_ScreenDistortionU("Screen Distortion U Speed",Float) = 0
        [Sub(_ScreenDistortion)]_ScreenDistortionV("Screen Distortion V Speed", Float) = 0
        [Sub(_ScreenDistortion)]_ScreenDistortStrength("Screen Distortion Strength", Range(0, 1)) = 0.1

        [Main(_KawaseBlur, _KAWASE_BLUR)]_KawaseBlur("Kawase Blur", Float) = 0
        [Sub(_KawaseBlur)]_KawaseBlurRadius("Kawase Blur Radius", Range(0, 4)) = 0.5
        [Sub(_KawaseBlur)]_KawaseBlurIteration("Kawase Blur Iteration", Range(1, 4)) = 2
        [Sub(_KawaseBlur)]_KawaseBlurEdgeWeakDistance("Kawase Blur EdgeWeak Distance", Range(0, 3)) = 0

        [Main(_GrainyBlur, _GRAINY_BLUR)]_GrainyBlur("Grainy Blur", Float) = 0
        [Sub(_GrainyBlur)]_GrainyBlurRadius("Grainy Blur Radius", Range(0, 50)) = 5
        [Sub(_GrainyBlur)]_GrainyBlurIteration("Grainy Blur Iteration", Range(1, 8)) = 1
        [Sub(_GrainyBlur)]_GrainyBlurEdgeWeakDistance("Grainy Blur EdgeWeak Distance", Range(0, 3)) = 0

        [Main(_GlitchImageBlock, _GLITCH_IMAGE_BLOCK)]_GlitchImageBlock("Glitch Image Block", Float) = 0
        [Sub(_GlitchImageBlock)] _GlitchImageBlockSpeed("Speed", Range(0, 50)) = 10
        [Sub(_GlitchImageBlock)] _GlitchImageBlockSize("Size", Range(0, 50)) = 8
        [Sub(_GlitchImageBlock)] _GlitchImageBlockMaxRGBSplitX("Max RGB SplitX", Range(0, 25)) = 1
        [Sub(_GlitchImageBlock)] _GlitchImageBlockMaxRGBSplitY("Max RGB SplitY", Range(0, 25)) = 1

        [Main(_GlitchScreenShake, _GLITCH_SCREEN_SHAKE)]_GlitchScreenShake("Glitch Screen Shake", Float) = 0
        [Sub(_GlitchScreenShake)] _GlitchScreenShakeIndensityX("Shake Indensity X", Range(0, 1)) = 0.5
        [Sub(_GlitchScreenShake)] _GlitchScreenShakeIndensityY("Shake Indensity Y", Range(0, 1)) = 0.5
	    
	    [Main(_ScreenDissolve, _SCREEN_DISSOLVE)]_ScreenDissolve("Screen Dissolve", Float) = 0
	    [Sub(_ScreenDissolve)] _DissolveTex("Dissolve Texture", 2D) = "black" {}
	    [Sub(_ScreenDissolve)] _DissolveProcess("Dissolve Process", Float) = 0
	    [Sub(_ScreenDissolve)] _InvertDissolve("Invert Dissolve", Float) = 0
    }

    SubShader
	{
        Tags 
		{
            "RenderType" = "Opaque"
			"RenderPipeline" = "UniversalPipeline"
            "ShaderModel"="4.5"
        }

        HLSLINCLUDE
            #include "Includes/UberPostEffectInput.hlsl"
            #include "Includes/UberPostEffectBase.hlsl"
        ENDHLSL

        Pass
		{
            Name "UberPostEffect"
            
            Cull Off
            ZWrite Off
            ZTest Always

			Tags
            {
                "LightMode" = "UberPostEffect"
            }
			
            HLSLPROGRAM
            
            #pragma prefer_hlslcc gles
            #pragma target 4.5

            #pragma multi_compile_local _ _FULL_SCREEN
            #pragma multi_compile_local_fragment _ _SCREEN_DISTORTION
            #pragma multi_compile_local_fragment _ _KAWASE_BLUR _GRAINY_BLUR _GLITCH_IMAGE_BLOCK _GLITCH_SCREEN_SHAKE _SCREEN_DISSOLVE

            #pragma vertex UberPostEffectVert
            #pragma fragment UberPostEffectFrag

            half4 UberPostEffectFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #if _FULL_SCREEN
                    float2 screenUV = input.uv;
                #else
                    float2 screenUV = input.screenPos.xy / input.screenPos.w;
                #endif

                return UberPostEffect(input.uv, screenUV);
            }

            ENDHLSL
        }
	}

    Fallback "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Matrix.EcosystemSimulate.UberPostEffectShaderGUI"
}