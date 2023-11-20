using UnityEditor;
using UnityEngine;

namespace IxDE
{
    [CustomEditor(typeof(FaceTarget))]
    public class FaceTargetEditor : Editor
    {
        private const float BUTTON_WIDTH = 80.0f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            var faceTarget = target as FaceTarget;
            
            GUILayout.Space(10);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Set Flat")) faceTarget.SetFlat();
            if (GUILayout.Button("Get Rotation")) faceTarget.GetRotation();
            if (GUILayout.Button("Face")) faceTarget.Face();
            EditorGUILayout.EndHorizontal();
            
        }
    }
}