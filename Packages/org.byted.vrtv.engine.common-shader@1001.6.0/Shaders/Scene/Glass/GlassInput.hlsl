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

#ifndef MATRIX_GLASS_INPUT_INCLUDED
#define MATRIX_GLASS_INPUT_INCLUDED

#if defined(_BLING_PHONG) && !defined(_SPECULARHIGHLIGHTS_OFF)
    #define _SPECULAR_COLOR
#endif

#if defined(_NORMALMAP) || defined(_INTERIOR_CUBEMAP) || defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE)
    #define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/Common.hlsl"

#if _NORMALMAP
    TEXTURE2D(_NormalMap);          SAMPLER(sampler_NormalMap);
#endif

#if _ALPHAMAP_ON
    TEXTURE2D(_AlphaMap);           SAMPLER(sampler_AlphaMap);
#endif

TEXTURECUBE(_InteriorCubemap);	    SAMPLER(sampler_InteriorCubemap);

TEXTURECUBE(_EnvironmentCubeMap);	SAMPLER(sampler_EnvironmentCubeMap);

#if _FROST
    TEXTURE2D(_FrostMaskMap);          SAMPLER(sampler_FrostMaskMap);
    #if _FROST_NOISE
        TEXTURE2D(_FrostNoiseMap);      SAMPLER(sampler_FrostNoiseMap);
    #endif
#endif

CBUFFER_START(UnityPerMaterial)
half _Surface;
half _Cutoff;
half3 _BlingPhongSpecColor;
half _GlassFresnelIntensity;
half _GlassMinFresnel;
half _GlassMaxFresnel;

half4 _BaseMap_ST;
half4 _BaseColor;
half4 _AlphaMap_ST;
half4 _NormalMap_ST;
half _NormalScale;
half _Smoothness;
half _Metallic;
half _OcclusionStrength;

half4 _InteriorCubemap_ST;
half _InteriorDepthScale;
half _InteriorIntensity;

half _EnvironmentCubemapLod;
half4 _EnvironmentReflectionColor;
half _EnvironmentReflectionIntensity;
half4 _EnvironmentRefractionColor;
half _EnvironmentRefractionIntensity;

half4 _FrostColor;
half4 _FrostCenter;
half _FrostDistance;
half _FrostReverse;
half4 _FrostMaskMap_ST;
half _FrostBlendFactor;
half4 _FrostNoiseMap_ST;
half _FrostNoiseDistance;
half _FrostNoiseIntensity;
half _FrostIntensity;

half4 _EmissionMap_ST;
half4 _EmissionColor;
half _EmissionIntensity;

half _CloudShadowIntensity;
half _AdjustColorIntensity;
half _ApplyVertexColor;
half _CustomSpecularIntensity;
CBUFFER_END

inline void InitializeGlassSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

#ifdef _ALPHAMAP_ON
    outSurfaceData.alpha = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, TRANSFORM_TEX(uv, _AlphaMap)).r * _BaseColor.a;
#else
    outSurfaceData.alpha = _BaseColor.a;
#endif
    
    outSurfaceData.alpha *= vertexColor.a;

#if _ALPHATEST_ON
    clip(outSurfaceData.alpha - _Cutoff);
#endif

    half4 baseMapColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(uv, _BaseMap));
    outSurfaceData.albedo = baseMapColor.rgb * _BaseColor.rgb * vertexColor.rgb;

#if _NORMALMAP
    half4 normalColor = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, TRANSFORM_TEX(uv, _NormalMap));
    outSurfaceData.normalTS = UnpackNormalRG(normalColor, _NormalScale);
	outSurfaceData.metallic = normalColor.b * _Metallic;
    outSurfaceData.smoothness = normalColor.a * _Smoothness;
#else
    outSurfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
	outSurfaceData.metallic = _Metallic;
    outSurfaceData.smoothness = _Smoothness;
#endif

    outSurfaceData.occlusion = baseMapColor.a;
	outSurfaceData.occlusion = lerp(1, outSurfaceData.occlusion, _OcclusionStrength);

#ifdef _EMISSION
	outSurfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(uv, _EmissionMap)).rgb * _EmissionColor.rgb * _EmissionIntensity;
#endif
}

#endif