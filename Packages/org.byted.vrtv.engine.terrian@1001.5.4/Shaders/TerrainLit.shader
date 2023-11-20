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

Shader "PicoVideo/Scene/TerrainLit"
{
    Properties
    {
        [Main(_SurfaceOptions, _, on, off)]_SurfaceOptions("Surface Options", Float) = 0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.SurfaceType,1,_Blend)]_Surface("Surface Type", Float) = 0
        [HideInInspector][ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.BlendMode)] _Blend("Blending Mode", Float) = 0.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.RenderFace)]_Cull("Render Face", Float) = 2
        [ExtendToggle(_SurfaceOptions, _ALPHATEST_ON, _Cutoff)] _AlphaClip("Alpha Clipping", Float) = 0.0
		[HideInInspector]_Cutoff("Threshold", Range(0.0, 1.0)) = 0.5
		[SubToggle(_SurfaceOptions)] _ReceiveShadows("Receive Shadows", Float) = 1.0
        [ExtendEnum(_SurfaceOptions,Matrix.ShaderGUI.LightingMode,1,_BlingPhongSpecColor)]_LightingMode("Lighting Mode", Float) = 0
        [HideInInspector][Sub(_SurfaceOptions)] [HDR]_BlingPhongSpecColor("SpecColor", Color) = (0,0,0,1)
        [SubToggle(_SurfaceOptions)] _UV2Weight("Use UV2 WeightMap", Float) = 0.0

        [Main(WeightBlendGroup, _, on, off)]_WeightBlendGroup("Weight Blend", Float) = 0
        [Sub(WeightBlendGroup)]_ControlMap("Control Weight Map (RGBA 4 Layer Weight)" , 2D) = "white" {}
        [Sub(WeightBlendGroup)]_FirstLayerWeight("First Layer Weight", Range(0.0, 1.0)) = 1
        [Sub(WeightBlendGroup)]_SecondLayerWeight("Second Layer Weight", Range(0.0, 1.0)) = 1
        [Sub(WeightBlendGroup)]_ThirdLayerWeight("Third Layer Weight", Range(0.0, 1.0)) = 1
        [Sub(WeightBlendGroup)]_FourLayerWeight("Four Layer Weight", Range(0.0, 1.0)) = 1

        [Main(HeightBlendGroup, _HEIGHT_BLEND)]_HeightBlendGroup("Height Blend", Float) = 0
        [Sub(HeightBlendGroup)]_HeightMap("Layer Height Map (RGBA 4 Layer Height)" , 2D) = "white" {}
        [Sub(HeightBlendGroup)]_FirsLayerTilingOffset("First Layer TilingOffset", Vector) = (1, 1, 0, 0)
        [Sub(HeightBlendGroup)]_SecondLayerTilingOffset("Second Layer TilingOffset", Vector) = (1, 1, 0, 0)
        [Sub(HeightBlendGroup)]_ThirdLayerTilingOffset("Third Layer TilingOffset", Vector) = (1, 1, 0, 0)
        [Sub(HeightBlendGroup)]_FourLayerTilingOffset("Four Height TilingOffset", Vector) = (1, 1, 0, 0)
        [Sub(HeightBlendGroup)]_FirstLayerHeight("First Layer Height", Range(0.0, 1.0)) = 1
        [Sub(HeightBlendGroup)]_SecondLayerHeight("Second Layer Height", Range(0.0, 1.0)) = 1
        [Sub(HeightBlendGroup)]_ThirdLayerHeight("Third Layer Height", Range(0.0, 1.0)) = 1
        [Sub(HeightBlendGroup)]_FourLayerHeight("Four Layer Height", Range(0.0, 1.0)) = 1
        [Sub(HeightBlendGroup)]_HeightBlendFactor("Height Blend Factor", Range(0.01, 1)) = 0.1

		[Main(_FirstLayerGroup, _, on, off)]_SurfaceInputs("First Layer", Float) = 0
		[Sub(_FirstLayerGroup)][MainTexture] _BaseMap("Base Map (RGB), AO(A)", 2D) = "white" {}
		[Sub(_FirstLayerGroup)][HDR][MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [SubToggle(_FirstLayerGroup)] _ApplyVertexColor("Apply Vertex Color", Float) = 0
        [Sub(_FirstLayerGroup)]_FirstAlphaMap("Alpha Map" , 2D) = "white" {}
        [Sub(_FirstLayerGroup)]_FirstNormalMap("Normal Map (RG), Metallic(B), Smoothness(A)", 2D) = "white" {}
        [Sub(_FirstLayerGroup)]_FirstNormalScale("Normal Scale", Float) = 1.0
        [Sub(_FirstLayerGroup)]_FirstMetallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Sub(_FirstLayerGroup)]_FirstSmoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Sub(_FirstLayerGroup)]_FirstOcclusionStrength("Occlusion", Range(0.0, 1.0)) = 1.0
		
        [Main(_SecondLayerGroup, _SECOND_LAYER_BLEND)]_SecondLayerGroup("Second Layer", Float) = 0
        [Sub(_SecondLayerGroup)]_SecondBaseMap("Base Map (RGB), AO(A)", 2D) = "white" {}
        [Sub(_SecondLayerGroup)][HDR]_SecondBaseColor("Base Color", Color) = (1,1,1,1)
        [Sub(_SecondLayerGroup)]_SecondAlphaMap("Alpha Map" , 2D) = "white" {}
        [Sub(_SecondLayerGroup)]_SecondNormalMap("Normal Map(RG), Metallic(B), Smoothness(A)" , 2D) = "white" {}
        [Sub(_SecondLayerGroup)]_SecondNormalScale("Normal Scale", Float) = 1.0
        [Sub(_SecondLayerGroup)]_SecondMetallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Sub(_SecondLayerGroup)]_SecondSmoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Sub(_SecondLayerGroup)]_SecondOcclusionStrength("Occlusion", Range(0.0, 1.0)) = 1.0

        [Main(_ThirdLayerGroup, _THIRD_LAYER_BLEND)]_ThirdLayerGroup("Third Layer", Float) = 0
        [Sub(_ThirdLayerGroup)]_ThirdBaseMap("Base Map (RGB), AO(A)", 2D) = "white" {}
        [Sub(_ThirdLayerGroup)][HDR]_ThirdBaseColor("Base Color", Color) = (1,1,1,1)
        [Sub(_ThirdLayerGroup)]_ThirdAlphaMap("Alpha Map" , 2D) = "white" {}
        [Sub(_ThirdLayerGroup)]_ThirdNormalMap("Normal Map(RG), Metallic(B), Smoothness(A)" , 2D) = "white" {}
        [Sub(_ThirdLayerGroup)]_ThirdNormalScale("Normal Scale", Float) = 1.0
        [Sub(_ThirdLayerGroup)]_ThirdMetallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Sub(_ThirdLayerGroup)]_ThirdSmoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Sub(_ThirdLayerGroup)]_ThirdOcclusionStrength("Occlusion", Range(0.0, 1.0)) = 1.0
        
        [Main(_FourLayerGroup, _FOUR_LAYER_BLEND)]_FourLayerGroup("Four Layer", Float) = 0
        [Sub(_FourLayerGroup)]_FourBaseMap("Base Map (RGB), AO(A)", 2D) = "white" {}
        [Sub(_FourLayerGroup)][HDR]_FourBaseColor("Base Color", Color) = (1,1,1,1)
        [Sub(_FourLayerGroup)]_FourAlphaMap("Alpha Map" , 2D) = "white" {}
        [Sub(_FourLayerGroup)]_FourNormalMap("Normal Map(RG), Metallic(B), Smoothness(A)" , 2D) = "white" {}
        [Sub(_FourLayerGroup)]_FourNormalScale("Normal Scale", Float) = 1.0
        [Sub(_FourLayerGroup)]_FourMetallic("Metallic", Range(0.0, 1.0)) = 0.0
        [Sub(_FourLayerGroup)]_FourSmoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        [Sub(_FourLayerGroup)]_FourOcclusionStrength("Occlusion", Range(0.0, 1.0)) = 1.0

        [Main(_EmissionOptions, _EMISSION)]_EmissionOptions("Emission Options", Float) = 0
		[Sub(_EmissionOptions)]_EmissionMap("Emission Map", 2D) = "white" {}
        [Sub(_EmissionOptions)][HDR]_EmissionColor("EmissionColor", Color) = (0,0,0,1)
        [Sub(_EmissionOptions)]_EmissionIntensity("Emission Intensity", Range(0.01, 10)) = 1
        [Title(_EmissionOptions, Cubemap)]
        [Tex(_EmissionOptions, _EmissionCubemapColor)]_EmssionCubemap("Emssion Cubemap", Cube) = "" {}
        [HideInInspector][HDR]_EmissionCubemapColor("EmissionCubemapColor", Color) = (1,1,1,1)
        [Sub(_EmissionOptions)]_EmissionCubemapIntensity("Emission Cubemap Intensity", Range(0.01, 10)) = 1
        [Sub(_EmissionOptions)]_EmissionCubemapLod("Emission Cubemap Lod", Range(0, 10)) = 0

        [Main(_AdvancedOptions, _, on, off)]_AdvancedOptions("Advanced Options", Float) = 0
		[SubToggle(_AdvancedOptions)] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [SubToggle(_AdvancedOptions, _CUSTOM_SPEC_LIGHT_DIRECTION)]_EnableCustomSpecular("Enable Custom Specular", Float) = 0
        [Sub(_AdvancedOptions_CUSTOM_SPEC_LIGHT_DIRECTION)]_CustomSpecularIntensity("Custom Specular Intensity", Range(0,1)) = 1
        [SubToggle(_AdvancedOptions)] _EnvironmentReflections("Environment Reflections", Float) = 1.0
        [SubToggle(_AdvancedOptions, _REFLECTION_PROBE_BOX_PROJECTION)] _EnableBoxProjection("Enable Box Projection", float) = 0
        [KWEnum(_AdvancedOptions, None, _, Rain, _GLOBAL_RAIN_SURFACE, Snow, _GLOBAL_SNOW_SURFACE)] _WeatherType("WeatherType", float) = 0
        [SubToggle(_AdvancedOptions, CUSTOM_FOG_FRAGMENT)] _CustomFogFragment("Custom Fog Use Fragment", float) = 0
        [ExtendToggle(_AdvancedOptions, _GLOBAL_CLOUD_SHADOW, _CloudShadowIntensity)] _GlobalCloudShadow("Global Cloud Shadow", float) = 0
        [HideInInspector][Sub(_AdvancedOptions)] _CloudShadowIntensity("Global Cloud Shadow Intensity", Range(0, 1)) = 1
        [Sub(_AdvancedOptions)] _AdjustColorIntensity("Global Adjust Color Intensity", Range(0, 1)) = 1
        [Sub(_AdvancedOptions)] _QueueOffset("Priority", Range(-50, 50)) = 0

        // ObsoleteProperties
        [HideInInspector] _MainTex("BaseMap", 2D) = "white" {}
        [HideInInspector] _Color("Base Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _GlossMapScale("Smoothness", Float) = 0.0
        [HideInInspector] _Glossiness("Smoothness", Float) = 0.0
        [HideInInspector] _GlossyReflections("EnvironmentReflections", Float) = 0.0

		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _ZWrite("__zw", Float) = 1.0

        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags
        {
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        LOD 300

        HLSLINCLUDE

        #pragma shader_feature_local_fragment _ALPHATEST_ON
        #pragma shader_feature_local _ _PBR _BLING_PHONG
        #pragma shader_feature_local_fragment _HEIGHT_BLEND
        #pragma shader_feature_local _SECOND_LAYER_BLEND
        #pragma shader_feature_local _THIRD_LAYER_BLEND
        #pragma shader_feature_local _FOUR_LAYER_BLEND
        #pragma shader_feature_local_fragment _FIRST_ALPHAMAP_ON
        #pragma shader_feature_local_fragment _SECOND_ALPHAMAP_ON
        #pragma shader_feature_local_fragment _THIRD_ALPHAMAP_ON
        #pragma shader_feature_local_fragment _FOUR_ALPHAMAP_ON
        #pragma shader_feature_local _FIRST_NORMALMAP
        #pragma shader_feature_local _SECOND_NORMALMAP
        #pragma shader_feature_local _THIRD_NORMALMAP
        #pragma shader_feature_local _FOUR_NORMALMAP

        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHAPREMULTIPLY_ON
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _EMISSION_CUBEMAP
            
            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS// _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ /*_ADDITIONAL_LIGHTS_VERTEX*/ _ADDITIONAL_LIGHTS
            //#pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            //#pragma multi_compile_fragment _ _SHADOWS_SOFT
            //#pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION

            // -------------------------------------
            // Unity defined keywords
            //#pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            //#pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma shader_feature_local_fragment _ _CUSTOM_SPEC_LIGHT_DIRECTION
			#pragma shader_feature_local_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ CUSTOM_FOG
            #pragma shader_feature _ CUSTOM_FOG_FRAGMENT
            #pragma shader_feature_local_fragment _GLOBAL_CLOUD_SHADOW
            #pragma shader_feature_local _ _GLOBAL_RAIN_SURFACE _GLOBAL_SNOW_SURFACE

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Includes/TerrainLitInput.hlsl"
            #include "Includes/TerrainLitForwardPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Includes/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "Includes/TerrainLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMeta

            #pragma shader_feature_local _LAYER_NORMALMAP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _EMISSION_CUBEMAP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"
            #include "Includes/TerrainLitInput.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv0          : TEXCOORD0;
                float2 uv1          : TEXCOORD1;
                float2 uv2          : TEXCOORD2;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;

                float3 normalWS     : TEXCOORD1;

                #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
                    float4 tangentWS                : TEXCOORD2;    // xyz: tangent, w: sign
                #endif

                #if (_EMISSION && _EMISSION_CUBEMAP)
                    float3 viewDirWS            : TEXCOORD3;
                #endif

                half4 color : COLOR;
                float2 uv2                       : TEXCOORD4;
            };

            Varyings UniversalVertexMeta(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = MetaVertexPosition(input.positionOS, input.uv1, input.uv2, unity_LightmapST, unity_DynamicLightmapST);
                output.uv = input.uv0;
                output.uv2 = input.uv1;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
                    real sign = input.tangentOS.w * GetOddNegativeScale();
                    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
                #endif

                #if (_EMISSION && _EMISSION_CUBEMAP)
                    float3 positionWS = TransformObjectToWorld(input.positionOS);
                    output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                #endif
                
                output.color = 1 - _ApplyVertexColor + input.color * _ApplyVertexColor;
                return output;
            }

            half4 UniversalFragmentMeta(Varyings input) : SV_Target
            {
                SurfaceData surfaceData;
                InitializeTerrainLitSurfaceData(input.uv, _UV2Weight * input.uv2 + (1 - _UV2Weight) * input.uv, input.color, surfaceData);

                #if _EMISSION && _EMISSION_CUBEMAP
                    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
                        float sgn = input.tangentWS.w;      // should be either +1 or -1
                        float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
                        half3 normalWS = TransformTangentToWorld(surfaceData.normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
                    #else
                        half3 normalWS = input.normalWS;
                    #endif
                        
                    half3 reflectVector = reflect(-SafeNormalize(input.viewDirWS), inputData.normalWS);
                    half3 cubemapEmission = SAMPLE_TEXTURECUBE(_EmssionCubemap, sampler_EmssionCubemap, reflectVector).rgb;
                    surfaceData.emission += cubemapEmission;
	            #endif

                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

                MetaInput metaInput;
                metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
                metaInput.SpecularColor = surfaceData.specular;
                metaInput.Emission = surfaceData.emission;

                return MetaFragment(metaInput);
            }

            ENDHLSL
        }
    	//PicoVideo;Ecosystem;ZhengLingFeng;Begin
        UsePass "PicoVideo/EcosystemSimulate/HeightMapSimulate/HeightMap"
    	//PicoVideo;Ecosystem;ZhengLingFeng;End
    }
    
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "Matrix.ShaderGUI.TerrainLitShaderGUI"
}