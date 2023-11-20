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

#ifndef MATRIX_BILLBOARD_CLOUD_INPUT_INCLUDED
#define MATRIX_BILLBOARD_CLOUD_INPUT_INCLUDED

#if defined(_BLING_PHONG) && !defined(_SPECULARHIGHLIGHTS_OFF)
    #define _SPECULAR_COLOR
#endif

#if defined(_NORMALMAP) || defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE)
    #define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/Common.hlsl"

#if _NORMALMAP
    TEXTURE2D(_NormalMap);   SAMPLER(sampler_NormalMap);
#endif

#if _MOTIONMAP
    TEXTURE2D(_CloudMotionMap);   SAMPLER(sampler_CloudMotionMap);
#endif

#if _FLOWMAP
    TEXTURE2D(_FlowMap);       SAMPLER(sampler_FlowMap);
#endif

#if _DISTORTION_MAP
    TEXTURE2D(_DistortionMap);       SAMPLER(sampler_DistortionMap);
    #if _DISTORTION_MASK_MAP
        TEXTURE2D(_DistortionMaskMap);   SAMPLER(sampler_DistortionMaskMap);
    #endif
#endif

#if _DISSOLVE_MAP
    TEXTURE2D(_DissolveMap);   SAMPLER(sampler_DissolveMap);
#endif

CBUFFER_START(UnityPerMaterial)
half _Surface;
half _Cutoff;
half3 _BlingPhongSpecColor;
half _CustomSpecularIntensity;
half _DepthFade;

half _BaseMapBias;
half4 _BaseColor;
half4 _BaseMap_ST;
half _ApplyVertexColor;

half _NormalMapBias;
half _NormalScale;
half4 _NormalMap_ST;
half _OcclusionStrength;
half _Smoothness;

float4 _CloudFrameAndSpeed;
half4 _CloudMotionMap_ST;
half _CloudMotionIntensity;

half4 _FlowMap_ST;
half _FlowDirSpeed;
half _FlowTimeSpeed;
half _FlowNormalScale;

half4 _DistortionMap_ST;
half _DistortionUSpeed;
half _DistortionVSpeed;
half _DistortionIntensity;
half4 _DistortionMaskMap_ST;
half4 _DistortionMaskChannelMask;

half4 _DissolveMap_ST;
half4 _DissolveChannelMask;
half _DissolveUSpeed;
half _DissolveVSpeed;
half _DissolveIntensity;
half _DissolveWidth;
half4 _DissolveEdgeColor;
half _DissolveEdgeIntensity;

half _EmissionMapBias;
half4 _EmissionMap_ST;
half4 _EmissionColor;
half _EmissionIntensity;

half _CloudShadowIntensity;
half _AdjustColorIntensity;
CBUFFER_END

half4 SampleTextureWithFlowMap(float2 uv, float2 uvPanner1, float2 uvPanner2, TEXTURE2D_PARAM(map, sampler_map), half bias, half flowLerp)
{
    half4 color1 = SAMPLE_TEXTURE2D_BIAS(map, sampler_map, uv + uvPanner1, bias);
    half4 color2 = SAMPLE_TEXTURE2D_BIAS(map, sampler_map, uv + uvPanner2, bias);
    return lerp(color1, color2, flowLerp);
}

#if _FLOWMAP
inline void InitializeBillboardCloudSurfaceData(float2 uv, float2 uvPanner1, float2 uvPanner2, float flowLerp, half4 vertexColor, out SurfaceData outSurfaceData)
#else
inline void InitializeBillboardCloudSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)
#endif
{
    outSurfaceData = (SurfaceData)0;

#if _FLOWMAP
	half4 baseMapColor = SampleTextureWithFlowMap(TRANSFORM_TEX(uv, _BaseMap), uvPanner1, uvPanner2, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), _BaseMapBias, flowLerp) * _BaseColor * vertexColor;
#else
    half4 baseMapColor = SAMPLE_TEXTURE2D_BIAS(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(uv, _BaseMap), _BaseMapBias) * _BaseColor * vertexColor;
#endif

    outSurfaceData.albedo = baseMapColor.rgb;
    outSurfaceData.alpha = baseMapColor.a;

#if _NORMALMAP
    #if _FLOWMAP
        half4 normalColor = SampleTextureWithFlowMap(TRANSFORM_TEX(uv, _NormalMap), uvPanner1, uvPanner2, TEXTURE2D_ARGS(_NormalMap, sampler_NormalMap), _NormalMapBias, flowLerp);
    #else
        half4 normalColor = SAMPLE_TEXTURE2D_BIAS(_NormalMap, sampler_NormalMap, TRANSFORM_TEX(uv, _NormalMap), _NormalMapBias);
    #endif

    outSurfaceData.normalTS = UnpackNormalRG(normalColor, _NormalScale);
    outSurfaceData.occlusion = lerp(1, normalColor.b, _OcclusionStrength);
    outSurfaceData.smoothness = normalColor.a * _Smoothness;
#else
    outSurfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
    outSurfaceData.occlusion = _OcclusionStrength;
    outSurfaceData.smoothness = _Smoothness;
#endif

#ifdef _EMISSION
    #if _FLOWMAP
        outSurfaceData.emission = SampleTextureWithFlowMap(TRANSFORM_TEX(uv, _EmissionMap), uvPanner1, uvPanner2, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap), _EmissionMapBias, flowLerp).rgb;
    #else
	    outSurfaceData.emission = SAMPLE_TEXTURE2D_BIAS(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(uv, _EmissionMap), _EmissionMapBias).rgb;
    #endif

    outSurfaceData.emission *= _EmissionColor.rgb * _EmissionIntensity;
#endif
}

#endif