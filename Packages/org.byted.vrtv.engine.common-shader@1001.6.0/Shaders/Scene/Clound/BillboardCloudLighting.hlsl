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

#ifndef MATRIX_BILLBOARD_CLOUD_LIGHTING_INCLUDED
#define MATRIX_BILLBOARD_CLOUD_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/Common.hlsl"

half4 BillboardCloudFragmentLighting(InputData inputData, SurfaceData surfaceData, half specularIntensity = 1.0)
{
    #ifdef _BLING_PHONG
        surfaceData.specular = _BlingPhongSpecColor;
    #endif

    half4 specularGloss = half4(surfaceData.specular, 1.0f) * surfaceData.occlusion; //云的高光接受AO影响，类似厚度
    half3 diffuse = surfaceData.albedo;// * surfaceData.occlusion;云的漫反射不要接受AO影响
    half smoothness = surfaceData.smoothness;
    half3 emission = surfaceData.emission;
    half alpha = surfaceData.alpha;

    Light mainLight = GetMainLight();

    half3 finalcolor = emission;

    #if !_ENVIRONMENTREFLECTIONS_OFF
        half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
        half mip = PerceptualRoughnessToMipmapLevel(PerceptualSmoothnessToPerceptualRoughness(smoothness));
        half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, mip);

        #if defined(UNITY_USE_NATIVE_HDR)
            half3 irradiance = encodedIrradiance.rgb;
        #else
            half3 irradiance = DecodeHDREnvironment(encodedIrradiance, unity_SpecCube0_HDR);
        #endif

        finalcolor += irradiance * surfaceData.occlusion;
    #endif

    #if _CUSTOM_LIGHT
        half3 attenuatedLightColor = _CustomDirectionLightData1.xyz * _CustomDirectionLightData0.x;
        half3 mainLightDir = _CustomDirectionLightData0.yzw;
    #else
        half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation * mainLight.shadowAttenuation;
        half3 mainLightDir = mainLight.direction;
    #endif

    mainLightDir = -mainLightDir;//翻转光的方向以从背面照亮云
    #if _BLING_PHONG
        half3 diffuseColor = diffuse * LightingLambert(attenuatedLightColor, mainLightDir, inputData.normalWS);
    #else
        half3 diffuseColor = diffuse;
    #endif

    finalcolor += diffuseColor;

    #if !_SPECULARHIGHLIGHTS_OFF
        #if _BLING_PHONG
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
