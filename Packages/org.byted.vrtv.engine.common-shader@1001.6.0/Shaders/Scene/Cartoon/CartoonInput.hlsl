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

#ifndef MATRIX_CARTOON_INPUT_INCLUDED
#define MATRIX_CARTOON_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/Common.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/CartoonCommon.hlsl" 

#if _ALPHAMAP_ON
    TEXTURE2D(_AlphaMap);           SAMPLER(sampler_AlphaMap);
#endif

#if _NORMALMAP
    TEXTURE2D(_NormalMap);          SAMPLER(sampler_NormalMap);
#endif

TEXTURECUBE(_EmssionCubemap);	SAMPLER(sampler_EmssionCubemap);

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _NormalMap_ST;
float4 _AlphaMap_ST;
half _BaseMapBias;
half _BumpMapBias;

half4 _ShadowColorFirst;
half4 _ShadowColorSecond;
half4 _ShadowColorThird;
half _ShadowBoundaryFirst;
half _ShadowBoundarySecond;
half _ShadowBoundaryThird;
half _ShadowSmoothFirst;
half _ShadowSmoothSecond;
half _ShadowSmoothThird;
half _ShadowAreaFirst;
half _ShadowAreaSecond;
half _ShadowAreaThird;

half4 _BaseColor;
half3 _BlingPhongSpecColor;

float4 _EmissionMap_ST;
half4 _EmissionColor;
half _EmissionIntensity;
half4 _EmissionCubemapColor;
half _EmissionCubemapIntensity;
half _EmissionCubemapLod;

half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _OcclusionStrength;
half _Surface;

half _CloudShadowIntensity;
half _AdjustColorIntensity;

half _ApplyVertexColor;
half _CustomSpecularIntensity;
CBUFFER_END

inline void InitializeStandardLitSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)    
{
    #ifdef _ALPHAMAP_ON
    outSurfaceData.alpha = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, TRANSFORM_TEX(uv, _AlphaMap)).a * _BaseColor.a * vertexColor.a;
    #else
    outSurfaceData.alpha = _BaseColor.a * vertexColor.a;
    #endif
    
    half4 albedoAlpha = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _BaseMap), TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), _BaseMapBias);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb * vertexColor.rgb;

    #if _NORMALMAP
        half4 normalColor = SAMPLE_TEXTURE2D_BIAS(_NormalMap, sampler_NormalMap, TRANSFORM_TEX(uv, _NormalMap), _BumpMapBias);
        half3 normal;
        normal.xy = normalColor.rg * 2.0 - 1.0;
        normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));
        normal.xy *= _BumpScale;
    #else
        half4 normalColor = half4(0, 0, 1, 1);
        half3 normal = half3(0.0h, 0.0h, 1.0h);
    #endif
    outSurfaceData.metallic = normalColor.b * _Metallic;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);

    outSurfaceData.smoothness = normalColor.a * _Smoothness;
    outSurfaceData.normalTS = normal;

    outSurfaceData.occlusion = LerpWhiteTo(albedoAlpha.a, _OcclusionStrength);
    outSurfaceData.emission = SampleEmission(TRANSFORM_TEX(uv, _EmissionMap), _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap)) * _EmissionIntensity;
    
    outSurfaceData.clearCoatMask       = 0.0h;
    outSurfaceData.clearCoatSmoothness = 0.0h;
}

inline void ResolveStylizedData(out StylizedData outStylizedData)
{
    outStylizedData.shadowColorFirst = _ShadowColorFirst;
    outStylizedData.shadowColorSecond = _ShadowColorSecond;
    outStylizedData.shadowColorThird = _ShadowColorThird;
    outStylizedData.shadowBoundaryFirst = _ShadowBoundaryFirst;
    outStylizedData.shadowBoundarySecond = _ShadowBoundarySecond;
    outStylizedData.shadowBoundaryThird = _ShadowBoundaryThird;
    outStylizedData.shadowSmoothFirst = _ShadowSmoothFirst;
    outStylizedData.shadowSmoothSecond = _ShadowSmoothSecond;
    outStylizedData.shadowSmoothThird = _ShadowSmoothThird;
    outStylizedData.shadowAreaFirst = _ShadowAreaFirst;
    outStylizedData.shadowAreaSecond = _ShadowAreaSecond;
    outStylizedData.shadowAreaThird = _ShadowAreaThird;
}
#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
