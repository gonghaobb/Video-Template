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

#ifndef MATRIX_BILLBOARD_CLOUD_FORWARDPASS_INCLUDED
#define MATRIX_BILLBOARD_CLOUD_FORWARDPASS_INCLUDED

//生态系统雾影响
#ifdef CUSTOM_FOG
    #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/FogSimulate/Resource/Shaders/Fog.hlsl"
#endif

//天气系统影响
#if defined(_GLOBAL_RAIN_SURFACE)
    #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/RainSimulate/Resource/Shader/RainSurface.hlsl"
#elif defined(_GLOBAL_SNOW_SURFACE)
    #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/SnowSimulate/Resource/Shaders/SnowSurface.hlsl"
#endif

//全局云投影
#if _GLOBAL_CLOUD_SHADOW
	#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/CloudShadowSimulate/Resource/Shaders/CloudShadow.hlsl"
#endif

#include "Packages/org.byted.vrtv.engine.common-shader/Shaders/Scene/Clound/BillboardCloudLighting.hlsl"

struct Attributes
{
	float3 positionOS : POSITION;
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
	float2 texcoord: TEXCOORD0;
	half4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

struct Varyings
{
	float2 uv: TEXCOORD0;
	DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD2;
#endif

	float3 normalWS                 : TEXCOORD3;

#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	float4 tangentWS            : TEXCOORD4;    // xyz: tangent, w: sign
#endif

	float3 viewDirWS                : TEXCOORD5;

	half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#if _DEPTH_FADE
    float4 screenPos                : TEXCOORD7;
#endif

	half4 color : COLOR;

	float4 positionCS : SV_POSITION;

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
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);
}

Varyings BillboardCloudVert(Attributes input)
{
	Varyings output = (Varyings)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if _BILLBOARD
	//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //make quad look at camera in view space
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    float3 quadPivotPosOS = float3(0,0,0);
    float3 quadPivotPosWS = TransformObjectToWorld(quadPivotPosOS);
    float3 quadPivotPosVS = TransformWorldToView(quadPivotPosWS);

    //get transform.lossyScale using:
    //https://forum.unity.com/threads/can-i-get-the-scale-in-the-transform-of-the-object-i-attach-a-shader-to-if-so-how.418345/
	float3 worldScale = float3(
    	length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x)), // scale x axis
    	length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y)), // scale y axis
    	length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z))  // scale z axis
    );

    float3 posVS = quadPivotPosVS + float3(input.positionOS * worldScale);//recontruct quad 4 points in view space
	float3 posWS = quadPivotPosWS + float3(input.positionOS * worldScale);
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //complete SV_POSITION's view space to HClip space transformation
    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    output.positionCS = TransformWViewToHClip(posVS);
#else
	float3 posWS = TransformObjectToWorld(input.positionOS);
	output.positionCS = TransformWorldToHClip(posWS);
#endif

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 viewDirWS = GetWorldSpaceViewDir(posWS);
    half3 vertexLight = VertexLighting(posWS, normalInput.normalWS);

#if defined(CUSTOM_FOG)
    half fogFactor = FogVert(posWS);
#else
    half fogFactor = ComputeFogFactor(output.positionCS.z);
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
    output.positionWS = posWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

#if _DEPTH_FADE
    output.screenPos = ComputeScreenPos(output.positionCS);
#endif

    output.color = lerp(1, input.color, _ApplyVertexColor);

    return output;
}

half4 BillboardFrag(Varyings input, half facing : VFACE) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if _CLOUD_FRAMES
        float2 uv = FlipBook(input.uv, _CloudFrameAndSpeed.x, _CloudFrameAndSpeed.y, _Time.y * _CloudFrameAndSpeed.z);
        #if _MOTIONMAP
		    uv += SAMPLE_TEXTURE2D(_CloudMotionMap, sampler_CloudMotionMap, TRANSFORM_TEX(uv, _CloudMotionMap)).rg * _CloudMotionIntensity;
        #endif
    #else
        float2 uv = input.uv;
    #endif

	#if _DISTORTION_MAP
		float2 distortionUV = uv + float2(_DistortionUSpeed, _DistortionVSpeed) * _Time.y;
        half4 distortionColor = SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, TRANSFORM_TEX(distortionUV, _DistortionMap));
        half2 distortion = (distortionColor.xy * 2 - 1) * _DistortionIntensity;

        #if _DISTORTION_MASK_MAP
            half distortionMask = dot(SAMPLE_TEXTURE2D(_DistortionMaskMap, sampler_DistortionMaskMap, TRANSFORM_TEX(uv, _DistortionMaskMap)), _DistortionMaskChannelMask);
            distortion *= distortionMask;
        #endif
    #else
        half2 distortion = half2(0, 0);
    #endif

	uv += distortion;

	#if _FLOWMAP
    	float2 flowDir = (SAMPLE_TEXTURE2D(_FlowMap, sampler_FlowMap, TRANSFORM_TEX(uv, _FlowMap)).rg * 2.0f - 1.0f) * (-_FlowDirSpeed);
    	float phase0 = frac(_Time.y * _FlowTimeSpeed);
    	float phase1 = frac(_Time.y * _FlowTimeSpeed + 0.5f);
    	float2 uvPanner1 = flowDir * phase0;
    	float2 uvPanner2 = flowDir * phase1;
		float flowLerp = abs(phase0 - 0.5f) / 0.5f;
	#endif
	
	SurfaceData surfaceData;
	#if _FLOWMAP
		InitializeBillboardCloudSurfaceData(uv, uvPanner1, uvPanner2, flowLerp, input.color, surfaceData);
	#else
    	InitializeBillboardCloudSurfaceData(uv, input.color, surfaceData);
	#endif

	InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

	#if _DISSOLVE_MAP
		float2 dissolveUV = uv + float2(_DissolveUSpeed, _DissolveVSpeed) * _Time.y;
        half disslove = dot(SAMPLE_TEXTURE2D(_DissolveMap, sampler_DissolveMap, TRANSFORM_TEX(dissolveUV, _DissolveMap)), _DissolveChannelMask);
        half dissolveWithParticle = (_DissolveIntensity * (1 + _DissolveWidth) - _DissolveWidth);
        half dissolveAlpha = saturate(smoothstep(dissolveWithParticle, (dissolveWithParticle + _DissolveWidth), disslove));
        surfaceData.alpha *= dissolveAlpha;
        surfaceData.albedo += _DissolveEdgeColor.rgb * _DissolveEdgeColor.a * _DissolveEdgeIntensity * (1 - dissolveAlpha);
    #endif

	half4 color = BillboardCloudFragmentLighting(inputData, surfaceData);

	color.rgb = ApplyGlobalColor(color.rgb, _AdjustColorIntensity);

    #if _DEPTH_FADE
        float screenEyeDepth = GetScreenEyeDepth(input.screenPos.xy / input.screenPos.w);
        float fsEyeDepth = GetFragmentEyeDepth(input.screenPos);
		float distanceDepth = abs((fsEyeDepth - screenEyeDepth) / _DepthFade);
        color.a *= saturate(distanceDepth);
    #endif

	return color;
}

#endif