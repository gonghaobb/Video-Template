using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(Bloom))]
    sealed class BloomEditor : VolumeComponentEditor
    {
        //PicoVideo;CustomBloom;ZhouShaoyang;Begin
        SerializedDataParameter m_IsCustomBloom;
        SerializedDataParameter m_FixedIterations;
        SerializedDataParameter m_IterationMode;
        SerializedDataParameter m_maskMode;
        //PicoVideo;CustomBloom;ZhouShaoyang;End
        SerializedDataParameter m_Threshold;
        SerializedDataParameter m_Intensity;
        SerializedDataParameter m_Scatter;
        SerializedDataParameter m_Clamp;
        SerializedDataParameter m_Tint;
        SerializedDataParameter m_HighQualityFiltering;
        SerializedDataParameter m_SkipIterations;
        SerializedDataParameter m_DirtTexture;
        SerializedDataParameter m_DirtIntensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<Bloom>(serializedObject);

            //PicoVideo;CustomBloom;ZhouShaoyang;Begin
            m_IsCustomBloom = Unpack(o.Find(x => x.isCustomBloom));
            m_IterationMode = Unpack(o.Find(x => x.iterationMode));
            m_FixedIterations = Unpack(o.Find(x => x.fixedIterations));
            m_maskMode = Unpack(o.Find(x => x.maskMode));
            //PicoVideo;CustomBloom;ZhouShaoyang;End
            m_Threshold = Unpack(o.Find(x => x.threshold));
            m_Intensity = Unpack(o.Find(x => x.intensity));
            m_Scatter = Unpack(o.Find(x => x.scatter));
            m_Clamp = Unpack(o.Find(x => x.clamp));
            m_Tint = Unpack(o.Find(x => x.tint));
            m_HighQualityFiltering = Unpack(o.Find(x => x.highQualityFiltering));
            m_SkipIterations = Unpack(o.Find(x => x.skipIterations));
            m_DirtTexture = Unpack(o.Find(x => x.dirtTexture));
            m_DirtIntensity = Unpack(o.Find(x => x.dirtIntensity));
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.LabelField("Bloom", EditorStyles.miniLabel);

            //PicoVideo;CustomBloom;ZhouShaoyang;Begin
            PropertyField(m_IsCustomBloom);
            PropertyField(m_IterationMode);
            if (m_IterationMode.value.intValue == (int)BloomIterationMode.Fixed)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_FixedIterations);
                EditorGUI.indentLevel--;
            }
            else if (m_IterationMode.value.intValue == (int)BloomIterationMode.Skip)
            {
                EditorGUI.indentLevel++;
                PropertyField(m_SkipIterations);
                EditorGUI.indentLevel--;
            }

            {
                var tintStyle = new GUIStyle(EditorStyles.miniLabel);
                tintStyle.normal.textColor = Color.red;
                if ((m_maskMode.value.intValue == (int)BloomMaskMode.Alpha_Forward ||
                     m_maskMode.value.intValue == (int)BloomMaskMode.Alpha_Reverse) &&
                    (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).supportsHDR &&
                    Camera.main.allowHDR)
                {
                    EditorGUILayout.LabelField("注意：Alpha Mask仅在未开启HDR情况下生效↓↓↓", tintStyle);
                }
                else if ((m_maskMode.value.intValue == (int)BloomMaskMode.DepthStencil_Forward ||
                          m_maskMode.value.intValue == (int)BloomMaskMode.DepthStencil_Reverse) &&
                         (GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset).msaaSampleCount != 1)
                {
                    EditorGUILayout.LabelField("注意：Stencil Mask在开启msaa时可能会影响帧数↓↓↓", tintStyle);
                }
            }
            PropertyField(m_maskMode);
            //PicoVideo;CustomBloom;ZhouShaoyang;End
            
            PropertyField(m_Threshold);
            PropertyField(m_Intensity);
            PropertyField(m_Scatter);
            PropertyField(m_Tint);
            PropertyField(m_Clamp);
            PropertyField(m_HighQualityFiltering);

            if (m_HighQualityFiltering.overrideState.boolValue && m_HighQualityFiltering.value.boolValue &&
                CoreEditorUtils.buildTargets.Contains(GraphicsDeviceType.OpenGLES2))
            {
                EditorGUILayout.HelpBox("High Quality Bloom isn't supported on GLES2 platforms.", MessageType.Warning);
            }

            EditorGUILayout.LabelField("Lens Dirt", EditorStyles.miniLabel);

            PropertyField(m_DirtTexture);
            PropertyField(m_DirtIntensity);
            
            //PicoVideo;CustomBloom;ZhouShaoyang;Begin
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            string url = "https://bytedance.feishu.cn/docx/doxcnuaDA12wQNBUpVBQkQ1MDwo";
            var caption = $"<color=#81ecec>{"说明文档"}</color>";
            var urlStyle = GUI.skin.button;
            urlStyle.richText = true;
            if (GUILayout.Button(caption, urlStyle))
            {
                Application.OpenURL(url);
            }
            //PicoVideo;CustomBloom;ZhouShaoyang;End
        }
    }
}
