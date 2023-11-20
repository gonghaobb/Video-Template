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
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Matrix.UniversalUIExtension
{
    public class MaterialDistributor
    {
        private readonly Func<Material, Material> m_ReplaceFunc;
        private readonly Action<Material> m_ApplyParameters;

        public MaterialDistributor(Func<Material, Material> replaceFunc, Action<Material> applyParameters)
        {
            m_ReplaceFunc = replaceFunc;
            m_ApplyParameters = applyParameters;
        }

        public static MaterialDistributor identity
        {
            get { return new MaterialDistributor(DefaultUIEffect.Replace, null); }
        }

        private class MaterialEntry
        {
            public Material original;
            public Material replacement;
            public int count;
        }

        private readonly List<MaterialEntry> m_MaterialEntryList = new List<MaterialEntry>();
        private readonly Dictionary<Material, int> m_OriginalDict = new Dictionary<Material, int>();
        private readonly Dictionary<Material, int> m_ReplacementDict = new Dictionary<Material, int>();

        public bool IsReplacementExists(Material material)
        {
            if (m_ReplacementDict.TryGetValue(material, out int _))
            {
                return true;
            }

            return false;
        }

        public Material Get(Material baseMaterial)
        {
            if (baseMaterial == null)
            {
                return baseMaterial;
            }

            if (m_OriginalDict.TryGetValue(baseMaterial, out int index))
            {
                MaterialEntry entry = m_MaterialEntryList[index];
                if (entry.original == baseMaterial)
                {
                    ++entry.count;
                    return entry.replacement;
                }
            }
            else
            {
                Material replacement = m_ReplaceFunc.Invoke(baseMaterial);
                if (replacement != baseMaterial)
                {
                    MaterialEntry newMaterialEntry = new MaterialEntry()
                    {
                        count = 1,
                        original = baseMaterial,
                        replacement = replacement
                    };
                    m_MaterialEntryList.Add(newMaterialEntry);
                    m_OriginalDict.Add(baseMaterial, m_MaterialEntryList.Count - 1);
                    m_ReplacementDict.Add(replacement, m_MaterialEntryList.Count - 1);
                }

                return replacement;
            }

            return baseMaterial;
        }

        public void Remove(Material effectMaterial)
        {
            if (effectMaterial == null)
            {
                return;
            }

            if (m_ReplacementDict.TryGetValue(effectMaterial, out int index))
            {
                MaterialEntry entry = m_MaterialEntryList[index];
                if (--entry.count == 0)
                {
                    RemoveAt(index);
                    if (entry.replacement != null)
                    {
                        if (Application.isEditor)
                        {
                            Object.DestroyImmediate(entry.replacement);
                        }
                        else
                        {
                            Object.Destroy(entry.replacement);
                        }
                    }
                }
            }
        }

        private void RemoveAt(int index)
        {
            MaterialEntry item = m_MaterialEntryList[index];
            m_OriginalDict.Remove(item.original);
            m_ReplacementDict.Remove(item.replacement);
            if (index == m_MaterialEntryList.Count - 1)
                m_MaterialEntryList.RemoveAt(index);
            else
            {
                int replaceItemIndex = m_MaterialEntryList.Count - 1;
                MaterialEntry replaceItem = m_MaterialEntryList[replaceItemIndex];
                m_MaterialEntryList[index] = replaceItem;
                m_OriginalDict[replaceItem.original] = index;
                m_ReplacementDict[replaceItem.replacement] = index;
                m_MaterialEntryList.RemoveAt(replaceItemIndex);
            }
        }

        public void ApplyParameters(Graphic graphic)
        {
            if (graphic != null && m_ApplyParameters != null)
            {
                MaterialManager.instance.RegisterForApplyParameters(graphic, m_ApplyParameters);
            }
        }
    }

    public class MaterialManager
    {
        private MaterialManager()
        {
        }

        private static readonly MaterialManager s_Instance = new MaterialManager();

        public static MaterialManager instance
        {
            get { return s_Instance; }
        }

        private readonly Dictionary<Material, Action<Material>> m_ApplyParametersDict =
            new Dictionary<Material, Action<Material>>();

        private readonly Dictionary<Graphic, Action<Material>> m_UpdateGraphicDict =
            new Dictionary<Graphic, Action<Material>>();

        private bool m_ApplyRequired = false;

        public void RegisterForApplyParameters(Graphic graphic, Action<Material> action)
        {
            if (m_UpdateGraphicDict.TryGetValue(graphic, out Action<Material> applyAction))
            {
                applyAction -= action;
                applyAction += action;
#if UNITY_EDITOR
                //编辑器进入时第0帧的组件与第1帧不一致
                m_UpdateGraphicDict.Remove(graphic);
                m_UpdateGraphicDict.Add(graphic, applyAction);
#else
                m_UpdateGraphicDict[graphic] = applyAction;
#endif
            }
            else
            {
                m_UpdateGraphicDict.Add(graphic, action);
            }

            if (!m_ApplyRequired)
            {
                Canvas.willRenderCanvases += ApplyAllParameters;
                m_ApplyRequired = true;
            }
        }

        private void ApplyAllParameters()
        {
            Canvas.willRenderCanvases -= ApplyAllParameters;
            m_ApplyRequired = false;

            foreach (KeyValuePair<Graphic, Action<Material>> keyValue in m_UpdateGraphicDict)
            {
                if (keyValue.Key == null)
                {
                    continue;
                }

                Material renderMaterial = keyValue.Key.canvasRenderer.GetMaterial();
                if (renderMaterial == null)
                {
                    continue;
                }

                if (m_ApplyParametersDict.TryGetValue(renderMaterial, out Action<Material> applyAction))
                {
                    applyAction -= keyValue.Value;
                    applyAction += keyValue.Value;
                    m_ApplyParametersDict[renderMaterial] = applyAction;
                }
                else
                {
                    m_ApplyParametersDict.Add(renderMaterial, keyValue.Value);
                }
            }

            if (m_ApplyParametersDict.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<Material, Action<Material>> keyValue in m_ApplyParametersDict)
            {
                Material material = keyValue.Key;
                Action<Material> action = keyValue.Value;
                if (material != null && action != null)
                {
                    action.Invoke(material);
                }
            }

            m_UpdateGraphicDict.Clear();
            m_ApplyParametersDict.Clear();
            
        }
    }
}