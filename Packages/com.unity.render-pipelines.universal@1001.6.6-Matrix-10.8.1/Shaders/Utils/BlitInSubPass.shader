Shader "Hidden/Universal Render Pipeline/BlitInSubPass"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        
        Pass
        {
            Name "BlitInSubPass"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma only_renderers vulkan
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION _SRGB_TO_LINEAR_CONVERSION
            #pragma multi_compile _SubPassMSAA1 _SubPassMSAA2 _SubPassMSAA4

            #define _USE_DRAW_PROCEDURAL 1

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "HLSLSupport.cginc"

            #if _SubPassMSAA2 || _SubPassMSAA4
            UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT_MS(0); 
            #elif _SubPassMSAA1
            UNITY_DECLARE_FRAMEBUFFER_INPUT_FLOAT(0); 
            #endif
            
            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                #if _SubPassMSAA2
                float4 col = UNITY_READ_FRAMEBUFFER_INPUT_MS(0, 0, input.positionCS) * 0.5 + UNITY_READ_FRAMEBUFFER_INPUT_MS(0, 1, input.positionCS) * 0.5;
                #elif _SubPassMSAA4
                float4 col = 0;
                [unroll(4)]
                for (int i = 0; i < 4; ++i)
                {
                    col += UNITY_READ_FRAMEBUFFER_INPUT_MS(0, i, input.positionCS) * 0.25;
                }
                #elif _SubPassMSAA1
                float4 col = UNITY_READ_FRAMEBUFFER_INPUT(0, input.positionCS);
                #endif

             #ifdef _LINEAR_TO_SRGB_CONVERSION
                col = LinearToSRGB(col);
             #endif

             #ifdef _SRGB_TO_LINEAR_CONVERSION
                col = SRGBToLinear(col);
             #endif

                return col;
            }
            ENDHLSL
        }
    }
}
