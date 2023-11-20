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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace Matrix.EcosystemSimulate
{
    using UnityObject = UnityEngine.Object;

    public static class CoreEditorUtils
    {
        // GUIContent cache utilities
        private static Dictionary<string, GUIContent> s_GUIContentCacheDict = new Dictionary<string, GUIContent>();

        public static GUIContent GetContent(string textAndTooltip)
        {
            if (string.IsNullOrEmpty(textAndTooltip))
                return GUIContent.none;

            if (!s_GUIContentCacheDict.TryGetValue(textAndTooltip, out GUIContent content))
            {
                var s = textAndTooltip.Split('|');
                content = new GUIContent(s[0]);

                if (s.Length > 1 && !string.IsNullOrEmpty(s[1]))
                    content.tooltip = s[1];

                s_GUIContentCacheDict.Add(textAndTooltip, content);
            }

            return content;
        }
        public static void DrawSplitter()
        {
            var rect = GUILayoutUtility.GetRect(1f, 1f);

            // Splitter rect should be full-width
            rect.xMin = 0f;
            rect.width += 4f;

            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, !EditorGUIUtility.isProSkin
                ? new Color(0.6f, 0.6f, 0.6f, 1.333f)
                : new Color(0.12f, 0.12f, 0.12f, 1.333f));
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
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.5f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 1f));

            // Title
            GUIStyle stype = new GUIStyle("BoldLabel");
            stype.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            EditorGUI.LabelField(labelRect, title, stype);
        }

        public static bool DrawHeaderFoldout(string title, bool state, Action<Vector2> contextAction = null)
        {
            var backgroundRect = GUILayoutUtility.GetRect(0f, 20f);

            var labelRect = backgroundRect;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            var foldoutRect = backgroundRect;
            foldoutRect.y += 3f;
            foldoutRect.width = 14f;
            foldoutRect.height = 14f;

            // Background rect should be full-width
            backgroundRect.xMin = 0f;
            backgroundRect.width += 4f;

            // Background
            float backgroundTint = EditorGUIUtility.isProSkin ? 0.1f : 1f;
            EditorGUI.DrawRect(backgroundRect, new Color(backgroundTint, backgroundTint, backgroundTint, 0.2f));

            // Title
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);

            // Active checkbox
            state = GUI.Toggle(foldoutRect, state, GUIContent.none, EditorStyles.foldout);

            var e = Event.current;
            if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
            {
                state = !state;
                e.Use();
            }


            // Context menu
            Texture2D menuIcon = CoreEditorStyles.paneOptionsIcon;
            Rect menuRect = new Rect(labelRect.xMax + 4f, labelRect.y + 4f, menuIcon.width, menuIcon.height);

            if (contextAction != null)
                GUI.DrawTexture(menuRect, menuIcon);

            // Handle events
            //var e = Event.current;

            if (e.type == EventType.MouseDown)
            {
                if (contextAction != null && menuRect.Contains(e.mousePosition))
                {
                    contextAction(new Vector2(menuRect.x, menuRect.yMax));
                    e.Use();
                }
                else if (labelRect.Contains(e.mousePosition))
                {
                    //if (e.button == 0)
                    //    group.isExpanded = !group.isExpanded;
                    //else 
                    if (contextAction != null)
                        contextAction(e.mousePosition);

                    e.Use();
                }
            }

            return state;
        }

        static readonly GUIContent[] k_DrawVector6_Label =
        {
            new GUIContent("X"),
            new GUIContent("Y"),
            new GUIContent("Z"),
        };

        private const int k_DrawVector6Slider_LabelSize = 60;
        private const int k_DrawVector6Slider_FieldSize = 80;

        static Vector3 DrawVector3(Rect rect, GUIContent[] labels, Vector3 value, Vector3 min, Vector3 max,
            bool addMinusPrefix, Color[] colors)
        {
            float[] multiFloat = new float[] {value.x, value.y, value.z};
            rect = EditorGUI.IndentedRect(rect);
            float fieldWidth = rect.width / 3f;
            EditorGUI.BeginChangeCheck();
            EditorGUI.MultiFloatField(rect, labels, multiFloat);
            if (EditorGUI.EndChangeCheck())
            {
                value.x = Mathf.Max(Mathf.Min(multiFloat[0], max.x), min.x);
                value.y = Mathf.Max(Mathf.Min(multiFloat[1], max.y), min.y);
                value.z = Mathf.Max(Mathf.Min(multiFloat[2], max.z), min.z);
            }

            //Suffix is a hack as sub label only work with 1 character
            if (addMinusPrefix)
            {
                Rect suffixRect = new Rect(rect.x - 33, rect.y, 100, rect.height);
                for (int i = 0; i < 3; ++i)
                {
                    EditorGUI.LabelField(suffixRect, "-");
                    suffixRect.x += fieldWidth + .5f;
                }
            }

            //Color is a hack as nothing is done to handle this at the moment
            if (colors != null)
            {
                if (colors.Length != 3)
                    throw new System.ArgumentException("colors must have 3 elements.");

                Rect suffixRect = new Rect(rect.x - 23, rect.y, 100, rect.height);
                GUIStyle colorMark = new GUIStyle(EditorStyles.label);
                colorMark.normal.textColor = colors[0];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += fieldWidth - .5f;
                colorMark.normal.textColor = colors[1];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += fieldWidth + .5f;
                colorMark.normal.textColor = colors[2];
                EditorGUI.LabelField(suffixRect, "|", colorMark);
                suffixRect.x += 1;
                EditorGUI.LabelField(suffixRect, "|", colorMark);
            }

            return value;
        }

        public static Vector2 DrawListGUI<T>(string name, Vector2 scrollViewPos, ref List<T> list,
            Action<int> drawObjectActionCurrentLine = null,
            Action<int> drawObjectActionNextLine = null) where T : UnityEngine.Object
        {
            int currentCount = list.Count;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{name} 列表");
            GUILayout.FlexibleSpace();
            int count = EditorGUILayout.IntField(currentCount);
            EditorGUILayout.EndHorizontal();
            if (count > currentCount)
            {
                for (int i = 0; i < count - currentCount; i++)
                {
                    list.Add(null);
                }
            }

            if (count < currentCount)
            {
                for (int i = 0; i < currentCount - count; i++)
                {
                    list.Remove(list[list.Count - 1]);
                }
            }

            EditorGUI.indentLevel++;
            EditorGUILayout.BeginVertical();
            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos);
            if (list.Count == 0)
            {
                EditorGUILayout.HelpBox("列表为空" , MessageType.Info);
            }

            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                list[i] = (T) EditorGUILayout.ObjectField($"{name} {i}", list[i], typeof(T), false);
                drawObjectActionCurrentLine?.Invoke(i);
                EditorGUILayout.EndHorizontal();
                drawObjectActionNextLine?.Invoke(i);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("新增" , GUILayout.MinWidth(100)))
            {
                list.Add(null);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUI.indentLevel--;
            GUILayout.FlexibleSpace();
            return scrollViewPos;
        }
    }
}