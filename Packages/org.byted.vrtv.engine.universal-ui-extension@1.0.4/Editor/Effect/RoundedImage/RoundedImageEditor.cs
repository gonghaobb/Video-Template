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
    [CustomEditor(typeof(RoundedImage), true)]
    [CanEditMultipleObjects]
    public class RoundedImageEditor : ImageEditor
    {
        private SerializedProperty m_CornerRadius;
        private SerializedProperty m_CornerSmoothing;
        private GUIContent m_CornerRadiusContent;
        private GUIContent m_CornerSmoothingContent;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_CornerRadiusContent = EditorGUIUtility.TrTextContent("Corner Radius");
            m_CornerSmoothingContent = EditorGUIUtility.TrTextContent("Corner Smoothing");
            m_CornerRadius = serializedObject.FindProperty("m_CornerRadius");
            m_CornerSmoothing = serializedObject.FindProperty("m_CornerSmoothing");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_CornerRadius, m_CornerRadiusContent);
            // EditorGUILayout.PropertyField(m_CornerSmoothing, m_CornerSmoothingContent);

            serializedObject.ApplyModifiedProperties();
        }
    }
}