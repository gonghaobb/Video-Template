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

#ifndef VEGETATION_LIT_INPUT_INCLUDED
#define VEGETATION_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/Common.hlsl"

#if defined(USE_BLINN_PHONG) && !defined(_SPECULARHIGHLIGHTS_OFF)
    #define _SPECULAR_COLOR
#endif

#if _NORMALMAP
    TEXTURE2D(_NormalMap);          SAMPLER(sampler_NormalMap);
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
half4 _NormalMap_ST;
half _NormalMapBias;
half _NormalScale;
half _Metallic;
half _Smoothness;
half _OcclusionStrength;

half4 _EmissionMap_ST;
half4 _EmissionColor;
half _EmissionIntensity;

half4 _GradientColor;
half4 _ColorVariation;
half _WindTrunkContrast;
half _WindTrunkPosition;
half _WindMultiplier;
half _MicroWindMultiplier;
half _GradientPosition;
half _GradientFalloff;
half _InvertGradient;
half _ColorVariationPower;
half _NoiseScale;

half _CloudShadowIntensity;
half _AdjustColorIntensity;
CBUFFER_END

inline void InitializeStandardLitSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;
    
    half4 baseColor = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _BaseMap), TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), _BaseMapBias) * _BaseColor;
    #if _VEGETATION_LEAVES && _LEAVES_COLOR
        surfaceData.albedo *= input.color;
    #endif
    
    #if defined(_ALPHATEST_ON)
        clip(outSurfaceData.alpha - _Cutoff);
    #endif

    outSurfaceData.albedo = baseColor.rgb;

    #if _NORMALMAP
        half4 normalColor = SAMPLE_TEXTURE2D_BIAS(_NormalMap, sampler_NormalMap, TRANSFORM_TEX(uv, _NormalMap), _NormalMapBias);
        half3 normal;
        normal.xy = normalColor.rg * 2.0 - 1.0;
        normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));
        normal.xy *= _NormalScale;
    #else
        half4 normalColor = half4(0, 0, 1, 1);
        half3 normal = half3(0.0h, 0.0h, 1.0h);
    #endif

    outSurfaceData.metallic = normalColor.b * _Metallic;
    outSurfaceData.smoothness = normalColor.a * _Smoothness;
    outSurfaceData.normalTS = normal;
    outSurfaceData.occlusion = LerpWhiteTo(1, _OcclusionStrength);

    outSurfaceData.emission = SampleEmission(TRANSFORM_TEX(uv, _EmissionMap), _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
}
#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
