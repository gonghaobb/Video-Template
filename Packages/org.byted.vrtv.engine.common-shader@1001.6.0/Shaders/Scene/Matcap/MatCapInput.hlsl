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

#ifndef MATRIX_MATCAP_INPUT_INCLUDED
#define MATRIX_MATCAP_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/Common.hlsl"
#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Common/CartoonCommon.hlsl"

#if _NORMALMAP
    TEXTURE2D(_NormalMap);   SAMPLER(sampler_NormalMap);
#endif

#if _MATCAP
    TEXTURE2D(_Matcap);      SAMPLER(sampler_Matcap);
#endif

//#if_EMISSION_CUBEMAP
    TEXTURECUBE(_EmssionCubemap);	SAMPLER(sampler_EmssionCubemap);
//#endif

#if _RIM_LIGHT && _RIM_REFRACTIONY
    TEXTURE2D(_RimRefractionMap);      SAMPLER(sampler_RimRefractionMap);
#endif

#if _SWEEP_LIGHT
    TEXTURE2D(_SweepLightMap);          SAMPLER(sampler_SweepLightMap);
    TEXTURE2D(_SweepLightMaskMap);      SAMPLER(sampler_SweepLightMaskMap);
#endif

CBUFFER_START(UnityPerMaterial)
half _Surface;
half _Cutoff;

half4 _BaseColor;
half4 _BaseMap_ST;
half _ApplyVertexColor;
half _AlphaScale;
half _NormalScale;
half4 _NormalMap_ST;
half _NormalUSpeed;
half _NormalVSpeed;

half _RealTimeLightStrength;
half _RealTimeShadowStrength;
half3 _RealTimeShadowColor;
half _RealTimeShadowColorStrength;

half4 _ShadowColorFirst;
half4 _ShadowColorSecond;
half4 _ShadowColorThird;
half _ShadowBoundaryFirst;
half _ShadowBoundarySecond;
half _ShadowBoundaryThird;
half _ShadowSmoothFirst;
half _ShadowSmoothSecond;
half _ShadowSmoothThird;
half _ShadowAreaFirst;
half _ShadowAreaSecond;
half _ShadowAreaThird;

half _MatcapUVScale;
half _MatcapStrength;
half4 _MatCapHighLightsColor;
half _MatCapHighLightsThreshold;
half _MatCapHighLightsStrength;

half3 _RimLightColor;
half _RimLightWidth;
half _RimLightSmoothness;
half _RimLightIntensity;
half _RimLightMinValue;
half _RimLightMaxValue;
half _RimLightReverse;
half _EnableRimLightVertexColorMask;
half4 _RimLightVertexColorMask;

half _RimTransparencyWidth;
half _RimTransparencySmoothness;
half _RimTransparencyIntensity;
half _RimTransparencyMinValue;
half _RimTransparencyMaxValue;
half _RimTransparencyReverse;
half _RimTransparencyBaseAlpha;

half _RimRefractionWidth;
half _RimRefractionSmoothness;
half _RimRefractionIntensity;
half _RimRefractionMinValue;
half _RimRefractionMaxValue;
half _RimRefractionReverse;
half4 _RimRefractionMap_ST;
half _RimRefractionWorldPosUV;

half4 _EmissionMap_ST;
half4 _EmissionColor;
half _EmissionWorldPosUV;
half _EmissioUSpeed;
half _EmissioVSpeed;
half _EmissionIntensity;
half4 _EmissionCubemapColor;
half _EmissionCubemapIntensity;
half _EmissionCubemapBias;
half _EmissionCubemapRimLodIntensity;
half _EmissionCubemapLod;
half4 _EmissionCubemapBoxPostion;
half4 _EmissionCubemapBoxMin;
half4 _EmissionCubemapBoxMax;

half4 _SweepLightMap_ST;
half4 _SweepLightMaskMap_ST;
half3 _SweepLightColor;
half _SweepLightViewDirOffsetIntensity;
half _SweepLightUSpeed;
half _SweepLightVSpeed;

half _CloudShadowIntensity;
half _AdjustColorIntensity;
CBUFFER_END

#endif