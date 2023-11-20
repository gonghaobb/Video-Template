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

#ifndef VEGETATION_COMMON
#define VEGETATION_COMMON

// #pragma shader_feature_local _ _VEGETATION_LEAVES _VEGETATION_TRUNK
// #pragma shader_feature_local _ _VEGETATION_GRASS

uniform half _VegetationWindSpeed;
uniform half _VegetationWindPower;
uniform half _VegetationWindBurstsSpeed;
uniform half _VegetationWindBurstsScale;
uniform half _VegetationWindBurstsPower;
uniform half _VegetationWindMicroFrequency;
uniform half _VegetationWindMicroSpeed;
uniform half _VegetationWindMicroPower;
uniform half2 _VegetationWindDirection;
uniform sampler2D _ColorVariationNoise;
uniform sampler2D _GrassColorNoise;

uniform float4 _VegetationSceneCaptureParams;
uniform sampler2D _VegetationSceneCaptureGroundColor;

#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/Core/Shaders/Common/EcosystemCommon.hlsl"

#if defined(_VEGETATION_GRASS)
inline void VegetationVertexGrass(float3 positionWS, half3 vertexColor, half heightOS, out half3 offset)
{
	float time = _Time.y % 3600;
	half noise = Snoise(0.5 * time * _VegetationWindMicroSpeed + positionWS.xz);
	noise = noise*0.5 + 0.5;
	float4 microWind = ((float4((((sin((positionWS.xyz + noise) * (_VegetationWindMicroFrequency )) * heightOS) * _VegetationWindMicroPower) * vertexColor.r), 0.0) * half4(12,3.6,1,1)) * 0.01);
	offset = microWind.xyz * _WindMultiplier;
}

inline void VegetationVertexGrassColorLerp(half3 positionWS, half2 uv, half3 albedo, out half3 outAlbedo)
{
	outAlbedo = lerp(albedo, _GrassGradientColor.xyz, (_GrassColorVariationPower * tex2Dlod(_GrassColorNoise, half4((positionWS.xz * _GrassColorNoise_ST.xy + _GrassColorNoise_ST.zw), 0, 0)).r));
	half rate = lerp(2 * uv.y * _GrassHeightFalloff, -2 * (1 - uv.y) * _GrassHeightFalloff, step(_GrassHeightFalloff, 0));
	outAlbedo = lerp(outAlbedo, _GrassHeightColor.xyz, rate);
}

half3 VegetationGrassGroundColor(half3 col, half2 uv, float3 positionWS)
{
	half4 groundColor;
	half2 uvCapture = (positionWS.xz - _VegetationSceneCaptureParams.xy) / _VegetationSceneCaptureParams.z + 0.5;
	groundColor.xyz = tex2Dlod(_VegetationSceneCaptureGroundColor, half4(uvCapture,0,0)).xyz;
	groundColor.w = _VegetationSceneCaptureParams.w;

	return lerp(col, groundColor.xyz, saturate(groundColor.w * (2 - uv.y)));
}

#elif defined(_VEGETATION_LEAVES) || defined(_VEGETATION_TRUNK)
inline void VegetationVertex(half3 positionWS, half4 vertexColor, half3 normal, out half3 offset)
{
	float time = _Time.y % 3600;
    half windSpeed = time * _VegetationWindSpeed;
	half2 positionUV = time * _VegetationWindBurstsSpeed + positionWS.xz;

	#ifdef _VEGETATION_LEAVES
		half windburstsScale = _VegetationWindBurstsScale / 10.0;
	#else
		half windburstsScale = _VegetationWindBurstsScale / 100.0;
	#endif
	
	half noise = Snoise(positionUV * windburstsScale);
	noise = noise * 0.5 + 0.5;
	half windPower = _VegetationWindPower * (noise * _VegetationWindBurstsPower);
	//顶点色b通道表示受基础风影响强度
	half vertexWindScale = pow(abs(1.0 - vertexColor.b), _WindTrunkPosition);
	half4 windScale = saturate((0.5 * _WindTrunkContrast + 0.5) * vertexWindScale.xxxx);
	half3 windOffset = half3(((-sin(windSpeed) * windPower * 0.2) * windScale).r, 0.0, ((cos(windSpeed) * (windPower)) * windScale).r);
	half2x2 rotateMatrix = half2x2(_VegetationWindDirection.y, -_VegetationWindDirection.x, _VegetationWindDirection.x, _VegetationWindDirection.y);
	windOffset.xz = mul(windOffset.xz, rotateMatrix);

	#ifdef _VEGETATION_LEAVES
		half noiseMicro = noise * _VegetationWindMicroSpeed * 5;
		half3 windScaleMicro = clamp(sin((_VegetationWindMicroFrequency * (positionWS + noiseMicro))), -1, 1);
		//顶点色r通道表示受扰动风影响强度
		half3 windOffsetMicro = (((windScaleMicro * normal.xyz) * _VegetationWindMicroPower) * vertexColor.r) * _MicroWindMultiplier;
		half3 baseWindOffsetOS = mul(unity_WorldToObject, half4(windOffset + windOffsetMicro, 0.0)).xyz * _WindMultiplier;
		offset = baseWindOffsetOS;
	#else
		half3 baseWindOffsetOS = mul(unity_WorldToObject, half4(windOffset, 0.0)).xyz * _WindMultiplier;
		offset = baseWindOffsetOS;
	#endif
}

inline void VegetationVertColorClip(half3 positionWS, half3 viewDirWS, half3 normalWS, out half3 colorLerpAndClip)
{
	colorLerpAndClip = 1;
	
#ifdef _VEGETATION_LEAVES
	normalWS.y = _InvertGradient > 0.5 ? (1 - normalWS.y) : normalWS.y;
	half normalFactor = clamp(((normalWS.y + (-2.0 + _GradientPosition * 3.0)) / _GradientFalloff) , 0.0, 1.0);
	half3 gradientColor = lerp(_BaseColor.xyz, _GradientColor.xyz, normalFactor);
	half3 lerpBlendMode = lerp(_ColorVariation.xyz, (_ColorVariation.xyz / max(1.0 - gradientColor, 0.00001)), _ColorVariationPower);
	half colorRate = (_ColorVariationPower * pow(tex2Dlod(_ColorVariationNoise, half4(positionWS.xz * (_NoiseScale / 100.0), 0, 0)).xyz, 3).r);
	colorLerpAndClip.xyz = lerp(gradientColor, (saturate(lerpBlendMode)), colorRate);

// #if _VEGETATION_HIDE_SIDE_VERTEX
// 	half normalResult = dot(normalize(viewDirWS), normalize(normalWS));
// 	normalResult = saturate(pow((1 - normalResult) * abs(_VegetationHideFalloff), abs(_VegetationHidePower)));
// 	colorLerpAndClip.w = normalResult;
// #else
//	colorLerpAndClip.w = 1;
// #endif
#endif
}

// inline half VegetationClip(float3 positionWS, half4 positionCS, half3 viewDirWS, half2 screenParams)
// {
// 	#ifdef _VEGETATION_HIDE_SIDE_FRAGMENT
// 	half2 clipUV = fmod(positionCS.xy, 8);
// 	half dither = Dither8x8Bayer(clipUV.x, clipUV.y);
// 	half dotResult = dot(normalize(viewDirWS), normalize(cross(ddy(positionWS) * 100, ddx(positionWS) * 100)));
// 	half fadeRate = clamp((((1.0 - ((1.0 - abs(dotResult)) * 2.0))) * _VegetationHidePower), 0.0, 1.0);
// 	dither = step(dither, fadeRate);
// 	return dither;
// 	#else
// 	return 1;
// 	#endif
// }
#endif
#endif
