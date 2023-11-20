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

using System.Collections.Generic;
using UnityEditor;

namespace Matrix.EcosystemSimulate
{
    [CustomEditor(typeof(FogManager))]
    public class FogManagerEditor : Editor
    {
        private static List<string> s_FogSkyDirectionModeExclude = new List<string>
        {
            "rawSky.cloudRotateSpeed"
        };
        private static List<string> s_FogSkyRotationModeExclude = new List<string>
        {
            "rawSky.cloudOrientation", "rawSky.cloudDirectionSpeed",
        };
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            serializedObject.Update();
            
            FogManager mgr = target as FogManager;
            if (mgr == null)
            {
                return;
            }
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rawCommon"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("rawFog"));
            SerializedProperty iterator = serializedObject.FindProperty("rawSky");
            EditorGUILayout.PropertyField(iterator, false);
            EditorGUI.indentLevel++;
            if (iterator.isExpanded)
            {
                for (bool enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
                {
                    if (!CheckSkyModeExclude(iterator.propertyPath,
                            mgr.rawSky.cloudMotionMode == FogManager.CloudMotionMode.Direction
                                ? s_FogSkyDirectionModeExclude
                                : s_FogSkyRotationModeExclude))
                    {
                        EditorGUILayout.PropertyField(iterator, true);
                    }
                }
            }
            EditorGUI.indentLevel--;
            serializedObject.ApplyModifiedProperties();
        }

        private bool CheckSkyModeExclude(string iteratorPropertyPath, List<string> skyModeExcludeList)
        {
            for (int i = 0; i < skyModeExcludeList.Count; ++i)
            {
                if (iteratorPropertyPath.Equals(skyModeExcludeList[i]))
                {
                    return true;
                }
            }

            return false;
        }
    }
}