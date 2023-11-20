Shader "Unlit/CameraMask"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OffsetStart ("OffsetStart", Range(0,1)) = 0
        _OffsetEnd("OffsetEnd", Range(0.01,1)) = 0
        _Opacity("Opacity", Range(0,1)) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        LOD 100
        Cull Front
        ZTest off
        ZWrite off
        Blend SrcAlpha OneMinusSrcAlpha


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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed _OffsetStart;
            fixed _OffsetEnd;
            fixed _Opacity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = v.normal;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                _OffsetStart = _OffsetStart * 2 - 1;
                _OffsetEnd = _OffsetEnd * 2;
                return fixed4(0, 0, 0, clamp((-i.normal.z - _OffsetStart) / _OffsetEnd,0,1.0) * _Opacity);
            }
            ENDCG
        }
    }
}
