// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "UI/DefaultGammaSpace(NoFetch)"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        [HideInspector]_ExternalOESTex("Oes Texture", 2D) = "black" {}
        
        _ColorMask ("Color Mask", Float) = 15
        [HideInInspector]_AlphaMaskTex("Alpha Mask Texture", 2D) = "white" {}
        [HideInInspector]_SoftMask("Soft Mask", 2D) = "white" {}
        
        //PicoVideo;UIShineEffect;ZhouShaoyang;Begin
        [HideInInspector]_ShineTex("Shine Texture", 2D) = "black" {}
        [HideInInspector]_ShineColor("Shine Color", Color) = (1,1,1,1)
        [HideInInspector]_ShineParams("Shine Params", Vector) = (0,0,0,0) 
        [HideInInspector]_GlobalUIDarkenColor("Global UI Darken Color", Color) = (1,1,1,1)
        //PicoVideo;UIShineEffect;ZhouShaoyang;End

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        
        Pass
        {
            PackageRequirements
            {
                "org.byted.vrtv.engine.universal-ui-extension": "1.0.2"
            }
            
            Name "UniversalUIExtension"
            HLSLPROGRAM
            
            #pragma multi_compile RENDER_IN_LINEAR _  //UI是否在线性空间下渲染(对于背景是不透明的UI模块或者透传模块可以直接在Linear空间下渲染)
            #pragma multi_compile_local _ UNITY_UI_SRGB     //用于标识Texture2D是否开启了SRGB标签
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_UI_ALPHAMASK
            #pragma multi_compile_local _ UNITY_UI_SHINE
            #pragma multi_compile_local _ SOFTMASK_SIMPLE SOFTMASK_SLICED SOFTMASK_TILED SOFTMASK_RADIALFILLED
            #pragma multi_compile_local _ _EXTERNAL_TEXTURE

            #define USE_FRAMEBUFFER_FETCH 0
            #define USE_UNIVERSAL_UI_EXTENSION 1
            #include "UICore.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }


        Pass
        {
            Name "Default"
            HLSLPROGRAM

            #pragma multi_compile RENDER_IN_LINEAR _  //UI是否在线性空间下渲染(对于背景是不透明的UI模块或者透传模块可以直接在Linear空间下渲染)
            #pragma multi_compile_local _ UNITY_UI_SRGB     //用于标识Texture2D是否开启了SRGB标签
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #pragma multi_compile_local _ UNITY_UI_ALPHAMASK
            #pragma multi_compile_local _ SOFTMASK_SIMPLE SOFTMASK_SLICED SOFTMASK_TILED
            #pragma multi_compile_local _ _EXTERNAL_TEXTURE
        
            #define USE_FRAMEBUFFER_FETCH 0
            #define USE_UNIVERSAL_UI_EXTENSION 0
            #include "UICore.hlsl"

            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}
