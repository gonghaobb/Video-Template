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
    [CustomEditor(typeof(CloudShadowManager))]
    public class CloudShadowManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            serializedObject.Update();

            CloudShadowManager mgr = target as CloudShadowManager;
            if (mgr == null)
            {
                return;
            }

            SerializedProperty enableProperty = serializedObject.FindProperty("m_Enable");

            EditorGUILayout.PropertyField(enableProperty);

            if (enableProperty.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CloudShadowMap"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_CloudShadowColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_firstCloudShadowParam"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_secondCloudShadowParam"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}