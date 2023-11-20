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
//      Procedural LOGO:https://www.shadertoy.com/view/WdyfDm 
//                  @us:https://kdocs.cn/l/sawqlPuqKX7f
//
//      The team was set up on September 4, 2019.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Matrix.EcosystemSimulate
{
    [CustomEditor(typeof(EcosystemVolume))]
    public class EcosystemVolumeEditor : Editor
    {
        private bool m_IsExpandAddSelectedRect = false;
        
        private static List<bool> s_VolumeEcosystemEditList = new List<bool>();
        private static List<EcosystemType> s_EcosystemTypeList = new List<EcosystemType>();
        private readonly List<EcosystemType> m_TempEcosystemTypeList = new List<EcosystemType>();

        private List<string> m_ExcludeParametersList = new List<string>();

        private void OnEnable()
        {
            s_EcosystemTypeList.Clear();
            s_EcosystemTypeList.AddRange(EcosystemVolume.s_ParametersConfigDict.Keys);
            
            m_TempEcosystemTypeList.Clear();
            
            if (s_VolumeEcosystemEditList.Count != EcosystemVolume.s_ParametersConfigDict.Count)
            {
                s_VolumeEcosystemEditList.Clear();
                for (int i = 0; i < EcosystemVolume.s_ParametersConfigDict.Count; i++)
                {
                    s_VolumeEcosystemEditList.Add(false);
                }
            }
        }

        public override void OnInspectorGUI()
        {
            EcosystemVolume volume = (EcosystemVolume) target;

            serializedObject.Update();
            CoreEditorUtils.DrawHeader("全局配置");
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField("权重", EditorStyles.boldLabel);
            float weight = EditorGUILayout.Slider("Weight", volume.weight, 0.0f, 1.0f);
            if (Math.Abs(weight - volume.weight) > 0.001f)
            {
                volume.weight = weight;
            }
            EditorGUILayout.LabelField("优先级", EditorStyles.boldLabel);
            float priority = EditorGUILayout.Slider("Priority", volume.priority, 0.0f, 1.0f);
            if (Mathf.Abs(priority - volume.priority) > 0.001f)
            {
                volume.priority = priority;
                EcosystemVolumeManager.ReSortVolumeListByPriority();
            }
            EditorGUILayout.LabelField("类型", EditorStyles.boldLabel);
            volume.volumeType = (EcosystemVolume.VolumeType) EditorGUILayout.EnumPopup("Volume Type", volume.volumeType);
            EditorGUILayout.EndVertical();
            
            if (!volume.isGlobal)
            {
                CoreEditorUtils.DrawHeader("Local Volume配置");
                DrawLocalVolumeEditor(volume);
            }
            else
            {
                CoreEditorUtils.DrawHeader("Global Volume配置");
                DrawGlobalVolumeEditor(volume);
                DrawSystemVolumeEditor(volume);
            }

            CoreEditorUtils.DrawHeader("Components 配置");
            DrawComponents(volume);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSystemVolumeEditor(EcosystemVolume volume)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.TextField("系统Volume切换配置", EditorStyles.boldLabel);
            volume.transitionParameters.startingDelayTime = EditorGUILayout.FloatField("启动延迟时间",
                volume.transitionParameters.startingDelayTime);
            volume.transitionParameters.startingTime = EditorGUILayout.FloatField("启动过渡时间",
                volume.transitionParameters.startingTime);
            volume.transitionParameters.endingDelayTime = EditorGUILayout.FloatField("结束延迟时间",
                volume.transitionParameters.endingDelayTime);
            volume.transitionParameters.endingTime = EditorGUILayout.FloatField("结束过渡时间",
                volume.transitionParameters.endingTime);
            EditorGUILayout.EndVertical();
        }

        private void DrawComponents(EcosystemVolume volume)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("组件配置列表", EditorStyles.boldLabel);
            for (int i = 0; i < volume.ecosystemParametersValueList.Count; i++)
            {
                if (volume.ecosystemParametersValueList[i] == null)
                {
                    continue;   
                }
                
                EcosystemType type = volume.ecosystemParametersValueList[i].GetEcosystemType();
                EcosystemParameters parameters = volume.ecosystemParametersValueList[i];

                EditorGUILayout.BeginHorizontal();
                EcosystemParameters newParameters = (EcosystemParameters) EditorGUILayout.ObjectField(
                    type.ToString(), parameters,
                    EcosystemVolume.s_ParametersConfigDict[type], false);
                if (newParameters != parameters)
                {
                    volume.ecosystemParametersValueList[i] = newParameters;
                    volume.SetChanged(type);
                }

                int editIndex = s_EcosystemTypeList.IndexOf(type);
                bool isEdit = s_VolumeEcosystemEditList[editIndex];
                if (GUILayout.Button($"{(isEdit ? "隐藏" : "展开")}配置"))
                {
                    s_VolumeEcosystemEditList[editIndex] = !s_VolumeEcosystemEditList[editIndex];
                }

                if (GUILayout.Button("删除"))
                {
                    volume.ecosystemParametersValueList.Remove(parameters);
                    volume.SetChanged(type);
                }
                EditorGUILayout.EndHorizontal();

                if (EcosystemManager.instance == null || EcosystemManager.instance.Get(type) == null || 
                    !EcosystemManager.instance.Get(type).enable)
                {
                    EditorGUILayout.HelpBox($"EcosystemManager未配置或者未启用{type}组件，" +
                                            "当前配置无法生效", MessageType.Error);
                }
                
                if (isEdit && parameters != null)
                {
                    EditorGUILayout.BeginVertical("Box");
                    EditorGUI.indentLevel++;
                    SerializedObject serObj = CreateEditor(parameters).serializedObject;
                    serObj.Update();
                    if (volume.isGlobal)
                    {
                        EditorGUILayout.TextField($"切换配置 ({parameters.GetStartType()} | {parameters.GetEndType()})", EditorStyles.boldLabel);
                        volume.TryGetTransitionParameters(parameters.GetEcosystemType() , out TransitionParameters tParameters);
                        tParameters.startingDelayTime = EditorGUILayout.FloatField("启动延迟时间",
                            tParameters.startingDelayTime);
                        tParameters.startingTime = EditorGUILayout.FloatField("启动过渡时间",
                            tParameters.startingTime);
                        tParameters.endingDelayTime = EditorGUILayout.FloatField("结束延迟时间",
                            tParameters.endingDelayTime);
                        tParameters.endingTime = EditorGUILayout.FloatField("结束过渡时间",
                            tParameters.endingTime);
                    }
                    EditorGUILayout.TextField("参数配置", EditorStyles.boldLabel);

                    m_ExcludeParametersList.Clear();
                    m_ExcludeParametersList.Add("m_Script");
                    foreach (FieldInfo fieldInfo in parameters.GetType().GetRuntimeFields())
                    {
                        foreach (Attribute attr in fieldInfo.GetCustomAttributes())
                        {
                            if (attr is VolumeParametersAttribute attribute)
                            {
                                if (volume.isGlobal && attribute.displayType == VolumeParametersAttribute.DisplayType.Local)
                                {
                                    m_ExcludeParametersList.Add(fieldInfo.Name);
                                }
                                
                                if (!volume.isGlobal && attribute.displayType == VolumeParametersAttribute.DisplayType.Global)
                                {
                                    m_ExcludeParametersList.Add(fieldInfo.Name);
                                }
                            }
                        }
                    }
                    
                    DrawInspectorExcept(serObj, m_ExcludeParametersList.ToArray());
                    if (serObj.hasModifiedProperties)
                    {
                        volume.SetChanged(type);
                    }
                    serObj.ApplyModifiedProperties();
                    EditorGUI.indentLevel--;
                    EditorGUILayout.EndVertical();
                }
            }

            for (int i = 0; i < m_TempEcosystemTypeList.Count; i++)
            {
                EcosystemType type = m_TempEcosystemTypeList[i];
                EditorGUILayout.BeginHorizontal();
                EcosystemParameters newParameters = (EcosystemParameters) EditorGUILayout.ObjectField(
                    type.ToString(), null,
                    EcosystemVolume.s_ParametersConfigDict[type], false);
                if (newParameters != null)
                {
                    m_TempEcosystemTypeList.Remove(type);
                    volume.ecosystemParametersValueList.Add(newParameters);
                    volume.SetChanged(type);
                }
                
                int editIndex = s_EcosystemTypeList.IndexOf(type);
                bool isEdit = s_VolumeEcosystemEditList[editIndex];
                if (GUILayout.Button($"{(isEdit ? "隐藏" : "展开")}配置"))
                {
                    s_VolumeEcosystemEditList[editIndex] = !s_VolumeEcosystemEditList[editIndex];
                }

                if (GUILayout.Button("删除"))
                {
                    m_TempEcosystemTypeList.Remove(type);
                }
                
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button($"{(m_IsExpandAddSelectedRect ? "关闭" : "打开")} 新增组件列表"))
            {
                m_IsExpandAddSelectedRect = !m_IsExpandAddSelectedRect;
            }
            
            if (m_IsExpandAddSelectedRect)
            {
                EditorGUILayout.BeginVertical("Box");
                foreach (EcosystemType t in s_EcosystemTypeList)
                {
                    bool isEnable = !volume.ContainsParameters(t);

                    GUI.enabled = isEnable;
                    if (GUILayout.Button($"添加 {t} 组件 {(!isEnable ? "(已添加)" : "")}"))
                    {
                        m_TempEcosystemTypeList.Add(t);
                    }

                    GUI.enabled = true;
                }

                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndVertical();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
        
        public void DrawInspectorExcept(SerializedObject serializedObject, string[] fieldsToSkip)
        {
            SerializedProperty prop = serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (fieldsToSkip.Any(v => prop.name.Contains(v)))
                    {
                        continue;
                    }

                    SerializedProperty sp = serializedObject.FindProperty(prop.name);
                    if (sp.isArray)
                    {
                        int oldSize = sp.arraySize;
                        int newSize = EditorGUILayout.IntField(sp.displayName + " List Size", sp.arraySize);
                        if (newSize != oldSize)
                        {
                            if (newSize > oldSize)
                            {
                                for (int i = 0; i < newSize - oldSize; i++)
                                {
                                    sp.InsertArrayElementAtIndex(sp.arraySize);
                                }
                            }
                            else
                            {
                                for (int i = 0; i < oldSize - newSize; i++)
                                {
                                    sp.DeleteArrayElementAtIndex(sp.arraySize - 1);
                                }
                            }
                        }
                        
                        EditorGUI.indentLevel++;
                        EditorGUILayout.BeginVertical("Box");
                        for (int i = 0; i < sp.arraySize; i++)
                        {
                            SerializedProperty elementProperty = sp.GetArrayElementAtIndex(i);
                            EditorGUILayout.PropertyField(elementProperty , true); 
                        }
                        EditorGUILayout.EndVertical();
                        EditorGUI.indentLevel--;
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(sp, true);
                    }
                }
                while (prop.NextVisible(false));
            }
        }

        private void DrawGlobalVolumeEditor(EcosystemVolume volume)
        {
            volume.switchSpeedMultiplier = EditorGUILayout.FloatField("切换速率倍数", volume.switchSpeedMultiplier);
            bool canSwitchToThis = CanSwitchToThis(volume);
            GUI.enabled = canSwitchToThis;
            EditorGUILayout.BeginVertical("box");
            EcosystemVolume tVolume = EcosystemVolumeManager.current;
            if (tVolume != null)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("当前天气 :", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(
                    $"{tVolume.name} | {(EcosystemVolumeManager.next ? "Processing" : "Running")}", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"过渡到{volume.name}"))
            {
                if (canSwitchToThis)
                {
                    EcosystemVolume.s_TransitionTime = TransitionToThis(volume);
                    EcosystemVolume.s_TransitionStartTime = Time.timeSinceLevelLoad;
                }
            }

            if (GUILayout.Button($"立即切换到{volume.name}"))
            {
                if (canSwitchToThis)
                {
                    SwitchToThis(volume);
                }
            }
            EditorGUILayout.EndHorizontal();

            float progress = EcosystemVolume.s_TransitionTime > 0
                ? (Time.timeSinceLevelLoad - EcosystemVolume.s_TransitionStartTime) / EcosystemVolume.s_TransitionTime
                : 0;
            if (progress > 0 && progress < 1) 
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect() , progress , "切换进度");
            }

            EditorGUILayout.EndVertical();
            GUI.enabled = true;
        }

        public static void SwitchToThis(EcosystemVolume volume)
        {
            if (EcosystemVolumeManager.current != null)
            {
                EcosystemVolumeManager.current.ResetState(EcosystemVolume.VolumeState.Pending);
            }
            
            volume.ResetState(EcosystemVolume.VolumeState.Running);
            EcosystemVolumeManager.current = volume;
        }

        private float TransitionToThis(EcosystemVolume volume)
        {
            Vector2 currentTransitionTime = EcosystemVolumeManager.current.MaxTransitionTimeToNextState();
            Vector2 nextTransitionTime = volume.MaxTransitionTimeToNextState();
            float transitionTime = Mathf.Max(currentTransitionTime.x, nextTransitionTime.x);
            float lateTransitionTime = Mathf.Max(currentTransitionTime.y, nextTransitionTime.y);
            EcosystemVolumeManager.SwitchVolume(volume , transitionTime , lateTransitionTime);

            EditorApplication.update += Repaint;
            return transitionTime + lateTransitionTime;
        }
        
        private static bool CanSwitchToThis(EcosystemVolume volume)
        {
            return EcosystemVolumeManager.CanSwitchGlobalVolume() && 
                   EcosystemVolumeManager.current != volume;
        }

        private void DrawLocalVolumeEditor(EcosystemVolume volume)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("触发器类型", EditorStyles.boldLabel);
            volume.colliderType = (EcosystemVolume.ColliderType)
                EditorGUILayout.EnumPopup("Collider Type", volume.colliderType);
            if (volume.colliderType == EcosystemVolume.ColliderType.Sphere)
            {
                if (volume.sphereCollider != null)
                {
                    EditorGUILayout.LabelField("半径", EditorStyles.boldLabel);
                    volume.sphereCollider.radius = EditorGUILayout.FloatField("Radius", volume.sphereCollider.radius);
                    EditorGUILayout.LabelField("渐变半径", EditorStyles.boldLabel);
                    volume.sphereInvFadeRadius =
                        EditorGUILayout.FloatField("Inv Fade Radius", volume.sphereInvFadeRadius);
                    volume.sphereInvFadeRadius =
                        Mathf.Clamp(volume.sphereInvFadeRadius, 0.001f, volume.sphereCollider.radius);
                }
                else
                {
                    volume.sphereCollider = volume.GetComponent<SphereCollider>();
                    if (volume.sphereCollider != null)
                    {
                        EditorUtility.SetDirty(target);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("缺少 Sphere Collider 组件", MessageType.Error);
                    }
                }
            }
            
            if (volume.colliderType == EcosystemVolume.ColliderType.Box)
            {
                if (volume.boxCollider != null)
                {
                    EditorGUILayout.LabelField("大小", EditorStyles.boldLabel);
                    volume.boxCollider.size = EditorGUILayout.Vector3Field("Size", volume.boxCollider.size);
                    EditorGUILayout.LabelField("渐变大小", EditorStyles.boldLabel);
                    Vector3 invFadeSize = EditorGUILayout.Vector3Field("Inv Fade Size", volume.boxInvFadeSize);
                    invFadeSize.x = Mathf.Clamp(invFadeSize.x, 0.001f, volume.boxCollider.size.x * 0.5f);
                    invFadeSize.y = Mathf.Clamp(invFadeSize.y, 0.001f, volume.boxCollider.size.y * 0.5f);
                    invFadeSize.z = Mathf.Clamp(invFadeSize.z, 0.001f, volume.boxCollider.size.z * 0.5f);
                    volume.boxInvFadeSize = invFadeSize;
                }
                else
                {
                    volume.boxCollider = volume.GetComponent<BoxCollider>();
                    if (volume.boxCollider != null)
                    {
                        EditorUtility.SetDirty(target);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("缺少 Box Collider 组件", MessageType.Error);
                    }
                }
            }
            EditorGUILayout.EndVertical();
            
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
                volume.SetChanged();
            }
        }
    }
}