#ifndef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
#define UNIVERSAL_FORWARD_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

//PicoVideo;Ecosystem;ZhengLingFeng;Begin
#ifdef _FORCE_DISABLE_SNOW_SURFACE
#undef _GLOBAL_SNOW_SURFACE
#endif
#ifdef _FORCE_DISABLE_RAIN_SURFACE
#undef _GLOBAL_RAIN_SURFACE
#endif
#if defined(_GLOBAL_RAIN_SURFACE)
#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/RainSimulate/Resource/Shader/RainSurface.hlsl"
#elif defined(_GLOBAL_SNOW_SURFACE)
#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/SnowSimulate/Resource/Shaders/SnowSurface.hlsl"
#endif
//PicoVideo;Ecosystem;ZhengLingFeng;End

//PicoVideo;CloudShadow;XiaoPengCheng;Begin
#if _GLOBAL_CLOUD_SHADOW
	#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/CloudShadowSimulate/Resource/Shaders/CloudShadow.hlsl"
#endif
//PicoVideo;CloudShadow;XiaoPengCheng;End

// GLES2 has limited amount of interpolators
#if defined(_PARALLAXMAP) && !defined(SHADER_API_GLES)
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
#endif

#if (defined(_NORMALMAP) || (defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR))) || defined(_DETAIL)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

//PicoVideo;Ecosystem;ZhengLingFeng;Begin
#if defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif
//PicoVideo;Ecosystem;ZhengLingFeng;End

// keep this file in sync with LitGBufferPass.hlsl

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

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD2;
#endif

    float3 normalWS                 : TEXCOORD3;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    float4 tangentWS                : TEXCOORD4;    // xyz: tangent, w: sign
#endif
    float3 viewDirWS                : TEXCOORD5;

    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD7;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    float3 viewDirTS                : TEXCOORD8;
#endif
    
    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

//PicoVideo;LightMode;Ernst;Begin
#if defined(_MATRIX_SPC_LIGHTINGMAP)
void InitializeInputData(Varyings input, half3 normalTS, half smoothness, half specControl, out InputData inputData)
#else
void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
#endif
//PicoVideo;LightMode;Ernst;End
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = SafeNormalize(input.viewDirWS);
    //PicoVideo;Ecosystem;ZhengLingFeng;Begin
#if defined(_NORMALMAP) || defined(_DETAIL) || defined(_GLOBAL_RAIN_SURFACE)
    //PicoVideo;Ecosystem;ZhengLingFeng;End
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
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
    //PicoVideo;LightMode;Ernst;Begin
    #if defined(_MATRIX_SPC_LIGHTINGMAP)
    // float fresnelNdot = dot(normalize(inputData.normalWS), inputData.viewDirectionWS);
    // fresnelNdot = (0.0 + 1.0 * pow(max(1.0 - fresnelNdot, 0.0001), 1.0));
    // float lerpResult = lerp(0.04, 1.0, fresnelNdot);
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS, smoothness, specControl, inputData.viewDirectionWS);
    #else
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    #endif
    //PicoVideo;LightMode;Ernst;End
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
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

    //PicoVideo;Ecosystem;ZhengLingFeng;Begin
#if defined(CUSTOM_FOG)
    half fogFactor = FogVert(vertexInput.positionWS);
#else
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
#endif
    //PicoVideo;Ecosystem;ZhengLingFeng;End
    
    //PicoVideo;LightMode;YangFan;Begin
    #ifdef _MATRIX_INDEPENDENT_OFFSET_SCALE
    output.uv = input.texcoord;
    #else
    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    #endif
    //PicoVideo;LightMode;YangFan;End

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
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

    output.positionCS = vertexInput.positionCS;
    
    return output;
}

// Used in Standard (Physically Based) shader
half4 LitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, input.viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    SurfaceData surfaceData;
    
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    //PicoVideo;LayerBlend;XiaoPengCheng;Begin
    #if _ENABLE_LAYER_BLEND
        LayerSurfaceData layerSurfaceData;
        InitializeLayerSurfaceData(input.uv, layerSurfaceData);
        surfaceData = BlendSurfaceDataAndLayerSurfaceData(input.normalWS, surfaceData, layerSurfaceData);
    #endif
    //PicoVideo;LayerBlend;XiaoPengCheng;End

    //PicoVideo;CloudShadow;XiaoPengCheng;Begin
    #if _GLOBAL_CLOUD_SHADOW
		surfaceData.albedo = ApplyGlobalCloudShadow(surfaceData.albedo, input.positionWS, _CloudShadowIntensity);
	#endif
    //PicoVideo;CloudShadow;XiaoPengCheng;End

    //PicoVideo;WeatherSimulate;ZhengLingFeng;Begin
    #ifdef _GLOBAL_RAIN_SURFACE
    float3 refleColor;
    half accumulated;
    ComputeWetSurface(input.normalWS, input.positionWS, surfaceData.normalTS, surfaceData.metallic, surfaceData.smoothness, surfaceData.albedo, accumulated);
    #elif defined(_GLOBAL_SNOW_SURFACE)
    half snowTexData;
    half accumulated;
    surfaceData.albedo = ComputeSnowSurface(surfaceData.albedo, input.positionWS, input.normalWS, surfaceData.normalTS, accumulated, snowTexData);
    #endif
    //PicoVideo;WeatherSimulate;ZhengLingFeng;End
    
    InputData inputData;
    
    //PicoVideo;LightMode;Ernst;Begin
    #if defined(_MATRIX_SPC_LIGHTINGMAP)
    InitializeInputData(input, surfaceData.normalTS, surfaceData.smoothness, _LightmapSpecular * _GlobalLightmapSpecular, inputData); 
    #else
    InitializeInputData(input, surfaceData.normalTS, inputData);
    #endif

    //PicoVideo;WeatherSimulate;ZhengLingFeng;Begin
    #ifdef _GLOBAL_RAIN_SURFACE
    refleColor = ComputeWetSurfaceReflection(inputData.normalWS, input.viewDirWS, accumulated);
    #elif defined(_GLOBAL_SNOW_SURFACE)
    ComputeSnowSurfaceColorAndAO(snowTexData, accumulated, inputData.normalWS, surfaceData.occlusion, surfaceData.albedo);
    #endif
    //PicoVideo;WeatherSimulate;ZhengLingFeng;End
    
    //PicoVideo;LightMode;XiaoPengCheng;Begin
    #if _EMISSION_CUBEMAP
		half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
		half3 cubemapEmission = SAMPLE_TEXTURECUBE_LOD(_EmssionCubemap, sampler_EmssionCubemap, reflectVector, _EmissionCubemapLod).rgb;
		surfaceData.emission += cubemapEmission * _EmissionCubemapIntensity;
	#endif
    //PicoVideo;LightMode;XiaoPengCheng;End

    //PicoVideo;LightMode;YangFan;Begin
    #ifdef _MATRIX_CUSTOM_FRESNEL
    half NoV = saturate(dot(inputData.normalWS, inputData.viewDirectionWS));
    half fresnelTerm = _FresnelBias + _FresnelScale * pow(max(1.0 - NoV,0.001) , _FresnelPower);
    surfaceData.smoothness = clamp( ( surfaceData.smoothness - fresnelTerm ) , 0.0 , 1.0 );
    #endif
    //PicoVideo;LightMode;YangFan;End
    //PicoVideo;LightMode;Ernst;End

    //PicoVideo;LightMode;YangFan;Begin
    #ifdef USE_BLINN_PHONG
    surfaceData.specular = _BlingPhongSpecColor * surfaceData.smoothness;//PicoVideo;BlingPhongSpecular;XiaoPengCheng
    half4 color = UniversalFragmentBlinnPhong(inputData, surfaceData);
    #else
    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    #endif
    //PicoVideo;LightMode;YangFan;End

    //PicoVideo;Ecosystem;ZhengLingFeng;Begin
    #ifdef _GLOBAL_RAIN_SURFACE
    color.xyz += refleColor;
    #endif
    //PicoVideo;Ecosystem;ZhengLingFeng;End
        
    //PicoVideo;LightMode;XiaoPengCheng;Begin
    color.rgb = ApplyGlobalColor(color.rgb, _AdjustColorIntensity);
    //PicoVideo;LightMode;XiaoPengCheng;End
    
    //PicoVideo;Ecosystem;ZhengLingFeng;Begin
    #if defined(CUSTOM_FOG)
    color.rgb = FogFrag(color.rgb, inputData.viewDirectionWS, inputData.positionWS, inputData.fogCoord);
    #else
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    #endif
    //PicoVideo;Ecosystem;ZhengLingFeng;End
    
    color.a = OutputAlpha(color.a, _Surface);
    return color;
}

#endif
