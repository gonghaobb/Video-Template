Shader "Hidden/Universal Render Pipeline/FinalPost"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        //PicoVideo;FSR;ZhengLingFeng;Begin
        #pragma multi_compile_local_fragment _ _POINT_SAMPLING _RCAS
        //PicoVideo;FSR;ZhengLingFeng;End
        //#pragma multi_compile_local_fragment _ _FXAA
        //#pragma multi_compile_local_fragment _ _FILM_GRAIN
        //#pragma multi_compile_local_fragment _ _DITHERING
        #pragma multi_compile_local_fragment _ _LINEAR_TO_SRGB_CONVERSION
        #pragma multi_compile _ _USE_DRAW_PROCEDURAL

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        TEXTURE2D_X(_SourceTex);
        TEXTURE2D(_Grain_Texture);
        TEXTURE2D(_BlueNoise_Texture);

        float4 _SourceSize;
        float2 _Grain_Params;
        float4 _Grain_TilingParams;
        float4 _Dithering_Params;

        #define GrainIntensity          _Grain_Params.x
        #define GrainResponse           _Grain_Params.y
        #define GrainScale              _Grain_TilingParams.xy
        #define GrainOffset             _Grain_TilingParams.zw

        #define DitheringScale          _Dithering_Params.xy
        #define DitheringOffset         _Dithering_Params.zw
    
        //PicoVideo;FSR;ZhengLingFeng;Begin
        #if SHADER_TARGET >= 45
            #define FSR_INPUT_TEXTURE _SourceTex
            #define FSR_INPUT_SAMPLER sampler_LinearClamp

            #include "Packages/com.unity.render-pipelines.universal/Runtime/Extend/FSR/PostProcessing/Shaders/FSRCommon.hlsl"
        #endif

        //移动到Common.hlsl
        // #define FXAA_SPAN_MAX           (8.0)
        // #define FXAA_REDUCE_MUL         (1.0 / 8.0)
        // #define FXAA_REDUCE_MIN         (1.0 / 128.0)
        //
        // half3 Fetch(float2 coords, float2 offset)
        // {
        //     float2 uv = coords + offset;
        //     return SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv).xyz;
        // }
        //
        // half3 Load(int2 icoords, int idx, int idy)
        // {
        //     #if SHADER_API_GLES
        //     float2 uv = (icoords + int2(idx, idy)) * _SourceSize.zw;
        //     return SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv).xyz;
        //     #else
        //     return LOAD_TEXTURE2D_X(_SourceTex, clamp(icoords + int2(idx, idy), 0, _SourceSize.xy - 1.0)).xyz;
        //     #endif
        // }
        //PicoVideo;FSR;ZhengLingFeng;End

        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            float2 positionNDC = uv;
            int2   positionSS  = uv * _SourceSize.xy;

            //PicoVideo;CompositorLayers;Ernst;Begin
            half alpha = 1.0f;
            //PicoVideo;CompositorLayers;Ernst;End

            //PicoVideo;FSR;ZhengLingFeng;Begin
            #if _POINT_SAMPLING
            half4 col = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_PointClamp, uv);
            half3 color = col.rgb;
            alpha = col.a;
            #elif _RCAS && SHADER_TARGET >= 45
            half4 col = ApplyRCAS(positionSS);
            half3 color = col.rgb;
            alpha = col.a;
            // When Unity is configured to use gamma color encoding, we must convert back from linear after RCAS is performed.
            // (The input color data for this shader variant is always linearly encoded because RCAS requires it)
            #if UNITY_COLORSPACE_GAMMA
            color = LinearToSRGB(color);
            #endif
            #else
            half3 color = SAMPLE_TEXTURE2D_X(_SourceTex, sampler_LinearClamp, uv).xyz;
            #endif

            #if _FXAA
            {
                color = ApplyFXAA(color, positionNDC, positionSS, _SourceSize, _SourceTex);
            }
            #endif
            //PicoVideo;FSR;ZhengLingFeng;End

            #if _FILM_GRAIN
            {
                color = ApplyGrain(color, positionNDC, TEXTURE2D_ARGS(_Grain_Texture, sampler_LinearRepeat), GrainIntensity, GrainResponse, GrainScale, GrainOffset);
            }
            #endif
			
			#if _LINEAR_TO_SRGB_CONVERSION
            {
                color = LinearToSRGB(color);
            }
            #endif

            #if _DITHERING
            {
                color = ApplyDithering(color, positionNDC, TEXTURE2D_ARGS(_BlueNoise_Texture, sampler_PointRepeat), DitheringScale, DitheringOffset);
            }
            #endif

            //PicoVideo;CompositorLayers;Ernst;Begin
            return half4(color, alpha);
            //PicoVideo;CompositorLayers;Ernst;End
        }

    ENDHLSL
    
    //PicoVideo;FSR;ZhengLingFeng;Begin
    /// Standard FinalPost shader variant with support for FSR
    /// Note: FSR requires shader target 4.5 because it relies on texture gather instructions
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "FinalPost"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment Frag
                #pragma target 4.5
            ENDHLSL
        }
    }
    //PicoVideo;FSR;ZhengLingFeng;End
    
    /// Fallback version of FinalPost shader which lacks support for FSR
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "FinalPost"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
