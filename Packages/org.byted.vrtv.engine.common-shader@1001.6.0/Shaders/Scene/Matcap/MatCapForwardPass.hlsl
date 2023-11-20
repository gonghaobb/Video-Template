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

#ifndef MATRIX_MATCAP_FORWARDPASS_INCLUDED
#define MATRIX_MATCAP_FORWARDPASS_INCLUDED

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

#if (_MATCAP || _RIM_LIGHT || _EMISSION_CUBEMAP || _SWEEP_LIGHT || _CARTOON) || defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE)
    #define REQUIRES_WORLDNORMAL
#endif

#if (defined(REQUIRES_WORLDNORMAL) && _NORMALMAP) || defined(_GLOBAL_RAIN_SURFACE) || defined(_GLOBAL_SNOW_SURFACE) || _SWEEP_LIGHT
    #define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

struct Attributes
{
	float3 positionOS : POSITION;

#if defined(REQUIRES_WORLDNORMAL)
	float3 normalOS : NORMAL;
	float4 tangentOS : TANGENT;
#endif

	float2 uv: TEXCOORD0;
	half4 color : COLOR;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

struct Varyings
{
	float4 uv: TEXCOORD0;

    float4 uv2: TEXCOORD1;

	float4 uv3: TEXCOORD2;

	float4 normalWSAndFogFactor     : TEXCOORD3;

#if _MATCAP && _MATCAP_FIX_EDGE_FLAW
	float3 positionVS            : TEXCOORD4;
#endif

#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	float4 tangentWS            : TEXCOORD5;    // xyz: tangent, w: sign
#endif

	float3 positionWS               : TEXCOORD6;

	float3 viewDirWS                : TEXCOORD7;

#if (_MAIN_LIGHT_SHADOWS && _ENABLE_LIGHT_AFFECT) || (_CARTOON && !_CUSTOM_CARTOON_LIGHT)
    float4 shadowCoord              : TEXCOORD8;
#endif

	half4 color : COLOR;

	float4 positionCS : SV_POSITION;

	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

Varyings MatcapVert(Attributes input)
{
	Varyings output = (Varyings)0;

	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_TRANSFER_INSTANCE_ID(input, output);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

	output.uv.xy = TRANSFORM_TEX(input.uv, _BaseMap);
    output.uv.zw = TRANSFORM_TEX(input.uv, _NormalMap);

	output.positionWS = TransformObjectToWorld(input.positionOS);

#if _EMISSION
	half2 uvOffsetEmission = half2(_EmissioUSpeed, _EmissioVSpeed) * _Time.y;
	output.uv2.xy = TRANSFORM_TEX(lerp(input.uv, output.positionWS.xy, _EmissionWorldPosUV), _EmissionMap) + uvOffsetEmission;
#endif

#if _RIM_LIGHT && _RIM_REFRACTIONY
	output.uv2.zw = TRANSFORM_TEX(lerp(input.uv, output.positionWS.xy, _RimRefractionWorldPosUV), _RimRefractionMap);
#endif

#if _SWEEP_LIGHT
	output.uv3.xy = TRANSFORM_TEX(input.uv, _SweepLightMap) + half2(_SweepLightUSpeed, _SweepLightVSpeed) * _Time.y;
	output.uv3.zw = TRANSFORM_TEX(input.uv, _SweepLightMaskMap);
#endif

#if _MATCAP && _MATCAP_FIX_EDGE_FLAW
	output.positionVS = TransformWorldToView(output.positionWS);
#endif

	output.viewDirWS = GetWorldSpaceViewDir(output.positionWS);
	output.positionCS = TransformWorldToHClip(output.positionWS);

#if defined(REQUIRES_WORLDNORMAL)
	VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
	output.normalWSAndFogFactor.xyz = normalInput.normalWS;
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
	    half sign = input.tangentOS.w * GetOddNegativeScale();
	    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
    #endif
#endif

#if (_MAIN_LIGHT_SHADOWS && _ENABLE_LIGHT_AFFECT) || (_CARTOON && !_CUSTOM_CARTOON_LIGHT)
    output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
#endif

#if CUSTOM_FOG
	output.normalWSAndFogFactor.w = FogVert(output.positionWS);
#else
	output.normalWSAndFogFactor.w = ComputeFogFactor(output.positionCS.z);
#endif

	output.color = input.color;

	return output;
}

half4 MatcapFrag(Varyings input, half facing : VFACE) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(input);
	UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

	half4 baseColor = _BaseColor * (_ApplyVertexColor * input.color + (1 - _ApplyVertexColor));

	#if _ALPHAMAP_ON
		baseColor.a *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy).r * _AlphaScale;
	#else
		baseColor *= SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv.xy);
	#endif

	#if _ALPHATEST_ON
		clip(baseColor.a - _Cutoff);
	#endif

	#if _GLOBAL_CLOUD_SHADOW
		baseColor.rgb = ApplyGlobalCloudShadow(baseColor.rgb, input.positionWS, _CloudShadowIntensity);
	#endif

	#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
		float sgn = input.tangentWS.w;      // should be either +1 or -1
		float3 bitangent = sgn * cross(input.normalWSAndFogFactor.xyz, input.tangentWS.xyz);
		half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWSAndFogFactor.xyz);
	#endif

	#if defined(REQUIRES_WORLDNORMAL)
		#if _NORMALMAP
			half3 normalTS = SampleNormalRG(input.uv.zw + half2(_NormalUSpeed, _NormalVSpeed) * _Time.y, TEXTURE2D_ARGS(_NormalMap, sampler_NormalMap), _NormalScale);
			half3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
		#else
			half3 normalTS = half3(0, 0, 1);
			half3 normalWS = input.normalWSAndFogFactor.xyz;
		#endif
        normalWS = NormalizeNormalPerPixel(normalWS * facing);
	#endif
	
	#if _RIM_LIGHT || (_EMISSION && _EMISSION_CUBEMAP) || _SWEEP_LIGHT || _GLOBAL_RAIN_SURFACE || CUSTOM_FOG
		half3 viewDirWS = SafeNormalize(input.viewDirWS);
	#endif

	#if defined(_GLOBAL_RAIN_SURFACE)
		half metallic = 0.5;
		half smoothness = 0;
		float3 refleColor = ComputeWetSurface(input.uv.xy, normalWS, input.positionWS, viewDirWS, input.tangentWS, normalTS, metallic, smoothness);
	#elif _GLOBAL_SNOW_SURFACE
		baseColor.rgb = ComputeSnowSurface(baseColor.rgb, input.uv.xy, input.positionWS, normalWS, input.tangentWS, normalTS, 0);
	#endif

	half4 color = baseColor;

	#if _RIM_LIGHT || (_EMISSION && _EMISSION_CUBEMAP)
		half cosTheta = saturate(dot(normalWS, viewDirWS));
	#endif

	#if _RIM_LIGHT && _RIM_REFRACTIONY
		half rimRefractionStrength = pow(saturate(1 - 1 / _RimRefractionWidth * cosTheta), _RimRefractionSmoothness);
        rimRefractionStrength = lerp(rimRefractionStrength, 1 - rimRefractionStrength, _RimRefractionReverse) * _RimRefractionIntensity;
        rimRefractionStrength = smoothstep(_RimRefractionMinValue, _RimRefractionMaxValue, rimRefractionStrength);
		half2 distortion = rimRefractionStrength * SAMPLE_TEXTURE2D(_RimRefractionMap, sampler_RimRefractionMap, input.uv2.zw);
    #endif

	#if _MATCAP
		float3 viewNormal = mul((float3x3)GetWorldToViewMatrix(), normalWS);
		#if _MATCAP_FIX_EDGE_FLAW
			float3 vTangent = normalize(cross(input.positionVS, float3(0,1,0)));
			float3 vBinormal = normalize(cross(-input.positionVS, vTangent));
			float2 matCapUV = float2(dot(vTangent, viewNormal), dot(vBinormal, viewNormal)) * 0.5 * _MatcapUVScale + 0.5;
		#else
			float2 matCapUV = viewNormal.xy * 0.5 * _MatcapUVScale + 0.5;
		#endif

		#if _RIM_LIGHT && _RIM_REFRACTIONY
			matCapUV += distortion;
		#endif

		half3 matcap = SAMPLE_TEXTURE2D(_Matcap, sampler_Matcap, matCapUV).rgb;
		color.rgb *= matcap * _MatcapStrength;

		#if _MATCAP_HIGHLIGHTS
			half3 highLights = saturate(matcap - _MatCapHighLightsThreshold) / (1.00001 - _MatCapHighLightsThreshold);
			color.rgb += matcap * _MatCapHighLightsStrength * highLights * _MatCapHighLightsColor.rgb;
		#endif
	#endif

	#if (_MAIN_LIGHT_SHADOWS && _ENABLE_LIGHT_AFFECT) || (_CARTOON && !_CUSTOM_CARTOON_LIGHT)
		Light mainLight = GetMainLight(input.shadowCoord);
	#endif

	#if _MAIN_LIGHT_SHADOWS && _ENABLE_LIGHT_AFFECT
		color.rgb *= GetAdjustLightColor(mainLight, _RealTimeLightStrength, _RealTimeShadowStrength, _RealTimeShadowColor, _RealTimeShadowColorStrength);
	#endif

	#if _CARTOON
		StylizedData stylizedData;
		stylizedData.shadowColorFirst = _ShadowColorFirst;
    	stylizedData.shadowColorSecond = _ShadowColorSecond;
    	stylizedData.shadowColorThird = _ShadowColorThird;
    	stylizedData.shadowBoundaryFirst = _ShadowBoundaryFirst;
    	stylizedData.shadowBoundarySecond = _ShadowBoundarySecond;
    	stylizedData.shadowBoundaryThird = _ShadowBoundaryThird;
    	stylizedData.shadowSmoothFirst = _ShadowSmoothFirst;
    	stylizedData.shadowSmoothSecond = _ShadowSmoothSecond;
    	stylizedData.shadowSmoothThird = _ShadowSmoothThird;
    	stylizedData.shadowAreaFirst = _ShadowAreaFirst;
    	stylizedData.shadowAreaSecond = _ShadowAreaSecond;
    	stylizedData.shadowAreaThird = _ShadowAreaThird;

		#if _CUSTOM_CARTOON_LIGHT
			half3 attenuatedLightColor = _CustomDirectionLightData1.xyz * _CustomDirectionLightData0.x;
    		half3 diffuseColor = CartoonLighting(attenuatedLightColor, _CustomDirectionLightData0.yzw, normalWS, stylizedData);
		#else
			half3 attenuatedLightColor = mainLight.color * mainLight.distanceAttenuation;
    		half3 diffuseColor = CartoonLighting(attenuatedLightColor, mainLight.direction, normalWS, stylizedData);
		#endif
		color.rgb *= diffuseColor;
	#endif

    #if _RIM_LIGHT
        half rimLightStrength = pow(saturate(1 - 1 / _RimLightWidth * cosTheta), _RimLightSmoothness);
        rimLightStrength = lerp(rimLightStrength, 1 - rimLightStrength, _RimLightReverse) * _RimLightIntensity;
        rimLightStrength = smoothstep(_RimLightMinValue, _RimLightMaxValue, rimLightStrength);
		rimLightStrength *= _EnableRimLightVertexColorMask * dot(input.color, _RimLightVertexColorMask) + (1 - _EnableRimLightVertexColorMask);
        color.rgb += _RimLightColor * rimLightStrength;

		#if _RIM_TRANSPARENCY
			half rimTransparencyStrength = pow(saturate(1 - 1 / _RimTransparencyWidth * cosTheta), _RimTransparencySmoothness);
        	rimTransparencyStrength = lerp(rimTransparencyStrength, 1 - rimTransparencyStrength, _RimTransparencyReverse) * _RimTransparencyIntensity;
        	rimTransparencyStrength = smoothstep(_RimTransparencyMinValue, _RimTransparencyMaxValue, rimTransparencyStrength);
			color.a *= _RimTransparencyBaseAlpha + rimTransparencyStrength;
		#endif
    #endif

	#if _EMISSION
		float2 emissionUV = input.uv2.xy;
		#if _RIM_LIGHT && _RIM_REFRACTIONY
			emissionUV += distortion;
		#endif

		half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, emissionUV).rgb * _EmissionColor.rgb * _EmissionIntensity;
        color.rgb += emission;
		#if _EMISSION_CUBEMAP
			half3 reflectVector = reflect(-viewDirWS, normalWS);
			#if _RIM_LIGHT && _RIM_REFRACTIONY //边缘折射扭曲
					reflectVector.xy += distortion;
			#endif

			#if _BOX_PROJECTION_CUBEMAP //Box投影反射
				reflectVector = BoxProjectedCubemapDirection(reflectVector, input.positionWS, _EmissionCubemapBoxPostion, _EmissionCubemapBoxMin, _EmissionCubemapBoxMax);
			#endif

			half lodBias = saturate(cosTheta) * _EmissionCubemapRimLodIntensity;
			#if _EMISSION_CUBEMAP_LOD
				half3 cubemapEmission = SAMPLE_TEXTURECUBE_LOD(_EmssionCubemap, sampler_EmssionCubemap, reflectVector, _EmissionCubemapLod + _EmissionCubemapBias + lodBias).rgb;
			#else
				half3 cubemapEmission = SAMPLE_TEXTURECUBE_BIAS(_EmssionCubemap, sampler_EmssionCubemap, reflectVector, _EmissionCubemapBias + lodBias).rgb;
			#endif

			color.rgb += cubemapEmission * _EmissionCubemapColor.rgb * _EmissionCubemapIntensity;
		#endif
	#endif

	#if _SWEEP_LIGHT
		half3 viewTS = TransformWorldToTangent(viewDirWS, tangentToWorld);
		viewTS = normalize(viewTS);
		half3 sweepLight = SAMPLE_TEXTURE2D(_SweepLightMap, sampler_SweepLightMap, input.uv3.xy).rgb * _SweepLightColor;
		half sweepLightMask = SAMPLE_TEXTURE2D(_SweepLightMaskMap, sampler_SweepLightMaskMap, input.uv3.zw + _SweepLightViewDirOffsetIntensity * viewTS.xy).r;
		color.rgb += sweepLight * sweepLightMask;
	#endif

	#ifdef _GLOBAL_RAIN_SURFACE
		color.rgb += refleColor;
	#endif

	color.rgb = ApplyGlobalColor(color.rgb, _AdjustColorIntensity);
	
	#if CUSTOM_FOG
		color.rgb = FogFrag(color.rgb, viewDirWS, input.positionWS, input.normalWSAndFogFactor.w);
	#else
		color.rgb = MixFog(color.rgb, input.normalWSAndFogFactor.w);
	#endif

	return color;
}

#endif