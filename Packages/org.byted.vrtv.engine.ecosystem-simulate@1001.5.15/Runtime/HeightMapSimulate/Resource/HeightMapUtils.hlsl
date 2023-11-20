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
#ifndef _HEIGHT_MAP_UTILS_
#define _HEIGHT_MAP_UTILS_
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
TEXTURE2D(_ESSDynamicHeightMapTexturePoint);
TEXTURE2D(_ESSDynamicHeightMapTexture);
SAMPLER(heightmap_point_clamp_sampler);
SAMPLER(s_linear_repeat_sampler);
uniform float4 _ESSDynamicHeightMapCameraWorldPos;
uniform float _ESSDynamicHeightMapHaxHeight;
uniform float _ESSDynamicHeightMapHeightRange;
uniform float _ESSDynamicHeightMapTextureSize;
uniform float _ESSDynamicHeightMapPixelPreMeter;
uniform float _EnableSnowSurfaceHeightMap;
uniform float _EnableRainSurfaceHeightMap;

half GetHeight(float3 worldPos)
{
    float2 deltaPos = worldPos.xz - _ESSDynamicHeightMapCameraWorldPos.xy;
    deltaPos.xy = deltaPos.xy / _ESSDynamicHeightMapTextureSize;
    float2 uv = float2(0.5f + deltaPos.x, 0.5f + deltaPos.y);
			
    // 在高度图范围内
    if (uv.x > 0 && uv.x < 1 && uv.y > 0 && uv.y < 1)
    {
    	float heightMapInfo = 1 - SAMPLE_TEXTURE2D_LOD(_ESSDynamicHeightMapTexturePoint, heightmap_point_clamp_sampler, uv, 0).x;
    	float height = _ESSDynamicHeightMapHaxHeight - heightMapInfo * _ESSDynamicHeightMapHeightRange;
    	return height;
    }
    else
    {
	    return -100;
    }
}
#endif