Shader "Hidden/Universal Render Pipeline/DebugPost"
{
    HLSLINCLUDE
        #pragma exclude_renderers gles
        #pragma multi_compile_fragment _DEBUG_MODE_1 _DEBUG_MODE_2 _DEBUG_MODE_3 _DEBUG_MODE_4
        #define _USE_DRAW_PROCEDURAL 1
    
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        uniform int _DebugPassMode;
        uniform TEXTURE2D_X(_CameraMVTexture);          //1
        uniform TEXTURE2D_X(_CameraDepthTexture);       //2
        uniform TEXTURE2D_X(_CameraOpaqueTexture);      //3
        uniform TEXTURE2D_X(_CameraTransparentTexture); //4

        half4 Frag(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.uv);
            half3 color = (0.0).xxx;
            half alpha = 1.0;
            
            const float debugSize = 0.4;
            const float debugStart = 0.2;
            #if UNITY_UV_STARTS_AT_TOP
            if (uv.x > debugStart && uv.x < debugSize + debugStart && uv.y < 1 - debugStart && uv.y > 1 - debugSize - debugStart)
            {
                float2 debugUV = 0.0;
                debugUV.x = (uv.x - debugStart) / debugSize;
                debugUV.y = (uv.y - (1 - debugStart - debugSize)) / debugSize;
            #else
            if (uv.x > debugStart && uv.x < debugSize + debugStart && uv.y > debugStart && uv.y < debugSize + debugStart)
            {
                float2 debugUV = 0.0;
                debugUV.x = (uv.x - debugStart) / debugSize;
                debugUV.y = (uv.y - debugStart) / debugSize;
            #endif
                #if defined(_DEBUG_MODE_1)
                {
                    color = SAMPLE_TEXTURE2D_X(_CameraMVTexture, sampler_PointClamp, saturate(debugUV)).xyz;
                }
                #elif defined(_DEBUG_MODE_2)
                {
                    color.x = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_PointClamp, saturate(debugUV)).x;
                    color.yz = 0;
                }
                #elif defined(_DEBUG_MODE_3)
                {
                    color = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_PointClamp, saturate(debugUV)).xyz;
                }
                #elif defined(_DEBUG_MODE_4)
                {
                    color = SAMPLE_TEXTURE2D_X(_CameraTransparentTexture, sampler_PointClamp, saturate(debugUV)).xyz;
                }
                #endif
                alpha = 0.0;
            }

            return half4(color, alpha);
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZTest Always ZWrite Off Cull Off
        Blend One SrcAlpha
        Pass
        {
            Name "DebugPost"

            HLSLPROGRAM
                #pragma vertex FullscreenVert
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
