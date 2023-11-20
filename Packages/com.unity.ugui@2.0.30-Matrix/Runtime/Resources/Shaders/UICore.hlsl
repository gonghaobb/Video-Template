#ifndef COLORSPACE_UI_CORE
#define COLORSPACE_UI_CORE

#include "UnityCG.cginc"
#if USE_UNIVERSAL_UI_EXTENSION
#include "Packages/org.byted.vrtv.engine.universal-ui-extension/Resources/Shader/UniversalEffect.hlsl"
#else
#include "SoftMask.cginc"
#endif

#ifdef _EXTERNAL_TEXTURE
sampler2D _ExternalOESTex;
#endif

sampler2D _MainTex;
sampler2D _AlphaMaskTex;
float4 _GlobalUIDarkenColor;
float4 _Color;
float4 _MainTex_ST;
float4 _TextureSampleAdd;
float4 _ClipRect;
float _UIMaskSoftnessY;
float _UIMaskSoftnessX;

inline float3 GammaToLinearExact(float3 value)
{
    float3 linearRGBLo  = value / 12.92;
    float3 linearRGBHi  = pow((value + 0.055) / 1.055, float3(2.4, 2.4, 2.4));
    float3 linearRGB    = (value <= 0.04045) ? linearRGBLo : linearRGBHi;
    return linearRGB;
}

inline float3 LinearToGammaExact(float3 c)
{
    float3 sRGBLo = c * 12.92;
    float3 sRGBHi = (pow(c, float3(1.0/2.4, 1.0/2.4, 1.0/2.4)) * 1.055) - 0.055;
    float3 sRGB   = (c <= 0.0031308) ? sRGBLo : sRGBHi;
    return sRGB;
}

struct appdata_t
{
    float4 vertex   : POSITION;
    float4 color    : TEXCOORD1;
    float2 texcoord : TEXCOORD0;
#if USE_UNIVERSAL_UI_EXTENSION
    ROUNDEDCORNER_COORDS(2)
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex   : SV_POSITION;
    float4 color    : COLOR;
    float2 texcoord  : TEXCOORD0;
    float4 worldPosition : TEXCOORD1;
    float4  mask : TEXCOORD2;
#if USE_UNIVERSAL_UI_EXTENSION
    ROUNDEDCORNER_COORDS(3)
#endif
    SOFTMASK_COORDS(4)
    UNITY_VERTEX_OUTPUT_STEREO
};

v2f Vert(appdata_t v)
{
    v2f OUT;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
    float4 vPosition = UnityObjectToClipPos(v.vertex);
    OUT.worldPosition = v.vertex;
    OUT.vertex = vPosition;

    float2 pixelSize = vPosition.w;
    pixelSize /= float2(1, 1) * abs(mul((float2x2)UNITY_MATRIX_P, _ScreenParams.xy));

    float4 clampedRect = clamp(_ClipRect, -2e10, 2e10);
    float2 maskUV = (v.vertex.xy - clampedRect.xy) / (clampedRect.zw - clampedRect.xy);
    OUT.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
    OUT.mask = float4(v.vertex.xy * 2 - clampedRect.xy - clampedRect.zw, 0.25 / (0.25 * half2(_UIMaskSoftnessX, _UIMaskSoftnessY) + abs(pixelSize.xy)));
    #if RENDER_IN_LINEAR
    v.color.rgb = GammaToLinearExact(v.color.rgb);
    #endif 
    OUT.color = v.color * _Color;
#if USE_UNIVERSAL_UI_EXTENSION
    ROUNDEDCORNER_CALCULATE_COORDS(OUT, v)
#endif
    SOFTMASK_CALCULATE_COORDS(OUT, v.vertex)
    return OUT;
}

#if USE_FRAMEBUFFER_FETCH
void Frag(v2f IN, inout float4 ocol : SV_Target)
#else
void Frag(v2f IN, out float4 ocol : SV_Target)
#endif
{
#if USE_UNIVERSAL_UI_EXTENSION
    ROUNDEDCORNER(IN)
#endif
    
    #ifdef _EXTERNAL_TEXTURE
        float2 uv = float2(IN.texcoord.x, 1.0 - IN.texcoord.y);
        float4 texCol = tex2D(_ExternalOESTex, uv);
    #else
        float4 texCol = tex2D(_MainTex, IN.texcoord);
    #endif
    
    #if defined(UNITY_UI_SRGB)//未勾选SRGB激活UNITY_UI_SRGB
        #if !RENDER_IN_LINEAR
        texCol.rgb = LinearToGammaExact(texCol.rgb);
        #endif
    #else
        #if RENDER_IN_LINEAR
        texCol.rgb = GammaToLinearExact(texCol.rgb);
        #endif
    #endif
    
    
    float4 color = IN.color * (texCol + _TextureSampleAdd);
    color = min(_GlobalUIDarkenColor, color);
    
    #ifdef UNITY_UI_CLIP_RECT
        float2 m = saturate((_ClipRect.zw - _ClipRect.xy - abs(IN.mask.xy)) * IN.mask.zw);
        color.a *= m.x * m.y;
    #endif
                
    #ifdef UNITY_UI_ALPHACLIP
        clip(color.a - 0.001);
    #endif

    #if UNITY_UI_ALPHAMASK
        color.a *= tex2D(_AlphaMaskTex, IN.texcoord).a;
    #endif

#if USE_UNIVERSAL_UI_EXTENSION
    #ifdef UNITY_UI_SHINE
        color += Shine(IN.texcoord);
    #endif
#endif

    color.a *= SOFTMASK_GET_MASK(IN);

    #if USE_FRAMEBUFFER_FETCH
        float3 dstCol = LinearToGammaExact(abs(ocol.rgb));
        color.a = saturate(color.a);
        ocol.rgb = GammaToLinearExact(abs(color.rgb * color.a + (1 - color.a) * dstCol));
        ocol.a = color.a + ocol.a * (1 - color.a);
    #else
        color.rgb *= color.a;
        ocol = color;
    #endif
}

#endif