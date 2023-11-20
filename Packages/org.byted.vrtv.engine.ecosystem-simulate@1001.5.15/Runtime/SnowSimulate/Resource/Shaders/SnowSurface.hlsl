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
#ifndef SNOW_SURFACE_INCLUDE
#define SNOW_SURFACE_INCLUDE
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/HeightMapSimulate/Resource/HeightMapUtils.hlsl"
TEXTURE2D(_SnowSurfaceTex);
TEXTURE2D(_SnowSurfaceNormal);
TEXTURE2D(_SnowSurfaceMaskTex);
TEXTURE2D(_SnowSurfaceNoiseTex);
TEXTURE2D(_SnowSurfaceCombineTex);
uniform half4 _SnowMaskColor;
uniform half4 _SnowSurfaceTex_Pos_Size;
uniform half2 _SnowSurfaceTex_Scale;
uniform half _SnowNormalRatio;
uniform half _SnowAORatio;
uniform half _EnableSnowDetailTex;
uniform half _GlobalSnowAmount;
uniform half _GlobalSnowStrength;

half3 ComputeSnowSurface(half3 mainTexColor, float3 worldPos, half3 normalWS, inout half3 bumpNormal, out half totalAccumulated, out half snowTexData)
{
	UNITY_BRANCH
	if(_GlobalSnowStrength < 0.01f)
	{
		snowTexData = 1;
		totalAccumulated = 0;
		return mainTexColor;
	}
    float2 worldUv;
    half2 snowUV;
	worldUv = (worldPos.xz - _SnowSurfaceTex_Pos_Size.xy) / _SnowSurfaceTex_Pos_Size.zw;
	worldUv += 0.5;
	snowUV = worldUv * _SnowSurfaceTex_Scale;
	half4 maskAndHeight = SAMPLE_TEXTURE2D(_SnowSurfaceCombineTex, s_linear_repeat_sampler, worldUv);
	half mask = maskAndHeight.g;
	half heightFactor = 1;
	
	half heightMapInfo = 1 - maskAndHeight.b * _EnableSnowSurfaceHeightMap;
	half height = _ESSDynamicHeightMapHaxHeight - heightMapInfo * _ESSDynamicHeightMapHeightRange;
		
	half fac = clamp((height - worldPos.y), 0, 6) * 0.16667;
	heightFactor -= fac * step(worldPos.y + 0.5, height.x);
	
	float accumulated = _GlobalSnowAmount * mask * _GlobalSnowStrength * heightFactor;
	totalAccumulated = accumulated;
    half c = SAMPLE_TEXTURE2D(_SnowSurfaceTex, s_linear_repeat_sampler, snowUV).x;
	snowTexData = c;
    #ifdef _NORMALMAP
    half3 snowNormal = UnpackNormal(SAMPLE_TEXTURE2D(_SnowSurfaceNormal, s_linear_repeat_sampler, snowUV));
	snowNormal = lerp(half3(0,0,1), snowNormal, _EnableSnowDetailTex);
    bumpNormal = lerp(bumpNormal, snowNormal, saturate(normalWS.y * accumulated * _SnowNormalRatio));
	return mainTexColor;
    #else
    return mainTexColor;
    #endif
}

void ComputeSnowSurfaceColorAndAO(half snowTexData, half accumulated, half3 surfaceNormalWS, inout half ao, inout half3 albedo)
{
	albedo = lerp(albedo, snowTexData * _SnowMaskColor.rgb, saturate(surfaceNormalWS.y) * accumulated);
	ao = lerp(ao, 1.0,  saturate((saturate(surfaceNormalWS.y) * accumulated) * _SnowAORatio));
}

//兼容旧版
half3 ComputeSnowSurface(half3 mainTexColor, half2 uv0, float3 worldPos, half3 normalWS, half4 tangentWS, inout half3 bumpNormal, half needMask)
{
	return mainTexColor;
}

#endif