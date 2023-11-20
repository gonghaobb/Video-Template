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
Shader "PicoVideo/EcosystemSimulate/HeightMapSimulate"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Pass
        {
            Name "HeightMap"
            Tags { "LightMode"="HeightMap" }

            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #pragma shader_feature_local _ _DISABLE_RENDER_HEIGHT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Vert
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Frag
            {
                float4 vertex : SV_POSITION;
                float height : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float4x4 _ESSDynamicHeightMapCameraVPMatrix;
            float4 _ESSDynamicHeightMapCameraWorldPos;
            float _ESSDynamicHeightMapHaxHeight;
            float _ESSDynamicHeightMapHeightRange;

            Frag vert (Vert v)
            {   
                Frag o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                #ifndef _DISABLE_RENDER_HEIGHT
                float4x4 o2w = UNITY_MATRIX_M;
                #if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
                o2w._m03_m13_m23 += _WorldSpaceCameraPos.xyz;
                #endif
                float4 posWS = mul(o2w , float4(v.vertex.xyz , 1));
                o.vertex = mul(_ESSDynamicHeightMapCameraVPMatrix , float4(posWS.xyz , 1));
                o.height = posWS.y;
                #else
                o.vertex = TransformObjectToHClip(v.vertex);
                o.vertex.y = -1000;
                o.height = -1000;
                #endif
                return o;
            }

            float frag (Frag i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                return 1 - (_ESSDynamicHeightMapHaxHeight - i.height) / _ESSDynamicHeightMapHeightRange;
            }
            
            ENDHLSL
        }
    }
}
