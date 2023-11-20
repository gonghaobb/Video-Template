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

#ifndef MATRIX_WATER_INPUT_INCLUDED
#define MATRIX_WATER_INPUT_INCLUDED

#if defined(_NORMALMAP) || defined(_ENVIRONMENT_CUBEMAP) || defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE)
    #define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

#if defined(_BLING_PHONG) && !defined(_SPECULARHIGHLIGHTS_OFF)
    #define _SPECULAR_COLOR
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

#if _NORMALMAP
    TEXTURE2D(_NormalMap);                  SAMPLER(sampler_NormalMap);
#endif

#if _FLOWMAP
    TEXTURE2D(_FlowMap);                    SAMPLER(sampler_FlowMap);
#endif

#if _CUBEMAP_REFLECTION
    TEXTURECUBE(_ReflectionCubeMap);	    SAMPLER(sampler_ReflectionCubeMap);
#endif

#if _REALTIME_DEPTH_MAP
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#else
    TEXTURE2D(_OfflineDepthMap);             SAMPLER(sampler_OfflineDepthMap);
#endif

#if defined(_REALTIME_REFRACTION)
    TEXTURE2D_X(_CameraTransparentTexture);  SAMPLER(sampler_CameraTransparentTexture);
#else
    TEXTURE2D(_OfflineRefractionMap);        SAMPLER(sampler_OfflineRefractionMap);
#endif

#if _CAUSTICS
    TEXTURE2D(_CausticsMaskMap);             SAMPLER(sampler_CausticsMaskMap);
#endif

#if _FOAM
    TEXTURE2D(_FoamMap);                     SAMPLER(sampler_FoamMap);
#endif

CBUFFER_START(UnityPerMaterial)
half _Cutoff;
half3 _BlingPhongSpecColor;

half4 _ShallowBaseColor;
half4 _DeepBaseColor;
half _WaterDepthMultiple;
half _WaterDepthAdd;
half _ShallowDepthCutOff;
half _TransparentDepthCutOff;
half _TransparentAdd;
half _OutputScale;

half4 _NormalMap_ST;
half3 _NormalScaleSpeed1;
half3 _NormalScaleSpeed2;
half _Smoothness;
half _Metallic;

half _FlowDirSpeed;
half _FlowTimeSpeed;
half _FlowNormalScale;

float _MaxWaterDepthDiff;
float3 _OfflineTextureWorldOrgin;
float4 _OfflineTextureCameraSettings;

half3 _RefractionColor;
half _OfflineRefractionLodMultiple;
half _RefractionNoiseIntensity;
half _RefractionDepthCutOff;
half _RefractionIntensity;

half _ReflectionCubemapLod;
half3 _ReflectionCubemapColor;
half _ReflectionCubemapIntensity;
half3 _ProbeReflectionColor;
half _ProbeReflectionIntensity;

half4 _CausticsMaskMap_ST;
half3 _CausticsColor;
half _CausticsSpeed;
half _CausticsDepthCutOff;
half _CausticsIntensity;

half4 _FoamMap_ST;
half3 _FoamColor;
half _FoamSpeed1;
half _FoamSpeed2;
half _FoamThickness;
half _FoamIntensity;

half3 _SSSColor;
half _SSSLightDistortion;
half _SSSLightPower;
half _SSSLighDepthCutOff;
half _SSSLightIntensity;

half _CloudShadowIntensity;
half _AdjustColorIntensity;
half _CustomSpecularIntensity;
CBUFFER_END

inline void InitializeWaterSurfaceData(float2 uv, float sceneDeltaDepth, out float2 UVPanner1, out float2 UVPanner2, out float flowLerp, out SurfaceData outSurfaceData)
{
    outSurfaceData = (SurfaceData)0;
    flowLerp = 0.0f;

    half4 baseColor = lerp(_ShallowBaseColor, _DeepBaseColor, smoothstep(_WaterDepthAdd, _ShallowDepthCutOff + _WaterDepthAdd, _WaterDepthMultiple * sceneDeltaDepth));
    outSurfaceData.alpha = baseColor.a;
    outSurfaceData.albedo = baseColor.rgb;

#if _NORMALMAP
    float2 baseNormalUV = TRANSFORM_TEX(uv, _NormalMap);

#if _FLOWMAP
    float2 flowDir = (SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, uv).rg * 2.0f - 1.0f) * (-_FlowDirSpeed);
    float phase0 = frac(_Time.y * _FlowTimeSpeed);
    float phase1 = frac(_Time.y * _FlowTimeSpeed + 0.5f);
    UVPanner1 = flowDir * phase0;
    UVPanner2 = flowDir * phase1;
#else
    UVPanner1 = _Time.y * 0.1f * _NormalScaleSpeed1.yz;
    UVPanner2 = _Time.y * 0.1f * _NormalScaleSpeed2.yz;
#endif

    half4 normalColor1 = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, baseNormalUV + UVPanner1);
    half4 normalColor2 = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, baseNormalUV + UVPanner2);

#if _FLOWMAP
    flowLerp = abs(phase0 - 0.5f) / 0.5f;
    half4 normalColor = lerp(normalColor1, normalColor2, flowLerp);
    outSurfaceData.normalTS = UnpackNormalRG(normalColor, _FlowNormalScale);
    outSurfaceData.metallic = normalColor.b * _Metallic;
    outSurfaceData.smoothness = normalColor.a * _Smoothness;
#else
    half3 normalTS1 = UnpackNormalRG(normalColor1, _NormalScaleSpeed1.x);
    half3 normalTS2 = UnpackNormalRG(normalColor2, _NormalScaleSpeed2.x);
    outSurfaceData.normalTS = BlendNormal(normalTS1, normalTS2);
    outSurfaceData.metallic = (normalColor1.b + normalColor2.b) * 0.5 * _Metallic;//金属度和粗糙度简单平均
    outSurfaceData.smoothness = (normalColor1.a + normalColor2.a) * 0.5 * _Smoothness;
#endif

#else
    UVPanner1 = float2(0, 0);
    UVPanner2 = float2(0, 0);
    outSurfaceData.normalTS = half3(0.0h, 0.0h, 1.0h);
	outSurfaceData.metallic = _Metallic;
    outSurfaceData.smoothness = _Smoothness;
#endif

	outSurfaceData.occlusion = 1;
}

#endif