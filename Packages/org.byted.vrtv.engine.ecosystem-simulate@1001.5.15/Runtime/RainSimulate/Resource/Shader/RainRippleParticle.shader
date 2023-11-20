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
Shader "PicoVideo/EcosystemSimulate/RainSimulate/RainRippleParticle" {
Properties {
    _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
    _MainTex ("Particle Texture", 2D) = "white" {}
    _InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
}

Category {
    Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
    Blend SrcAlpha One, Zero One //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin
    ColorMask RGB
    Cull Off Lighting Off ZWrite Off

    SubShader {
        Pass {

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            #pragma multi_compile_particles

            #include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/HeightMapSimulate/Resource/HeightMapUtils.hlsl"
            
            sampler2D _MainTex;
            float4 _TintColor;

            struct appdata_t {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float4 texcoord : TEXCOORD0;
            	float2 center : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

			float _CurrentHeight;
            float4 _MainTex_ST;
            uniform float _EnableRainParticleHeightMap;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.color = v.color;
                o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);

            	if(_EnableRainParticleHeightMap > 0.5)
            	{
                	float3 positionWS = TransformObjectToWorld(v.vertex.xyz);
            		float3 center = float3(v.texcoord.zw, v.center.x);
            		float3 centerWS = TransformObjectToWorld(center);
            		float height = GetHeight(centerWS);
            		positionWS.y += height - _CurrentHeight;
            		v.vertex.xyz = TransformWorldToObject(positionWS);
            	}
                else
                {
	                v.vertex.xyz = 0;
                }
                
                o.vertex = TransformObjectToHClip(v.vertex.xyz);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                float4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
                col.a = saturate(col.a); // alpha should not have double-brightness applied to it, but we can't fix that legacy behavior without breaking everyone's effects, so instead clamp the output to get sensible HDR behavior (case 967476)

                return col;
            }
            ENDHLSL
        }
    }
}
}
