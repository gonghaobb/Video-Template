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

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Matrix.UniversalUIExtension
{
    public static class DefaultUIEffect
    {
#if !PICO_UGUI
        private const string DEFAULT_UI_SHADER = "UI/Default";
        private const string DEFAULT_EFFECT_SHADER = "PICO Universal UI Extension/Default";
        private static readonly Shader s_DefaultEffectUIShader = Shader.Find(DEFAULT_EFFECT_SHADER);
#endif
        private static Material s_DefaultUIEffect = null;

        public static Material defaultUniversalUIEffectMaterial
        {
            get
            {
                if (s_DefaultUIEffect == null)
#if PICO_UGUI
                    s_DefaultUIEffect = new Material(UIMaterials.GetDefaultShader())
#else
                    s_DefaultUIEffect = new Material(DEFAULT_EFFECT_SHADER)
#endif
                    {
                        hideFlags = HideFlags.HideAndDontSave
                    };
                return s_DefaultUIEffect;
            }
        }

        public static Material Replace(Material original)
        {
            if (original == null ||
                original.shader == Canvas.GetDefaultCanvasMaterial().shader
#if !PICO_UGUI
                || original.shader == Shader.Find(DEFAULT_UI_SHADER)
                || original.shader == Shader.Find(DEFAULT_EFFECT_SHADER)
#else
                || UIMaterials.IsDefaultShader(original.shader)
#endif
               )
            {
                Material replace = new Material(original)
                {
                    hideFlags = HideFlags.HideAndDontSave,
#if !PICO_UGUI
                    shader = s_DefaultEffectUIShader
#else
                    //定制ugui版本下已经直接使用效果Shader，无需替换
                    //shader = UIMaterials.GetDefaultShader()
#endif
                };
                return replace;
            }

            return original;
        }

        public static bool IsSupportEffect(Shader shader)
        {
#if PICO_UGUI
            return UIMaterials.IsDefaultShader(shader);
#else
            return shader == s_DefaultEffectUIShader;
#endif
        }
    }
}