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

#ifndef MATRIX_GLASS_FORWARDPASS_INCLUDED
#define MATRIX_GLASS_FORWARDPASS_INCLUDED

#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/PBR/PBRLighting.hlsl"

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
    half4 color         : COLOR;
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

#if _INTERIOR_CUBEMAP
    float3 interiorViewDir          : TEXCOORD8;
    #if !_INTERIOR_TANGENT
        float3 positionOS           : TEXCOORD9;
    #endif
#endif
    half4 color                     : COLOR;

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

#if _INTERIOR_CUBEMAP
    float3 objCamPos = TransformWorldToObject(GetCameraPositionWS());
    output.interiorViewDir = objCamPos - input.positionOS.xyz;
    #if _INTERIOR_TANGENT
        float tangentSign = input.tangentOS.w * GetOddNegativeScale();
        float3 bitangent = cross(input.normalOS.xyz, input.tangentOS.xyz) * tangentSign;
        output.interiorViewDir = float3(
                    dot(output.interiorViewDir, input.tangentOS.xyz),
                    dot(output.interiorViewDir, bitangent),
                    dot(output.interiorViewDir, input.normalOS)
                    );
    #else
        output.positionOS = input.positionOS;
    #endif
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
    InitializeGlassSurfaceData(input.uv, input.color, surfaceData);

    #if _GLOBAL_CLOUD_SHADOW
		surfaceData.albedo = ApplyGlobalCloudShadow(surfaceData.albedo, input.positionWS, _CloudShadowIntensity);
	#endif

    //天气系统雨雪
    #if defined(_GLOBAL_RAIN_SURFACE)
        float3 refleColor = ComputeWetSurface(input.uv, input.normalWS, input.positionWS, input.viewDirWS, input.tangentWS, surfaceData.normalTS, surfaceData.metallic, surfaceData.smoothness);
    #elif defined(_GLOBAL_SNOW_SURFACE)
        surfaceData.albedo = ComputeSnowSurface(surfaceData.albedo, input.uv, input.positionWS, input.normalWS, input.tangentWS, surfaceData.normalTS, 0);
    #endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

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

    half3 reflectColor = half3(0.0, 0.0, 0.0);
    half3 refractColor = half3(0.0, 0.0, 0.0);
    half fresnel = 0;
    #if _INTERIOR_CUBEMAP
        input.interiorViewDir.z *= 1 / (1 - _InteriorDepthScale) - 1;
        half3 revseseViewDir = SafeNormalize(-input.interiorViewDir);

        #if _INTERIOR_TANGENT
            float2 interiorUV = frac(TRANSFORM_TEX(input.uv, _InteriorCubemap) + 0.0001);
            // raytrace box from tangent view dir
            float3 pos = float3(interiorUV * 2.0 - 1.0, 1.0);
        #else
            float3 pos = frac(input.positionOS * _InteriorCubemap_ST.xyx + _InteriorCubemap_ST.zwz + 0.0001);

            // raytrace box from object view dir
            // transform object space uvw( min max corner = (0,0,0) & (+1,+1,+1))  
            // to normalized box space(min max corner = (-1,-1,-1) & (+1,+1,+1))
            pos = pos * 2.0 - 1.0;
        #endif

        float3 id = 1.0 / revseseViewDir;
        float3 k = abs(id) - pos * id;
        float kMin = min(min(k.x, k.y), k.z);
        pos += kMin * revseseViewDir;

        refractColor += SAMPLE_TEXTURECUBE(_InteriorCubemap, sampler_InteriorCubemap, pos.xyz) * _InteriorIntensity;
    #endif

    #if _ENVIRONMENT_CUBEMAP
        half3 reflectVector = reflect(-inputData.viewDirectionWS, inputData.normalWS);
        fresnel = 1;
        reflectColor += SAMPLE_TEXTURECUBE_LOD(_EnvironmentCubeMap, sampler_EnvironmentCubeMap, reflectVector, _EnvironmentCubemapLod).rgb * _EnvironmentReflectionColor * _EnvironmentReflectionIntensity;
        refractColor += SAMPLE_TEXTURECUBE_LOD(_EnvironmentCubeMap, sampler_EnvironmentCubeMap, -inputData.viewDirectionWS, _EnvironmentCubemapLod).rgb * _EnvironmentRefractionColor * _EnvironmentRefractionIntensity;
	#endif

    #if _INTERIOR_CUBEMAP || _ENVIRONMENT_CUBEMAP
        fresnel *= saturate(dot(inputData.viewDirectionWS, inputData.normalWS)) * _GlassFresnelIntensity;
        fresnel = smoothstep(_GlassMinFresnel, _GlassMaxFresnel, fresnel);
        color.rgb += reflectColor * fresnel + refractColor * (1 - fresnel);
    #endif

    #if _FROST
        float2 frostUV = frac(TRANSFORM_TEX(input.uv, _FrostMaskMap));
        float4 frostMask = SAMPLE_TEXTURE2D(_FrostMaskMap, sampler_FrostMaskMap, frostUV);
        float uDis = min(abs(_FrostCenter.x - frostUV.x), abs(1 - _FrostCenter.x - frostUV.x));
        float vDis = min(abs(_FrostCenter.y - frostUV.y), abs(1 - _FrostCenter.y - frostUV.y));

        float dis = length(float2(uDis, vDis)) / 0.707;//斜边距离，然后归一化
        dis = _FrostReverse * (1 - dis) + (1 - _FrostReverse) * dis;//反转距离

        #if _FROST_NOISE
            dis += (1 - smoothstep(0, _FrostNoiseDistance, dis)) * SAMPLE_TEXTURE2D(_FrostNoiseMap, sampler_FrostNoiseMap, TRANSFORM_TEX(input.uv, _FrostNoiseMap)).r * _FrostNoiseIntensity;
        #endif

        float weight = 1 - smoothstep(0, _FrostDistance / 0.707, dis);
        
        weight = lerp(weight, frostMask.x, _FrostBlendFactor);//blend with mask

        half4 frostColor = weight * _FrostColor;

        color.rgb += frostColor * _FrostIntensity;
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