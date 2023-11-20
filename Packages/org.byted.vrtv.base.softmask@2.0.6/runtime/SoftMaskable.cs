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

using UnityEngine;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class SoftMaskable : UIBehaviour, IMaterialModifier
    {
        private ISoftMask m_Mask;
        private Graphic m_Graphic;
        private Material m_Replacement;
        private bool m_AffectedByMask;
        private bool m_Destroyed;

        public bool shaderIsNotSupported { get; private set; }

        public bool isMaskingEnabled
        {
            get
            {
                return mask != null
                    && mask.isAlive
                    && mask.isMaskingEnabled
                    && m_AffectedByMask;
            }
        }

        public ISoftMask mask
        {
            get { return m_Mask; }
            private set
            {
                if (m_Mask != value)
                {
                    if (m_Mask != null)
                    {
                        replacement = null;
                    }
                    m_Mask = (value != null && value.isAlive) ? value : null;
                    Invalidate();
                }
            }
        }

        public Material GetModifiedMaterial(Material baseMaterial)
        {
            if (isMaskingEnabled)
            {
                // First get a new material, then release the old one. It allows us to reuse 
                // the old material if it's still actual.
                var newMat = mask.GetReplacement(baseMaterial);
                replacement = newMat;

                if (replacement)
                {
                    shaderIsNotSupported = false;
                    return replacement;
                }
                // Warn only if material has non-default UI shader. Otherwise, it seems that
                // replacement is null because SoftMask.defaultShader isn't set. If so, it's
                // SoftMask's business.
                if (!baseMaterial.HasDefaultUIShader())
                {
                    SetShaderNotSupported(baseMaterial);
                }
            }
            else
            {
                shaderIsNotSupported = false;
                replacement = null;
            }
            return baseMaterial;
        }
        
        private void CheckReplacement()
        {
            if (isActiveAndEnabled && isMaskingEnabled && graphic != null)
            {
                replacement = graphic.cacheRendererMaterial;
            }
        }

        // Called when replacement material might changed, so, material should be reevaluated.
        public void Invalidate()
        {
            if (graphic != null)
                graphic.SetMaterialDirty();
        }

        // Called when active mask might changed, so, mask should be searched again.
        public void MaskMightChanged()
        {
            if (FindMaskOrDie())
                Invalidate();
        }
        
        public void ApplyMaterialParameters()
        {
            if (mask != null)
            {
                mask.ApplyParameters(replacement);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            hideFlags = HideFlags.HideInInspector;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (graphic != null)
            {
                graphic.RegisterUpdateCompleteCallback(CheckReplacement);
            }

            if (FindMaskOrDie())
                RequestChildTransformUpdate();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (graphic != null)
            {
                graphic.UnregisterUpdateCompleteCallback(CheckReplacement);
            }

            mask = null; // To invalidate the Graphic and free the material
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_Destroyed = true;
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            FindMaskOrDie();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            // Change of override sorting might changed the mask instance we masked by
            FindMaskOrDie();
        }

        void OnTransformChildrenChanged()
        {
            RequestChildTransformUpdate();
        }

        void RequestChildTransformUpdate()
        {
            if (mask != null)
                mask.UpdateTransformChildren(transform);
        }

        Graphic graphic { get { return m_Graphic ? m_Graphic : (m_Graphic = GetComponent<Graphic>()); } }

        public Material replacement
        {
            get { return m_Replacement; }
            private set
            {
                if (m_Replacement != value && value != null)
                {
                    if (m_Replacement != null && mask != null)
                    {
                        mask.ReleaseReplacement(m_Replacement);
                        mask.ApplyParameters(value);
                    }
                    
                    m_Replacement = value;
                    
                }
            }
        }

        bool FindMaskOrDie()
        {
            if (m_Destroyed)
            {
                return false;
            }

            mask = NearestMask(transform, out m_AffectedByMask)
                ?? NearestMask(transform, out m_AffectedByMask, enabledOnly: false);

            if (mask == null)
            {
                m_Destroyed = true;
                if (Application.isPlaying)
                {
                    Destroy(this);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        DestroyImmediate(this);
                    };
#endif
                }

                return false;
            }

            return true;
        }

        static ISoftMask NearestMask(Transform transform, out bool processedByThisMask, bool enabledOnly = true)
        {
            processedByThisMask = true;
            var current = transform;
            while (true)
            {
                if (!current)
                {
                    return null;
                }
                
                var mask = GetISoftMask(current, shouldBeEnabled: enabledOnly);
                if (mask != null)
                {
                    if (current == transform)
                    {
                        if (mask is SoftMask softmask && softmask.selfMask == true)
                        {
                            return mask;
                        }
                    }
                    else
                    {
                        return mask;
                    }
                }

                if (IsOverridingSortingCanvas(current))
                {
                    processedByThisMask = false;
                }
                current = current.parent;
            }
        }

        static ISoftMask GetISoftMask(Transform current, bool shouldBeEnabled = true)
        {
            var mask = current.GetComponent<ISoftMask>();
            if (mask != null && mask.isAlive && (!shouldBeEnabled || mask.isMaskingEnabled))
                return mask;
            return null;
        }

        static bool IsOverridingSortingCanvas(Transform transform)
        {
            var canvas = transform.GetComponent<Canvas>();
            if (canvas && canvas.overrideSorting)
                return true;
            return false;
        }

        void SetShaderNotSupported(Material material)
        {
            if (!shaderIsNotSupported)
            {
                Debug.LogWarningFormat(
                    gameObject,
                    "SoftMask will not work on {0} because material {1} doesn't support masking. " +
                    "Add masking support to your material or set Graphic's material to None to use " +
                    "a default one.",
                    graphic,
                    material);
                shaderIsNotSupported = true;
            }
        }
    }
}