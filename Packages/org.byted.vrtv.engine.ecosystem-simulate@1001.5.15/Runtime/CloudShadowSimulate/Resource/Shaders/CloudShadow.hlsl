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

#ifndef CLOUD_SHADOW_INCLUDED
#define CLOUD_SHADOW_INCLUDED

#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/VegetationSimulate/Resource/Shaders/Vegetation.hlsl"

TEXTURE2D(_CloudShadowMap);           SAMPLER(sampler_CloudShadowMap);

half3 _CloudShadowColor;

float4 _FirstCloudShadowMapTiling;
float _FirstCloudShadowIntensity;
float2 _FirstCloudShadowDirection;
float _FirstCloudShadowUseWindDirection;
float4 _FirstCloudShadowSpeed;
float _FirstCloudShadowNoiseIntensity;

float4 _SecondCloudShadowMapTiling;
float _SecondCloudShadowIntensity;
float2 _SecondCloudShadowDirection;
float _SecondCloudShadowUseWindDirection;
float4 _SecondCloudShadowSpeed;
float _SecondCloudShadowNoiseIntensity;

float GetFirstGlobalCloudShadowIntensity(float3 positionWS)
{
    float2 direction = lerp(_FirstCloudShadowDirection, _VegetationWindDirection, _FirstCloudShadowUseWindDirection);
    float2x2 rotateMatrix = float2x2(direction.y, -direction.x, direction.x, direction.y);

    float2 worldUV = mul(positionWS.xz, rotateMatrix);
    float2 cloudUV = _Time.x * _FirstCloudShadowSpeed.xy + worldUV * _FirstCloudShadowMapTiling.xy;
    float intensity = SAMPLE_TEXTURE2D(_CloudShadowMap, sampler_CloudShadowMap, cloudUV).r;
    float2 cloudNoiseUV = _Time.x * _FirstCloudShadowSpeed.zw + worldUV * _FirstCloudShadowMapTiling.xy * _FirstCloudShadowMapTiling.zw;
    float noiseIntensity = SAMPLE_TEXTURE2D(_CloudShadowMap, sampler_CloudShadowMap, cloudNoiseUV).g;

    intensity = 2 * intensity + noiseIntensity * _FirstCloudShadowNoiseIntensity - 1;

    return lerp(0, clamp(intensity, 0, 1), _FirstCloudShadowIntensity);
}

float GetSecondGlobalCloudShadowIntensity(float3 positionWS)
{
    float2 direction = lerp(_SecondCloudShadowDirection, _VegetationWindDirection, _SecondCloudShadowUseWindDirection);
    float2x2 rotateMatrix = float2x2(direction.y, -direction.x, direction.x, direction.y);

    float2 worldUV = mul(positionWS.xz, rotateMatrix);
    float2 cloudUV = _Time.x * _SecondCloudShadowSpeed.xy + worldUV * _SecondCloudShadowMapTiling.xy;
    float intensity = SAMPLE_TEXTURE2D(_CloudShadowMap, sampler_CloudShadowMap, cloudUV).b;
    float2 cloudNoiseUV =_Time.x * _SecondCloudShadowSpeed.zw + worldUV * _SecondCloudShadowMapTiling.xy * _SecondCloudShadowMapTiling.zw;
    float noiseIntensity = SAMPLE_TEXTURE2D(_CloudShadowMap, sampler_CloudShadowMap, cloudNoiseUV).a;

    intensity = 2 * intensity + noiseIntensity * _SecondCloudShadowNoiseIntensity - 1;

    return lerp(0, clamp(intensity, 0, 1), _SecondCloudShadowIntensity);
}

inline half3 ApplyGlobalCloudShadow(half3 baseColor, float3 positionWS, half intensity = 1.0f)
{
    half firstCloudShadowIntensity = GetFirstGlobalCloudShadowIntensity(positionWS);
    half secondCloudShadowIntensity = GetSecondGlobalCloudShadowIntensity(positionWS);

    baseColor *= lerp(half3(1, 1, 1), _CloudShadowColor, intensity * max(firstCloudShadowIntensity, secondCloudShadowIntensity));
    return baseColor;
}

#endif