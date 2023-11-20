Shader "PXR_SDK/PXR_OESBlit"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Name "Blit"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex FullscreenVert
            #pragma fragment Fragment
            #pragma multi_compile_fragment _ _LINEAR_TO_SRGB_CONVERSION
            #pragma multi_compile_fragment _ _SRGB_TO_LINEAR_CONVERSION
            #pragma multi_compile _ _USE_DRAW_PROCEDURAL

            #include "Packages/com.unity.render-pipelines.universal/Shaders/Utils/Fullscreen.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D_X(_ExternalOESTex);
            SAMPLER(sampler_ExternalOESTex);


            int texWidth;
            int texHeight;
            int dstWidth;
            int dstHeight;
            int offsetX;
            int offsetY;

            float4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = float2(input.uv.x, input.uv.y);
                uv.x *= dstWidth / (float)texWidth; 
                uv.y *= dstHeight / (float)texHeight;
                uv += float2(offsetX / (float)texWidth, offsetY / (float)texHeight);
                uv = float2(uv.x, 1.0 - uv.y);
                
                float4 col = SAMPLE_TEXTURE2D_X(_ExternalOESTex, sampler_ExternalOESTex, uv);

                #ifdef _SRGB_TO_LINEAR_CONVERSION
                col = SRGBToLinear(col);
                #endif

                // 从PXR SDK层面支持了SRGB贴图，这里不需要转码
//                half3 srgb = col.rgb;
//                srgb = srgb * (srgb * (srgb * 0.305306011 + 0.682171111) + 0.012522878); 
//                col.rgb = srgb;

//                col.rgb = pow((col.rgb + 0.055F)/1.055F, 2.4F);
//                col.rgb = pow(abs(col.rgb), 2.2);
                return col;
            }
            ENDHLSL
        }
    }
}
