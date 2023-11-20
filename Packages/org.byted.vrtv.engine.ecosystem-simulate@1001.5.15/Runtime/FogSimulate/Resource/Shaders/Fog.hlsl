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

#ifndef FOG_COMMON_INCLUDED
#define FOG_COMMON_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

#define UNITY_PI 3.1415926

#define UNITY_CALC_FOG_FACTOR_RAW(coord) float unityFogFactor = (coord) * unity_FogParams.z + unity_FogParams.w

inline float4 FogAndSkyEncodeHDR(float4 col)
{
	return float4(clamp(col.rgb, 0, 4), saturate(col.a));
}

inline float FogAndSkyLuminance(float3 c)
{
    return (c.r + c.g + c.b) * 0.333;
}

// Decodes HDR textures
// handles dLDR, RGBM formats, Compressed(BC6H, BC1), and Uncompressed(RGBAHalf, RGBA32)
inline float3 FogAndSkyDecodeHDR(float4 data, float4 decodeInstructions)
{
    return decodeInstructions.x * data.rgb; // Multiplier for future HDRI relative to absolute conversion.
}

inline float3 RotateAroundYInDegrees(float3 vertex, float degrees)
{
    float alpha = degrees * UNITY_PI / 180.0;
    float sina, cosa;
    sincos(alpha, sina, cosa);
    float2x2 m = float2x2(cosa, -sina, sina, cosa);
    return float3(mul(m, vertex.xz), vertex.y).xzy;
}

uniform float4 _FogHeight;
uniform float4 _FogColorStart;
uniform float4 _FogColorEnd;
uniform float4 _FogDirectionalColor;
uniform float4 _FogDirectionalParam;
uniform float4 _FogNoiseParam0;
uniform float4 _FogNoiseParam1;
uniform float4 _FogLinearParam;
uniform sampler3D _FogNoise3D;

inline float3 BlendDirection(float3 viewDirectionWS, float3 fogColor)
{
	float directionalIntensity = saturate(dot(viewDirectionWS, normalize(_FogDirectionalParam.xyz)));
	fogColor = lerp(fogColor.rgb, _FogDirectionalColor.xyz, pow(directionalIntensity, _FogNoiseParam1.w) * _FogDirectionalParam.w);
	return fogColor;
}

inline float GetFogNoiseFactor(float3 positionWS)
{
	float noise = tex3Dlod(_FogNoise3D, float4(positionWS * _FogNoiseParam1.y + _FogNoiseParam0.xyz * _Time.y, 0)).a;
	float distance = saturate((_FogNoiseParam1.x - length(positionWS - _WorldSpaceCameraPos.xyz)) / _FogNoiseParam1.x);
	return lerp( 1.0,noise, saturate(distance * _FogNoiseParam0.w * _FogNoiseParam1.z));
}

// 场景物件（VF版本）
// 顶点
inline float FogVert(float3 wPos)
{
#if defined(CUSTOM_FOG)
	float unityFogFactor;
	float fogNoise = GetFogNoiseFactor(wPos) * _FogHeight.w;
#if defined(CUSTOM_FOG_FRAGMENT)
	float hFac = (_FogHeight.x - (wPos.y)) * _FogHeight.y;
	unityFogFactor = hFac * fogNoise;
#else
	if(_FogDirectionalColor.w > 0.4)
	{
		float hFac = (_FogHeight.x - (wPos.y)) * _FogHeight.y;
		float z = length(_WorldSpaceCameraPos.xyz - wPos);
		unityFogFactor = 1 - saturate(z * _FogLinearParam.x + _FogLinearParam.y);
		unityFogFactor = lerp(unityFogFactor, hFac * lerp(1, unityFogFactor, step(0.7, _FogDirectionalColor.w)), _FogHeight.z);
		unityFogFactor *= fogNoise;
	}
	else
	{
		float hFac = (lerp(1, ((_FogHeight.x - (wPos.y)) * _FogHeight.y), _FogHeight.z));
		hFac = lerp(hFac, saturate(hFac), step(0.2, _FogDirectionalColor.w));
		float z = length(_WorldSpaceCameraPos.xyz - wPos) * hFac;
		unityFogFactor = 1 - (z * _FogLinearParam.x + _FogLinearParam.y);
		unityFogFactor *= fogNoise;
	}
#endif
	return unityFogFactor;
#else
	return 0;
#endif
}

// 像素
inline float3 FogFrag(float3 col, float3 viewDirectionWS, float3 positionWS, float fogFac)
{
#if defined(CUSTOM_FOG)
#if defined(CUSTOM_FOG_FRAGMENT)
	float fogHeightFactor0 = saturate(lerp(1, fogFac, _FogHeight.z));
	float z = length(_WorldSpaceCameraPos.xyz - positionWS);
	float depthFogFactorOrigin = 1 - saturate(z * _FogLinearParam.x + _FogLinearParam.y);
	float depthFogFactor = 1 - (z * fogHeightFactor0 * _FogLinearParam.x + _FogLinearParam.y);
	fogFac = lerp(depthFogFactor, saturate(fogFac) * depthFogFactorOrigin, step(0.7, _FogDirectionalColor.w));
#else
	fogFac = saturate(fogFac);
#endif
	float3 fogColor = lerp(_FogColorStart, _FogColorEnd, fogFac).xyz;
	return lerp(col.rgb, BlendDirection(-viewDirectionWS, fogColor), saturate(fogFac));
#else
	return col;
#endif
}

// 像素, 雾色由外部控制，比如有的特效需乘以alpha
float3 FogFrag(float3 col, float fogFac, float3 fogColor)
{
#if defined(CUSTOM_FOG)
	return lerp(fogColor.rgb, col.rgb, saturate(fogFac));
#else
	return col;
#endif
}

// 只和深度有关和摄像机距离无关的，暂时没用到
float FogVertDepth(float3 cPos, float3 wPos) 
{
	float waveH = _FogHeight.w * (cos(_Time.x * 40 + (wPos.x + wPos.z) * 0.01)); 
	float hFac = lerp(1, saturate((_FogHeight.x - 0.5 * (_WorldSpaceCameraPos.y + wPos.y  + waveH)) * _FogHeight.y), _FogHeight.z); 
	UNITY_CALC_FOG_FACTOR_RAW(UNITY_Z_0_FAR_FROM_CLIPSPACE((cPos).z) * hFac); 
	return saturate(unityFogFactor);
}
#endif 