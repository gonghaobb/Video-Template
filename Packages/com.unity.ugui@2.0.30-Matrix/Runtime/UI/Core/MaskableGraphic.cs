using System;
using UnityEngine.Events;
using UnityEngine.Rendering;
//PicoVideo;Linear;Ernst;Begin
using System.Collections.Generic;
using UnityEngine.Serialization;
//PicoVideo;Linear;Ernst;End
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif

namespace UnityEngine.UI
{
    /// <summary>
    /// A Graphic that is capable of being masked out.
    /// </summary>
    public abstract class MaskableGraphic : Graphic, IClippable, IMaskable, IMaterialModifier
    {
        //PicoVideo;Linear;Ernst;Begin
        public const string GAMMASPACE_UI_LAYER = "Gammaspace";
        public const string OVERLAY_UI_LAYER = "OverlayUI";
        private static HashSet<Texture> m_GammaTextureHash = new HashSet<Texture>();

        private static HashSet<string> m_DefaultUguiSpriteNameHash = new HashSet<string>();

        public static void AddGammaTexture(Texture tex)
        {
            if (!m_GammaTextureHash.Contains(tex))
            {
                m_GammaTextureHash.Add(tex);
            }
        }
        public static void RemoveGammaTexture(Texture tex)
        {
            if (m_GammaTextureHash.Contains(tex))
            {
                m_GammaTextureHash.Remove(tex);
            }
        }
        //PicoVideo;Linear;Ernst;End
        
        
        //PicoVideo;DirectOES;BaoQinShun;Begin
        private static readonly int ExternalOesTex = Shader.PropertyToID("_ExternalOESTex");
        private static Dictionary<Texture, Material> m_OESTextureHash = new Dictionary<Texture, Material>();

        public static void AddOESTexture(Texture tex)
        {
            if (!m_OESTextureHash.ContainsKey(tex))
            {
                m_OESTextureHash.Add(tex, null);
            }
        }

        public static void RemoveOESTexture(Texture tex)
        {
            if (m_OESTextureHash.ContainsKey(tex))
            {
                m_OESTextureHash.Remove(tex);
            }
        }
        //PicoVideo;DirectOES;BaoQinShun;End
        
        [NonSerialized]
        protected bool m_ShouldRecalculateStencil = true;

        [NonSerialized]
        protected Material m_MaskMaterial;

        [NonSerialized]
        private RectMask2D m_ParentMask;

        // m_Maskable is whether this graphic is allowed to be masked or not. It has the matching public property maskable.
        // The default for m_Maskable is true, so graphics under a mask are masked out of the box.
        // The maskable property can be turned off from script by the user if masking is not desired.
        // m_IncludeForMasking is whether we actually consider this graphic for masking or not - this is an implementation detail.
        // m_IncludeForMasking should only be true if m_Maskable is true AND a parent of the graphic has an IMask component.
        // Things would still work correctly if m_IncludeForMasking was always true when m_Maskable is, but performance would suffer.
        [SerializeField]
        private bool m_Maskable = true;

        private bool m_IsMaskingGraphic = false;

        [NonSerialized]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Not used anymore.", true)]
        protected bool m_IncludeForMasking = false;

        [Serializable]
        public class CullStateChangedEvent : UnityEvent<bool> {}

        // Event delegates triggered on click.
        [SerializeField]
        private CullStateChangedEvent m_OnCullStateChanged = new CullStateChangedEvent();

        /// <summary>
        /// Callback issued when culling changes.
        /// </summary>
        /// <remarks>
        /// Called whene the culling state of this MaskableGraphic either becomes culled or visible. You can use this to control other elements of your UI as culling happens.
        /// </remarks>
        public CullStateChangedEvent onCullStateChanged
        {
            get { return m_OnCullStateChanged; }
            set { m_OnCullStateChanged = value; }
        }

        /// <summary>
        /// Does this graphic allow masking.
        /// </summary>
        public bool maskable
        {
            get { return m_Maskable; }
            set
            {
                if (value == m_Maskable)
                    return;
                m_Maskable = value;
                m_ShouldRecalculateStencil = true;
                SetMaterialDirty();
            }
        }


        /// <summary>
        /// Is this graphic the graphic on the same object as a Mask that is enabled.
        /// </summary>
        /// <remarks>
        /// If toggled ensure to call MaskUtilities.NotifyStencilStateChanged(this); manually as it changes how stenciles are calculated for this image.
        /// </remarks>
        public bool isMaskingGraphic
        {
            get { return m_IsMaskingGraphic; }
            set
            {
                if (value == m_IsMaskingGraphic)
                    return;

                m_IsMaskingGraphic = value;
            }
        }

        [NonSerialized]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Not used anymore", true)]
        protected bool m_ShouldRecalculate = true;

        [NonSerialized]
        protected int m_StencilValue;

        /// <summary>
        /// See IMaterialModifier.GetModifiedMaterial
        /// </summary>
        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            var toUse = baseMaterial;

            toUse = GetAdjustmentUIMaterial(baseMaterial); //PicoVideo;UIMaterialManagement;WuJunLin

            if (m_ShouldRecalculateStencil)
            {
                if (maskable)
                {
                    var rootCanvas = MaskUtilities.FindRootSortOverrideCanvas(transform);
                    m_StencilValue = MaskUtilities.GetStencilDepth(transform, rootCanvas);
                }
                else
                    m_StencilValue = 0;

                m_ShouldRecalculateStencil = false;
            }

            // if we have a enabled Mask component then it will
            // generate the mask material. This is an optimization
            // it adds some coupling between components though :(
            if (m_StencilValue > 0 && !isMaskingGraphic)
            {
                var maskMat = StencilMaterial.Add(toUse, (1 << m_StencilValue) - 1, StencilOp.Keep, CompareFunction.Equal, ColorWriteMask.All, (1 << m_StencilValue) - 1, 0);
                StencilMaterial.Remove(m_MaskMaterial);
                m_MaskMaterial = maskMat;
                toUse = m_MaskMaterial;
            }
            return toUse;
        }

        /// <summary>
        /// See IClippable.Cull
        /// </summary>
        public virtual void Cull(Rect clipRect, bool validRect)
        {
            var cull = !validRect || !clipRect.Overlaps(rootCanvasRect, true);
            UpdateCull(cull);
        }

        private void UpdateCull(bool cull)
        {
            if (canvasRenderer.cull != cull)
            {
                canvasRenderer.cull = cull;
                UISystemProfilerApi.AddMarker("MaskableGraphic.cullingChanged", this);
                m_OnCullStateChanged.Invoke(cull);
                OnCullingChanged();
            }
        }

        /// <summary>
        /// See IClippable.SetClipRect
        /// </summary>
        public virtual void SetClipRect(Rect clipRect, bool validRect)
        {
            if (validRect)
                canvasRenderer.EnableRectClipping(clipRect);
            else
                canvasRenderer.DisableRectClipping();
        }

        public virtual void SetClipSoftness(Vector2 clipSoftness)
        {
            canvasRenderer.clippingSoftness = clipSoftness;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            //PicoVideo;UIMaterialManagement;WuJunLin;Begin
            UpdateCanvasAdditionalShaderChannels();
            //PicoVideo;UIMaterialManagement;WuJunLin;End
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();

            if (isMaskingGraphic)
            {
                MaskUtilities.NotifyStencilStateChanged(this);
            }

            //PicoVideo;SoftMask;XiaoPengCheng;Begin
#if UNITY_EDITOR
            if (m_EnableAlphaMask && (this is Image || this is RawImage))
            {
                AddAlphaMaskMaterialModifier();
            }
#endif
            //PicoVideo;SoftMask;XiaoPengCheng;End
        }
        
        //PicoVideo;Linear;Ernst;Begin
        //PicoVideo;UIMaterialManagement;WuJunLin;Begin
        private Material GetAdjustmentUIMaterial(Material baseMaterial)
        {
            if (this is Image || this is RawImage || this is Text)
            {
                Material adjustmentMaterial = null;
                bool isRenderInLinear = false;
                bool isTextureSRGB = true;
                Texture tex = null;
                if (this is Image)
                {
                    tex = (this as Image).mainTexture;
                }
                else if (this is RawImage)
                {
                    tex = (this as RawImage).mainTexture;
                }
                if (gameObject.layer == LayerMask.NameToLayer(GAMMASPACE_UI_LAYER)
                    || (
#if UNITY_EDITOR
                        !(StageUtility.GetCurrentStage() is PrefabStage) && 
#endif
                        canvas != null && canvas.gameObject.layer == LayerMask.NameToLayer(OVERLAY_UI_LAYER)))
                {
                    if (m_GammaTextureHash.Contains(tex))
                    {
                        adjustmentMaterial = UIMaterials.gammaMaterial;
                        isTextureSRGB = false;
                    }
                    else
                    {
                        adjustmentMaterial = UIMaterials.gammaSRGBMaterial;
                    }
                }
                else
                {
                    adjustmentMaterial = UIMaterials.linearSRGBMaterial;
                    isRenderInLinear = true;
                }
                    
                if (baseMaterial != defaultMaterial)
                {
                    if (UIMaterials.IsDefaultShader(baseMaterial.shader)) //兼容本地使用默认UIShader新建材质的特殊用法
                    {
                        adjustmentMaterial = UIMaterials.GetModifyMaterial(baseMaterial, isRenderInLinear, isTextureSRGB);
                    }
                    else
                    {
                        return baseMaterial;
                    }
                }

#if PLATFORM_ANDROID
                if (tex != null && m_OESTextureHash.TryGetValue(tex, out var m))
                {
                    if (m == null || (m != null && m.IsKeywordEnabled("RENDER_IN_LINEAR") != adjustmentMaterial.IsKeywordEnabled("RENDER_IN_LINEAR")))
                    {
                        m = new Material(adjustmentMaterial)
                        {
                            mainTexture = tex
                        };
                        m.name += " - OES";
                        m.EnableKeyword("_EXTERNAL_TEXTURE");
                        m.DisableKeyword("UNITY_UI_SRGB");
                        m.mainTexture = tex;
                        m.SetTexture(ExternalOesTex, tex);
                        m_OESTextureHash[tex] = m;
                    }
                    adjustmentMaterial = m;
                }
#endif
                return adjustmentMaterial;
            }

            return baseMaterial;
        }
        //PicoVideo;UIMaterialManagement;WuJunLin;End
        //PicoVideo;Linear;Ernst;End

        //PicoVideo;DirectOES;BaoQinShun;Begin
        public override void GraphicUpdateComplete()
        {
            base.GraphicUpdateComplete();
            if (m_CacheRenderMaterial != null)
            {
                if (m_CacheRenderMaterial.IsKeywordEnabled("_EXTERNAL_TEXTURE"))
                {
                    m_CacheRenderMaterial.SetTexture(ExternalOesTex, mainTexture);
                }    
            }
            
        }
        //PicoVideo;DirectOES;BaoQinShun;End
        
        protected override void OnDisable()
        {
            base.OnDisable();
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
            UpdateClipParent();
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;
            UIMaterials.RemoveModifyMaterial(m_Material); //PicoVideo;UIMaterialManagement;WuJunLin

            if (isMaskingGraphic)
            {
                MaskUtilities.NotifyStencilStateChanged(this);
            }
        }
        
#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

#endif

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();

            if (!isActiveAndEnabled)
                return;
            //PicoVideo;UIMaterialManagement;WuJunLin;Begin
            UpdateCanvasAdditionalShaderChannels();
            //PicoVideo;UIMaterialManagement;WuJunLin;End
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        [Obsolete("Not used anymore.", true)]
        public virtual void ParentMaskStateChanged() {}

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();

            if (!isActiveAndEnabled)
                return;
            //PicoVideo;UIMaterialManagement;WuJunLin;Begin
            UpdateCanvasAdditionalShaderChannels();
			//PicoVideo;UIMaterialManagement;WuJunLin;End
            m_ShouldRecalculateStencil = true;
            UpdateClipParent();
            SetMaterialDirty();
        }
        
        //PicoVideo;UIMaterialManagement;WuJunLin;Begin
        private void UpdateCanvasAdditionalShaderChannels()
        {
            if (canvas != null)
            {
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
#if UNIVERSAL_UI_EXTENSION
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
#endif
                if (canvas.rootCanvas != null)
                {
                    canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
#if UNIVERSAL_UI_EXTENSION
                    canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
#endif
                }
            }
        }
        //PicoVideo;UIMaterialManagement;WuJunLin;End

        readonly Vector3[] m_Corners = new Vector3[4];
        private Rect rootCanvasRect
        {
            get
            {
                rectTransform.GetWorldCorners(m_Corners);

                if (canvas)
                {
                    Matrix4x4 mat = canvas.rootCanvas.transform.worldToLocalMatrix;
                    for (int i = 0; i < 4; ++i)
                        m_Corners[i] = mat.MultiplyPoint(m_Corners[i]);
                }

                // bounding box is now based on the min and max of all corners (case 1013182)

                Vector2 min = m_Corners[0];
                Vector2 max = m_Corners[0];
                for (int i = 1; i < 4; i++)
                {
                    min.x = Mathf.Min(m_Corners[i].x, min.x);
                    min.y = Mathf.Min(m_Corners[i].y, min.y);
                    max.x = Mathf.Max(m_Corners[i].x, max.x);
                    max.y = Mathf.Max(m_Corners[i].y, max.y);
                }

                return new Rect(min, max - min);
            }
        }

        private void UpdateClipParent()
        {
            var newParent = (maskable && IsActive()) ? MaskUtilities.GetRectMaskForClippable(this) : null;

            // if the new parent is different OR is now inactive
            if (m_ParentMask != null && (newParent != m_ParentMask || !newParent.IsActive()))
            {
                m_ParentMask.RemoveClippable(this);
                UpdateCull(false);
            }

            // don't re-add it if the newparent is inactive
            if (newParent != null && newParent.IsActive())
                newParent.AddClippable(this);

            m_ParentMask = newParent;
        }

        /// <summary>
        /// See IClippable.RecalculateClipping
        /// </summary>
        public virtual void RecalculateClipping()
        {
            UpdateClipParent();
        }

        /// <summary>
        /// See IMaskable.RecalculateMasking
        /// </summary>
        public virtual void RecalculateMasking()
        {
            // Remove the material reference as either the graphic of the mask has been enable/ disabled.
            // This will cause the material to be repopulated from the original if need be. (case 994413)
            StencilMaterial.Remove(m_MaskMaterial);
            m_MaskMaterial = null;
            m_ShouldRecalculateStencil = true;
            SetMaterialDirty();
        }

        //PicoVideo;SoftMask;XiaoPengCheng;Begin
#if UNITY_EDITOR
        [FormerlySerializedAs("m_EnableSoftMask")]
        [SerializeField]
        private bool m_EnableAlphaMask = false;
#endif
        
        [FormerlySerializedAs("m_SoftMaskTexture")]
        [SerializeField]
        private Texture2D m_AlphaMaskTexture = null;

        private void AddAlphaMaskMaterialModifier()
        {
            if (!TryGetComponent(out AlphaMaskMaterialModifier alphaMaskMaterialModifier))
            {
                alphaMaskMaterialModifier = gameObject.AddComponent<AlphaMaskMaterialModifier>();
            }

            alphaMaskMaterialModifier.hideFlags = HideFlags.HideInInspector;
            alphaMaskMaterialModifier.alphaMaskTexture = m_AlphaMaskTexture;
        }

#if UNITY_EDITOR
        private bool m_LastEnableAlphaMask = false;
        private Texture2D m_LastAlphaMaskTexture = null;

        private void Update()
        {
            //PicoVideo;UIMaterialManagement;WuJunLin;Begin
            UpdateCanvasAdditionalShaderChannels();
            //PicoVideo;UIMaterialManagement;WuJunLin;End
            
            if (this is Image || this is RawImage)
            {
                if (m_EnableAlphaMask != m_LastEnableAlphaMask || m_LastAlphaMaskTexture != m_AlphaMaskTexture)
                {
                    if (TryGetComponent(out AlphaMaskMaterialModifier alphaMaskMaterialModifier))
                    {
                        
                    }
                    else
                    {
                        if (m_EnableAlphaMask && m_AlphaMaskTexture != null)
                        {
                            alphaMaskMaterialModifier = gameObject.AddComponent<AlphaMaskMaterialModifier>();
                            alphaMaskMaterialModifier.hideFlags = HideFlags.HideInInspector;
                        }
                    }

                    if (alphaMaskMaterialModifier != null)
                    {
                        alphaMaskMaterialModifier.alphaMaskTexture = m_EnableAlphaMask ? m_AlphaMaskTexture : null;
                    }

                    SetMaterialDirty();
                    m_LastEnableAlphaMask = m_EnableAlphaMask;
                    m_LastAlphaMaskTexture = m_AlphaMaskTexture;
                }
            }
        }
#endif
        //PicoVideo;SoftMask;XiaoPengCheng;End
    }
}
