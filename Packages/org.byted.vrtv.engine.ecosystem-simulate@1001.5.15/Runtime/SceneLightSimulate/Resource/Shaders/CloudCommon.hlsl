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

#ifndef CLOUD_COMMON_INCLUDE
#define CLOUD_COMMON_INCLUDE

// #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/Core/Shaders/Common/EcosystemCommon.hlsl"
uniform float4 _CustomDirectionLightData0;  // x : intensity yzw : direction
uniform float4 _CustomDirectionLightData1;  // xyz : color
uniform float4 _PointCustomLightData0[4];   // x : intensity yzw : position
uniform float4 _PointCustomLightData1[4];   // x : range yzw : color
uniform float _CustomAdditionLightCounts;

Light GetAdditionLightCloud(int index, float3 positionWS)
{
    #ifdef _CLOUD_CUSTOM_LIGHT
    Light light;
    light.color = 0;
    light.direction = 0;
    light.shadowAttenuation = 0;
    light.distanceAttenuation = 0;
    if(index >= _CustomAdditionLightCounts)
    {
        return light;
    }
    light.color = _PointCustomLightData1[index].yzw * _PointCustomLightData0[index].x;
    float3 lightVector = _PointCustomLightData0[index].yzw - positionWS;
    float distanceSqr = max(dot(lightVector, lightVector), HALF_MIN);

    half3 lightDirection = half3(lightVector * rsqrt(distanceSqr));
    
    half lightRangeSqr = 1 / _PointCustomLightData1[index].x;
    half fadeStartDistanceSqr = 0.8f * 0.8f * lightRangeSqr;
    half fadeRangeSqr = (fadeStartDistanceSqr - lightRangeSqr);
    half lightRangeSqrOverFadeRangeSqr = -lightRangeSqr / fadeRangeSqr;
    
    half attenuation = DistanceAttenuation(distanceSqr, half2(_PointCustomLightData1[index].x, lightRangeSqrOverFadeRangeSqr));
    light.direction = lightDirection;
    light.shadowAttenuation = 1;
    light.distanceAttenuation = attenuation;
    return light;
    #else
    return GetAdditionalLight(index, positionWS);
    #endif
}

//LightingFuctions
void AdditionalLightsFloat(float3 specColor, float smoothness, float3 worldPosition, float3 worldNormal, float3 worldView, out float3 diffuse, out float3 specular, out float intensity)
{
    float3 diffuseColor = 0;
    float3 specularColor = 0;
    intensity = 0;
    #ifndef SHADERGRAPH_PREVIEW
    smoothness = exp2(10 * smoothness + 1);
    worldNormal = normalize(worldNormal);
    worldView = SafeNormalize(worldView);
    #ifdef _CLOUD_CUSTOM_LIGHT
    int pixelLightCount = _CustomAdditionLightCounts;
    #else
    int pixelLightCount = GetAdditionalLightsCount();
    #endif
    for (int i = 0; i < pixelLightCount; ++i)
    {
        Light light = GetAdditionLightCloud(i, worldPosition);
        float additionLightDir = saturate((dot(worldNormal.xyz, light.direction.xyz) + 1) / 2);
        intensity += additionLightDir * (length(light.color)) * light.distanceAttenuation;
        half3 attenuatedLightColor = light.color * light.distanceAttenuation;
        diffuseColor += LightingLambert(attenuatedLightColor, light.direction, worldNormal);
        specularColor += LightingSpecular(attenuatedLightColor, light.direction, worldNormal, worldView, float4(specColor, 0), smoothness);
    }
    #endif
    
    diffuse = diffuseColor;
    specular = specularColor;
}

void MainLightFloat(float3 worldPos, out float3 direction, out float3 color, out float distanceAtten, out float shadowAtten)
{
    #ifdef _CLOUD_CUSTOM_LIGHT
    direction = _CustomDirectionLightData0.yzw;
    color = _CustomDirectionLightData1.xyz * _CustomDirectionLightData0.x;
    distanceAtten = 0;
    shadowAtten = 1;
    #else
    
    Light mainLight = GetMainLight();
    direction = mainLight.direction;
    color = mainLight.color;
    distanceAtten = mainLight.distanceAttenuation;
 
    half cascadeIndex = ComputeCascadeIndex(worldPos);
    float4 shadowCoord = mul(_MainLightWorldToShadow[cascadeIndex], float4(worldPos, 1.0));
 
    ShadowSamplingData shadowSamplingData = GetMainLightShadowSamplingData();
    float shadowStrength = GetMainLightShadowStrength();
    shadowAtten = SampleShadowmap(shadowCoord, TEXTURE2D_ARGS(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture), shadowSamplingData, shadowStrength, false);
    
    #endif
}

inline void GetMainLight(float3 positionWS, float3 normalWS, out float intensity, out float3 lightColor, out float3 direction)
{
    float shadowAtten;
    float distance;
    MainLightFloat(positionWS, direction, lightColor, distance, shadowAtten);
    intensity = saturate(dot(normalWS, direction)) * saturate(length(lightColor)) * shadowAtten;
}

inline void GetAdditionLight(float3 positionWS, float3 normalWS, float3 viewDirectionWS, out float intensity, out float3 color)
{
    float3 specular;
    AdditionalLightsFloat(float3(1, 1, 1), 0.5, positionWS, normalWS, viewDirectionWS, color, specular, intensity);
}

inline float3 BlendOverlay(float3 Base, float3 Blend, float Opacity)
{
    float3 result1 = 1.0 - 2.0 * (1.0 - Base) * (1.0 - Blend);
    float3 result2 = 2.0 * Base * Blend;
    float3 zeroOrOne = step(Base, 0.5);
    float3 value = result2 * zeroOrOne + (1 - zeroOrOne) * result1;
    value = lerp(Base, value, Opacity);
    return value;
}

inline float4 FuzzyShading(float4 color, float power, float edgeBrightness, float coreDarkness, float3 normalWS, float3 viewDirectionWS, float lightDir)
{
    float temp = saturate(dot(normalize(viewDirectionWS), normalWS));
    float4 fuzzyColor = (pow(1 - temp, power) * edgeBrightness * lightDir + 1 - temp * coreDarkness * (1 - lightDir)) * color;
    fuzzyColor.a = 1 - pow(1 - temp, power) */* lightDir **/ edgeBrightness;
    return fuzzyColor;
}
#endif