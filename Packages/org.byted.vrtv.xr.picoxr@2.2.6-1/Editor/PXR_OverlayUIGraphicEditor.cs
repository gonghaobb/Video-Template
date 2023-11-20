using UnityEngine;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(PXR_OverlayUIGraphic), true)]
    [CanEditMultipleObjects]
    public class PXR_OverlayUIGraphicEditor : GraphicEditor
    {
        SerializedProperty m_FixedLengthMode;
        SerializedProperty m_RoundedCornersRatio;
        SerializedProperty m_RoundedCornersRadius;
        SerializedProperty m_SoftEdgeRatio;
        SerializedProperty m_SoftEdgeLength;
        SerializedProperty m_OverlaySize;
        SerializedProperty m_Overlay;
    
        protected override void OnEnable()
        {
            base.OnEnable();
            m_FixedLengthMode = serializedObject.FindProperty("fixedLengthMode");
            m_RoundedCornersRatio = serializedObject.FindProperty("roundedCornersRatio");
            m_RoundedCornersRadius = serializedObject.FindProperty("roundedCornersRadius");
            m_SoftEdgeRatio = serializedObject.FindProperty("softEdgeRatio");
            m_SoftEdgeLength = serializedObject.FindProperty("softEdgeLength");
            m_OverlaySize = serializedObject.FindProperty("overlaySize");
            m_Overlay = serializedObject.FindProperty("overlay");
        }
    
        protected override void OnDisable()
        {
            Tools.hidden = false;
        }
    
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Overlay);
            EditorGUILayout.PropertyField(m_OverlaySize);
            EditorGUILayout.PropertyField(m_FixedLengthMode);
            if (m_FixedLengthMode.boolValue)
            {
                EditorGUILayout.PropertyField(m_RoundedCornersRadius);
                EditorGUILayout.PropertyField(m_SoftEdgeLength);
            }
            else
            {
                EditorGUILayout.PropertyField(m_RoundedCornersRatio,new GUIContent("Rounded Corners Ratio (Based on width)"));
                EditorGUILayout.PropertyField(m_SoftEdgeRatio);
            }

            RaycastControlsGUI();
            serializedObject.ApplyModifiedProperties();
        }
    }
}