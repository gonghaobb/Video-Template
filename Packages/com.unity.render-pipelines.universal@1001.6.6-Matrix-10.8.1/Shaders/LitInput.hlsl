#ifndef UNIVERSAL_LIT_INPUT_INCLUDED
#define UNIVERSAL_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParallaxMapping.hlsl"

//PicoVideo;LightMode;XiaoPengCheng;Begin
#if _EMISSION_CUBEMAP
TEXTURECUBE(_EmssionCubemap);	SAMPLER(sampler_EmssionCubemap);
#endif
//PicoVideo;LightMode;XiaoPengCheng;End

#if defined(_DETAIL_MULX2) || defined(_DETAIL_SCALED)
#define _DETAIL
#endif

#if defined(USE_BLINN_PHONG) && !defined(_SPECULARHIGHLIGHTS_OFF)
#define _SPECULAR_COLOR
#endif

// NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
//PicoVideo;LightMode;YangFan;Begin
// #ifdef _MATRIX_INDEPENDENT_OFFSET_SCALE
float4 _BumpMap_ST;
float4 _EmissionMap_ST;
float4 _DetailNormalMap_ST;
// #endif
// #ifdef _MATRIX_ALPHA_MAP
float4 _AlphaMap_ST;
// #endif
// #ifdef _MATRIX_CUSTOM_FRESNEL
float _FresnelBias;
float _FresnelScale;
float _FresnelPower;
// #endif
//PicoVideo;LightMode;YangFan;End

//PicoVideo;LayerBlend;XiaoPengCheng;Begin
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
//PicoVideo;LayerBlend;XiaoPengCheng;End

float4 _DetailAlbedoMap_ST;
half4 _BaseColor;
half4 _SpecColor;
half4 _EmissionColor;

//PicoVideo;LightMode;XiaoPengCheng;Begin
half _EmissionCubemapIntensity;
half _EmissionCubemapLod;
//PicoVideo;LightMode;XiaoPengCheng;End

half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _Parallax;
half _OcclusionStrength;
half _ClearCoatMask;
half _ClearCoatSmoothness;
half _DetailAlbedoMapScale;
half _DetailNormalMapScale;
half _Surface;
//PicoVideo;LightMode;Ernst;Begin
// #if defined(_MATRIX_SPC_LIGHTINGMAP)
half _LightmapSpecular;
// #endif

//PicoVideo;CloudShadow;XiaoPengCheng;Begin
half _CloudShadowIntensity;
//PicoVideo;CloudShadow;XiaoPengCheng;End

//PicoVideo;LightMode;XiaoPengCheng;Begin
half _AdjustColorIntensity;
//PicoVideo;LightMode;XiaoPengCheng;End

//PicoVideo;LightMode;Ernst;End
CBUFFER_END
//PicoVideo;LightMode;Ernst;Begin
uniform half _GlobalLightmapSpecular;
//PicoVideo;LightMode;Ernst;End

// NOTE: Do not ifdef the properties for dots instancing, but ifdef the actual usage.
// Otherwise you might break CPU-side as property constant-buffer offsets change per variant.
// NOTE: Dots instancing is orthogonal to the constant buffer above.
#ifdef UNITY_DOTS_INSTANCING_ENABLED
UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
    UNITY_DOTS_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _SpecColor)
    UNITY_DOTS_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DOTS_INSTANCED_PROP(float , _Cutoff)
    UNITY_DOTS_INSTANCED_PROP(float , _Smoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _Metallic)
    UNITY_DOTS_INSTANCED_PROP(float , _BumpScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Parallax)
    UNITY_DOTS_INSTANCED_PROP(float , _OcclusionStrength)
    UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatMask)
    UNITY_DOTS_INSTANCED_PROP(float , _ClearCoatSmoothness)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailAlbedoMapScale)
    UNITY_DOTS_INSTANCED_PROP(float , _DetailNormalMapScale)
    UNITY_DOTS_INSTANCED_PROP(float , _Surface)
UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

#define _BaseColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata__BaseColor)
#define _SpecColor              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata__SpecColor)
#define _EmissionColor          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float4 , Metadata__EmissionColor)
#define _Cutoff                 UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__Cutoff)
#define _Smoothness             UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__Smoothness)
#define _Metallic               UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__Metallic)
#define _BumpScale              UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__BumpScale)
#define _Parallax               UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__Parallax)
#define _OcclusionStrength      UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__OcclusionStrength)
#define _ClearCoatMask          UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__ClearCoatMask)
#define _ClearCoatSmoothness    UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__ClearCoatSmoothness)
#define _DetailAlbedoMapScale   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__DetailAlbedoMapScale)
#define _DetailNormalMapScale   UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__DetailNormalMapScale)
#define _Surface                UNITY_ACCESS_DOTS_INSTANCED_PROP_FROM_MACRO(float  , Metadata__Surface)
#endif

TEXTURE2D(_ParallaxMap);        SAMPLER(sampler_ParallaxMap);
TEXTURE2D(_OcclusionMap);       SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_DetailMask);         SAMPLER(sampler_DetailMask);
TEXTURE2D(_DetailAlbedoMap);    SAMPLER(sampler_DetailAlbedoMap);
TEXTURE2D(_DetailNormalMap);    SAMPLER(sampler_DetailNormalMap);
TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_SpecGlossMap);       SAMPLER(sampler_SpecGlossMap);
TEXTURE2D(_ClearCoatMap);       SAMPLER(sampler_ClearCoatMap);

#ifdef _SPECULAR_SETUP
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_SpecGlossMap, sampler_SpecGlossMap, uv)
#else
    #define SAMPLE_METALLICSPECULAR(uv) SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv)
#endif

half4 SampleMetallicSpecGloss(float2 uv, half albedoAlpha)
{
    half4 specGloss;

#ifdef _METALLICSPECGLOSSMAP
    specGloss = SAMPLE_METALLICSPECULAR(uv);
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * _Smoothness;
    #else
        specGloss.a *= _Smoothness;
    #endif
#else // _METALLICSPECGLOSSMAP
    #if _SPECULAR_SETUP
        specGloss.rgb = _SpecColor.rgb;
    #else
        specGloss.rgb = _Metallic.rrr;
    #endif

    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        specGloss.a = albedoAlpha * _Smoothness;
    #else
        specGloss.a = _Smoothness;
    #endif
#endif

    return specGloss;
}

half SampleOcclusion(float2 uv)
{
#ifdef _OCCLUSIONMAP
// TODO: Controls things like these by exposing SHADER_QUALITY levels (low, medium, high)
#if defined(SHADER_API_GLES)
    return SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
#else
    half occ = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, uv).g;
    return LerpWhiteTo(occ, _OcclusionStrength);
#endif
#else
    return 1.0;
#endif
}


// Returns clear coat parameters
// .x/.r == mask
// .y/.g == smoothness
half2 SampleClearCoat(float2 uv)
{
#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoatMaskSmoothness = half2(_ClearCoatMask, _ClearCoatSmoothness);

#if defined(_CLEARCOATMAP)
    clearCoatMaskSmoothness *= SAMPLE_TEXTURE2D(_ClearCoatMap, sampler_ClearCoatMap, uv).rg;
#endif

    return clearCoatMaskSmoothness;
#else
    return half2(0.0, 1.0);
#endif  // _CLEARCOAT
}

void ApplyPerPixelDisplacement(half3 viewDirTS, inout float2 uv)
{
#if defined(_PARALLAXMAP)
    uv += ParallaxMapping(TEXTURE2D_ARGS(_ParallaxMap, sampler_ParallaxMap), viewDirTS, _Parallax, uv);
#endif
}

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

//PicoVideo;LayerMaterials;XiaoPengCheng;Begin
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

    #ifdef _MATRIX_INDEPENDENT_OFFSET_SCALE
        half4 albedoAlpha = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _LayerBaseMap), TEXTURE2D_ARGS(_LayerBaseMap, sampler_LayerBaseMap)) * _LayerBaseColor;
    #else
        half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_LayerBaseMap, sampler_LayerBaseMap)) * _LayerBaseColor;
    #endif

    outSurfaceData.albedo = albedoAlpha.rgb;

    #if defined(_ABEDO_ACHANEL_ALPHA)
        outSurfaceData.alpha = albedoAlpha.a;
    #else
        outSurfaceData.alpha = 1.0f;
    #endif

    #ifdef _MATRIX_ALPHA_MAP
        #if _LAYER_ALPHAMAP_ON
            outSurfaceData.alpha = SAMPLE_TEXTURE2D(_LayerAlphaMap, sampler_LayerAlphaMap, TRANSFORM_TEX(uv, _AlphaMap)).a * _LayerBaseColor.a;
        #endif
    #endif

    #if _LAYER_NORMALMAP
        #ifdef _MATRIX_INDEPENDENT_OFFSET_SCALE
            half4 normalColor = SAMPLE_TEXTURE2D(_LayerBumpMap, sampler_LayerBumpMap, TRANSFORM_TEX(uv, _BumpMap));
        #else
            half4 normalColor = SAMPLE_TEXTURE2D(_LayerBumpMap, sampler_LayerBumpMap, uv);
        #endif
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
//PicoVideo;LayerMaterials;XiaoPengCheng;End

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    #define _ABEDO_ACHANEL_AO
    //PicoVideo;LightMode;Ernst;Begin
#if defined(_MATRIX_MIX_NORMAL_MR)
    #ifdef _MATRIX_INDEPENDENT_OFFSET_SCALE
    half4 albedoAlpha = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _BaseMap), TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    #else
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    #endif
    
    #if defined(_ABEDO_ACHANEL_ALPHA)
        outSurfaceData.alpha = albedoAlpha.a;
    #else
        outSurfaceData.alpha = 1.0f;
    #endif
    
    #ifdef _MATRIX_ALPHA_MAP
    #ifdef _ALPHAMAP_ON
    outSurfaceData.alpha = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, TRANSFORM_TEX(uv, _AlphaMap)).a * _BaseColor.a;
    #if defined(_ALPHATEST_ON)
    clip(outSurfaceData.alpha - _Cutoff);
    #endif
    #else
    outSurfaceData.alpha = Alpha(outSurfaceData.alpha, _BaseColor, _Cutoff);
    #endif
    #endif

    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;

    #if _NORMALMAP
    #ifdef _MATRIX_INDEPENDENT_OFFSET_SCALE
    half4 normalColor = SAMPLE_TEXTURE2D_BIAS(_BumpMap, sampler_BumpMap, TRANSFORM_TEX(uv, _BumpMap));
    #else
    half4 normalColor = SAMPLE_TEXTURE2D_BIAS(_BumpMap, sampler_BumpMap, uv);
    #endif
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

    #if defined(_ABEDO_ACHANEL_AO)
        outSurfaceData.occlusion = LerpWhiteTo(albedoAlpha.a, _OcclusionStrength);
    #else
        outSurfaceData.occlusion = 1.0;
    #endif

    #ifdef _MATRIX_INDEPENDENT_OFFSET_SCALE
    outSurfaceData.emission = SampleEmission(TRANSFORM_TEX(uv, _EmissionMap), _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
    #else
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
    #endif
    
#else
    //PicoVideo;LightMode;Ernst;End
    
    //PicoVideo;LightMode;YangFan;Begin
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    #ifdef _MATRIX_ALPHA_MAP
        outSurfaceData.alpha = 1.0f;
        #ifdef _ALPHAMAP_ON
            outSurfaceData.alpha = SAMPLE_TEXTURE2D(_AlphaMap, sampler_AlphaMap, TRANSFORM_TEX(uv, _AlphaMap)).a * _BaseColor.a;
            #if defined(_ALPHATEST_ON)
                clip(outSurfaceData.alpha - _Cutoff);
            #endif
        #endif
    #else
        outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);
    #endif
    //PicoVideo;LightMode;YangFan;End
    
    half4 specGloss = SampleMetallicSpecGloss(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;

    #if _SPECULAR_SETUP
    outSurfaceData.metallic = 1.0h;
    outSurfaceData.specular = specGloss.rgb;
    #else
    outSurfaceData.metallic = specGloss.r;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    #endif

    outSurfaceData.smoothness = specGloss.a;
    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
    outSurfaceData.occlusion = SampleOcclusion(uv);
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
#endif

#if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
    half2 clearCoat = SampleClearCoat(uv);
    outSurfaceData.clearCoatMask       = clearCoat.r;
    outSurfaceData.clearCoatSmoothness = clearCoat.g;
#else
    outSurfaceData.clearCoatMask       = 0.0h;
    outSurfaceData.clearCoatSmoothness = 0.0h;
#endif

#if defined(_DETAIL)
    half detailMask = SAMPLE_TEXTURE2D(_DetailMask, sampler_DetailMask, uv).a;
    float2 detailUv = uv * _DetailAlbedoMap_ST.xy + _DetailAlbedoMap_ST.zw;
    outSurfaceData.albedo = ApplyDetailAlbedo(detailUv, outSurfaceData.albedo, detailMask);
    //PicoVideo;LightMode;YangFan;Begin
    #ifdef _MATRIX_INDEPENDENT_OFFSET_SCALE
    float2 detailNormalUv = uv * _DetailNormalMap_ST.xy + _DetailNormalMap_ST.zw;
    outSurfaceData.normalTS = ApplyDetailNormal(detailNormalUv, outSurfaceData.normalTS, detailMask);
    #else
    outSurfaceData.normalTS = ApplyDetailNormal(detailUv, outSurfaceData.normalTS, detailMask);
    #endif
    //PicoVideo;LightMode;YangFan;End
#endif
}
#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
