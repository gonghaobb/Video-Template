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
    [CustomEditor(typeof(PostEffectManager))]
    public class PostEffectManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // base.OnInspectorGUI();
            serializedObject.Update();

            PostEffectManager mgr = target as PostEffectManager;
            if (mgr == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Enable"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("postEffectShader"));

            //屏幕扭曲
            SerializedProperty enableScreenDistortion = serializedObject.FindProperty("enableScreenDistortion");
            EditorGUILayout.PropertyField(enableScreenDistortion);
            if (enableScreenDistortion.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenDistortionTexture"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenDistortionTextureScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenDistortionTextureOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenDistortionU"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenDistortionV"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("screenDistortStrength"));
                EditorGUI.indentLevel--;
            }

            SerializedProperty enableKawaseBlur = serializedObject.FindProperty("enableKawaseBlur");
            EditorGUILayout.PropertyField(enableKawaseBlur);
            if (enableKawaseBlur.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("kawaseBlurRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("kawaseBlurIteration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("kawaseBlurEdgeWeakDistance"));
                EditorGUI.indentLevel--;
            }

            SerializedProperty enableGrainyBlur = serializedObject.FindProperty("enableGrainyBlur");
            EditorGUILayout.PropertyField(enableGrainyBlur);
            if (enableGrainyBlur.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("grainyRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("grainyBlurIteration"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("grainyBlurEdgeWeakDistance"));
                EditorGUI.indentLevel--;
            }

            SerializedProperty enableGlitchImageBlock = serializedObject.FindProperty("enableGlitchImageBlock");
            EditorGUILayout.PropertyField(enableGlitchImageBlock);
            if (enableGlitchImageBlock.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glitchImageBlockSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glitchImageBlockSize"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glitchImageBlockMaxRGBSplitX"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glitchImageBlockMaxRGBSplitY"));
                EditorGUI.indentLevel--;
            }

            SerializedProperty enableGlitchScreenShake = serializedObject.FindProperty("enableGlitchScreenShake");
            EditorGUILayout.PropertyField(enableGlitchScreenShake);
            if (enableGlitchScreenShake.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glitchScreenShakeIndensityX"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("glitchScreenShakeIndensityY"));
                EditorGUI.indentLevel--;
            }

            SerializedProperty enableScreenDissolve = serializedObject.FindProperty("enableScreenDissolve");
            EditorGUILayout.PropertyField(enableScreenDissolve);
            if (enableScreenDissolve.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dissolveTexture"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dissolveTextureScale"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dissolveTextureOffset"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dissolveWidth"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dissolveEdgeColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dissolveBackgroundColor"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dissolveHardEdge"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("dissolveProcess"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("invertDissolve"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lensDistortionStrength"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lensDistortionIntensity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("lensDistortionRange"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}