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
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine;

namespace Matrix.EcosystemSimulate
{
    public class UberPostEffectShaderGUI : Matrix.ShaderGUI.BaseShaderGUI
    {
        protected override void SetupMaterialCustomKeyWord(Material material)
        {
            base.SetupMaterialCustomKeyWord(material);

            float kawaseBlur = material.HasProperty("_KawaseBlur") ? material.GetFloat("_KawaseBlur") : 0.0f;

            if (kawaseBlur != 0.0f)
            {
                if (material.HasProperty("_GrainyBlur"))
                {
                    material.SetFloat("_GrainyBlur", 0.0f);
                    material.DisableKeyword("_GRAINY_BLUR");
                }

                if (material.HasProperty("_GlitchImageBlock"))
                {
                    material.SetFloat("_GlitchImageBlock", 0.0f);
                    material.DisableKeyword("_GLITCH_IMAGE_BLOCK");
                }

                if (material.HasProperty("_GlitchScreenShake"))
                {
                    material.SetFloat("_GlitchScreenShake", 0.0f);
                    material.DisableKeyword("_GLITCH_SCREEN_SHAKE");
                }
            }
            else
            {
                float grainyBlur = material.HasProperty("_GrainyBlur") ? material.GetFloat("_GrainyBlur") : 0.0f;

                if (grainyBlur != 0.0f)
                {
                    if (material.HasProperty("_GlitchImageBlock"))
                    {
                        material.SetFloat("_GlitchImageBlock", 0.0f);
                        material.DisableKeyword("_GLITCH_IMAGE_BLOCK");
                    }

                    if (material.HasProperty("_GlitchScreenShake"))
                    {
                        material.SetFloat("_GlitchScreenShake", 0.0f);
                        material.DisableKeyword("_GLITCH_SCREEN_SHAKE");
                    }
                }
                else
                {
                    float glitchImageBlock = material.HasProperty("_GlitchImageBlock") ? material.GetFloat("_GlitchImageBlock") : 0.0f;
                    if (glitchImageBlock != 0.0f)
                    {
                        if (material.HasProperty("_GlitchScreenShake"))
                        {
                            material.SetFloat("_GlitchScreenShake", 0.0f);
                            material.DisableKeyword("_GLITCH_SCREEN_SHAKE");
                        }
                    }
                }
            }
        }
    }
}