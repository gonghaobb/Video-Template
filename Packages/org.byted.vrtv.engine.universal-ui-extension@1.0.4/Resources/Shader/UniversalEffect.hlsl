#include "UnityUI.cginc"

// inline float RoundedCorner(float radius, float ratio, float2 pos)
// {
//     const float2 ratioUV =  0.5 * float2(1, ratio);
//     pos = max(0, abs(pos) - ratioUV + abs(radius));
//     return step(dot(pos, pos) , radius * radius);
//     // const float inside = step(dot(pos, pos) , radius * radius);
//     // return radius >= 0 ? inside : 1 - inside;
// }

#ifdef USE_UNIVERSAL_UI_EXTENSION
# define ROUNDEDCORNER_COORDS(idx)                      float4 roundedSetting : TEXCOORD ## idx;
# define ROUNDEDCORNER_CALCULATE_COORDS(OUT, IN)   (OUT).roundedSetting = float4((IN).roundedSetting.xy,((IN).roundedSetting.zw - 0.5) * float2(1, (IN).roundedSetting.y));
# define ROUNDEDCORNER(IN)                              RoundedCorner(IN.roundedSetting.x,IN.roundedSetting.y,IN.roundedSetting.zw);
inline void RoundedCorner(float radius, float ratio, float2 pos)
{
    const float2 ratioUV =  0.5 * float2(1, ratio);
    pos = max(0, abs(pos) - ratioUV + abs(radius));
    clip(radius * radius - dot(pos, pos));
}
#else
# define ROUNDEDCORNER_COORDS(idx)
# define ROUNDEDCORNER_CALCULATE_COORDS(OUT, IN)
# define ROUNDEDCORNER(IN)
inline void RoundedCorner(float radius, float ratio, float2 pos) {return;}
#endif

#if defined(SOFTMASK_SIMPLE) || defined(SOFTMASK_SLICED) || defined(SOFTMASK_TILED) || defined(SOFTMASK_RADIALFILLED)
#   define __SOFTMASK_ENABLE
#   if defined(SOFTMASK_SLICED) || defined(SOFTMASK_TILED)
#       define __SOFTMASK_USE_BORDER
#   endif
#   if defined(SOFTMASK_RADIALFILLED)
#       define __SOFTMASK_RADIALFILLED
#   endif
#endif

#ifdef __SOFTMASK_ENABLE

# define SOFTMASK_COORDS(idx)                  float4 maskPosition : TEXCOORD ## idx;
# define SOFTMASK_CALCULATE_COORDS(OUT, pos)   (OUT).maskPosition = mul(_SoftMask_WorldToMask, pos);
# define SOFTMASK_GET_MASK(IN)                 SoftMask_GetMask((IN).maskPosition.xy)

    sampler2D _SoftMask;
    float4 _SoftMask_Rect;
    float4 _SoftMask_UVRect;
    float4x4 _SoftMask_WorldToMask;
    float4 _SoftMask_ChannelWeights;

    float _RoundedRadius;
    float _RoundedRatio;
    float4 _RoundedRect;

# ifdef __SOFTMASK_RADIALFILLED
    float _RadialFillAmount;
    float _RadialFillStartAngle;
    float2 _RadialFillUVCenter;
    float2 _RadialFillUVRatio;
        
    void RadialFill(float2 uv)
    {
        uv = uv - _RadialFillUVCenter;
        uv *= _RadialFillUVRatio;
        float anglePI = atan2(uv.x, uv.y);
        float angle01 = abs(step(0, _RadialFillAmount) - frac(anglePI / UNITY_PI / 2 - _RadialFillStartAngle));
        clip(angle01 - abs(_RadialFillAmount));
    }
# endif
# ifdef __SOFTMASK_USE_BORDER
    float4 _SoftMask_BorderRect;
    float4 _SoftMask_UVBorderRect;
# endif
# ifdef SOFTMASK_TILED
    float2 _SoftMask_TileRepeat;
# endif

    inline float2 __SoftMask_Inset(float2 a, float2 a1, float2 a2, float2 u1, float2 u2, float2 repeat)
    {
        float2 w = (a2 - a1);
        float2 d = (a - a1) / w;
        return lerp(u1, u2, (w * repeat != 0.0f ? frac(d * repeat) : d));
    }

    inline float2 __SoftMask_Inset(float2 a, float2 a1, float2 a2, float2 u1, float2 u2)
    {
        float2 w = (a2 - a1);
        return lerp(u1, u2, (w != 0.0f ? (a - a1) / w : 0.0f));
    }

# ifdef __SOFTMASK_USE_BORDER
    inline float2 __SoftMask_XY2UV(
            float2 a,
            float2 a1, float2 a2, float2 a3, float2 a4,
            float2 u1, float2 u2, float2 u3, float2 u4)
    {
        float2 s1 = step(a2, a);
        float2 s2 = step(a3, a);
        float2 s1i = 1 - s1;
        float2 s2i = 1 - s2;
        float2 s12 = s1 * s2;
        float2 s12i = s1 * s2i;
        float2 s1i2i = s1i * s2i;
        float2 aa1 = a1 * s1i2i + a2 * s12i + a3 * s12;
        float2 aa2 = a2 * s1i2i + a3 * s12i + a4 * s12;
        float2 uu1 = u1 * s1i2i + u2 * s12i + u3 * s12;
        float2 uu2 = u2 * s1i2i + u3 * s12i + u4 * s12;
        return
            __SoftMask_Inset(a, aa1, aa2, uu1, uu2
#   if SOFTMASK_TILED
                , s12i * _SoftMask_TileRepeat
#   endif
            );
    }

    inline float2 SoftMask_GetMaskUV(float2 maskPosition)
    {
        return
            __SoftMask_XY2UV(
                maskPosition,
                _SoftMask_Rect.xy, _SoftMask_BorderRect.xy, _SoftMask_BorderRect.zw, _SoftMask_Rect.zw,
                _SoftMask_UVRect.xy, _SoftMask_UVBorderRect.xy, _SoftMask_UVBorderRect.zw, _SoftMask_UVRect.zw);
    }
# else
    inline float2 SoftMask_GetMaskUV(float2 maskPosition)
    {
        return
            __SoftMask_Inset(
                maskPosition,
                _SoftMask_Rect.xy, _SoftMask_Rect.zw, _SoftMask_UVRect.xy, _SoftMask_UVRect.zw);
    }
# endif
    inline float4 SoftMask_GetMaskTexture(float2 maskPosition)
    {
        return tex2D(_SoftMask, SoftMask_GetMaskUV(maskPosition));
    }

    inline float SoftMask_GetMask(float2 maskPosition)
    {
        float2 uv = SoftMask_GetMaskUV(maskPosition);
# ifdef __SOFTMASK_RADIALFILLED
        RadialFill(uv);
# endif
        float2 fullRectUV = __SoftMask_Inset(maskPosition,_RoundedRect.xy, _RoundedRect.zw, 0.0, 1.0);
        RoundedCorner(_RoundedRadius, _RoundedRatio, (fullRectUV - 0.5) * float2(1, _RoundedRatio));
        float4 sampledMask = tex2D(_SoftMask, uv) * _SoftMask_ChannelWeights;
        float isInsideRect = UnityGet2DClipping(maskPosition, _SoftMask_Rect);
        return dot(sampledMask, 1) * isInsideRect;
    }
#else // __SOFTMASK_ENABLED

# define SOFTMASK_COORDS(idx)
# define SOFTMASK_CALCULATE_COORDS(OUT, pos)
# define SOFTMASK_GET_MASK(IN)                 (1.0f)
    inline float4 SoftMask_GetMaskTexture(float2 maskPosition) { return 1.0f; }
    inline float SoftMask_GetMask(float2 maskPosition) { return 1.0f; }
#endif


#ifdef UNITY_UI_SHINE
# define SHINE(uv)  Shine(uv);

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
    
    fixed4 Shine(in float2 uv)
    {
        float2 shine_uv = uv * _ShineTex_ST.xy;
        const float2 center = float2(0.5, 0.5);
        shine_uv = Unity_Rotate_Radians_float(shine_uv, center, _ShineParams.x);
        shine_uv += _ShineTex_ST.zw + _Time.y * _ShineParams.yz;
        fixed4 shine = tex2D(_ShineTex, shine_uv) * _ShineColor;
        return float4(shine.rgb * shine.a,0);
    }

#else
# define SHINE(uv)  float4(0,0,0,0);

#endif

