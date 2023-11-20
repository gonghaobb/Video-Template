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
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace UnityEditor.UI
{
    [CustomEditor(typeof(SoftMask))]
    [CanEditMultipleObjects]
    public class SoftMaskEditor : UnityEditor.Editor
    {
        SerializedProperty m_Source;
        SerializedProperty m_SeparateMask;
        SerializedProperty m_Sprite;
        SerializedProperty m_SpriteBorderMode;
        SerializedProperty m_Texture;
        SerializedProperty m_TextureUVRect;
        SerializedProperty m_ChannelWeights;
        SerializedProperty m_RaycastThreshold;
        SerializedProperty m_SelfMask;

        bool m_CustomWeightsExpanded;

        static class Labels
        {
            public static readonly GUIContent MaskChannel = new GUIContent("Mask Channel");
            public static readonly GUIContent ChannelWeights = new GUIContent("Channel Weights");
            public static readonly GUIContent R = new GUIContent("R");
            public static readonly GUIContent G = new GUIContent("G");
            public static readonly GUIContent B = new GUIContent("B");
            public static readonly GUIContent A = new GUIContent("A");
            public static readonly string UnsupportedShaders =
                "Some of children's shaders aren't supported. Mask won't work on these elements. " +
                "See the documentation for more details about how to add Soft Mask support to " +
                "custom shaders.";
            public static readonly string NestedMasks =
                "Mask may work not as expected because a child or a parent SoftMask exists. " +
                "SoftMask doesn't support nesting. You can work around this limitation by nesting " +
                "a SoftMask into a Unity standard Mask or RectMask2D or vice versa.";
            public static readonly string TightPackedSprite =
                "SoftMask doesn't support tight packed sprites. Disable packing for the mask sprite " +
                "or use Rectangle pack mode.";
            public static readonly string AlphaSplitSprite =
                "SoftMask doesn't support sprites with an alpha split texture. Disable compression of " +
                "the sprite texture or use another compression type.";
            public static readonly string UnsupportedImageType =
                "SoftMask doesn't support this image type. Supported image types are Simple, Sliced " +
                "and Tiled.";
        }

        public void OnEnable()
        {
            m_Source = serializedObject.FindProperty("m_Source");
            m_SeparateMask = serializedObject.FindProperty("m_SeparateMask");
            m_Sprite = serializedObject.FindProperty("m_Sprite");
            m_SpriteBorderMode = serializedObject.FindProperty("m_SpriteBorderMode");
            m_Texture = serializedObject.FindProperty("m_Texture");
            m_TextureUVRect = serializedObject.FindProperty("m_TextureUVRect");
            m_ChannelWeights = serializedObject.FindProperty("m_ChannelWeights");
            m_RaycastThreshold = serializedObject.FindProperty("m_RaycastThreshold");
            m_SelfMask = serializedObject.FindProperty("m_SelfMask");
            Assert.IsNotNull(m_Source);
            Assert.IsNotNull(m_SeparateMask);
            Assert.IsNotNull(m_Sprite);
            Assert.IsNotNull(m_SpriteBorderMode);
            Assert.IsNotNull(m_Texture);
            Assert.IsNotNull(m_TextureUVRect);
            Assert.IsNotNull(m_ChannelWeights);
            Assert.IsNotNull(m_RaycastThreshold);
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_Source);
            CustomEditors.WithIndent(() =>
            {
                switch ((SoftMask.MaskSource)m_Source.enumValueIndex)
                {
                    case SoftMask.MaskSource.Graphic:
                        break;
                    case SoftMask.MaskSource.Sprite:
                        EditorGUILayout.PropertyField(m_Sprite);
                        EditorGUILayout.PropertyField(m_SpriteBorderMode);
                        break;
                    case SoftMask.MaskSource.Texture:
                        EditorGUILayout.PropertyField(m_Texture);
                        EditorGUILayout.PropertyField(m_TextureUVRect);
                        break;
                }
            });
            EditorGUILayout.PropertyField(m_SeparateMask);
            EditorGUILayout.Slider(m_RaycastThreshold, 0, 1);
            CustomEditors.ChannelWeights(Labels.MaskChannel, m_ChannelWeights, ref m_CustomWeightsExpanded);
            EditorGUILayout.PropertyField(m_SelfMask);
            ShowErrorsIfAny();
            serializedObject.ApplyModifiedProperties();
        }

        void ShowErrorsIfAny()
        {
            var errors = CollectErrors();
            if ((errors & SoftMask.Errors.UnsupportedShaders) != 0)
                EditorGUILayout.HelpBox(Labels.UnsupportedShaders, MessageType.Warning);
            //if ((errors & SoftMask.Errors.NestedMasks) != 0)
            //    EditorGUILayout.HelpBox(Labels.NestedMasks, MessageType.Warning);
            if ((errors & SoftMask.Errors.TightPackedSprite) != 0)
                EditorGUILayout.HelpBox(Labels.TightPackedSprite, MessageType.Error);
            if ((errors & SoftMask.Errors.AlphaSplitSprite) != 0)
                EditorGUILayout.HelpBox(Labels.AlphaSplitSprite, MessageType.Error);
            if ((errors & SoftMask.Errors.UnsupportedImageType) != 0)
                EditorGUILayout.HelpBox(Labels.UnsupportedImageType, MessageType.Error);
        }

        SoftMask.Errors CollectErrors()
        {
            SoftMask.Errors result = SoftMask.Errors.NoError;
            foreach (var t in targets)
            {
                result |= ((SoftMask)t).PollErrors();
            }

            return result;
        }

        public static class CustomEditors
        {
            public static void ChannelWeights(GUIContent label, SerializedProperty weightsProp, ref bool customWeightsExpanded)
            {
                var rect = GUILayoutUtility.GetRect(GUIContent.none, KnownChannelStyle);
                if (customWeightsExpanded)
                {
                    rect.max = GUILayoutUtility.GetRect(GUIContent.none, CustomWeightsStyle).max;
                }
                ChannelWeights(rect, label, weightsProp, ref customWeightsExpanded);
            }

            public static void WithIndent(Action f)
            {
                ++EditorGUI.indentLevel;
                try
                {
                    f();
                }
                finally
                {
                    --EditorGUI.indentLevel;
                }
            }

            static readonly GUIStyle KnownChannelStyle = EditorStyles.popup;
            static readonly GUIStyle CustomWeightsStyle = EditorStyles.textField;

            static void ChannelWeights(Rect rect, GUIContent label, SerializedProperty weightsProp, ref bool customWeightsExpanded)
            {
                var knownChannel =
                    customWeightsExpanded
                        ? KnownMaskChannel.Custom
                        : KnownChannel(weightsProp.colorValue);
                label = EditorGUI.BeginProperty(rect, label, weightsProp);
                EditorGUI.BeginChangeCheck();
                if (customWeightsExpanded)
                    rect.height = HeightOf(KnownChannelStyle);
                knownChannel = (KnownMaskChannel)EditorGUI.EnumPopup(rect, label, knownChannel);
                var weights = Weights(knownChannel, weightsProp.colorValue);
                if (customWeightsExpanded)
                {
                    rect.y += rect.height + Mathf.Max(KnownChannelStyle.margin.bottom, CustomWeightsStyle.margin.top);
                    rect.height = HeightOf(CustomWeightsStyle);
                    WithIndent(() =>
                    {
                        weights = ColorField(rect, Labels.ChannelWeights, weights);
                    });
                }
                if (EditorGUI.EndChangeCheck())
                    weightsProp.colorValue = weights;
                if (Event.current.type != EventType.Layout)
                    customWeightsExpanded = knownChannel == KnownMaskChannel.Custom;
                EditorGUI.EndProperty();
            }

            static Color ColorField(Rect rect, GUIContent label, Color color)
            {
                rect = EditorGUI.PrefixLabel(rect, label);
                color.r = ColorComponentField(Part(rect, 0, 4, 2), Labels.R, color.r);
                color.g = ColorComponentField(Part(rect, 1, 4, 2), Labels.G, color.g);
                color.b = ColorComponentField(Part(rect, 2, 4, 2), Labels.B, color.b);
                color.a = ColorComponentField(Part(rect, 3, 4, 2), Labels.A, color.a);
                return color;
            }

            static float ColorComponentField(Rect rect, GUIContent label, float value)
            {
                return WithZeroIndent(() =>
                {
                    var labelWidth = EditorStyles.label.CalcSize(label).x + 1;
                    EditorGUI.LabelField(new Rect(rect) { width = labelWidth }, label);
                    rect.width -= labelWidth;
                    rect.x += labelWidth;
                    return EditorGUI.FloatField(rect, value);
                });
            }

            static Rect Part(Rect whole, int part, int partCount, int spacing)
            {
                var result = new Rect(whole);
                result.width -= (partCount - 1) * spacing;
                result.width /= partCount;
                result.x += part * (result.width + spacing);
                return result;
            }

            static T WithZeroIndent<T>(Func<T> f)
            {
                var prev = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                try
                {
                    return f();
                }
                finally
                {
                    EditorGUI.indentLevel = prev;
                }
            }

            static float HeightOf(GUIStyle style) { return style.CalcSize(GUIContent.none).y; }

            enum KnownMaskChannel { Alpha, Red, Green, Blue, Gray, Custom }

            static KnownMaskChannel KnownChannel(Color weights)
            {
                if (weights == MaskChannel.alpha) return KnownMaskChannel.Alpha;
                else if (weights == MaskChannel.red) return KnownMaskChannel.Red;
                else if (weights == MaskChannel.green) return KnownMaskChannel.Green;
                else if (weights == MaskChannel.blue) return KnownMaskChannel.Blue;
                else if (weights == MaskChannel.gray) return KnownMaskChannel.Gray;
                else return KnownMaskChannel.Custom;
            }

            static Color Weights(KnownMaskChannel known, Color custom)
            {
                switch (known)
                {
                    case KnownMaskChannel.Alpha: return MaskChannel.alpha;
                    case KnownMaskChannel.Red: return MaskChannel.red;
                    case KnownMaskChannel.Green: return MaskChannel.green;
                    case KnownMaskChannel.Blue: return MaskChannel.blue;
                    case KnownMaskChannel.Gray: return MaskChannel.gray;
                    default: return custom;
                }
            }
        }
    }
}
