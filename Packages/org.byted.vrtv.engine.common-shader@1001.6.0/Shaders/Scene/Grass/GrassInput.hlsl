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

#ifndef GRASS_INPUT_INCLUDED
#define GRASS__INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/Common.hlsl"

#if defined(_BLING_PHONG) && !defined(_SPECULARHIGHLIGHTS_OFF)
    #define _SPECULAR_COLOR
#endif

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
half _Surface;
half3 _BlingPhongSpecColor;
half _Cutoff;
half _CustomSpecularIntensity;
half _ApplyVertexColor;

half4 _BaseColor;
half4 _BaseMap_ST;
half _BaseMapBias;
half _BackfaceNormalFlip;
half _NormalAlwaysUp;
half _Metallic;
half _Smoothness;
half _OcclusionStrength;

half4 _EmissionMap_ST;
half4 _EmissionColor;
half _EmissionIntensity;

half _SpecularMinV;
half _SpecularMaxV;
half4 _GrassHeightColor;
half _GrassHeightFalloff;
half4 _GrassColorNoise_ST;
half4 _GrassGradientColor;
half _GrassColorVariationPower;
half _WindMultiplier;

half _CloudShadowIntensity;
half _AdjustColorIntensity;
CBUFFER_END

#define _VEGETATION_GRASS
#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/VegetationSimulate/Resource/Shaders/Vegetation.hlsl"

inline void InitializeStandardLitSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;

    half4 baseColor = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _BaseMap), TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), _BaseMapBias) * _BaseColor;
    #if _GRASS_COLOR
        baseColor *= vertexColor;
    #endif
    
    outSurfaceData.alpha = baseColor.a;

    #if defined(_ALPHATEST_ON)
        clip(outSurfaceData.alpha - _Cutoff);
    #endif

    outSurfaceData.albedo = baseColor.rgb;

    outSurfaceData.metallic = _Metallic;
    outSurfaceData.smoothness = _Smoothness;
    outSurfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
    outSurfaceData.occlusion = LerpWhiteTo(1, _OcclusionStrength);

    outSurfaceData.emission = SampleEmission(TRANSFORM_TEX(uv, _EmissionMap), _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap)) * _EmissionIntensity;
}

#endif