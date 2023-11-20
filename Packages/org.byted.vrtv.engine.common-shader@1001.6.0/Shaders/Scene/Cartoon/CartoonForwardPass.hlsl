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

#ifndef MATRIX_CARTOON_PASS_INCLUDED
#define MATRIX_CARTOON_PASS_INCLUDED

#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/Cartoon/CartoonLighting.hlsl"

#if defined(_GLOBAL_RAIN_SURFACE)
#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/RainSimulate/Resource/Shader/RainSurface.hlsl"
#elif defined(_GLOBAL_SNOW_SURFACE)
#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/SnowSimulate/Resource/Shaders/SnowSurface.hlsl"
#endif

#if _GLOBAL_CLOUD_SHADOW
	#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/CloudShadowSimulate/Resource/Shaders/CloudShadow.hlsl"
#endif

#if defined(USE_BLINN_PHONG) && !defined(_SPECULARHIGHLIGHTS_OFF)
#define _SPECULAR_COLOR
#endif

#if defined(_NORMALMAP)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

#if defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

// keep this file in sync with LitGBufferPass.hlsl

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    half4 color         : COLOR;
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

    half4 color                     : COLOR;
    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

#if defined(_MATRIX_SPC_LIGHTINGMAP)
void InitializeInputData(Varyings input, half3 normalTS, half smoothness, half specControl, out InputData inputData)
#else
void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
#endif
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = SafeNormalize(input.viewDirWS);
#if defined(_NORMALMAP) || defined(_GLOBAL_RAIN_SURFACE)
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
    #if defined(_MATRIX_SPC_LIGHTINGMAP)
    // float fresnelNdot = dot(normalize(inputData.normalWS), inputData.viewDirectionWS);
    // fresnelNdot = (0.0 + 1.0 * pow(max(1.0 - fresnelNdot, 0.0001), 1.0));
    // float lerpResult = lerp(0.04, 1.0, fresnelNdot);
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS, smoothness, specControl, inputData.viewDirectionWS);
    #else
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    #endif
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
    // half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

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
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
#endif

    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.fogFactorAndVertexLight = half4(fogFactor, 0, 0, 0);

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;
    output.color = 1 - _ApplyVertexColor + input.color * _ApplyVertexColor;
    return output;
}

// Used in Standard (Physically Based) shader
half4 LitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, input.color, surfaceData);

    #if _GLOBAL_CLOUD_SHADOW
		surfaceData.albedo = ApplyGlobalCloudShadow(surfaceData.albedo, input.positionWS, _CloudShadowIntensity);
	#endif

    #ifdef _GLOBAL_RAIN_SURFACE
    float3 refleColor;
    half accumulated;
    ComputeWetSurface(input.normalWS, input.positionWS, surfaceData.normalTS, surfaceData.metallic, surfaceData.smoothness, surfaceData.albedo, accumulated);
    #elif defined(_GLOBAL_SNOW_SURFACE)
    half snowTexData;
    half accumulated;
    surfaceData.albedo = ComputeSnowSurface(surfaceData.albedo, input.positionWS, input.normalWS, surfaceData.normalTS, accumulated, snowTexData);
    #endif
    
    InputData inputData;
    
    #if defined(_MATRIX_SPC_LIGHTINGMAP)
    InitializeInputData(input, surfaceData.normalTS, surfaceData.smoothness, _LightmapSpecular * _GlobalLightmapSpecular, inputData); 
    #else
    InitializeInputData(input, surfaceData.normalTS, inputData);
    #endif

    #ifdef _GLOBAL_RAIN_SURFACE
    refleColor = ComputeWetSurfaceReflection(inputData.normalWS, input.viewDirWS, accumulated);
    #elif defined(_GLOBAL_SNOW_SURFACE)
    ComputeSnowSurfaceColorAndAO(snowTexData, accumulated, inputData.normalWS, surfaceData.occlusion, surfaceData.albedo);
    #endif
    
    StylizedData stylizedData;
    ResolveStylizedData(stylizedData);
    
    half4 color = CartoonUniversalFragment(inputData, surfaceData, stylizedData);
    
    #if _EMISSION && _EMISSION_CUBEMAP
		half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
		half3 cubemapEmission = SAMPLE_TEXTURECUBE_LOD(_EmssionCubemap, sampler_EmssionCubemap, reflectVector, _EmissionCubemapLod).rgb;
		color.rgb += cubemapEmission * _EmissionCubemapColor.rgb * _EmissionCubemapIntensity;
	#endif

    #ifdef _GLOBAL_RAIN_SURFACE
        color.xyz += refleColor;
    #endif
        
    color.rgb = ApplyGlobalColor(color.rgb, _AdjustColorIntensity);
    
    #if defined(CUSTOM_FOG)
    color.rgb = FogFrag(color.rgb, inputData.viewDirectionWS, inputData.positionWS, inputData.fogCoord);
    #else
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    #endif
    
    color.a = OutputAlpha(color.a, _Surface);
    return color;
}

#endif
