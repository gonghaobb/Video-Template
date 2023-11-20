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

#ifndef MATRIX_UBER_POST_EFFECT_BASE_INCLUDED
#define MATRIX_UBER_POST_EFFECT_BASE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/Core/Shaders/Common/EcosystemCommon.hlsl"

#if _SCREEN_DISTORTION
half2 GetScreenDistortionOffset(TEXTURE2D_PARAM(_distortionTexture, sampler_distortionTexture), float2 uv, half4 screenDistortionTexture_ST, half screenDistortionU,
	half screenDistortionV, half screenDistortStrength)
{
	float2 uvDistortion = screenDistortionTexture_ST.xy * uv + screenDistortionTexture_ST.zw + float2(screenDistortionU, screenDistortionV) * _Time.y;
	half2 screeDistortionOffset = SAMPLE_TEXTURE2D(_distortionTexture, sampler_distortionTexture, uvDistortion).xy * screenDistortStrength;

	return screeDistortionOffset;
}
#endif

#if _GRAINY_BLUR
half4 GrainyBlur(float2 uv, float2 screenUV, float grainyBlurRadius, half iteration, float edgeWeakDistance)
{
	float2 randomOffset = float2(0.0, 0.0);
	float random = Rand(screenUV);
	half4 finalColor = half4(0, 0, 0, 0);

	float blurRadius = grainyBlurRadius / max(_SourceSize.x, _SourceSize.y);
	//根据离边缘距离减弱alpha和随机距离
	float minDisX = min(uv.x, 1 - uv.x);
	float minDisY = min(uv.y, 1 - uv.y);
	float minDis = min(minDisX, minDisY);
	blurRadius *= smoothstep(0, edgeWeakDistance, minDis);

	for (int k = 0; k < int(iteration); k++)
	{
		random = frac(43758.5453 * random + 0.61432);
		randomOffset.x = (random - 0.5) * 2.0;

		random = frac(43758.5453 * random + 0.61432);
		randomOffset.y = (random - 0.5) * 2.0;

		finalColor += SampleScreenColor(screenUV + randomOffset * blurRadius);
	}
	finalColor /= iteration;

	return finalColor;
}
#endif

#if _KAWASE_BLUR
half4 KawaseBlur(float2 uv, float2 texelSize, float pixelOffset, half iteration, float edgeWeakDistance)
{
	half3 sceneColor = SampleScreenColor(uv);

	//根据离边缘距离减弱alpha和随机距离
	float uDis = abs(uv.x - 0.5);
	float vDis = abs(uv.y - 0.5);
	float dis = (1 - (length(half2(uDis, vDis)) / 0.707));
	pixelOffset *= smoothstep(0, edgeWeakDistance, dis);

	half3 finalColor = half3(0, 0, 0);
	for (int k = 0; k < int(iteration); k++)
	{
		finalColor += sceneColor;
		float2 stepSize = texelSize * (k + 1);
		finalColor += SampleScreenColor(uv + float2( pixelOffset,  pixelOffset) * stepSize); 
		finalColor += SampleScreenColor(uv + float2(-pixelOffset,  pixelOffset) * stepSize); 
		finalColor += SampleScreenColor(uv + float2(-pixelOffset, -pixelOffset) * stepSize); 
		finalColor += SampleScreenColor(uv + float2( pixelOffset, -pixelOffset) * stepSize);
	}

	finalColor = finalColor / (5 * iteration);

	return half4(finalColor, 1);
}
#endif

#if _GLITCH_IMAGE_BLOCK
half4 GlitchImageBlock(float2 screenUV, half glitchImageBlockSpeed, half glitchImageBlockSize, half glitchImageBlockMaxRGBSplitX, half glitchImageBlockMaxRGBSplitY)
{
	half2 block = RandomNoiseWithSpeed(floor(screenUV * glitchImageBlockSize), glitchImageBlockSpeed);

	float displaceNoise = pow(block.x, 8.0) * pow(block.x, 3.0);
	float splitRGBNoise = pow(RandomNoiseWithSpeed(7.2341, glitchImageBlockSpeed), 17.0);
	float offsetX = displaceNoise - splitRGBNoise * glitchImageBlockMaxRGBSplitX;
	float offsetY = displaceNoise - splitRGBNoise * glitchImageBlockMaxRGBSplitY;

	float noiseX = 0.05 * RandomNoiseWithSpeed(13.0, glitchImageBlockSpeed);
	float noiseY = 0.05 * RandomNoiseWithSpeed(7.0, glitchImageBlockSpeed);
	float2 offset = float2(offsetX * noiseX, offsetY * noiseY);

	half4 colorR = SampleScreenColor(screenUV);
	half4 colorG = SampleScreenColor(screenUV + offset);
	half4 colorB = SampleScreenColor(screenUV - offset);

	return half4(colorR.r, colorG.g, colorB.z, (colorR.a + colorG.a + colorB.a));
}
#endif

#if _GLITCH_SCREEN_SHAKE
half4 GlitchScreenShake(float2 screenUV, half glitchScreenShakeIndensityX, half glitchScreenShakeIndensityY)
{
	float randomNoise = RandomNoise(_Time.x, 2) - 0.5;
	float shakeX = randomNoise * glitchScreenShakeIndensityX;
	float shakeY = randomNoise * glitchScreenShakeIndensityY;

	return SampleScreenColor(frac(screenUV + float2(shakeX, shakeY)));
}
#endif

#if _SCREEN_DISSOLVE
half2 DefaultDistortion(in float2 screenPos , in float4 distortionData)
{    
	float2 center = float2(0.5f , 0.5f);
	float2 targetScreenPos = screenPos;
	float2 p2c = screenPos - center;
	float distance = length(p2c);
	float isDistortion = step(distortionData.z , distance) * step(distance, distortionData.w); 
	if (isDistortion > 0.5f)
	{
		float2 p2cN = normalize(p2c);
		float2 distortionStart = center + distortionData.z * p2cN;
                    
		float halfRange = (distortionData.w - distortionData.z) / 2;
		float2 distortionCenter = distortionStart + halfRange * p2cN;
                    
		float2 s2c = screenPos - distortionCenter;
		float distanceToC = length(screenPos - distortionCenter);
		float intensity = pow(1 - distanceToC / halfRange , distortionData.x);
		float2 s2cN = normalize(s2c);
		targetScreenPos = distortionCenter + (distanceToC * intensity + (1 - intensity) * halfRange) * s2cN ;
	}
	
	return lerp(screenPos, targetScreenPos, distortionData.y); 
}

half2 GetScreenDissolveUV(float2 uv, half4 dissolveTex_ST)
{
	half2 originUV = uv * dissolveTex_ST.xy + dissolveTex_ST.zw;
	return DefaultDistortion(originUV, _lensDistortionParams);
	return uv * dissolveTex_ST.xy + dissolveTex_ST.zw;
}

half4 ScreenDissolve(float2 uv, float2 screenUV)
{
	half4 finalColor = SampleScreenColor(uv);
	half alpha = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, screenUV).x;
	alpha = lerp(alpha, 1 - alpha, _InvertDissolve);
	half dissolve = _DissolveProcess * (1 + _DissolveWidth) - _DissolveWidth;
	alpha = saturate(smoothstep(dissolve, dissolve + _DissolveWidth, alpha));
	//这里直接给a通道赋值，丢弃掉原始alpha，防止透传显示在透明物体上。
	finalColor.a = _DissolveHardEdge > 0.5 ? sign(alpha) : alpha;
	finalColor.rgb += _DissolveEdgeColor.rgb * _DissolveEdgeColor.a * (_DissolveHardEdge > 0.5 ? sign(1 - alpha) : (1 - alpha));
	finalColor.rgb = lerp(finalColor.rgb, _DissolveBackgroundColor.rgb, (1 -finalColor.a) * _DissolveBackgroundColor.a);
	return finalColor;
}
#endif

half4 UberPostEffect(float2 uv, float2 screenUV)
{
	half4 finalColor = half4(0.0, 0.0, 0.0, 1.0);
	half2 screeDistortionOffset = half2(0, 0);

#if _SCREEN_DISTORTION
	screenUV += GetScreenDistortionOffset(TEXTURE2D_ARGS(_ScreenDistortionTexture, sampler_ScreenDistortionTexture), uv, _ScreenDistortionTexture_ST, _ScreenDistortionU, _ScreenDistortionV, _ScreenDistortStrength);
#elif _SCREEN_DISSOLVE
	screenUV = GetScreenDissolveUV(screenUV, _DissolveTex_ST);
#endif

#if _GRAINY_BLUR
	finalColor = GrainyBlur(uv, screenUV, _GrainyBlurRadius, _GrainyBlurIteration, _GrainyBlurEdgeWeakDistance);
#elif _KAWASE_BLUR
	finalColor = KawaseBlur(screenUV, _CameraTransparentTexture_TexelSize.xy, _KawaseBlurRadius, _KawaseBlurIteration, _KawaseBlurEdgeWeakDistance);
#elif _GLITCH_IMAGE_BLOCK
	finalColor = GlitchImageBlock(screenUV, _GlitchImageBlockSpeed, _GlitchImageBlockSize, _GlitchImageBlockMaxRGBSplitX, _GlitchImageBlockMaxRGBSplitY);
#elif _GLITCH_SCREEN_SHAKE
	finalColor = GlitchScreenShake(screenUV, _GlitchScreenShakeIndensityX, _GlitchScreenShakeIndensityY);
#elif _SCREEN_DISSOLVE
	finalColor = ScreenDissolve(uv, screenUV);
#else
	finalColor = SampleScreenColor(screenUV);
#endif

	return finalColor;
}

#endif