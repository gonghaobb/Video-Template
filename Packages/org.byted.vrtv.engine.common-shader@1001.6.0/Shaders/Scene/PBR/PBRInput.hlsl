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

#ifndef MATRIX_PBR_INPUT_INCLUDED
#define MATRIX_PBR_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/Common.hlsl"

#if _EMISSION_CUBEMAP
TEXTURECUBE(_EmssionCubemap);	SAMPLER(sampler_EmssionCubemap);
#endif

#if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
#define _DETAIL
#endif

#if defined(USE_BLINN_PHONG) && !defined(_SPECULARHIGHLIGHTS_OFF)
#define _SPECULAR_COLOR
#endif

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half3 _BlingPhongSpecColor;
float4 _BumpMap_ST;
float4 _EmissionMap_ST;
float4 _DetailNormalMap_ST;
float4 _AlphaMap_ST;
float _FresnelBias;
float _FresnelScale;
float _FresnelPower;
half _BaseMapBias;
half _BumpMapBias;

half _LayerBlendStrength;
half _LayerBlendSmoothness;
half4 _LayerBaseColor;
half4 _LayerBaseMap_ST;
half4 _LayerAlphaMap_ST;
half4 _LayerBumpMap_ST;
half _LayerBumpScale;
half _LayerMetallic;
half _LayerSmoothness;
half _LayerOcclusionStrength;

float4 _DetailAlbedoMap_ST;
half4 _BaseColor;
half4 _EmissionColor;

half _DetailAlbedoMapScale;
half _DetailNormalMapScale;
half _EmissionCubemapIntensity;
half _EmissionCubemapLod;

half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _OcclusionStrength;
half _Surface;
half _LightmapSpecular;

half _CloudShadowIntensity;

half _AdjustColorIntensity;

half _ApplyVertexColor;
half _ApplyEmissionVertexColor;
half _CustomSpecularIntensity;
CBUFFER_END

#if _ALPHAMAP_ON
    TEXTURE2D(_AlphaMap);           SAMPLER(sampler_AlphaMap);
#endif

TEXTURE2D(_DetailMask);         SAMPLER(sampler_DetailMask);
TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);

// Used for scaling detail albedo. Main features:
// - Depending if detailAlbedo brightens or darkens, scale magnifies effect.
// - No effect is applied if detailAlbedo is 0.5.
half3 ScaleDetailAlbedo(half3 detailAlbedo, half scale)
{
    // detailAlbedo = detailAlbedo * 2.0h - 1.0h;
    // detailAlbedo *= _DetailAlbedoMapScale;
    // detailAlbedo = detailAlbedo * 0.5h + 0.5h;
    // return detailAlbedo * 2.0f;

    // A bit more optimized
    return 2.0h * detailAlbedo * scale - scale + 1.0h;
}

half3 ApplyDetailAlbedo(float2 detailUv, half3 albedo, half detailMask)
{
#if defined(_DETAIL)
    half3 detailAlbedo = SAMPLE_TEXTURE2D(_DetailAlbedoMap, sampler_DetailAlbedoMap, detailUv).rgb;

    // In order to have same performance as builtin, we do scaling only if scale is not 1.0 (Scaled version has 6 additional instructions)
#if defined(_DETAIL_SCALED)
    detailAlbedo = ScaleDetailAlbedo(detailAlbedo, _DetailAlbedoMapScale);
#else
    detailAlbedo = 2.0h * detailAlbedo;
#endif

    return albedo * LerpWhiteTo(detailAlbedo, detailMask);
#else
    return albedo;
#endif
}

half3 ApplyDetailNormal(float2 detailUv, half3 normalTS, half detailMask)
{
#if defined(_DETAIL)
#if BUMP_SCALE_NOT_SUPPORTED
    half3 detailNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv));
#else
    half3 detailNormalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, detailUv), _DetailNormalMapScale);
#endif

    // With UNITY_NO_DXT5nm unpacked vector is not normalized for BlendNormalRNM
    // For visual consistancy we going to do in all cases
    detailNormalTS = normalize(detailNormalTS);

    return lerp(normalTS, BlendNormalRNM(normalTS, detailNormalTS), detailMask); // todo: detailMask should lerp the angle of the quaternion rotation, not the normals
#else
    return normalTS;
#endif
}

#if defined(_ENABLE_LAYER_BLEND)

#define REQUIRES_WORLD_SPACE_POS_INTERPOLATOR

struct LayerSurfaceData
{
    half3 albedo;
    half  metallic;
    half  smoothness;
    half3 normalTS;
    half3 emission;
    half  occlusion;
    half  alpha;
};

TEXTURE2D(_LayerBaseMap);               SAMPLER(sampler_LayerBaseMap);
TEXTURE2D(_LayerAlphaMap);              SAMPLER(sampler_LayerAlphaMap);
TEXTURE2D(_LayerBumpMap);               SAMPLER(sampler_LayerBumpMap);

void InitializeLayerSurfaceData(half2 uv, out LayerSurfaceData outSurfaceData)
{
    outSurfaceData = (LayerSurfaceData)0;

    half4 albedoAlpha = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _LayerBaseMap), TEXTURE2D_ARGS(_LayerBaseMap, sampler_LayerBaseMap)) * _LayerBaseColor;
    outSurfaceData.albedo = albedoAlpha.rgb;

    #if defined(_ABEDO_ACHANEL_ALPHA)
        outSurfaceData.alpha = albedoAlpha.a;
    #else
        outSurfaceData.alpha = 1.0f;
    #endif

    #if _LAYER_ALPHAMAP_ON
        outSurfaceData.alpha = SAMPLE_TEXTURE2D(_LayerAlphaMap, sampler_LayerAlphaMap, TRANSFORM_TEX(uv, _AlphaMap)).a * _LayerBaseColor.a;
    #endif
    
    #if _LAYER_NORMALMAP
        half4 normalColor = SAMPLE_TEXTURE2D(_LayerBumpMap, sampler_LayerBumpMap, TRANSFORM_TEX(uv, _BumpMap));
        half3 normal;
        normal.xy = normalColor.rg * 2.0 - 1.0;
        normal.z = max(1.0e-16, sqrt(1.0 - saturate(dot(normal.xy, normal.xy))));
        normal.xy *= _LayerBumpScale;
    #else
        half4 normalColor = half4(0, 0, 1, 1);
        half3 normal = half3(0.0h, 0.0h, 1.0h);
    #endif

    outSurfaceData.metallic = normalColor.b * _LayerMetallic;
    outSurfaceData.smoothness = normalColor.a * _LayerSmoothness;
    outSurfaceData.normalTS = normal;

    #if defined(_ABEDO_ACHANEL_AO)
        outSurfaceData.occlusion = LerpWhiteTo(albedoAlpha.a, _LayerOcclusionStrength);
    #else
        outSurfaceData.occlusion = 1.0;
    #endif
}

inline SurfaceData BlendSurfaceDataAndLayerSurfaceData(float3 normalWS, SurfaceData surfaceData, LayerSurfaceData layerSurfaceData)
{
    SurfaceData outSurfaceData = surfaceData;

    half blendAlpha = saturate(pow(saturate(dot(normalWS, float3(0, 1, 0)) + _LayerBlendStrength), 1 / _LayerBlendSmoothness));

    outSurfaceData.albedo = lerp(surfaceData.albedo, layerSurfaceData.albedo, blendAlpha);
    outSurfaceData.metallic = lerp(surfaceData.metallic, layerSurfaceData.metallic, blendAlpha);
    outSurfaceData.smoothness = lerp(surfaceData.smoothness, layerSurfaceData.smoothness, blendAlpha);
    outSurfaceData.occlusion = lerp(surfaceData.occlusion, layerSurfaceData.occlusion, blendAlpha);
    outSurfaceData.alpha = lerp(surfaceData.alpha, layerSurfaceData.alpha, blendAlpha);
    outSurfaceData.normalTS = lerp(surfaceData.normalTS, layerSurfaceData.normalTS, blendAlpha); 

    return outSurfaceData;
}
#endif

inline void InitializeStandardLitSurfaceData(float2 uv, half4 vertexColor, out SurfaceData outSurfaceData)    
{
    #ifdef _ALPHAMAP_ON
        outSurfaceData.alpha = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, TRANSFORM_TEX(uv, _AlphaMap)).a * _BaseColor.a * lerp(1,vertexColor.a,_ApplyVertexColor);
    #else
        outSurfaceData.alpha = _BaseColor.a * lerp(1,vertexColor.a,_ApplyVertexColor);
    #endif
    
    #if defined(_ALPHATEST_ON)
        clip(outSurfaceData.alpha - _Cutoff);
    #endif

    half4 albedoAlpha = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _BaseMap), TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), _BaseMapBias);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb * lerp(1,vertexColor.rgb,_ApplyVertexColor);

    #if _NORMALMAP
        half4 normalColor = SAMPLE_TEXTURE2D_BIAS(_BumpMap, sampler_BumpMap, TRANSFORM_TEX(uv, _BumpMap), _BumpMapBias);
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

    outSurfaceData.emission = SampleEmission(TRANSFORM_TEX(uv, _EmissionMap), _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap)) * lerp(1,vertexColor.a * vertexColor.rgb , _ApplyEmissionVertexColor);;
    
    outSurfaceData.clearCoatMask       = 0.0h;
    outSurfaceData.clearCoatSmoothness = 0.0h;

#if defined(_DETAIL)
    half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
    float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    outSurfaceData.albedo = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
  
    float2 detailNormalUv = uv * _DetailNormalMap_ST.xy + _DetailNormalMap_ST.zw;
    outSurfaceData.normalTS = ApplyDetailNormal(detailNormalUv, outSurfaceData.normalTS, detailMask);
#endif
}
#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
