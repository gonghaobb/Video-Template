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
using UnityEngine.Rendering;

namespace Matrix.ShaderGUI
{
    public class BaseShaderGUI : LWGUI.LWGUI
    {
        protected virtual void SetupMaterialKeyWord(Material material)
        {
            SetupMaterialUrpKeyWord(material);

            SetupMaterialCustomKeyWord(material);
        }

        protected virtual void SetupMaterialUrpKeyWord(Material material)
        {
            //迁移Urp的跟Toggle取反的关键字
            if (material.HasProperty("_ReceiveShadows"))
            {
                CoreUtils.SetKeyword(material, "_RECEIVE_SHADOWS_OFF", material.GetFloat("_ReceiveShadows") == 0.0f);
            }

            if (material.HasProperty("_SpecularHighlights"))
            {
                CoreUtils.SetKeyword(material, "_SPECULARHIGHLIGHTS_OFF", material.GetFloat("_SpecularHighlights") == 0.0f);
            }

            if (material.HasProperty("_EnvironmentReflections"))
            {
                CoreUtils.SetKeyword(material, "_ENVIRONMENTREFLECTIONS_OFF", material.GetFloat("_EnvironmentReflections") == 0.0f);
            }

            //迁移Urp的贴图关联的关键字
            if (material.HasProperty("_BumpMap"))
            {
                CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") != null);
            }

            if (material.HasProperty("_NormalMap"))
            {
                CoreUtils.SetKeyword(material, "_NORMALMAP", material.GetTexture("_NormalMap") != null);
            }

            if (material.HasProperty("_MetallicGlossMap"))
            {
                CoreUtils.SetKeyword(material, "_METALLICSPECGLOSSMAP", material.GetTexture("_MetallicGlossMap") != null);
            }

            if (material.HasProperty("_OcclusionMap"))
            {
                CoreUtils.SetKeyword(material, "_OCCLUSIONMAP", material.GetTexture("_OcclusionMap") != null);
            }

            if (material.HasProperty("_ParallaxMap"))
            {
                CoreUtils.SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap") != null);
            }

            bool opaque = true;
            if (material.HasProperty("_Surface"))
            {
                opaque = ((SurfaceType)material.GetFloat("_Surface") == SurfaceType.Opaque);
            }

            //迁移Urp其它的关键字
            if (material.HasProperty("_SmoothnessTextureChannel"))
            {
                CoreUtils.SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", (SmoothnessMapChannel)material.GetFloat("_SmoothnessTextureChannel") == SmoothnessMapChannel.AlbedoAlpha && opaque);
            }
            
            if (material.HasProperty("_DetailAlbedoMap") && material.HasProperty("_DetailNormalMap") && material.HasProperty("_DetailAlbedoMapScale"))
            {
                bool isScaled = material.GetFloat("_DetailAlbedoMapScale") != 1.0f;
                bool hasDetailMap = material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap");
                CoreUtils.SetKeyword(material, "_DETAIL_MULX2", !isScaled && hasDetailMap);
                CoreUtils.SetKeyword(material, "_DETAIL_SCALED", isScaled && hasDetailMap);
            }
        }

        protected virtual void SetupMaterialCustomKeyWord(Material material)
        {
            if (material.HasProperty("_EmssionCubemap"))
            {
                CoreUtils.SetKeyword(material, "_EMISSION_CUBEMAP", material.GetTexture("_EmssionCubemap") != null);
            }
            
            if (material.HasProperty("_LightingMode"))
            {
                LightingMode lightingMode = (LightingMode)material.GetFloat("_LightingMode");
                switch (lightingMode)
                {
                    case LightingMode.PBR:
                        CoreUtils.SetKeyword(material, "_BLING_PHONG", false);
                        CoreUtils.SetKeyword(material, "_PBR", true);
                        break;
                    
                    case LightingMode.BlingPhong:
                        CoreUtils.SetKeyword(material, "_BLING_PHONG", true);
                        CoreUtils.SetKeyword(material, "_PBR", false);
                        break;
                    
                    default:
                        CoreUtils.SetKeyword(material, "_BLING_PHONG", false);
                        CoreUtils.SetKeyword(material, "_PBR", false);
                        break;
                }
            }
        }

        protected virtual void SetupMaterialBlendMode(Material material)
        {
            if (!material.HasProperty("_Surface"))
            {
                return;
            }

            bool alphaClip = (material.HasProperty("_AlphaClip") && material.GetFloat("_AlphaClip") == 1);
            if (alphaClip)
            {
                material.EnableKeyword("_ALPHATEST_ON");
            }
            else
            {
                material.DisableKeyword("_ALPHATEST_ON");
            }

            SurfaceType surfaceType = (SurfaceType)material.GetInt("_Surface");

            bool customZWrite = (material.HasProperty("_CustomZWrite") && material.GetFloat("_CustomZWrite") == 1);

            if (surfaceType == SurfaceType.Opaque)
            {
                if (alphaClip)
                {
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                    material.SetOverrideTag("RenderType", "TransparentCutout");
                }
                else
                {
                    material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                    material.SetOverrideTag("RenderType", "Opaque");
                }

                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;Start
                material.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.Zero);
                //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;End
                if (!customZWrite)
                {
                    material.SetInt("_ZWrite", 1);
                }

                material.SetShaderPassEnabled("DepthOnly", true);
            }
            else
            {
                BlendMode blendMode = (BlendMode)material.GetFloat("_Blend");

                // Specific Transparent Mode Settings
                switch (blendMode)
                {
                    case BlendMode.Alpha:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); 
                        //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;Start
                        material.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;End
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Premultiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;Start
                        material.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;End
                        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Additive:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;Start
                        material.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                        //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;End
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        break;
                    case BlendMode.Multiply:
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;Start
                        material.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.DstColor);
                        material.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.Zero);
                        //PicoVideo;OptimizedOverlayAlphaBlend;WuJunLin;End
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.EnableKeyword("_ALPHAMODULATE_ON");
                        break;
                }

                // General Transparent Material Settings
                material.SetOverrideTag("RenderType", "Transparent");

                if (!customZWrite)
                {
                    material.SetInt("_ZWrite", 0);
                }

                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetShaderPassEnabled("DepthOnly", false);
            }

            if (material.HasProperty("_QueueOffset"))
            {
                material.renderQueue += (int)material.GetFloat("_QueueOffset");
            }

            if (material.IsKeywordEnabled("_EMISSION"))
            {
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
        }

        public virtual void OnMaterialChanged(Material material)
        {
            SetupMaterialKeyWord(material);

            SetupMaterialBlendMode(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            OnMaterialChanged(material);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
        {
            base.OnGUI(materialEditor, props);

            foreach (var obj in materialEditor.targets)
            {
                Material material = obj as Material;

                OnMaterialChanged(material);
            }
        }

        
    }
}