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
    public class WaterShaderGUI : Matrix.ShaderGUI.BaseShaderGUI
    {
        protected override void SetupMaterialCustomKeyWord(Material material)
        {
            base.SetupMaterialCustomKeyWord(material);

            float realtimeRefraction = material.HasProperty("_RealtimeRefraction") ? material.GetFloat("_RealtimeRefraction") : 0.0f;

            if (realtimeRefraction != 0.0f)
            {
                material.SetShaderPassEnabled("ForwardWater", true);
                material.SetShaderPassEnabled("UniversalForward", false);
            }
            else
            {
                material.SetShaderPassEnabled("ForwardWater", false);
                material.SetShaderPassEnabled("UniversalForward", true);
            }
        }
    }
}