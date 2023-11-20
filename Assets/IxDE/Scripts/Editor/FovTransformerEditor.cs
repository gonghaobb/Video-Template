using UnityEditor;
using UnityEngine;

namespace IxDE
{
    [CustomEditor(typeof(FovTransformer))]
    public class FovTransformerEditor : Editor
    {
        private SerializedProperty m_Distance;
        private SerializedProperty m_FromFov;
        private SerializedProperty m_FromValue;

        private void OnEnable()
        {
            m_Distance = serializedObject.FindProperty("m_Distance");
            m_FromFov = serializedObject.FindProperty("m_FromFov");
            m_FromValue = serializedObject.FindProperty("m_FromValue");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawCustomInspector();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCustomInspector()
        {
            var transformer = (FovTransformer)target;

            EditorGUILayout.PropertyField(m_Distance, EditorGUIUtility.TrTextContent("Distance"));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Get Distance"))
            {
                transformer.GetDistance();
            }

            if (GUILayout.Button("Set Distance"))
            {
                transformer.SetPosition(FovTransformer.Axis.Z);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("From FOV", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_FromFov, EditorGUIUtility.TrTextContent("fov"));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("value", transformer.toValue);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Scale X"))
            {
                transformer.SetScale(FovTransformer.Axis.X);
            }

            if (GUILayout.Button("Set Scale Y"))
            {
                transformer.SetScale(FovTransformer.Axis.Y);
            }

            if (GUILayout.Button("Set Scale Z"))
            {
                transformer.SetScale(FovTransformer.Axis.Z);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Position X"))
            {
                transformer.SetPosition(FovTransformer.Axis.X);
            }

            if (GUILayout.Button("Set Position Y"))
            {
                transformer.SetPosition(FovTransformer.Axis.Y);
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("From Value", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_FromValue, EditorGUIUtility.TrTextContent("value"));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.FloatField("fov", transformer.toFov);
            EditorGUI.EndDisabledGroup();
        }
    }
}