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

namespace Matrix.ShaderGUI
{
    public class TerrainLitShaderGUI : BaseShaderGUI
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
            
            if (material.HasProperty("_FirstAlphaMap"))
            {
                CoreUtils.SetKeyword(material, "_FIRST_ALPHAMAP_ON", material.GetTexture("_FirstAlphaMap") != null);
            }

            if (material.HasProperty("_SecondAlphaMap"))
            {
                CoreUtils.SetKeyword(material, "_SECOND_ALPHAMAP_ON", material.GetTexture("_SecondAlphaMap") != null);
            }

            if (material.HasProperty("_ThirdAlphaMap"))
            {
                CoreUtils.SetKeyword(material, "_THIRD_ALPHAMAP_ON", material.GetTexture("_ThirdAlphaMap") != null);
            }

            if (material.HasProperty("_FourAlphaMap"))
            {
                CoreUtils.SetKeyword(material, "_FOUR_ALPHAMAP_ON", material.GetTexture("_FourAlphaMap") != null);
            }

            if (material.HasProperty("_FirstNormalMap"))
            {
                CoreUtils.SetKeyword(material, "_FIRST_NORMALMAP", material.GetTexture("_FirstNormalMap") != null);
            }

            if (material.HasProperty("_SecondNormalMap"))
            {
                CoreUtils.SetKeyword(material, "_SECOND_NORMALMAP", material.GetTexture("_SecondNormalMap") != null);
            }

            if (material.HasProperty("_ThirdNormalMap"))
            {
                CoreUtils.SetKeyword(material, "_THIRD_NORMALMAP", material.GetTexture("_ThirdNormalMap") != null);
            }

            if (material.HasProperty("_FourNormalMap"))
            {
                CoreUtils.SetKeyword(material, "_FOUR_NORMALMAP", material.GetTexture("_FourNormalMap") != null);
            }
        }
    }
}