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
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEngine.UI
{
    class SoftMaskMaterialReplacements
    {
        Func<Material, Material> m_Replace;
        Action<Material> m_ApplyParameters;

        readonly List<SoftMaskMaterialOverride> m_Overrides = new List<SoftMaskMaterialOverride>();

        public SoftMaskMaterialReplacements(Func<Material, Material> replace, Action<Material> applyParameters)
        {
            m_Replace = replace;
            m_ApplyParameters = applyParameters;
        }

        public Material Get(Material original)
        {
            for (int i = 0; i < m_Overrides.Count; ++i)
            {
                var entry = m_Overrides[i];
                if (ReferenceEquals(entry.original, original))
                {
                    var existing = entry.Get();
                    if (existing)// null may be stored in _overrides
                    { 
                        //existing.CopyPropertiesFromMaterial(original);
                        m_ApplyParameters(existing);
                    }
                    return existing;
                }   
            }

            var replacement = m_Replace(original);
            if (replacement)
            {
                replacement.hideFlags = HideFlags.HideAndDontSave;
                m_ApplyParameters(replacement);
            }

            m_Overrides.Add(new SoftMaskMaterialOverride(original, replacement));
            return replacement;
        }

        public void Release(Material replacement)
        {
            for (int i = 0; i < m_Overrides.Count; ++i)
            {
                var entry = m_Overrides[i];
                if (entry.replacement == replacement)
                {
                    if (entry.Release())
                    {
                        UnityEngine.Object.DestroyImmediate(replacement);
                        m_Overrides.RemoveAt(i);
                        return;
                    }
                }
            }
        }

        public void ApplyAll()
        {
            for (int i = 0; i < m_Overrides.Count; ++i)
            {
                var mat = m_Overrides[i].replacement;
                if (mat)
                {
                    m_ApplyParameters(mat);
                }
            }
        }

        public void DestroyAllAndClear()
        {
            for (int i = 0; i < m_Overrides.Count; ++i)
            {
                UnityEngine.Object.DestroyImmediate(m_Overrides[i].replacement);
            }

            m_Overrides.Clear();
        }

        class SoftMaskMaterialOverride
        {
            int m_UseCount;

            public SoftMaskMaterialOverride(Material original, Material replacement)
            {
                this.original = original;
                this.replacement = replacement;
                m_UseCount = 1;
            }

            public Material original { get; private set; }
            public Material replacement { get; private set; }

            public Material Get()
            {
                ++m_UseCount;
                return replacement;
            }

            public bool Release()
            {
                Assert.IsTrue(m_UseCount > 0);
                return --m_UseCount == 0;
            }
        }
    }
}
