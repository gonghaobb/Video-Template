#ifdef UNITY_UI_SHINE

sampler2D _ShineTex;
float4 _ShineTex_ST;
fixed4 _ShineColor;
half3 _ShineParams;

float2 Unity_Rotate_Radians_float(float2 UV, float2 Center, float Rotation)
{
    float2 Out = 0;
    UV -= Center;
    float s = sin(Rotation);
    float c = cos(Rotation);
    float2x2 rMatrix = float2x2(c, -s, s, c);
    rMatrix *= 0.5;
    rMatrix += 0.5;
    rMatrix = rMatrix * 2 - 1;
    UV.xy = mul(UV.xy, rMatrix);
    UV += Center;
    Out = UV;
    return Out;
}

fixed4 Shine(in fixed4 color , in float2 uv)
{
    float2 shine_uv = uv * _ShineTex_ST.xy;
    const float2 center = float2(0.5, 0.5);
    shine_uv = Unity_Rotate_Radians_float(shine_uv, center, _ShineParams.x);
    shine_uv += _ShineTex_ST.zw + _Time.y * _ShineParams.yz;
    fixed4 shine = tex2D(_ShineTex, shine_uv) * _ShineColor;
    color.rgb += shine.rgb * shine.a;
    return color;
}



#endif
