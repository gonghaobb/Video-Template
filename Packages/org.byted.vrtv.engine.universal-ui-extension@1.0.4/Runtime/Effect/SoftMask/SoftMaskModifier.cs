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
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Matrix.UniversalUIExtension
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("UniversalUIEffect/SoftMaskModifier", 11)]
    [HideInInspector]
    public class SoftMaskModifier : UIBehaviour, IMaterialModifier
    {
        [NonSerialized]
        private Material m_MaskMaterial = null;

        [NonSerialized]
        private RectTransform m_RectTransform;

        public RectTransform rectTransform
        {
            get { return m_RectTransform ? m_RectTransform : m_RectTransform = GetComponent<RectTransform>(); }
        }

        [NonSerialized]
        private Graphic m_Graphic;

        private Graphic graphic
        {
            get
            {
                if (m_Graphic != null)
                {
                    return m_Graphic;
                }

                if (!TryGetComponent(out m_Graphic))
                {
                    SelfDestroy();
                }

                return m_Graphic;
            }
        }

        [NonSerialized]
        private SoftMask m_ParentSoftMask = null;

        public SoftMask parentSoftMask
        {
            get { return m_ParentSoftMask; }
        }

        [SerializeField]
        private bool m_AutoDestroy = false;

        public bool autoDestroy
        {
            get { return m_AutoDestroy; }
            set
            {
                m_AutoDestroy = value;
                RebindParentSoftMask();
            }
        }

        private bool m_IsDestroy = false;

        protected override void Start()
        {
            base.Start();
            RebindParentSoftMask();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SetMaterialDirty();
            if (graphic != null)
            {
                Canvas canvas = graphic.canvas;
                if (canvas != null)
                { canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                    if (canvas.rootCanvas != null)
                    { canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                    }
                }
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetMaterialDirty();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (m_ParentSoftMask != null)
            {
                m_ParentSoftMask.materialDistributor.Remove(m_MaskMaterial);
                m_ParentSoftMask.UnregisterModifier(this);
            }
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            RebindParentSoftMask();
            if (graphic != null)
            {
                Canvas canvas = graphic.canvas;
                if (canvas != null)
                {
                    canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                    if (canvas.rootCanvas != null)
                    {
                        canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                    }
                }
            }
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            RebindParentSoftMask();
        }

        public void RebindParentSoftMask()
        {
            Canvas rootSortOverrideCanvas = UIUtilities.FindRootSortOverrideCanvas(transform, true);
            if (transform.parent == null
                || !UIUtilities.TryFindParentSoftMask(transform.parent, out SoftMask mask)
                || (rootSortOverrideCanvas != null 
                    && rootSortOverrideCanvas.overrideSorting
                    && rootSortOverrideCanvas.transform.IsChildOf(mask.transform)))
            {
                m_ParentSoftMask = null;
                if (autoDestroy)
                {
                    SelfDestroy();
                }
            }
            else
            {
                if (mask != m_ParentSoftMask)
                {
                    mask.RegisterModifier(this);
                    if (m_ParentSoftMask != null)
                    {
                        m_ParentSoftMask.UnregisterModifier(this);
                    }

                    m_ParentSoftMask = mask;
                }
            }
        }

        internal void SelfDestroy()
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

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (!isActiveAndEnabled || m_ParentSoftMask == null || !m_ParentSoftMask.isMasking || m_IsDestroy)
            {
                return baseMaterial;
            }

            MaterialDistributor materialDistributor = m_ParentSoftMask.materialDistributor;
            Material maskMaterial = materialDistributor.Get(baseMaterial);
            if (maskMaterial != m_MaskMaterial)
            {
                materialDistributor.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMaterial;
            }

            materialDistributor.ApplyParameters(graphic);
            return m_MaskMaterial;
        }

        internal void SetMaterialDirty()
        {
            if (graphic != null)
            {
                graphic.SetMaterialDirty();
            }
        }

        internal void ApplyParameters()
        {
            if (!isActiveAndEnabled || m_ParentSoftMask == null || !m_ParentSoftMask.isMasking || graphic == null)
            {
                return;
            }

            MaterialDistributor materialDistributor = m_ParentSoftMask.materialDistributor;
            materialDistributor.ApplyParameters(graphic);
        }
    }
}