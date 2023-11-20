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
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    /// <summary>
    /// Image is a textured element in the UI hierarchy.
    /// </summary>
    /// <summary>
    ///   Alpha Mask For Image And Others.
    /// </summary>
    ///
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MaskableGraphic))]
    public class AlphaMaskMaterialModifier : UIBehaviour, IMaterialModifier
    {
        [FormerlySerializedAs("softMaskTexture")]
        public Texture2D alphaMaskTexture;
        private MaskableGraphic m_Graphic = null;
        private Material m_MaskMaterial;
        private bool m_IsDestroy = false;
        
        private MaskableGraphic graphic
        {
            get
            {
                if (m_Graphic == null)
                {
                    TryGetComponent(out m_Graphic);
                }

                return m_Graphic;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (graphic != null)
            {
                if (alphaMaskTexture != null)
                {
                    graphic.SetMaterialDirty();
                }
            }
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (graphic != null)
            {
                if (alphaMaskTexture != null)
                {
                    graphic.SetMaterialDirty();
                }

            }
        }
        
        private void SelfDestroy()
        {
            if (Application.isPlaying)
            {
                Destroy(this);
            }
#if UNITY_EDITOR
            else
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        DestroyImmediate(this);
                    }
                };
            }
#endif
            m_IsDestroy = true;
        }

        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || m_IsDestroy)
            {
                return baseMaterial;
            }

            if (alphaMaskTexture == null)
            {
                SelfDestroy();
            }

            if (!UIMaterials.IsDefaultShader(baseMaterial.shader))
            {
                return baseMaterial;
            }

            Material maskMaterial = AlphaMaskMaterialManager.Add(baseMaterial, alphaMaskTexture);
            if (m_MaskMaterial != maskMaterial)
            {
                AlphaMaskMaterialManager.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMaterial;
            }
            
            return m_MaskMaterial;
        }
    }
}