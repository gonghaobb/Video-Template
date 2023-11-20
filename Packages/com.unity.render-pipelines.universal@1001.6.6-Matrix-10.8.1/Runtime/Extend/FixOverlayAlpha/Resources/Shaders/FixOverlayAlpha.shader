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

Shader "Hidden/FixOverlayAlpha"
{
    Properties
    {
        [HideInInspector] _StencilRef("StencilRef", Range(0,255)) = 55
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        Stencil
        {
            Ref [_StencilRef]
            Comp NotEqual
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend One Zero

        Pass
        {
            Name "FixOverlayAlpha"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma only_renderers framebufferfetch

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                uint vertexID     : SV_VertexID;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv     : TEXCOORD0;
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.vertex = GetQuadVertexPosition(input.vertexID);
                output.vertex.xy = output.vertex.xy * float2(2.0f, -2.0f) + float2(-1.0f, 1.0f);
                output.uv = GetQuadTexCoord(input.vertexID);
                return output;
            }

            void frag(Varyings i, inout float4 color : CoLoR)
            {
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                if (color.a != 0)
                {
                    color.a = 1;
                }
            }
            ENDHLSL
        }
    }
}