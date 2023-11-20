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
using UnityEditor.UI;
using UnityEngine.UI;

namespace Matrix.UniversalUIExtension
{
    [CustomEditor(typeof(SoftMask), true)]
    [CanEditMultipleObjects]
    public class SoftMaskEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            if (EditorGUI.EndChangeCheck())
            {
                ((SoftMask)target).graphic.SetMaterialDirty();
            }
        }

        [MenuItem("CONTEXT/SoftMask/DeployModifierInChildren")]
        static void DeployModifierInChildren(MenuCommand cmd)
        {
            SoftMask softMask = (SoftMask)cmd.context;
            if (softMask != null)
            {
                SoftMask.DeployModifierInChildren(softMask, softMask.transform);
            }
        }
    }
}