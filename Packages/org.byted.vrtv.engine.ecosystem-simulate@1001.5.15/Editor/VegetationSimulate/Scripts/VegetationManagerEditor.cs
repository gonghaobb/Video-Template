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

using UnityEditor;
using UnityEngine;

namespace Matrix.EcosystemSimulate
{
    [CustomEditor(typeof(VegetationManager))]
    public class VegetationManagerEditor : Editor
    {
        private VegetationManager m_VegetationManager;
        private bool m_SceneCaptureFoldout = false;
        private void OnEnable()
        {
            m_VegetationManager = target as VegetationManager;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (m_VegetationManager.sceneCaptureSettings != null)
            {
                m_VegetationManager.sceneCaptureSettings.centerPos = EditorGUILayout.Vector3Field("Center Pos", m_VegetationManager.sceneCaptureSettings.centerPos);
                m_VegetationManager.sceneCaptureSettings.orthographicSize =
                    EditorGUILayout.FloatField("Orthographic Size",
                        m_VegetationManager.sceneCaptureSettings.orthographicSize);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Capture Texture");
                m_VegetationManager.sceneCaptureSettings.sceneColorTexture =
                    EditorGUILayout.ObjectField(m_VegetationManager.sceneCaptureSettings.sceneColorTexture, typeof(Texture2D), false) as Texture2D;
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}