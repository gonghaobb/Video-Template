using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class SimpleLitShader : BaseShaderGUI
    {
        // Properties
        private SimpleLitGUI.SimpleLitProperties shadingModelProperties;
       

        //PicoVideo;Ecosystem;ZhengLingFeng;Begin
        private EcosystemGUI.EcosystemProperties litEcosystemProperties;
        private SavedBool m_EcosystemInputsFoldout;
        //PicoVideo;Ecosystem;ZhengLingFeng;End
        
        public override void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            base.OnOpenGUI(material, materialEditor);
            //PicoVideo;Ecosystem;ZhengLingFeng;Begin
            m_EcosystemInputsFoldout = new SavedBool($"{headerStateKey}.EcosystemInputsFoldout", true);
            //PicoVideo;Ecosystem;ZhengLingFeng;End
        }

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            shadingModelProperties = new SimpleLitGUI.SimpleLitProperties(properties);

            //PicoVideo;Ecosystem;ZhengLingFeng;Begin
            litEcosystemProperties = new EcosystemGUI.EcosystemProperties(properties);
            //PicoVideo;Ecosystem;ZhengLingFeng;End
        }
        
        public override void DrawAdditionalFoldouts(Material material)
        {
            //PicoVideo;Ecosystem;ZhengLingFeng;Begin
            m_EcosystemInputsFoldout.value = EditorGUILayout.BeginFoldoutHeaderGroup(m_EcosystemInputsFoldout.value, EcosystemGUI.Styles.ecosystemInputs);
            if (m_EcosystemInputsFoldout.value)
            {
                EcosystemGUI.DoEcosystemArea(litEcosystemProperties, materialEditor, material);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            //PicoVideo;Ecosystem;ZhengLingFeng;End
        }

        // material changed check
        public override void MaterialChanged(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);

            //PicoVideo;Ecosystem;ZhengLingFeng;Begin
            EcosystemGUI.SetMateiralKeywords(litEcosystemProperties, material);
            //PicoVideo;Ecosystem;ZhengLingFeng;End
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;

            // Detect any changes to the material
            EditorGUI.BeginChangeCheck();
            {
                base.DrawSurfaceOptions(material);
            }
            if (EditorGUI.EndChangeCheck())
            {
                foreach (var obj in blendModeProp.targets)
                    MaterialChanged((Material)obj);
            }
        }

        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            SimpleLitGUI.Inputs(shadingModelProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            SimpleLitGUI.Advanced(shadingModelProperties);
            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);

            MaterialChanged(material);
        }
    }
}
