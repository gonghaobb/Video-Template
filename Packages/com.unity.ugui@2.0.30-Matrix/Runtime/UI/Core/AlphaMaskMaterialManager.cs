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

namespace UnityEngine.UI
{
    public static class AlphaMaskMaterialManager
    {
        private class MaterialEntry
        {
            public Material original;
            public Material replacement;
            public Texture maskTexture;
            public int count;
        }

        private static readonly int s_AlphaMaskTex = Shader.PropertyToID("_AlphaMaskTex");
        private const string UNITY_UI_ALPHAMASK_KEYWORD = "UNITY_UI_ALPHAMASK";
        private static List<MaterialEntry> m_MaterialList = new List<MaterialEntry>();

        public static Material Add(Material baseMaterial, Texture maskTexture)
        {
            if (baseMaterial == null || maskTexture == null)
            {
                return baseMaterial;
            }

            if (!baseMaterial.HasProperty(s_AlphaMaskTex))
            {
                Debug.LogWarning("material:" + baseMaterial + " doesn't support AlphaMask.");
                return baseMaterial;
            }

            for (int i = 0; i < m_MaterialList.Count; ++i)
            {
                MaterialEntry entry = m_MaterialList[i];

                if (entry.original == baseMaterial &&
                    entry.maskTexture == maskTexture)
                {
                    ++entry.count;
                    return entry.replacement;
                }
            }

            Material replacement = new Material(baseMaterial)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };
            replacement.name += " - AlphaMask";
            replacement.EnableKeyword(UNITY_UI_ALPHAMASK_KEYWORD);
            replacement.SetTexture(s_AlphaMaskTex, maskTexture);

            MaterialEntry newMaterialEntry = new MaterialEntry()
            {
                count = 1,
                maskTexture = maskTexture,
                original = baseMaterial,
                replacement = replacement
            };

            m_MaterialList.Add(newMaterialEntry);
            return replacement;
        }

        public static void Remove(Material maskMaterial)
        {
            if (maskMaterial == null)
                return;

            for (int i = 0; i < m_MaterialList.Count; ++i)
            {
                MaterialEntry entry = m_MaterialList[i];

                if (entry.replacement != maskMaterial)
                {
                    continue;
                }

                if (--entry.count == 0)
                {
                    Misc.DestroyImmediate(entry.replacement);
                    entry.original = null;
                    m_MaterialList.RemoveAt(i);
                }

                return;
            }
        }
    }
}