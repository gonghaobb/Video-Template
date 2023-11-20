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
using UnityEditor;
using UnityEngine;

namespace Matrix.UniversalUIExtension
{
    [InitializeOnLoad]
    public class UIEffectReloadHandler
    {
        static UIEffectReloadHandler()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        private static void OnAfterAssemblyReload()
        {
            SoftMask[] softMasks = UnityEngine.Object.FindObjectsOfType<SoftMask>();
            foreach (var softMask in softMasks)
            {
                SoftMask.DeployModifierInChildren(softMask, softMask.transform);
            }
        }
    }
}