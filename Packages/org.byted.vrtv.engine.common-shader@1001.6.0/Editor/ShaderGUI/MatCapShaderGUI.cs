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
    public class MatCapShaderGUI : Matrix.ShaderGUI.BaseShaderGUI
    {
        protected override void SetupMaterialCustomKeyWord(Material material)
        {
            base.SetupMaterialCustomKeyWord(material);

            if (material.HasProperty("_EnableExtraPrePass"))
            {
                material.SetShaderPassEnabled("SRPDefaultUnlit", material.GetFloat("_EnableExtraPrePass") != 0.0);
            }
        }
    }
}