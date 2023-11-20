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

#ifndef MATRIX_WATER_BASE_INCLUDED
#define MATRIX_WATER_BASE_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/PBR/PBRLighting.hlsl"

inline half CalculateFresnelTerm(half3 normalWS, half3 viewDirectionWS)
{
    return 1.0 - dot(normalWS, viewDirectionWS);
}

inline float2 GetOfflineWorldUV(float3 positionWS)
{
    return (positionWS.xz - (_OfflineTextureWorldOrgin.xz - _OfflineTextureCameraSettings.xx)) / (2 * _OfflineTextureCameraSettings.xx);
}

inline float GetRealtimeWaterEyeDepth(float4 projection)
{
    float depth = projection.z / projection.w;
    #if !UNITY_REVERSED_Z
        depth = depth * 0.5 + 0.5;
    #endif
    return LinearEyeDepth(depth, _ZBufferParams);
}

inline float GetOfflineWaterEyeDepth(float3 positionWS)
{
    float eyeDepth = _OfflineTextureWorldOrgin.y - positionWS.y;//离线相机从上往下拍摄
    float n = _OfflineTextureCameraSettings.y;
    float f = _OfflineTextureCameraSettings.z;

    eyeDepth = clamp(eyeDepth, n, f);
    return eyeDepth;
}

inline float GetSceneEyeDepth(float2 uv)
{
#if _REALTIME_DEPTH_MAP
    float depth = SampleSceneDepth(uv);
    depth = LinearEyeDepth(depth, _ZBufferParams);
#else
    float depth = SAMPLE_TEXTURE2D_X(_OfflineDepthMap, sampler_OfflineDepthMap, uv).r;//offline tool has fixed UNITY_REVERSED_Z
    float n = _OfflineTextureCameraSettings.y;
    float f = _OfflineTextureCameraSettings.z;
    depth = n + (f - n) * depth;
#endif

    return depth;
}

inline half3 GetCausticColor(float2 uv, float sceneDeltaDepth)
{
#if _CAUSTICS
    uv = TRANSFORM_TEX(uv, _CausticsMaskMap);
    half weight = saturate(1 - smoothstep(_WaterDepthAdd, _CausticsDepthCutOff + _WaterDepthAdd, sceneDeltaDepth));
    half3 causticsMask1 = SAMPLE_TEXTURE2D(_CausticsMaskMap, sampler_CausticsMaskMap, uv + _Time.y * _CausticsSpeed * 0.1).rgb;
    half3 causticsMask2 = SAMPLE_TEXTURE2D(_CausticsMaskMap, sampler_CausticsMaskMap, uv - _Time.y * _CausticsSpeed * 0.1).rgb;
    return weight * _CausticsColor * _CausticsIntensity * min(causticsMask1, causticsMask2) * 10;
#else
    return half3(0, 0, 0);
#endif
}

inline half3 GetRefractionColor(float2 uv, float2 noise, float sceneDeltaDepth)
{
    half weight = saturate(1 - smoothstep(_WaterDepthAdd, _RefractionDepthCutOff + _WaterDepthAdd, sceneDeltaDepth));
    uv += noise * _RefractionNoiseIntensity * 0.1;

#if _REALTIME_REFRACTION
    half3 sceneColor = SAMPLE_TEXTURE2D_X(_CameraTransparentTexture, sampler_CameraTransparentTexture, UnityStereoTransformScreenSpaceTex(uv)).rgb;
#else
    half3 sceneColor = SAMPLE_TEXTURE2D_LOD(_OfflineRefractionMap, sampler_OfflineRefractionMap, uv, sceneDeltaDepth * 10 * _OfflineRefractionLodMultiple).rgb;
#endif

    return weight * sceneColor * _RefractionColor * _RefractionIntensity;
}

inline half3 GetCubemapReflection(half3 reflectVector)
{
#if _CUBEMAP_REFLECTION
    return SAMPLE_TEXTURECUBE_LOD(_ReflectionCubeMap, sampler_ReflectionCubeMap, reflectVector, _ReflectionCubemapLod).rgb * _ReflectionCubemapColor * _ReflectionCubemapIntensity;
#else
    return half3(0, 0, 0);
#endif
}

inline half3 GetProbeReflection(half3 reflectVector, float3 positionWS, half smoothness, half occlusion)
{
#if _PROBE_REFLECTION
    reflectVector = BoxProjectedCubemapDirection(reflectVector, positionWS, unity_SpecCube0_ProbePosition, unity_SpecCube0_BoxMin, unity_SpecCube0_BoxMax);
    half mip = PerceptualRoughnessToMipmapLevel(PerceptualSmoothnessToPerceptualRoughness(smoothness));
    half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip);

#if defined(UNITY_USE_NATIVE_HDR)
    half3 irradiance = encodedIrradiance.rgb;
#else
    half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
#endif

    return irradiance * occlusion * _ProbeReflectionColor * _ProbeReflectionIntensity;
#else
    return _GlossyEnvironmentColor.rgb * occlusion;
#endif
}

inline half3 GetFoamColor(float2 uv, float2 UVPanner1, float2 UVPanner2, float flowLerp, float sceneDeltaDepth)
{
#if _FOAM
    uv = TRANSFORM_TEX(uv, _FoamMap);
    half weight = 1 - smoothstep(_WaterDepthAdd, _FoamThickness + _WaterDepthAdd, sceneDeltaDepth);

#if _FLOWMAP
    half foamWeight11 = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, uv + UVPanner1 * _FoamSpeed1).r;
    half foamWeight12 = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, uv + UVPanner1 * _FoamSpeed2).r;
    half foamWeight1 = saturate(2 * foamWeight11 + foamWeight12 - 1);

    half foamWeight21 = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, uv + UVPanner2 * _FoamSpeed1).r;
    half foamWeight22 = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, uv + UVPanner2 * _FoamSpeed2).r;
    half foamWeight2 = saturate(2 * foamWeight21 + foamWeight22 - 1);

    half foamWeight = lerp(foamWeight1, foamWeight2, flowLerp);
#else
    half foamWeight1 = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, uv + UVPanner1 * _FoamSpeed1).r;
    half foamWeight2 = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap, uv + UVPanner2 * _FoamSpeed2).r;
    half foamWeight = saturate(2 * foamWeight1 + foamWeight2 - 1);
#endif

    return _FoamColor * weight * foamWeight * _FoamIntensity;
#else
    return half3(0, 0, 0);
#endif
}

inline half3 GetSSSColor(Light light, half3 viewDirectionWS, half3 normalWS, float sceneDeltaDepth)
{
#if _SSS
    half3 sssLightDirection = normalize(light.direction + normalWS * _SSSLightDistortion);
    half sss = pow(saturate(dot(viewDirectionWS, sssLightDirection)), _SSSLightPower) * _SSSLightIntensity;
    half weight = 1 - smoothstep(_WaterDepthAdd, _SSSLighDepthCutOff + _WaterDepthAdd, sceneDeltaDepth);

    return _SSSColor * light.color * sss * weight;
#else
    return half3(0, 0, 0);
#endif
}

#endif