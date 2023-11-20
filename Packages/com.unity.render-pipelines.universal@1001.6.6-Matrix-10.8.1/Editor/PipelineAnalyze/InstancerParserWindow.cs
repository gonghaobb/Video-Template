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
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UnityEngine.Rendering.Universal
{
    public class PipelineAnalyzeWindow : EditorWindow
    {
        private static PipelineAnalyzeWindow s_Window = null;
        private string m_InputPipelineMaskCode = "Input pipeline mask code ...";

        [MenuItem("Window/PicoVideo/PipelineAnalyze")]
        private static void ShowWindow()
        {
            GetWindow();
        }

        private static PipelineAnalyzeWindow GetWindow()
        {
            if (s_Window == null)
            {
                s_Window = GetWindow<PipelineAnalyzeWindow>("Pipeline Analyze");
            }

            return s_Window;
        }

        private void OnGUI()
        {
            DrawHeader("Common");
            m_InputPipelineMaskCode = GUILayout.TextField(m_InputPipelineMaskCode);
            if (GUILayout.Button("Analyze", GUILayout.Width(180), GUILayout.Height(50)))
            {
                if (uint.TryParse(m_InputPipelineMaskCode, out var result))
                {
                    PipelineState pipelineState = new PipelineState();
                    MaskPipelineEncoder maskPipelineEncoder = new MaskPipelineEncoder();
                    maskPipelineEncoder.pipelineMaskValue = result;
                    maskPipelineEncoder.Decode(ref pipelineState);
                    pipelineState.Printf();
                    EditorUtility.DisplayDialog("Pipeline Analyze","解析成功，在Console中查看结果！", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Pipeline Analyze", "请输入正确的编码!", "OK");
                }
            }
        }
        
        public static void DrawHeader(string title)
        {
            var backgroundRect = GUILayoutUtility.GetRect(1f, 20f);

            var labelRect = backgroundRect;
            labelRect.xMin += 0f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 1f;
            foldoutRect.width = 13f;
            foldoutRect.height = 13f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            var backgroundTint = EditorGUIUtility.isProSkin ? 0.5f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 1f));

            // Title
            var stype = new GUIStyle("BoldLabel");
            stype.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            EditorGUI.LabelField(labelRect, title, stype);
        }
        
        public static void DrawSplitter()
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
        }
    }
}

