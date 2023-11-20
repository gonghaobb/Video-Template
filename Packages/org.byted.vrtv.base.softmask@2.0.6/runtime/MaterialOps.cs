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

using UnityEngine;

namespace UnityEngine.UI
{
    public static class MaterialOps
    {
        public static bool SupportsSoftMask(this Material mat)
        {
            return mat.shader.FindPropertyIndex("_SoftMask") != -1 && !mat.HasDefaultUIShader();
        }

        public static bool HasDefaultUIShader(this Material mat)
        {
            return mat.shader == Canvas.GetDefaultCanvasMaterial().shader;
        }

        public static void EnableKeyword(this Material mat, string keyword, bool enabled)
        {
            if (enabled)
            {
                mat.EnableKeyword(keyword);
            }
            else
            {
                mat.DisableKeyword(keyword);
            }
        }
    }
}
