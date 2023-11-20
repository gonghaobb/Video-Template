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

Shader "PicoVideo/EcosystemSimulate/VegetationSimulate/VegetationBillboard"
{
    Properties
    {
        [Header(Main Maps)][Space(10)]_MainColor("Leaves Color", Color) = (1,1,1,0)
        _TrunkColor("Trunk Color", Color) = (0,0,0,0)
        _DetailColor("Detail Color", Color) = (1,1,1,1)
        _MainTex("Main Tex", 2D) = "white" {}
        _Normal("Normal", 2D) = "bump" {}
        _NormalPower("Normal Power", Range(0 , 1)) = 1
        [Space(10)][Header(Gradient Parameters)][Space(10)]_GradientColor("Gradient Color", Color) = (1,1,1,0)
        _GradientFalloff("Gradient Falloff", Range(0 , 2)) = 2
        _GradientPosition("Gradient Position", Range(0 , 1)) = 0.5
        [Toggle(_INVERTGRADIENT_ON)] _InvertGradient("Invert Gradient", float) = 0
        [Space(10)][Header(Color Variation)][Space(10)]_ColorVariation("Color Variation", Color) = (1,0,0,0)
        _ColorVariationPower("Color Variation Power", Range(0 , 1)) = 1
        _ColorVariationNoise("Color Variation Noise", 2D) = "white" {}
        _NoiseScale("Noise Scale", float) = 0.5
        [Space(10)][Header(Wind)][Space(10)]_WindMultiplier("Wind Multiplier", float) = 0
    }
    SubShader
    {

        HLSLINCLUDE
        half3 mod2D289(half3 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }

        half2 mod2D289(half2 x) { return x - floor(x * (1.0 / 289.0)) * 289.0; }

        half3 permute(half3 x) { return mod2D289(((x * 34.0) + 1.0) * x); }

        half snoise(half2 v)
        {
            const half4 C = half4(0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439);
            half2 i = floor(v + dot(v, C.yy));
            half2 x0 = v - i + dot(i, C.xx);
            half2 i1;
            i1 = (x0.x > x0.y) ? half2(1.0, 0.0) : half2(0.0, 1.0);
            half4 x12 = x0.xyxy + C.xxzz;
            x12.xy -= i1;
            i = mod2D289(i);
            half3 p = permute(permute(i.y + half3(0.0, i1.y, 1.0)) + i.x + half3(0.0, i1.x, 1.0));
            half3 m = max(0.5 - half3(dot(x0, x0), dot(x12.xy, x12.xy), dot(x12.zw, x12.zw)), 0.0);
            m = m * m;
            m = m * m;
            half3 x = 2.0 * frac(p * C.www) - 1.0;
            half3 h = abs(x) - 0.5;
            half3 ox = floor(x + 0.5);
            half3 a0 = x - ox;
            m *= 1.79284291400159 - 0.85373472095314 * (a0 * a0 + h * h);
            half3 g;
            g.x = a0.x * x0.x + h.x * x0.y;
            g.yz = a0.yz * x12.xz + h.yz * x12.yw;
            return 130.0 * dot(m, g);
        }
        ENDHLSL

        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #pragma shader_feature_local _INVERTGRADIENT_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #pragma multi_compile _ CUSTOM_FOG
            #ifdef CUSTOM_FOG
			#include "Packages/org.byted.vrtv.engine.ecosystem-simulate/Runtime/FogSimulate/Resource/Shaders/Fog.hlsl"
            #endif

            struct appdata
            {
                half4 vertex : POSITION;
                half2 uv : TEXCOORD0;
                // half3 normal : NORMAL;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                // UNITY_FOG_COORDS(1)
                half4 vertex : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float4 centerPosAndFogFactor : TEXCOORD2;
                #ifdef CUSTOM_FOG
            	float3 viewDirectionWS : TEXCOORD3;
                #endif
            };
CBUFFER_START(UnityPerMaterial)
            uniform half _WindMultiplier;
            uniform half4 _MainTex_ST;
            uniform half _ColorVariationPower;
            uniform half4 _ColorVariation;
            uniform half4 _DetailColor;
            uniform half4 _GradientColor;
            uniform half4 _MainColor;
            uniform half4 _TrunkColor;
            uniform half4 _Normal_ST;

            uniform half _GradientPosition;
            uniform half _GradientFalloff;
            uniform half _NoiseScale;
            uniform half _NormalPower;
CBUFFER_END
            uniform half _VegetationWindSpeed;
            uniform half _VegetationWindPower;
            uniform half _VegetationWindBurstsSpeed;
            uniform half _VegetationWindBurstsScale;
            uniform half _VegetationWindBurstsPower;
            uniform half2 _VegetationWindDirection;
            uniform sampler2D _MainTex;
            uniform sampler2D _ColorVariationNoise;
            uniform sampler2D _Normal;

            v2f vert(appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //计算顶点动画
                half windSpeed = (_Time.y * _VegetationWindSpeed);
                half3 originPositionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                half2 uv = (1.0 * _Time.y * _VegetationWindBurstsSpeed + originPositionWS.xz);
                half windNoise = snoise(uv * (_VegetationWindBurstsScale / 100.0));
                windNoise = windNoise * 0.5 + 0.5;
                half windPower = (_VegetationWindPower * (windNoise * _VegetationWindBurstsPower));
                half3 windOffset = (half3(((sin(windSpeed) * windPower) * v.uv.xy.y), 0.0,
                                          ((cos(windSpeed) * (windPower * 0.5)) * v.uv.xy.y)));
                half3 positionWS = originPositionWS + windOffset * _WindMultiplier;
                v.vertex.xyz = TransformWorldToObject(positionWS);
                v.vertex.w = 1;
                //计算Billboard顶点位置
                half3 center = half3(0, 0, 0);
                half3 viewer = mul(unity_WorldToObject, half4(_WorldSpaceCameraPos, 1)).xyz;
                half3 normalDir = -viewer;
                normalDir.y = 0;
                normalDir = normalize(normalDir);
                half3 upDir = abs(normalDir.y) > 0.999 ? half3(0, 0, 1) : half3(0, 1, 0);
                half3 rightDir = normalize(cross(upDir, normalDir));
                upDir = normalize(cross(normalDir, rightDir));
                half3 centerOffs = v.vertex.xyz - center;
                centerOffs.x *= length(unity_ObjectToWorld._m00_m10_m20);
                centerOffs.y *= length(unity_ObjectToWorld._m01_m11_m21);
                centerOffs.z *= length(unity_ObjectToWorld._m02_m12_m22);
                half3 localPos = center + rightDir * centerOffs.x + upDir * centerOffs.y + normalDir * centerOffs.z;
                half4x4 objectToWorld = half4x4(normalize(unity_ObjectToWorld._m00_m10_m20), unity_ObjectToWorld._m03,
                                                 normalize(unity_ObjectToWorld._m01_m11_m21), unity_ObjectToWorld._m13,
                                                 normalize(unity_ObjectToWorld._m02_m12_m22), unity_ObjectToWorld._m23,
                                                 unity_ObjectToWorld._m03_m13_m23_m33);
                o.positionWS = mul(objectToWorld, half4(localPos, 1)).xyz;
                o.vertex = TransformWorldToHClip(o.positionWS);
                o.centerPosAndFogFactor.xyz = unity_ObjectToWorld._m03_m13_m23;

                #if defined(CUSTOM_FOG)
    			half fogFactor = FogVert(o.positionWS);
				o.viewDirectionWS = GetWorldSpaceViewDir(o.positionWS);
                #else
                half fogFactor = ComputeFogFactor(o.vertex.z);
                #endif
                o.centerPosAndFogFactor.w = fogFactor;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 c;
                c.a = 1;
                half4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.1);
                //计算颜色渐变参数
                #ifdef _INVERTGRADIENT_ON
                half uv = i.uv.y;
                #else
					half uv = ( 1.0 - i.uv.y );
                #endif
                half gradientRate = clamp(
                    ((uv + (-2.0 + (_GradientPosition - 0.0) * (1.0 - -2.0) / (1.0 - 0.0))) / _GradientFalloff), 0.0,
                    1.0);
                half4 gradientColor = lerp(_MainColor, _GradientColor, gradientRate);
                half4 variationColor = lerp(_ColorVariation, (_ColorVariation / max(1.0 - gradientColor, 0.00001)),
                                            _ColorVariationPower);
                half4 colorResult = lerp(gradientColor, (saturate(variationColor)),
                                         (_ColorVariationPower * pow(
                                             tex2D(_ColorVariationNoise,
                                                   (i.centerPosAndFogFactor.xz * (_NoiseScale / 100.0))).r, 3.0)));
                half4 treeColor = ((_DetailColor * (col).b) + ((colorResult * (col).g) + (_TrunkColor * (col).r)));

                half2 uvNormal = i.uv * _Normal_ST.xy + _Normal_ST.zw;
                half3 normal = UnpackNormalScale(tex2D(_Normal, uvNormal), _NormalPower);
                Light mainLight = GetMainLight(half4(0, 0, 0, 0), i.positionWS, 0);
                half3 lightDirVS = mul(UNITY_MATRIX_V, half4(normalize(mainLight.direction), 0)).xyz;
                half lightResult = dot(normal, lightDirVS);
                c.rgb = clamp(((mainLight.color.xyz * saturate(lightResult)) + 0.25), 0, 1);
                c *= treeColor;

                #if defined(CUSTOM_FOG)
    			c.rgb = FogFrag(c.rgb, i.viewDirectionWS, i.positionWS.xyz, i.centerPosAndFogFactor.w);
                #else
                c.rgb = MixFog(c.rgb, i.centerPosAndFogFactor.w);
                #endif

                return c;
            }
            ENDHLSL
        }

        pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                half4 vertex : POSITION;
                half2 uv : TEXCOORD0;
            };

            struct v2f
            {
                half2 uv : TEXCOORD0;
                half4 vertex : SV_POSITION;
                float3 positionWS : TEXCOORD1;
            };

            uniform half _VegetationWindSpeed;
            uniform half _VegetationWindPower;
            uniform half _VegetationWindBurstsSpeed;
            uniform half _VegetationWindBurstsScale;
            uniform half _VegetationWindBurstsPower;
            uniform half2 _VegetationWindDirection;
CBUFFER_START(UnityPerMaterial)
            uniform half _WindMultiplier;
            uniform half4 _MainTex_ST;
            uniform half _ColorVariationPower;
            uniform half4 _ColorVariation;
            uniform half4 _DetailColor;
            uniform half4 _GradientColor;
            uniform half4 _MainColor;
            uniform half4 _TrunkColor;
            uniform half4 _Normal_ST;

            uniform half _GradientPosition;
            uniform half _GradientFalloff;
            uniform half _NoiseScale;
            uniform half _NormalPower;
CBUFFER_END
            uniform sampler2D _MainTex;

            v2f vert(appdata v)
            {
                v2f o;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //计算顶点动画
                half windSpeed = (_Time.y * _VegetationWindSpeed);
                half3 originPositionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                half2 uv = (1.0 * _Time.y * _VegetationWindBurstsSpeed + originPositionWS.xz);
                half windNoise = snoise(uv * (_VegetationWindBurstsScale / 100.0));
                windNoise = windNoise * 0.5 + 0.5;
                half windPower = (_VegetationWindPower * (windNoise * _VegetationWindBurstsPower));
                half3 windOffset = (half3(((sin(windSpeed) * windPower) * v.uv.xy.y), 0.0,
                                          ((cos(windSpeed) * (windPower * 0.5)) * v.uv.xy.y)));
                half3 positionWS = originPositionWS + windOffset * _WindMultiplier;
                v.vertex.xyz = TransformWorldToObject(positionWS);
                v.vertex.w = 1;
                //计算Billboard顶点位置
                Light light = GetMainLight();
                half3 center = half3(0, 0, 0);
                half3 normalDir = -light.direction;
                normalDir.y = 0;
                normalDir = normalize(normalDir);
                half3 upDir = abs(normalDir.y) > 0.999 ? half3(0, 0, 1) : half3(0, 1, 0);
                half3 rightDir = normalize(cross(upDir, normalDir));
                upDir = normalize(cross(normalDir, rightDir));
                half3 centerOffs = v.vertex.xyz - center;
                centerOffs.x *= length(unity_ObjectToWorld._m00_m10_m20);
                centerOffs.y *= length(unity_ObjectToWorld._m01_m11_m21);
                centerOffs.z *= length(unity_ObjectToWorld._m02_m12_m22);
                half3 localPos = center + rightDir * centerOffs.x + upDir * centerOffs.y + normalDir * centerOffs.z;
                half4x4 objectoToWorld = half4x4(normalize(unity_ObjectToWorld._m00_m10_m20), unity_ObjectToWorld._m03,
                                                 normalize(unity_ObjectToWorld._m01_m11_m21), unity_ObjectToWorld._m13,
                                                 normalize(unity_ObjectToWorld._m02_m12_m22), unity_ObjectToWorld._m23,
                                                 unity_ObjectToWorld._m03_m13_m23_m33);
                o.positionWS = mul(objectoToWorld, half4(localPos, 1)).xyz;
                o.vertex = TransformWorldToHClip(o.positionWS);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 c;
                half4 col = tex2D(_MainTex, i.uv);
                clip(col.a - 0.1);

                return 0;
            }
            ENDHLSL
        }
    }
}