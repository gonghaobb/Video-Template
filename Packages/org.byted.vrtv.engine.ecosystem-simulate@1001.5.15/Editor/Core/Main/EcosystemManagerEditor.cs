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
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Assertions;

namespace Matrix.EcosystemSimulate
{
    [CustomEditor(typeof(EcosystemManager))]
    public class EcosystemManagerEditor : Editor
    {
        private EcosystemManager m_EcoSystem;
        private Assembly m_Assembly;
        private SubEcosystem m_ClipboardContent;

        public readonly Dictionary<EcosystemType, string> SUB_ECOSYSTEM_NAME_DICT = new Dictionary<EcosystemType, string>()
        {
            //底层模块
            {EcosystemType.HeightMapManager, "高度图模拟"},
            {EcosystemType.WindManager, "风模拟"},
            
            //场景视觉模块
            {EcosystemType.SunLightManager, "太阳光模拟"},
            {EcosystemType.FogManager, "雾-天空模拟"},
            {EcosystemType.RainManager, "雨模拟"},
            {EcosystemType.SnowManager, "雪模拟"},
            {EcosystemType.PostEffectManager, "后处理特效模拟"},
            {EcosystemType.MistVFXManager, "雾气粒子模拟"},
            {EcosystemType.CloudShadowManager, "云阴影模拟"},
            {EcosystemType.RainScreenEffectManager, "屏幕雨滴模拟"},
            {EcosystemType.VegetationManager, "植被模拟"},
            {EcosystemType.WaterManager, "水模拟"},
            {EcosystemType.SceneLightManager, "场景灯光模拟"},
        };

        protected void OnEnable()
        {
            m_EcoSystem = target as EcosystemManager;
            m_Assembly = Assembly.Load("EcosystemSimulate.Runtime");
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            
            const int labelWidth = 110;
            const int buttonWidth = 45;
            Rect lineRect = EditorGUILayout.GetControlRect();
            Rect labelRect = new Rect(lineRect.x, lineRect.y, labelWidth, lineRect.height);
            Rect fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth * 2, lineRect.height);
            
            GUI.backgroundColor = Color.grey;
            GUIContent pro = EditorGUIUtility.TrTextContent("跟随目标", "当前高度图跟随的目标");
            EditorGUI.PrefixLabel(labelRect, pro);
            Transform transformTarget = (Transform)EditorGUI.ObjectField(fieldRect, m_EcoSystem.GetTarget(), typeof(Transform), true);
            if (transformTarget != m_EcoSystem.GetTarget())
            {
                m_EcoSystem.SetTarget(transformTarget);
            }

            lineRect = EditorGUILayout.GetControlRect();
            labelRect = new Rect(lineRect.x, lineRect.y, labelWidth, lineRect.height);
            fieldRect = new Rect(labelRect.xMax, lineRect.y, lineRect.width - labelRect.width - buttonWidth * 2, lineRect.height);
            EditorGUI.PrefixLabel(labelRect, EditorGUIUtility.TrTextContent("效果分档", "效果分档"));
            EcosystemManager.QualityLevel qualityLevel = (EcosystemManager.QualityLevel) 
                EditorGUI.EnumPopup(fieldRect , m_EcoSystem.qualityLevel);
            if (qualityLevel != m_EcoSystem.qualityLevel)
            {
                m_EcoSystem.qualityLevel = qualityLevel;
            }
            // 全局
            CoreEditorUtils.DrawHeader("模块配置");
            Dictionary<EcosystemType, SubEcosystem> tempDic = new Dictionary<EcosystemType, SubEcosystem>(m_EcoSystem.subEcosystemDict);
            using Dictionary<EcosystemType, SubEcosystem>.Enumerator map = tempDic.GetEnumerator();
            while (map.MoveNext())
            {
                KeyValuePair<EcosystemType, SubEcosystem> item = map.Current;

                if (!SUB_ECOSYSTEM_NAME_DICT.ContainsKey(item.Key))
                {
                    continue;
                }

                string title = SUB_ECOSYSTEM_NAME_DICT[item.Key];
                lineRect = EditorGUILayout.GetControlRect();
                labelRect = new Rect(lineRect.x + 35, lineRect.y, labelWidth, lineRect.height);
                fieldRect = new Rect(
                    labelRect.xMax, lineRect.y,
                    lineRect.width - labelRect.width - buttonWidth * 2 - 35,
                    lineRect.height);
                Rect buttonGizmosRect = new Rect(0, lineRect.y, buttonWidth  / 2, lineRect.height);
                Rect buttonNewRect = new Rect(fieldRect.xMax, lineRect.y, buttonWidth, lineRect.height);
                Rect buttonCopyRect = new Rect(buttonNewRect.xMax, lineRect.y, buttonWidth, lineRect.height);

                if (item.Value != null)
                {
                    //Gizmos开关
                    if (item.Value.GetType().GetMethod("OnDrawGizmos").DeclaringType.Name != "SubEcosystem")
                    {
                        GUI.backgroundColor = item.Value.enableGizmos ? Color.yellow : Color.gray;
                        if (GUI.Button(buttonGizmosRect, EditorGUIUtility.TrTextContent("G", item.Value.enableGizmos ? "点击关闭Gizmos" : "点击开启Gizmos"),
                            EditorStyles.miniButtonLeft))
                        {
                            item.Value.enableGizmos = !item.Value.enableGizmos;
                        }
                    }
                    //Enable开关
                    buttonGizmosRect.x += buttonWidth / 2.0f;
                    GUI.backgroundColor = item.Value.enable ? Color.blue : Color.gray;
                    if (GUI.Button(buttonGizmosRect, EditorGUIUtility.TrTextContent("E", item.Value.enable ? "点击关闭组件" : "点击开启组件"),
                        EditorStyles.miniButtonLeft))
                    {
                        item.Value.enable = !item.Value.enable;
                    }
                }
                
                GUI.backgroundColor = Color.grey;
                // 名称
                GUIContent guiContent = EditorGUIUtility.TrTextContent(title, title);
                EditorGUI.LabelField(labelRect, guiContent);
                // 资源
                Type type;
                try
                {
                    type = m_Assembly.GetType("Matrix.EcosystemSimulate." + item.Key, true, true);
                }
                catch (Exception e)
                {
                    Debugger.LogError("生态系统子组件类型获取失败：" + item.Key + "," + e.Message);
                    continue;
                }
                
                using (new EditorGUI.ChangeCheckScope())
                {
                    SubEcosystem profile = (SubEcosystem)EditorGUI.ObjectField(fieldRect, item.Value, type, false);
                    if (profile != m_EcoSystem.Get(item.Key))
                    {
                        if (profile == null)
                        {
                            m_EcoSystem.Remove(item.Key, false);
                        }
                        else
                        {
                            m_EcoSystem.Set(item.Key, profile);
                        }
                    }
                }
                // 新建
                GUI.backgroundColor = Color.green;
                if (GUI.Button(buttonNewRect, EditorGUIUtility.TrTextContent("新建", "创建一个新的配置"), EditorStyles.miniButtonLeft))
                {
                    var scenePath = Path.GetDirectoryName(SceneManager.GetActiveScene().path);
                    var extPath = SceneManager.GetActiveScene().name;
                    var profilePath = scenePath + "/" + extPath;
                    if (!AssetDatabase.IsValidFolder(profilePath))
                    {
                        AssetDatabase.CreateFolder(scenePath, extPath);
                    }

                    string newAssetPath = profilePath + "/" + type.Name.Replace("Manager", "Simulate") + ".asset";
                    SubEcosystem subSystem = (SubEcosystem)CreateNewSubSystem(type, newAssetPath);
                    subSystem.Create();
                    m_EcoSystem.Set(item.Key, subSystem);
                }
                // 删除
                GUI.backgroundColor = Color.red;
                if (GUI.Button(buttonCopyRect, EditorGUIUtility.TrTextContent("删除", "移除此功能"), EditorStyles.miniButton))
                {
                    m_EcoSystem.Remove(item.Key);
                }
            }

            using (EditorGUILayout.HorizontalScope scope = new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button(EditorGUIUtility.TrTextContent("添加模块"), EditorStyles.miniButton))
                {
                    Rect r = scope.rect;
                    Vector2 pos = new Vector2(r.x + r.width / 2f, r.yMax + 18f);
                    FilterWindow.Show(pos, new SubEcosystemProvider(m_EcoSystem, this));
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(m_EcoSystem);
            }
            GUI.backgroundColor = Color.white;
            // 是否打开子组件
            using Dictionary<EcosystemType, SubEcosystem>.Enumerator mapSubSystem = tempDic.GetEnumerator();
            while (mapSubSystem.MoveNext())
            {
                if (!SUB_ECOSYSTEM_NAME_DICT.ContainsKey(mapSubSystem.Current.Key))
                {
                    continue;
                }
                string title = SUB_ECOSYSTEM_NAME_DICT[mapSubSystem.Current.Key];
                DrawSubSystem(mapSubSystem.Current.Key, mapSubSystem.Current.Value, title, mapSubSystem.Current.Value);
            }
        }

        private ScriptableObject CreateNewSubSystem(Type type, string path)
        {
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            var profile = ScriptableObject.CreateInstance(type);
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return profile;
        }

        private void DrawSubSystem(EcosystemType type, SubEcosystem item, string settingName, UnityEngine.Object obj)
        {
            if(obj == null)
            {
                return;
            }
            CoreEditorUtils.DrawSplitter();
            item.isOpen = CoreEditorUtils.DrawHeaderFoldout(settingName, item.isOpen, pos => OnContextClick(pos, type));
            if (item.isOpen)
            {
                EditorGUI.indentLevel++;
                Editor editor = Editor.CreateEditor(obj);
                editor.OnInspectorGUI();
                EditorGUI.indentLevel--;
            }
        }

        private void OnContextClick(Vector2 position, EcosystemType type)
        {
            var menu = new GenericMenu();
            var targetComponent = m_EcoSystem.subEcosystemDict[type];

            menu.AddItem(EditorGUIUtility.TrTextContent("重置参数"), false, () => ResetSetting(type, targetComponent));
            menu.AddItem(EditorGUIUtility.TrTextContent("复制参数"), false, () => CopySettings(targetComponent));
            if (CanPaste(targetComponent))
            {
                menu.AddItem(EditorGUIUtility.TrTextContent("粘贴参数"), false, () => PasteSettings(targetComponent));
            }
            else
            {
                menu.AddDisabledItem(EditorGUIUtility.TrTextContent("粘贴参数"));
            }

            menu.AddItem(EditorGUIUtility.TrTextContent("向上移动"), false, () => MoveComponent(type, -1));
            menu.AddItem(EditorGUIUtility.TrTextContent("向下移动"), false, () => MoveComponent(type, 1));
            menu.AddItem(EditorGUIUtility.TrTextContent("删除"), false, () => RemoveComponent(type));
            menu.DropDown(new Rect(position, Vector2.zero));
        }

        private void RemoveComponent(EcosystemType type)
        {
            if (m_EcoSystem == null)
            {
                return;
            }
            m_EcoSystem.Remove(type);
        }

        private void MoveComponent(EcosystemType type, int offset)
        {
            if(m_EcoSystem == null)
            {
                return;
            }
            List<KeyValuePair<EcosystemType, SubEcosystem>> list = new List<KeyValuePair<EcosystemType, SubEcosystem>>(m_EcoSystem.subEcosystemDict);
            int curIndex = 0;
            KeyValuePair<EcosystemType, SubEcosystem> curItem = new KeyValuePair<EcosystemType, SubEcosystem>();
            for (int i = 0; i < list.Count; i ++)
            {
                if(list[i].Key == type)
                {
                    curIndex = i;
                    curItem = list[i];
                }
            }

            if(curIndex + offset >= list.Count)
            {
                return;
            }
            list[curIndex] = list[curIndex + offset];
            list[curIndex + offset] = curItem;

            m_EcoSystem.subEcosystemDict.Clear();
            foreach (KeyValuePair<EcosystemType, SubEcosystem> pair in list)
            {
                m_EcoSystem.subEcosystemDict.Add(pair.Key, pair.Value);
            }
        }

        private bool CanPaste(SubEcosystem targetComponent)
        {
            return m_ClipboardContent != null
                && m_ClipboardContent.GetType() == targetComponent.GetType();
        }

        private void CopySettings(SubEcosystem targetComponent)
        {
            if (m_ClipboardContent != null)
            {
                OnSafeDestroy(m_ClipboardContent);
                m_ClipboardContent = null;
            }

            m_ClipboardContent = (SubEcosystem)ScriptableObject.CreateInstance(targetComponent.GetType());
            EditorUtility.CopySerializedIfDifferent(targetComponent, m_ClipboardContent);
        }

        private void PasteSettings(SubEcosystem targetComponent)
        {
            Assert.IsNotNull(m_ClipboardContent);
            Assert.AreEqual(m_ClipboardContent.GetType(), targetComponent.GetType());

            Undo.RecordObject(targetComponent, "Paste Settings");
            EditorUtility.CopySerializedIfDifferent(m_ClipboardContent, targetComponent);
        }

        private void ResetSetting(EcosystemType eType, SubEcosystem targetComponent)
        {
            Type type = targetComponent.GetType();
            string path = AssetDatabase.GetAssetPath(targetComponent);
            AssetDatabase.DeleteAsset(path);
            SubEcosystem sub = (SubEcosystem)CreateNewSubSystem(type, path);
            sub.Create();
            m_EcoSystem.subEcosystemDict[eType] = sub;
        }

        public void OnSafeDestroy(UnityEngine.Object obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
#else
                Destroy(obj);
#endif
            }
        }
    }
}
