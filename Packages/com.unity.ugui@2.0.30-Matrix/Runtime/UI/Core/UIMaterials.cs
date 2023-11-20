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
using UnityEngine.Rendering;

namespace UnityEngine.UI
{
    public static class UIMaterials
    {
        public static bool useFrameBufferFetch
        {
            get
            {
#if !UNITY_EDITOR
                if (Application.isMobilePlatform && SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                {
                    return true;
                }
#endif
                return false;
            }
        }
        
        public const string DEFAULT_UI_SHADER = "UI/Default";
        public const string DEFAULT_ADJUSTMENT_UI_SHADER = "UI/DefaultGammaSpace";
        public const string DEFAULT_ADJUSTMENT_UI_SHADER_NOFETCH = "UI/DefaultGammaSpace(NoFetch)";
        public const string RENDER_IN_LINEAR = "RENDER_IN_LINEAR";
        public const string UNITY_UI_SRGB = "UNITY_UI_SRGB";

        private static readonly Shader s_DefaultUIShader = Shader.Find(DEFAULT_UI_SHADER);
        private static readonly Shader s_DefaultAdjustmentUIShader = Shader.Find(DEFAULT_ADJUSTMENT_UI_SHADER);
        private static readonly Shader s_DefaultAdjustmentUIShaderNoFetch = Shader.Find(DEFAULT_ADJUSTMENT_UI_SHADER_NOFETCH);
        
        private static Material s_GammaSRGBMaterial = null; //渲染在Gamma空间，Texture2D选中SRGB
        private static Material s_GammaMaterial = null;     //渲染在Gamma空间，Texture2D未选中SRGB
        private static Material s_LinearSRGBMaterial = null;//渲染在Linear空间，Texture2D选中SRGB
        private static Material s_LinearMaterial = null;    //渲染在Linear空间，Texture2D未选中SRGB

        public static Material gammaSRGBMaterial
        {
            get
            {
                if (s_GammaSRGBMaterial == null)
                {
                    s_GammaSRGBMaterial = new Material(Graphic.defaultGraphicMaterial)
                    {
                        shader = Shader.Find(DEFAULT_ADJUSTMENT_UI_SHADER_NOFETCH),
                        hideFlags = HideFlags.HideAndDontSave,
                        name = "GammaSRGBMaterial",
                    };
                    // s_GammaSRGBMaterial.DisableKeyword(RENDER_IN_LINEAR);
                    s_GammaSRGBMaterial.EnableKeyword(UNITY_UI_SRGB);
                }

                return s_GammaSRGBMaterial;
            }
        }
        
        public static Material gammaMaterial
        {
            get
            {
                if (s_GammaMaterial == null)
                {
                    s_GammaMaterial = new Material(Graphic.defaultGraphicMaterial)
                    {
                        shader = Shader.Find(DEFAULT_ADJUSTMENT_UI_SHADER_NOFETCH),
                        hideFlags = HideFlags.HideAndDontSave,
                        name = "GammaMaterial",
                    };
                    // s_GammaMaterial.DisableKeyword(RENDER_IN_LINEAR);
                    s_GammaMaterial.DisableKeyword(UNITY_UI_SRGB);
                }

                return s_GammaMaterial;
            }
        }
        
        public static Material linearSRGBMaterial
        {
            get
            {
                if (s_LinearSRGBMaterial == null)
                {
                    s_LinearSRGBMaterial = new Material(Graphic.defaultGraphicMaterial)
                    {
                        shader = GetDefaultShader(),
                        hideFlags = HideFlags.HideAndDontSave,
                        name = "LinearSRGBMaterial",
                    };
                    // s_LinearSRGBMaterial.EnableKeyword(RENDER_IN_LINEAR);
                    s_LinearSRGBMaterial.EnableKeyword(UNITY_UI_SRGB);
                }

                return s_LinearSRGBMaterial;
            }
        }

        public static Material linearMaterial
        {
            get
            {
                if (s_LinearMaterial == null)
                {
                    s_LinearMaterial = new Material(Graphic.defaultGraphicMaterial)
                    {
                        shader = GetDefaultShader(),
                        hideFlags = HideFlags.HideAndDontSave,
                        name = "LinearMaterial",
                    };
                    // s_LinearMaterial.EnableKeyword(RENDER_IN_LINEAR);
                    s_LinearMaterial.DisableKeyword(UNITY_UI_SRGB);
                }

                return s_LinearMaterial;
            }
        }

#if PLATFORM_ANDROID
        private static Material s_OESMaterial = null;

        public static Material OESMaterial
        {
            get
            {
                if (s_OESMaterial == null)
                {
                    s_OESMaterial = new Material(Graphic.defaultGraphicMaterial)
                    {
                        shader = GetDefaultShader(),
                        hideFlags = HideFlags.HideAndDontSave,
                        name = "OESMaterial"
                    };
                    // s_OESMaterial.DisableKeyword(RENDER_IN_LINEAR);
                    s_OESMaterial.DisableKeyword(UNITY_UI_SRGB);
                    s_OESMaterial.EnableKeyword("_EXTERNAL_TEXTURE");
                }

                return s_OESMaterial;
            }
        }
#endif
        
        public static Shader GetDefaultShader()
        {
            return useFrameBufferFetch ? s_DefaultAdjustmentUIShader :s_DefaultAdjustmentUIShaderNoFetch;
        }

        public static bool IsDefaultShader(Shader shader)
        {
            if (shader == s_DefaultUIShader
                || shader == s_DefaultAdjustmentUIShader
                || shader == s_DefaultAdjustmentUIShaderNoFetch)
            {
                return true;
            }

            return false;
        }
        
        private class MaterialEntry
        {
            public Material original;
            public Material replacement;
            public int count;
        }
        
        private static readonly List<MaterialEntry> s_MaterialEntryList = new List<MaterialEntry>();
        private static readonly Dictionary<Material, int> s_OriginalDict = new Dictionary<Material, int>();

        public static Material GetModifyMaterial(Material baseMaterial, bool isRenderInLinear, bool isTextureSRGB)
        {
            if (s_OriginalDict.TryGetValue(baseMaterial, out var index))
            {
                var entry = s_MaterialEntryList[index];
                if (entry.original == baseMaterial)
                {
                    ++entry.count;
                    return entry.replacement;
                }
            }
            else
            {
                Material replacement = new Material(baseMaterial)
                {
                    shader = isRenderInLinear ? s_DefaultAdjustmentUIShaderNoFetch : GetDefaultShader(),
                    hideFlags = HideFlags.HideAndDontSave,
                    renderQueue = baseMaterial.renderQueue
                };
                
                if (isRenderInLinear)
                {
                    // replacement.EnableKeyword(RENDER_IN_LINEAR);
                    replacement.name += " - RenderInLinear";
                }
                else
                {
                    // replacement.DisableKeyword(RENDER_IN_LINEAR);
                }

                if (isTextureSRGB)
                {
                    replacement.EnableKeyword(UNITY_UI_SRGB);
                    replacement.name += " - SRGB";
                }
                else
                {
                    replacement.DisableKeyword(UNITY_UI_SRGB);
                }
                
                
                MaterialEntry newMaterialEntry = new MaterialEntry()
                {
                    count = 1,
                    original = baseMaterial,
                    replacement = replacement
                };
                s_MaterialEntryList.Add(newMaterialEntry);
                s_OriginalDict.Add(baseMaterial, s_MaterialEntryList.Count - 1);
                return replacement;
            }

            return baseMaterial;
        }
        
        public static void RemoveModifyMaterial(Material baseMaterial)
        {
            if (baseMaterial == null)
            {
                return;
            }

            if (s_OriginalDict.TryGetValue(baseMaterial, out var index))
            {
                MaterialEntry entry = s_MaterialEntryList[index];
                if (--entry.count == 0)
                {
                    RemoveAt(index);
                    if (entry.replacement != null)
                    {
#if UNITY_EDITOR
                        if (Application.isEditor)
                        {
                            Object.DestroyImmediate(entry.replacement);
                        }
                        else
#endif
                        {
                            Object.Destroy(entry.replacement);
                        }
                    }
                }
            }
        }
        
        private static void RemoveAt(int index)
        {
            MaterialEntry item = s_MaterialEntryList[index];
            s_OriginalDict.Remove(item.original);
            if (index == s_MaterialEntryList.Count - 1)
            {
                s_MaterialEntryList.RemoveAt(index);
            }
            else
            {
                int replaceItemIndex = s_MaterialEntryList.Count - 1;
                MaterialEntry replaceItem = s_MaterialEntryList[replaceItemIndex];
                s_MaterialEntryList[index] = replaceItem;
                s_OriginalDict[replaceItem.original] = index;
                s_MaterialEntryList.RemoveAt(replaceItemIndex);
            }
        }
    }
}