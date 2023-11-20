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

#ifndef MATRIX_TERRAIN_LIT_INPUT_INCLUDED
#define MATRIX_TERRAIN_LIT_INPUT_INCLUDED

#if _FIRST_NORMALMAP || (_SECOND_LAYER_BLEND && _SECOND_NORMALMAP) || (_THIRD_LAYER_BLEND && _THIRD_NORMALMAP) || (_FOUR_LAYER_BLEND && _FOUR_NORMALMAP)
	#define _NORMALMAP
#endif

#if defined(_NORMALMAP) || defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE)
	#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

#if defined(_BLING_PHONG) && !defined(_SPECULARHIGHLIGHTS_OFF)
	#define _SPECULAR_COLOR
#endif

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
half _Surface;
half _Cutoff;
half3 _BlingPhongSpecColor;
half _UV2Weight;

half4 _ControlMap_ST;
half _FirstLayerWeight;
half _SecondLayerWeight;
half _ThirdLayerWeight;
half _FourLayerWeight;

half4 _FirsLayerTilingOffset;
half4 _SecondLayerTilingOffset;
half4 _ThirdLayerTilingOffset;
half4 _FourLayerTilingOffset;
half _FirstLayerHeight;
half _SecondLayerHeight;
half _ThirdLayerHeight;
half _FourLayerHeight;
half _HeightBlendFactor;

half4 _BaseMap_ST;
half4 _BaseColor;
half4 _FirstAlphaMap_ST;
half4 _FirstNormalMap_ST;
half _FirstNormalScale;
half _FirstSmoothness;
half _FirstMetallic;
half _FirstOcclusionStrength;

half4 _SecondBaseMap_ST;
half4 _SecondBaseColor;
half4 _SecondAlphaMap_ST;
half4 _SecondNormalMap_ST;
half _SecondNormalScale;
half _SecondMetallic;
half _SecondSmoothness;
half _SecondOcclusionStrength;

half4 _ThirdBaseMap_ST;
half4 _ThirdBaseColor;
half4 _ThirdAlphaMap_ST;
half4 _ThirdNormalMap_ST;
half _ThirdNormalScale;
half _ThirdMetallic;
half _ThirdSmoothness;
half _ThirdOcclusionStrength;

half4 _FourBaseMap_ST;
half4 _FourBaseColor;
half4 _FourAlphaMap_ST;
half4 _FourNormalMap_ST;
half _FourNormalScale;
half _FourMetallic;
half _FourSmoothness;
half _FourOcclusionStrength;

half4 _EmissionMap_ST;
half4 _EmissionColor;
half _EmissionIntensity;
half4 _EmissionCubemapColor;
half _EmissionCubemapIntensity;
half _EmissionCubemapLod;

half _CloudShadowIntensity;
half _AdjustColorIntensity;
half _ApplyVertexColor;
half _CustomSpecularIntensity;
CBUFFER_END

TEXTURE2D(_ControlMap);   		SAMPLER(sampler_ControlMap);

#if defined(_HEIGHT_BLEND)
TEXTURE2D(_HeightMap);   		SAMPLER(sampler_HeightMap);
#endif

TEXTURE2D(_FirstAlphaMap);   		SAMPLER(sampler_FirstAlphaMap);
TEXTURE2D(_FirstNormalMap);   		SAMPLER(sampler_FirstNormalMap);
TEXTURECUBE(_EmssionCubemap);	SAMPLER(sampler_EmssionCubemap);

#if defined(_SECOND_LAYER_BLEND)
TEXTURE2D(_SecondBaseMap);               SAMPLER(sampler_SecondBaseMap);
TEXTURE2D(_SecondAlphaMap);              SAMPLER(sampler_SecondAlphaMap);
TEXTURE2D(_SecondNormalMap);             SAMPLER(sampler_SecondNormalMap);
#endif

#if defined(_THIRD_LAYER_BLEND)
TEXTURE2D(_ThirdBaseMap);               SAMPLER(sampler_ThirdBaseMap);
TEXTURE2D(_ThirdAlphaMap);              SAMPLER(sampler_ThirdAlphaMap);
TEXTURE2D(_ThirdNormalMap);             SAMPLER(sampler_ThirdNormalMap);
#endif

#if defined(_FOUR_LAYER_BLEND)
TEXTURE2D(_FourBaseMap);               SAMPLER(sampler_FourBaseMap);
TEXTURE2D(_FourAlphaMap);              SAMPLER(sampler_FourAlphaMap);
TEXTURE2D(_FourNormalMap);             SAMPLER(sampler_FourNormalMap);
#endif

half WeightBlend(half4 weight, half layer1, half layer2, half layer3, half layer4)
{
	return (layer1 * weight.r + layer2 * weight.g + layer3 * weight.b + layer4 * weight.a) / (weight.r + weight.g + weight.b + weight.a);
}

half3 WeightBlend(half4 weight, half3 layer1, half3 layer2, half3 layer3, half3 layer4)
{
	return (layer1 * weight.r + layer2 * weight.g + layer3 * weight.b + layer4 * weight.a) / (weight.r + weight.g + weight.b + weight.a);
}

#if defined(_HEIGHT_BLEND)
half HeightBlend(half4 height, half input1, half input2, half input3, half input4)
{
	half heightStart = max(max(height.r, height.g), max(height.b, height.a)) - _HeightBlendFactor;
	half b1 = max(height.r - heightStart, 0);
	half b2 = max(height.g - heightStart, 0);
	half b3 = max(height.b - heightStart, 0);
	half b4 = max(height.a - heightStart, 0);
	return ((input1 * b1) + (input2 * b2) + (input3 * b3) + (input4 * b4)) / (b1 + b2 + b3 + b4);
}

half3 HeightBlend(half4 height, half3 input1, half3 input2, half3 input3, half3 input4)
{
	half heightStart = max(max(height.r, height.g), max(height.b, height.a)) - _HeightBlendFactor;
	half b1 = max(height.r - heightStart, 0);
	half b2 = max(height.g - heightStart, 0);
	half b3 = max(height.b - heightStart, 0);
	half b4 = max(height.a - heightStart, 0);
	return ((input1 * b1) + (input2 * b2) + (input3 * b3) + (input4 * b4)) / (b1 + b2 + b3 + b4);
}
#endif

#if defined(_HEIGHT_BLEND)
inline half GetAlpha(float2 uv, half4 weight, half4 height)
#else
inline half GetAlpha(float2 uv, half4 weight)
#endif
{
#ifdef _FIRST_ALPHAMAP_ON
    half firstAlpha = SAMPLE_TEXTURE2D(_FirstAlphaMap, sampler_FirstAlphaMap, TRANSFORM_TEX(uv, _FirstAlphaMap)).r * _BaseColor.a;
#else
	half firstAlpha = _BaseColor.a;
#endif

#if _SECOND_LAYER_BLEND
	#ifdef _SECOND_ALPHAMAP_ON
    	half secondAlpha = SAMPLE_TEXTURE2D(_SecondAlphaMap, sampler_SecondAlphaMap, TRANSFORM_TEX(uv, _SecondAlphaMap)).a * _SecondBaseColor.a;
	#else
		half secondAlpha = _SecondBaseColor.a;
	#endif
#else
	half secondAlpha = 0;
#endif

#if _THIRD_LAYER_BLEND
	#ifdef _THIRD_ALPHAMAP_ON
    	half thirdAlpha = SAMPLE_TEXTURE2D(_ThirdAlphaMap, sampler_ThirdAlphaMap, TRANSFORM_TEX(uv, _ThirdAlphaMap)).a * _ThirdBaseColor.a;
	#else
		half thirdAlpha = _ThirdBaseColor.a;
	#endif
#else
	half thirdAlpha = 0;
#endif

#if _FOUR_LAYER_BLEND
	#ifdef _FOUR_ALPHAMAP_ON
    	half fourAlpha = SAMPLE_TEXTURE2D(_FourAlphaMap, sampler_FourAlphaMap, TRANSFORM_TEX(uv, _FourAlphaMap)).a * _FourBaseColor.a;
	#else
		half fourAlpha = _FourBaseColor.a;
	#endif
#else
	half fourAlpha = 0;
#endif

#if defined(_HEIGHT_BLEND)
	half alpha = HeightBlend(height * weight, firstAlpha, secondAlpha, thirdAlpha, fourAlpha);
#else
	half alpha = WeightBlend(weight, firstAlpha, secondAlpha, thirdAlpha, fourAlpha);
#endif

	return alpha;
}

#if defined(_HEIGHT_BLEND)
inline half3 GetAlbedoOcclusion(float2 uv, half4 weight, half4 height, out half occlusion)
#else
inline half3 GetAlbedoOcclusion(float2 uv, half4 weight, out half occlusion)
#endif
{
	half4 firstAlbedoAlpha = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _BaseMap), TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)) * _BaseColor;
	half3 firstAlbedo = firstAlbedoAlpha.rgb;
	half firstOcclusion = lerp(1, firstAlbedoAlpha.a, _FirstOcclusionStrength);
	
#if _SECOND_LAYER_BLEND
	half4 secondAlbedoAlpha = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _SecondBaseMap), TEXTURE2D_ARGS(_SecondBaseMap, sampler_SecondBaseMap)) * _SecondBaseColor;
	half3 secondAlbedo = secondAlbedoAlpha.rgb;
	half secondOcclusion = lerp(1, secondAlbedoAlpha.a, _SecondOcclusionStrength);
#else
	half3 secondAlbedo = half3(0, 0, 0);
	half secondOcclusion = 0;
#endif

#if _THIRD_LAYER_BLEND
	half4 thirdAlbedoAlpha = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _ThirdBaseMap), TEXTURE2D_ARGS(_ThirdBaseMap, sampler_ThirdBaseMap)) * _ThirdBaseColor;
	half3 thirdAlbedo = thirdAlbedoAlpha.rgb;
	half thirdOcclusion = lerp(1, thirdAlbedoAlpha.a, _ThirdOcclusionStrength);
#else
	half3 thirdAlbedo = half3(0, 0, 0);
	half thirdOcclusion = 0;
#endif

#if _FOUR_LAYER_BLEND
	half4 fourAlbedoAlpha = SampleAlbedoAlpha(TRANSFORM_TEX(uv, _FourBaseMap), TEXTURE2D_ARGS(_FourBaseMap, sampler_FourBaseMap)) * _FourBaseColor;
	half3 fourAlbedo = fourAlbedoAlpha.rgb;
	half fourOcclusion = lerp(1, fourAlbedoAlpha.a, _FourOcclusionStrength);
#else
	half3 fourAlbedo = half3(0, 0, 0);
	half fourOcclusion = 0;
#endif

#if defined(_HEIGHT_BLEND)
	half3 albedo = HeightBlend(height * weight, firstAlbedo, secondAlbedo, thirdAlbedo, fourAlbedo);
	occlusion = HeightBlend(height * weight, firstOcclusion, secondOcclusion, thirdOcclusion, fourOcclusion);
#else
	half3 albedo = WeightBlend(weight, firstAlbedo, secondAlbedo, thirdAlbedo, fourAlbedo);
	occlusion = WeightBlend(weight, firstOcclusion, secondOcclusion, thirdOcclusion, fourOcclusion);
#endif

	return albedo;
}

#if defined(_HEIGHT_BLEND)
inline half3 GetNormalMetallicSmoothness(float2 uv, half4 weight, half4 height, out half metallic, out half smoothness)
#else
inline half3 GetNormalMetallicSmoothness(float2 uv, half4 weight, out half metallic, out half smoothness)
#endif
{
#if _FIRST_NORMALMAP
    half4 firstNormalColor = SAMPLE_TEXTURE2D(_FirstNormalMap, sampler_FirstNormalMap, TRANSFORM_TEX(uv, _FirstNormalMap));
    half3 firstNormalTS = UnpackNormalRG(firstNormalColor, _FirstNormalScale);
	half firstMetallic = firstNormalColor.b * _FirstMetallic;
    half firstSmoothness = firstNormalColor.a * _FirstSmoothness;
#else
    half3 firstNormalTS = half3(0.0h, 0.0h, 1.0h);
	half firstMetallic = _FirstMetallic;
    half firstSmoothness = _FirstSmoothness;
#endif

#if _SECOND_LAYER_BLEND
	#if _SECOND_NORMALMAP
    	half4 secondNormalColor = SAMPLE_TEXTURE2D(_SecondNormalMap, sampler_SecondNormalMap, TRANSFORM_TEX(uv, _SecondNormalMap));
    	half3 secondNormalTS = UnpackNormalRG(secondNormalColor, _SecondNormalScale);
		half secondMetallic = secondNormalColor.b * _SecondMetallic;
    	half secondSmoothness = secondNormalColor.a * _SecondSmoothness;
	#else
    	half3 secondNormalTS = half3(0.0h, 0.0h, 1.0h);
		half secondMetallic = _SecondMetallic;
    	half secondSmoothness = _SecondSmoothness;
	#endif
#else
	half3 secondNormalTS = half3(0.0h, 0.0h, 0.0h);
	half secondMetallic = 0;
    half secondSmoothness = 0;
#endif

#if _THIRD_LAYER_BLEND
	#if _THIRD_NORMALMAP
    	half4 thirdNormalColor = SAMPLE_TEXTURE2D(_ThirdNormalMap, sampler_ThirdNormalMap, TRANSFORM_TEX(uv, _ThirdNormalMap));
    	half3 thirdNormalTS = UnpackNormalRG(thirdNormalColor, _ThirdNormalScale);
		half thirdMetallic = thirdNormalColor.b * _ThirdMetallic;
    	half thirdSmoothness = thirdNormalColor.a * _ThirdSmoothness;
	#else
    	half3 thirdNormalTS = half3(0.0h, 0.0h, 1.0h);
		half thirdMetallic = _ThirdMetallic;
    	half thirdSmoothness = _ThirdSmoothness;
	#endif
#else
	half3 thirdNormalTS = half3(0.0h, 0.0h, 0.0h);
	half thirdMetallic = 0;
    half thirdSmoothness = 0;
#endif

#if _FOUR_LAYER_BLEND
	#if _FOUR_NORMALMAP
    	half4 fourNormalColor = SAMPLE_TEXTURE2D(_FourNormalMap, sampler_FourNormalMap, TRANSFORM_TEX(uv, _FourNormalMap));
    	half3 fourNormalTS = UnpackNormalRG(fourNormalColor, _FourNormalScale);
		half fourMetallic = fourNormalColor.b * _FourMetallic;
    	half fourSmoothness = fourNormalColor.a * _FourSmoothness;
	#else
    	half3 fourNormalTS = half3(0.0h, 0.0h, 1.0h);
		half fourMetallic = _FourMetallic;
    	half fourSmoothness = _FourSmoothness;
	#endif
#else
	half3 fourNormalTS = half3(0.0h, 0.0h, 0.0h);
	half fourMetallic = 0;
    half fourSmoothness = 0;
#endif

#if defined(_HEIGHT_BLEND)
	half3 normalTS = HeightBlend(height * weight, firstNormalTS, secondNormalTS, thirdNormalTS, fourNormalTS);
	metallic = HeightBlend(height * weight, firstMetallic, secondMetallic, thirdMetallic, fourMetallic);
	smoothness = HeightBlend(height * weight, firstSmoothness, secondSmoothness, thirdSmoothness, fourSmoothness);
#else
	half3 normalTS = WeightBlend(weight, firstNormalTS, secondNormalTS, thirdNormalTS, fourNormalTS);
	metallic = WeightBlend(weight, firstMetallic, secondMetallic, thirdMetallic, fourMetallic);
	smoothness = WeightBlend(weight, firstSmoothness, secondSmoothness, thirdSmoothness, fourSmoothness);
#endif

	return normalTS;
}

inline void InitializeTerrainLitSurfaceData(float2 uv, float2 controlUV, half4 vertexColor, out SurfaceData outSurfaceData)
{
	outSurfaceData = (SurfaceData)0;

	half4 weight = SAMPLE_TEXTURE2D(_ControlMap, sampler_ControlMap, TRANSFORM_TEX(controlUV, _ControlMap));
	weight.r *= _FirstLayerWeight;
	weight.g *= _SecondLayerWeight;
	weight.b *= _ThirdLayerWeight;
	weight.a *= _FourLayerWeight;

#ifndef _SECOND_LAYER_BLEND
	weight.g = 0;
#endif

#ifndef _THIRD_LAYER_BLEND
	weight.b = 0;
#endif

#ifndef _FOUR_LAYER_BLEND
	weight.a = 0;
#endif

#if defined(_HEIGHT_BLEND)
	half2 firstLayerUV = uv * _FirsLayerTilingOffset.xy + _FirsLayerTilingOffset.zw;
	half4 height = half4(0, 0, 0, 0);
	height.r = SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, firstLayerUV).r * _FirstLayerHeight;

	#if _SECOND_LAYER_BLEND
		half2 secondLayerUV = uv * _SecondLayerTilingOffset.xy + _SecondLayerTilingOffset.zw;
		height.g =  SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, secondLayerUV).g * _SecondLayerHeight;
	#endif

	#if _THIRD_LAYER_BLEND
		half2 thirdLayerUV = uv * _ThirdLayerTilingOffset.xy + _ThirdLayerTilingOffset.zw;
		height.b =  SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, thirdLayerUV).b * _ThirdLayerHeight;
	#endif

	#if _FOUR_LAYER_BLEND
		half2 fourLayerUV = uv * _FourLayerTilingOffset.xy + _FourLayerTilingOffset.zw;
		height.a =  SAMPLE_TEXTURE2D(_HeightMap, sampler_HeightMap, fourLayerUV).a * _FourLayerHeight;
	#endif
#endif

#if defined(_HEIGHT_BLEND)
	outSurfaceData.alpha = GetAlpha(uv, weight, height);
#else
	outSurfaceData.alpha = GetAlpha(uv, weight);
#endif
	outSurfaceData.alpha *= vertexColor.a;

#ifdef _ALPHATEST_ON
	clip(outSurfaceData.alpha - _Cutoff);
#endif

#if defined(_HEIGHT_BLEND)
	outSurfaceData.albedo = GetAlbedoOcclusion(uv, weight, height, outSurfaceData.occlusion);
	outSurfaceData.normalTS = GetNormalMetallicSmoothness(uv, weight, height, outSurfaceData.metallic, outSurfaceData.smoothness);
#else
	outSurfaceData.albedo = GetAlbedoOcclusion(uv, weight, outSurfaceData.occlusion);
	outSurfaceData.normalTS = GetNormalMetallicSmoothness(uv, weight, outSurfaceData.metallic, outSurfaceData.smoothness);
#endif
	outSurfaceData.albedo *= vertexColor.rgb;
#ifdef _EMISSION
	outSurfaceData.emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, TRANSFORM_TEX(uv, _EmissionMap)).rgb * _EmissionColor.rgb * _EmissionIntensity;
#endif
}

#endif