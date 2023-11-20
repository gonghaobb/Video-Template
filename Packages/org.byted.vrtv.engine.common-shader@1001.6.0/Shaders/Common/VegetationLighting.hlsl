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

#ifndef MATRIX_VEGETATION_LIGHTING_INCLUDED
#define MATRIX_VEGETATION_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/Common.hlsl"

half4 VegetationFragmentLighting(InputData inputData, SurfaceData surfaceData, half specularIntensity = 1.0)
{
    #ifdef _BLING_PHONG
        surfaceData.specular = _BlingPhongSpecColor;
    #endif

    #if defined(SHADOWS_SHADOWMASK) && defined(LIGHTMAP_ON)
        half4 shadowMask = inputData.shadowMask;
    #elif !defined (LIGHTMAP_ON)
        half4 shadowMask = unity_ProbesOcclusion;
    #else
        half4 shadowMask = half4(1, 1, 1, 1);
    #endif

    half4 specularGloss = half4(surfaceData.specular, 1.0f);
    half3 diffuse = surfaceData.albedo * surfaceData.occlusion;
    half smoothness = surfaceData.smoothness;
    half3 emission = surfaceData.emission;
    half alpha = surfaceData.alpha;

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, shadowMask);

    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);

    inputData.bakedGI *= _BakeGIIntensityMultiplier;

    half3 finalcolor = emission;

    #if !_ENVIRONMENTREFLECTIONS_OFF || _PBR
        BRDFData brdfData;
        InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);
    #endif

    #if !_ENVIRONMENTREFLECTIONS_OFF
        const BRDFData noClearCoat = (BRDFData)0;
        finalcolor += GlobalIllumination(brdfData, noClearCoat, 0, inputData.bakedGI, surfaceData.occlusion, 
            inputData.normalWS, inputData.viewDirectionWS, inputData.positionWS);
    #endif

    #if _CUSTOM_LIGHT
        half3 attenuatedLightColor = _CustomDirectionLightData1.xyz * _CustomDirectionLightData0.x;
        half3 mainLightDir = _CustomDirectionLightData0.yzw;
    #else
        half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
        half3 mainLightDir = mainLight.direction;
    #endif

    #if _PBR
        half NdotL = saturate(dot(inputData.normalWS, mainLightDir));
        half3 radiance = attenuatedLightColor * NdotL;
        half3 diffuseColor = brdfData.diffuse * radiance;
    #elif _BLING_PHONG
        half3 diffuseColor = diffuse * LightingLambert(attenuatedLightColor, mainLightDir, inputData.normalWS);
    #else
        half3 diffuseColor = diffuse * attenuatedLightColor;
    #endif

    finalcolor += diffuseColor;

    #if !_SPECULARHIGHLIGHTS_OFF
        #if _PBR
            half3 specularColor = DirectBRDFSpecular(brdfData, inputData.normalWS, mainLightDir, inputData.viewDirectionWS) * radiance * brdfData.specular;
        #elif _BLING_PHONG
            smoothness = exp2(10 * smoothness + 1);
            half3 specularColor = LightingSpecular(attenuatedLightColor, mainLightDir, inputData.normalWS, inputData.viewDirectionWS, specularGloss, smoothness);
        #else
            half3 specularColor = half3(0, 0, 0);
        #endif

        #if _CUSTOM_LIGHT
            specularColor *= _CustomSpecularIntensity;
        #endif

        finalcolor += specularColor * specularIntensity;
    #endif

    return half4(finalcolor, alpha);
}

#endif
