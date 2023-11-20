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

#if UNITY_EDITOR
namespace Matrix.OverlayUI
{
    [CustomEditor(typeof(OverlayCanvas), true)]
    [CanEditMultipleObjects]
    public class OverlayCanvasEditor : Editor
    {
        SerializedProperty m_Width = null;
        SerializedProperty m_Height = null;
        SerializedProperty m_UseCustomSize = null;
        SerializedProperty m_OverlayDepth = null;
        SerializedProperty m_Offset = null;
        SerializedProperty m_OcclusionBetweenOverlayLayers = null;
        SerializedProperty m_IsPerspective = null;
        SerializedProperty m_Distance = null;
        private void OnEnable()
        {
            m_Width = serializedObject.FindProperty("m_Width");
            m_Height = serializedObject.FindProperty("m_Height");
            m_UseCustomSize = serializedObject.FindProperty("m_UseCustomSize");
            m_OverlayDepth = serializedObject.FindProperty("m_OverlayDepth");
            m_Offset = serializedObject.FindProperty("m_Offset");
            m_OcclusionBetweenOverlayLayers = serializedObject.FindProperty("m_OcclusionBetweenOverlayLayers");
            m_IsPerspective = serializedObject.FindProperty("m_IsPerspective");
            m_Distance = serializedObject.FindProperty("m_Distance");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_OverlayDepth);
            if (EditorGUI.EndChangeCheck())
            {
                OverlayCanvas overlayCanvas = target as OverlayCanvas;
                overlayCanvas.overlayDepth = m_OverlayDepth.intValue;
            }
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_UseCustomSize);
            if (m_UseCustomSize.boolValue)
            {
                EditorGUILayout.PropertyField(m_Width);
                EditorGUILayout.PropertyField(m_Height);
            }
            
            EditorGUILayout.PropertyField(m_OcclusionBetweenOverlayLayers);
            EditorGUILayout.PropertyField(m_IsPerspective);
            if (m_IsPerspective.boolValue)
            {
                EditorGUILayout.PropertyField(m_Distance);
            }
            EditorGUILayout.PropertyField(m_Offset);
            
            serializedObject.ApplyModifiedProperties();
            if (EditorGUI.EndChangeCheck())
            {
                OverlayCanvas overlayCanvas = target as OverlayCanvas;
                overlayCanvas.UpdateOverlayStatus();
                overlayCanvas.ResetRenderTexture();
            }
        }
    }
}
#endif
