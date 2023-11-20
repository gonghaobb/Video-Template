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

#ifndef RAIN_SURFACE_INCLUDE
#define RAIN_SURFACE_INCLUDE  
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/HeightMapSimulate/Resource/HeightMapUtils.hlsl"

// 全局常量缓冲区
// TEXTURE2D(_RainSurfaceWaterMaskTex);
TEXTURE2D(_RainSurfaceWaveTex);
TEXTURE2D(_RainSurfaceCombineTex);
uniform half4 _RainSurfaceWaterMaskTex_Pos_Size;
uniform half2 _RainSurfaceWaveTex_Scale;
uniform half _GlobalRainSurface;  
uniform half _RainSurfaceColorDump;
uniform half _RainSurfaceMetallicDump;
uniform half _RainHumidnessStrength;
uniform half4 _RainSurfaceUVSlice;
uniform half _RainSurfaceGlobalReflectionStrength;
uniform half _RainSurfaceNonMaskStrength;
uniform half _RainSurfaceFresnelPow;
TEXTURECUBE(_RainSurfaceReflectionMap);
SAMPLER(sampler_RainSurfaceReflectionMap);

void ComputeWetSurface(half3 normalWS, float3 worldPos, inout half3 bumpNormal, inout half metallic, inout half smoothness, inout half3 albedo, out half totalAccumulated)
{
	// 波纹UV改为映射世界坐标
	float2 worldUv = (worldPos.xz - _RainSurfaceWaterMaskTex_Pos_Size.xy) / _RainSurfaceWaterMaskTex_Pos_Size.zw;
	worldUv += 0.5;
	half2 waveUV = worldUv * _RainSurfaceWaveTex_Scale;
	half4 maskAndHeight = SAMPLE_TEXTURE2D(_RainSurfaceCombineTex, s_linear_repeat_sampler, worldUv);
	half mask = maskAndHeight.r;

	half heightFactor = 1;
	half heightMapInfo = 1 - maskAndHeight.b * _EnableRainSurfaceHeightMap;
	half height = _ESSDynamicHeightMapHaxHeight - heightMapInfo * _ESSDynamicHeightMapHeightRange;
	
	half fac = clamp((height - worldPos.y), 0, 6) * 0.16667;
	heightFactor -= fac;
	
	// 先处理全局潮湿效果
	albedo *= lerp(1, 1 - _RainSurfaceColorDump, _GlobalRainSurface * heightFactor);
	metallic = lerp(metallic, max(_RainSurfaceMetallicDump, metallic), _GlobalRainSurface * heightFactor);
	smoothness = lerp(smoothness, max(_RainSurfaceMetallicDump, smoothness), _GlobalRainSurface * heightFactor);

	// 积水区域遮罩图
	half accumulatedWater = min(_GlobalRainSurface, mask);

	accumulatedWater = (accumulatedWater * (1 - _RainSurfaceNonMaskStrength) + _RainSurfaceNonMaskStrength) * heightFactor;

	// 表面涟漪和波纹
	float2 rainUV= (frac(waveUV) + _RainSurfaceUVSlice.xy) * _RainSurfaceUVSlice.zw;
	half3 distuNormal = UnpackNormal(SAMPLE_TEXTURE2D(_RainSurfaceWaveTex, s_linear_repeat_sampler, rainUV)).rgb;
	
	half faceup = saturate(dot(normalWS, half3(0, 1, 0)) - (1 - _GlobalRainSurface));
	accumulatedWater *= faceup;
	distuNormal = lerp(bumpNormal, distuNormal, accumulatedWater * _RainHumidnessStrength);
	bumpNormal = distuNormal;
	totalAccumulated = accumulatedWater;
}

half3 ComputeWetSurfaceReflection(half3 surfaceNormalWS, half3 viewDir, half accumulated)
{
	// 波纹法线转换到世界空间，采样cubemap
	half3 reflectDir = reflect(-viewDir, surfaceNormalWS);
	half fresnel = pow(1 - dot(normalize(viewDir), normalize(surfaceNormalWS)), abs(_RainSurfaceFresnelPow));
	half3 reflectColor = SAMPLE_TEXTURECUBE(_RainSurfaceReflectionMap, sampler_RainSurfaceReflectionMap, reflectDir).xyz;
	half3 reflect = reflectColor * accumulated * _RainSurfaceGlobalReflectionStrength;
	reflect = lerp(0, reflect, accumulated);
	return reflect * fresnel;
}

// 兼容旧版
half3 ComputeWetSurface(half2 uv0, half3 normalWS, half3 worldPos, half3 viewDir, half4 tangentWS, inout half3 bumpNormal, inout half metallic, inout half smoothness)
{
	return 0;
}
#endif