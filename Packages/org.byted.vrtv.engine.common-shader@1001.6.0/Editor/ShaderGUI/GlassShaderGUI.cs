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
using Matrix.ShaderGUI;
using UnityEngine;
using LWGUI;

namespace Matrix.CommonShader
{
    public class GlassShaderGUI : Matrix.ShaderGUI.BaseShaderGUI
    {
        protected override void SetupMaterialCustomKeyWord(Material material)
        {
            base.SetupMaterialCustomKeyWord(material);

            if (material.HasProperty("_LightingMode"))
            {
                LightingMode lightingMode = (LightingMode)material.GetFloat("_LightingMode");
                switch (lightingMode)
                {
                    case LightingMode.PBR:
                        CoreUtils.SetKeyword(material, "_BLING_PHONG", false);
                        CoreUtils.SetKeyword(material, "_PBR", true);
                        break;
                    
                    case LightingMode.BlingPhong:
                        CoreUtils.SetKeyword(material, "_BLING_PHONG", true);
                        CoreUtils.SetKeyword(material, "_PBR", false);
                        break;
                    
                    default:
                        CoreUtils.SetKeyword(material, "_BLING_PHONG", false);
                        CoreUtils.SetKeyword(material, "_PBR", false);
                        break;
                }
            }
            
            if (material.HasProperty("_AlphaMap"))
            {
                CoreUtils.SetKeyword(material, "_ALPHAMAP_ON", material.GetTexture("_AlphaMap") != null);
            }

            if (material.HasProperty("_InteriorCubemap"))
            {
                CoreUtils.SetKeyword(material, "_INTERIOR_CUBEMAP", material.GetTexture("_InteriorCubemap") != null);
            }

            if (material.HasProperty("_EnvironmentCubeMap"))
            {
                CoreUtils.SetKeyword(material, "_ENVIRONMENT_CUBEMAP", material.GetTexture("_EnvironmentCubeMap") != null);
            }

            if (material.HasProperty("_FrostNoiseMap"))
            {
                CoreUtils.SetKeyword(material, "_FROST_NOISE", material.GetTexture("_FrostNoiseMap") != null);
            }
        }
    }
}