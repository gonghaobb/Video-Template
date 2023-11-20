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

Shader "PicoVideo/EcosystemSimulate/CombineTexShader"
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _CombineSrcTexR;
            sampler2D _CombineSrcTexG;
            uniform float4 _TexRParams;      // x : size y : pixelPerMeter zw : centerPos.xz
            uniform float4 _TexGParams;
            uniform float _OutputTexSize;
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half2 texRUV = i.uv * _TexRParams.xx / _OutputTexSize;
                float2 worldPos = ((i.uv - 0.5) * _TexRParams.x) * _TexRParams.y + _TexRParams.zw;
                float2 texGUV = (worldPos - _TexGParams.zw) / _TexGParams.y / _TexGParams.x + 0.5;
                fixed2 rg = tex2D(_CombineSrcTexR, texRUV).rg;
                fixed b = tex2D(_CombineSrcTexG, texGUV).r;

                return fixed4(rg.r,rg.g,b,0);
            }
            ENDCG
        }
    }
}
