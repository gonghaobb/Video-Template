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

#ifndef MATRIX_CARTOON_LIGHTING_INCLUDED
#define MATRIX_CARTOON_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half4 CartoonUniversalFragment(InputData inputData, SurfaceData surfaceData, StylizedData stylizedData)
{
    #ifdef USE_BLINN_PHONG
        #ifdef _CUSTOM_LIGHT
            surfaceData.specular = _BlingPhongSpecColor * surfaceData.smoothness * _CustomSpecularIntensity;
        #else
            surfaceData.specular = _BlingPhongSpecColor * surfaceData.smoothness;
        #endif
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

    #if !_ENVIRONMENTREFLECTIONS_OFF || !USE_BLINN_PHONG
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

    #if _CUSTOM_LIGHT
        half3 diffuseColor = CartoonLighting(attenuatedLightColor, mainLightDir, inputData.normalWS, stylizedData);
    #else
        half3 diffuseColor = CartoonLighting(attenuatedLightColor, mainLightDir, inputData.normalWS, stylizedData);
    #endif

    finalcolor += diffuse * diffuseColor;

    #if !_SPECULARHIGHLIGHTS_OFF
        #ifdef USE_BLINN_PHONG
            smoothness = exp2(10 * smoothness + 1);
            half3 specularColor = LightingSpecular(attenuatedLightColor, mainLightDir, inputData.normalWS, inputData.viewDirectionWS, specularGloss, smoothness);
            finalcolor += surfaceData.specular * specularColor;
        #else
            half NdotL = saturate(dot(inputData.normalWS, mainLightDir));
            half3 radiance = attenuatedLightColor * NdotL;
            half3 specularColor = DirectBRDFSpecular(brdfData, inputData.normalWS, mainLightDir, inputData.viewDirectionWS) * radiance;
            #if _CUSTOM_LIGHT
                specularColor *= _CustomSpecularIntensity;
            #endif
            finalcolor += brdfData.specular * specularColor;
        #endif
    #endif

    return half4(finalcolor, alpha);
}

#endif
