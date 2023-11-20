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
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    [ExecuteAlways]
    public class FixOverlayAlpha : MonoBehaviour
    {
        private void OnEnable()
        {
            FixOverlayAlphaManager.instance.RegisterOcclusionUI(this.gameObject);
        }

        private void OnDisable()
        {
            FixOverlayAlphaManager.instance.UnRegisterOcclusionUI(this.gameObject);
        }
    }
}