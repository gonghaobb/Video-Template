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

#ifndef MATRIX_UBER_POST_EFFECT_INPUT_INCLUDED
#define MATRIX_UBER_POST_EFFECT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float4 _SourceSize;

TEXTURE2D_X(_CameraTransparentTexture);
SAMPLER(sampler_CameraTransparentTexture);
half4 _CameraTransparentTexture_TexelSize;

TEXTURE2D(_ScreenDistortionTexture);
SAMPLER(sampler_ScreenDistortionTexture);

TEXTURE2D(_DissolveTex);
SAMPLER(sampler_DissolveTex);

CBUFFER_START(UnityPerMaterial)
half4 _ScreenDistortionTexture_ST;
half _ScreenDistortionU;
half _ScreenDistortionV;
half _ScreenDistortStrength;

float _KawaseBlurRadius;
half _KawaseBlurIteration;
float _KawaseBlurEdgeWeakDistance;

float _GrainyBlurRadius;
half _GrainyBlurIteration;
float _GrainyBlurEdgeWeakDistance;

half _GlitchImageBlockSpeed;
half _GlitchImageBlockSize;
half _GlitchImageBlockMaxRGBSplitX;
half _GlitchImageBlockMaxRGBSplitY;

half _GlitchScreenShakeIndensityX;
half _GlitchScreenShakeIndensityY;

half4 _DissolveTex_ST;
half4 _DissolveEdgeColor;
half4 _DissolveBackgroundColor;
half _DissolveWidth;
half _DissolveProcess;
half _DissolveHardEdge;
half _InvertDissolve;
half4 _lensDistortionParams;
CBUFFER_END

struct Attributes
{
#if _FULL_SCREEN
	uint vertexID     : SV_VertexID;
#else
	float4 positionOS : POSITION;
	float2 uv         : TEXCOORD0;
#endif

	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
	float2 uv : TEXCOORD0;
	float4 positionCS : SV_POSITION;

#if !_FULL_SCREEN
	float4 screenPos : TEXCOORD2;
#endif

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

float4 SampleScreenColor(float2 uv)
{
	return SAMPLE_TEXTURE2D_X(
		_CameraTransparentTexture,
		sampler_CameraTransparentTexture,
		UnityStereoTransformScreenSpaceTex(uv)
	);
}

Varyings UberPostEffectVert(Attributes input)
{
	Varyings output = (Varyings)0;
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if _FULL_SCREEN
	output.positionCS = GetQuadVertexPosition(input.vertexID);
	output.positionCS.xy = output.positionCS.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
	output.uv = GetQuadTexCoord(input.vertexID);
#else
	output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
	output.uv = input.uv;
	output.screenPos = ComputeScreenPos(output.positionCS);
#endif

	return output;
}

#endif