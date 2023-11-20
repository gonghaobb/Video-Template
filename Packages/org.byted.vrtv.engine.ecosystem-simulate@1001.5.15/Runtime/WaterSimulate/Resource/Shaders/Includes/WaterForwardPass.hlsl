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

#ifndef MATRIX_WATER_FORWARD_INCLUDED
#define MATRIX_WATER_FORWARD_INCLUDED

#include "WaterBase.hlsl"

//生态系统雾影响
#ifdef CUSTOM_FOG
    #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/FogSimulate/Resource/Shaders/Fog.hlsl"
#endif

//天气系统
#if defined(_GLOBAL_RAIN_SURFACE)
    #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/RainSimulate/Resource/Shader/RainSurface.hlsl"
#elif defined(_GLOBAL_SNOW_SURFACE)
    #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/SnowSimulate/Resource/Shaders/SnowSurface.hlsl"
#endif

//全局云投影
#if _GLOBAL_CLOUD_SHADOW
	#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/CloudShadowSimulate/Resource/Shaders/CloudShadow.hlsl"
#endif

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

    float3 positionWS               : TEXCOORD2;

    float3 normalWS                 : TEXCOORD3;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    float4 tangentWS                : TEXCOORD4;    // xyz: tangent, w: sign
#endif
    float3 viewDirWS                : TEXCOORD5;

    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD7;
#endif

#if _REALTIME_DEPTH_MAP || _REALTIME_REFRACTION
    float4 screenPos                : TEXCOORD8;
#endif

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = SafeNormalize(input.viewDirWS);
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

#if defined(CUSTOM_FOG)
    half fogFactor = FogVert(vertexInput.positionWS);
#else
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif

    output.uv = input.texcoord;

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif

    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

#if _REALTIME_DEPTH_MAP || _REALTIME_REFRACTION
    output.screenPos = vertexInput.positionNDC;
#endif

    output.positionCS = vertexInput.positionCS;

    return output;
}

// Used in Standard (Physically Based) shader
half4 LitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if _REALTIME_DEPTH_MAP || _REALTIME_REFRACTION
        float2 screenUV = input.screenPos.xy / input.screenPos.w;
    #endif

    float2 offlineWorldUV = GetOfflineWorldUV(input.positionWS);

    #if _REALTIME_DEPTH_MAP
        float waterDepth = GetRealtimeWaterEyeDepth(input.screenPos);
        float sceneDepth = GetSceneEyeDepth(screenUV);
    #else
        float waterDepth = GetOfflineWaterEyeDepth(input.positionWS);
        float sceneDepth = GetSceneEyeDepth(offlineWorldUV);
    #endif

    float sceneDeltaDepth = max((sceneDepth - waterDepth) / _MaxWaterDepthDiff, 0.0);

    float2 UVPanner1;
    float2 UVPanner2;
    float flowLerp;
    SurfaceData surfaceData;
    InitializeWaterSurfaceData(input.uv, sceneDeltaDepth, UVPanner1, UVPanner2, flowLerp, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    #if _GLOBAL_CLOUD_SHADOW
		surfaceData.albedo = ApplyGlobalCloudShadow(surfaceData.albedo, input.positionWS, _CloudShadowIntensity);
	#endif

    half3 reflectColor = half3(0.0, 0.0, 0.0);
    half3 refractColor = half3(0.0, 0.0, 0.0);

    //天气系统雨雪
    #if defined(_GLOBAL_RAIN_SURFACE)
        reflectColor += ComputeWetSurface(input.uv, input.normalWS, input.positionWS, input.viewDirWS, input.tangentWS, surfaceData.normalTS, surfaceData.metallic, surfaceData.smoothness);
    #elif defined(_GLOBAL_SNOW_SURFACE)
        surfaceData.albedo = ComputeSnowSurface(surfaceData.albedo, input.uv, input.positionWS, input.normalWS, input.tangentWS, surfaceData.normalTS, 0);
    #endif

    #if _REALTIME_REFRACTION
        refractColor = GetRefractionColor(screenUV, inputData.normalWS.xz, sceneDeltaDepth);
    #else
        refractColor = GetRefractionColor(offlineWorldUV, inputData.normalWS.xz, sceneDeltaDepth);
    #endif

    #if _CAUSTICS
        refractColor += max(refractColor, 0.1) * GetCausticColor(input.uv, sceneDeltaDepth);//折射乘以焦散再叠加上去,模拟焦散在水底的感觉
    #endif

    #if _CUBEMAP_REFLECTION || _PROBE_REFLECTION
        half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
    #endif

    #if _CUBEMAP_REFLECTION
        reflectColor += GetCubemapReflection(reflectVector);
	#endif

    #if _PROBE_REFLECTION
        reflectColor += GetProbeReflection(reflectVector, input.positionWS, surfaceData.smoothness, surfaceData.occlusion);
    #endif

    #if defined(_PBR)
        #ifdef _CUSTOM_SPEC_LIGHT_DIRECTION
            surfaceData.clearCoatMask = _CustomSpecularIntensity;
        #else
            surfaceData.clearCoatMask = 1;
        #endif
            half4 color = PicoUniversalFragmentPBR(inputData, surfaceData);
    #elif defined(_BLING_PHONG)
        #ifdef _CUSTOM_SPEC_LIGHT_DIRECTION
            surfaceData.specular = _BlingPhongSpecColor * surfaceData.smoothness * _CustomSpecularIntensity;
        #else
            surfaceData.specular = _BlingPhongSpecColor * surfaceData.smoothness;
        #endif
            half4 color = PicoUniversalFragmentBlinnPhong(inputData, surfaceData);
    #else
        half4 color = half4(surfaceData.albedo * surfaceData.occlusion, surfaceData.alpha);
    #endif

    //3S独立叠加
    #if _SSS
        color.rgb += GetSSSColor(GetMainLight(), inputData.viewDirectionWS, inputData.normalWS, sceneDeltaDepth);
    #endif

    half fresnelTerm = CalculateFresnelTerm(inputData.normalWS, inputData.viewDirectionWS);

    color.rgb += lerp(refractColor, reflectColor, fresnelTerm);

    #if _FOAM
        color.rgb += GetFoamColor(input.uv, UVPanner1, UVPanner2, flowLerp, sceneDeltaDepth);
    #endif
    
    color.rgb *= _OutputScale;
    color.rgb = ApplyGlobalColor(color.rgb, _AdjustColorIntensity);

    #if defined(CUSTOM_FOG)
        color.rgb = FogFrag(color.rgb, inputData.viewDirectionWS, inputData.positionWS, inputData.fogCoord);
    #else
        color.rgb = MixFog(color.rgb, inputData.fogCoord);
    #endif

    return half4(color.rgb, color.a * max(0.0, smoothstep(-0.001, _TransparentDepthCutOff, sceneDeltaDepth) - _TransparentAdd));
}

#endif