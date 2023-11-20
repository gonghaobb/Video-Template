#ifndef UNIVERSAL_SIMPLE_LIT_PASS_INCLUDED
#define UNIVERSAL_SIMPLE_LIT_PASS_INCLUDED

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

struct Attributes
{
    float4 positionOS    : POSITION;
    float3 normalOS      : NORMAL;
    float4 tangentOS     : TANGENT;
    float2 texcoord      : TEXCOORD0;
    float2 lightmapUV    : TEXCOORD1;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

    float3 posWS                    : TEXCOORD2;    // xyz: posWS

#ifdef _NORMALMAP
    float4 normal                   : TEXCOORD3;    // xyz: normal, w: viewDir.x
    float4 tangent                  : TEXCOORD4;    // xyz: tangent, w: viewDir.y
    float4 bitangent                : TEXCOORD5;    // xyz: bitangent, w: viewDir.z
#else
    float3  normal                  : TEXCOORD3;
    float3 viewDir                  : TEXCOORD4;
#endif

    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD7;
#endif
    
    //PicoVideo;Ecosystem;ZhengLingFeng;Begin
    #if (defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE)) && !defined(_NORMALMAP)
    float4 tangent                  : TEXCOORD5;
    #endif
    //PicoVideo;Ecosystem;ZhengLingFeng;End

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData, float smoothness)
{
    inputData.positionWS = input.posWS;

#ifdef _NORMALMAP
    half3 viewDirWS = half3(input.normal.w, input.tangent.w, input.bitangent.w);
    inputData.normalWS = TransformTangentToWorld(normalTS,
        half3x3(input.tangent.xyz, input.bitangent.xyz, input.normal.xyz));
#else
    half3 viewDirWS = input.viewDir;
    inputData.normalWS = input.normal;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    viewDirWS = SafeNormalize(viewDirWS);

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
    //inputData.bakedGI = SAMPLE_GI_Re(input.lightmapUV, input.vertexSH, inputData.normalWS, viewDirWS, smoothness);
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Simple Lighting) shader
Varyings LitPassVertexSimple(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
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

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.posWS.xyz = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

#ifdef _NORMALMAP
    output.normal = half4(normalInput.normalWS, viewDirWS.x);
    output.tangent = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangent = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normal = NormalizeNormalPerVertex(normalInput.normalWS);
    output.viewDir = viewDirWS;
    //PicoVideo;Ecosystem;ZhengLingFeng;Begin
    #if (defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE))
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangent = half4(normalInput.tangentWS, sign);
    #endif
    //PicoVideo;Ecosystem;ZhengLingFeng;End
#endif

    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normal.xyz, output.vertexSH);

    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif
    
    return output;
}
TEXTURE2D(_NoiseTex);
SAMPLER(sampler_NoiseTex);

#ifdef _SCREEN_GI
StructuredBuffer<float> _ScreenColorBuffer;
float4 _ScreenGIDisAtten;
float3 _ScreenGIPos;
float3 _ScreenGIDir;
float _ScreenGIIntensity;
float _ScreenGIAngleAtten;
#endif
// Used for StandardSimpleLighting shader
half4 LitPassFragmentSimple(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.uv;
    half4 diffuseAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    
    half3 diffuse = diffuseAlpha.rgb * _BaseColor.rgb;

    half alpha = diffuseAlpha.a * _BaseColor.a;
    AlphaDiscard(alpha, _Cutoff);

    //PicoVideo;CloudShadow;XiaoPengCheng;Begin
    #if _GLOBAL_CLOUD_SHADOW
		diffuse = ApplyGlobalCloudShadow(diffuse, input.posWS, _CloudShadowIntensity);
	#endif
    //PicoVideo;CloudShadow;XiaoPengCheng;End

    #ifdef _ALPHAPREMULTIPLY_ON
        diffuse *= alpha;
    #endif

    half3 normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));
    half3 emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
    half4 specular = SampleSpecularSmoothness(uv, alpha, _SpecColor, TEXTURE2D_ARGS(_SpecGlossMap, sampler_SpecGlossMap));
    half smoothness = specular.a;

    //PicoVideo;Ecosystem;ZhengLingFeng;Begin
    #ifdef _GLOBAL_RAIN_SURFACE
    float3 refleColor;
    float metallic = 0;
    half occlusion = 1;
    half accumulated = 0;
    half test1;
    ComputeWetSurface(input.normal, input.posWS, normalTS, metallic, smoothness, diffuse, accumulated);
    #elif defined(_GLOBAL_SNOW_SURFACE)
    half snowTexData;
    half accumulated;
    diffuse = ComputeSnowSurface(diffuse, input.posWS, input.normal, normalTS, accumulated, snowTexData);
    #endif
    //PicoVideo;Ecosystem;ZhengLingFeng;End

    InputData inputData;
    InitializeInputData(input, normalTS, inputData, smoothness);

    //PicoVideo;WeatherSimulate;ZhengLingFeng;Begin
    #ifdef _GLOBAL_RAIN_SURFACE
    refleColor = ComputeWetSurfaceReflection(inputData.normalWS, inputData.viewDirectionWS, accumulated);
    #elif defined(_GLOBAL_SNOW_SURFACE)
    half occlusion = 1;
    ComputeSnowSurfaceColorAndAO(snowTexData, accumulated, inputData.normalWS, occlusion, diffuse);
    #endif
    //PicoVideo;WeatherSimulate;ZhengLingFeng;End
    
#ifdef _SCREEN_GI
    float3 diff = _ScreenGIPos - inputData.positionWS;
    float distanceSq = dot(diff, diff);
    float distanceAttenuation =  clamp(_ScreenGIIntensity / (_ScreenGIDisAtten.x + distanceSq * _ScreenGIDisAtten.y), _ScreenGIDisAtten.z, _ScreenGIDisAtten.w);
    diff = normalize(diff);
    float dotVal = dot(_ScreenGIDir, diff); 
    float angleAtten = step(_ScreenGIAngleAtten, dotVal);
    inputData.bakedGI += distanceAttenuation * angleAtten * half3(_ScreenColorBuffer[0],_ScreenColorBuffer[1],_ScreenColorBuffer[2]);
#endif
    
    //PicoVideo;LightMode;XiaoPengCheng;Begin
    #if _EMISSION_CUBEMAP
		half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
		half3 cubemapEmission = SAMPLE_TEXTURECUBE_LOD(_EmssionCubemap, sampler_EmssionCubemap, reflectVector, _EmissionCubemapLod).rgb;
		emission += cubemapEmission * _EmissionCubemapIntensity;
	#endif
    //PicoVideo;LightMode;XiaoPengCheng;End

    half4 color = UniversalFragmentBlinnPhong(inputData, diffuse, specular, smoothness, emission, alpha);
    
    //PicoVideo;Ecosystem;ZhengLingFeng;Begin
    #ifdef _GLOBAL_RAIN_SURFACE
    color.xyz += saturate(refleColor);
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

    // PicoVide;Bloom;ZhouShaoyang;Begin
    // color.a = OutputAlpha(color.a, _Surface);
    // PicoVide;Bloom;ZhouShaoyang;End
    return color;
}

#endif
