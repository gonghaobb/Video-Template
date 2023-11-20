Shader "PXR_SDK/PXR_UnderlayHole_UI"
{
	Properties
	{
		[HideInInspector] _SrcBlend("SrcBlend", Float) = 0.0
		[HideInInspector] _DstBlend("DstBlend", Float) = 1.0	    
		[HideInInspector] _SrcAlphaBlend("SrcAlphaBlend", Float) = 1.0
		[HideInInspector] _DstAlphaBlend("DstAlphaBlend", Float) = 0.0
		[HideInInspector] _AlphaBlendOp("AlphaBlendOp", Range(0,255)) = 62
	}
	
	SubShader
	{
		Tags {"Queue" = "Transparent" "RenderType" = "Transparent"}
		LOD 200

		Pass
		{
			Cull Off
			Lighting Off
			ZWrite Off
			Blend [_SrcBlend] [_DstBlend], [_SrcAlphaBlend] [_DstAlphaBlend]
			BlendOp Add,[_AlphaBlendOp]
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex      : POSITION;
				float4 color       : COLOR;
				float2 texcoord    : TEXCOORD0;
				float4 holeSetting : TEXCOORD2;
			};

			struct v2f
			{
				float4 vertex      : SV_POSITION;
				float2 texcoord    : TEXCOORD0;
				float4 color       : COLOR;
				float4 holeSetting : TEXCOORD1;
				float2 offsetUV    : TEXCOORD2;
			};
			
			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord = v.texcoord;
				o.color = v.color;
				o.holeSetting = v.holeSetting;
				o.offsetUV = (v.texcoord.xy - 0.5) * float2(1, o.holeSetting.z);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{ 
				float radius = i.holeSetting.x;
				float softedge = i.holeSetting.y;
				float2 ratio =  0.5 * float2(1, i.holeSetting.z);
				float2 uv = abs(i.offsetUV);
				float2 softXY = smoothstep(ratio,ratio - softedge, uv);
				uv -= ratio - radius;
				uv = max(0, uv);
				float roundCorner = 1 - smoothstep(-min(softedge, radius), 0, length(uv) - radius);
				fixed4 color = min(roundCorner , softXY.x * softXY.y) ;
				return color;
			}
			ENDCG
		}
	}
}
