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
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    /// <summary>
    /// Contains some predefined combinations of mask channel weights.
    /// </summary>
    public static class MaskChannel
    {
        public static Color alpha = new Color(0, 0, 0, 1);
        public static Color red = new Color(1, 0, 0, 0);
        public static Color green = new Color(0, 1, 0, 0);
        public static Color blue = new Color(0, 0, 1, 0);
        public static Color gray = new Color(1, 1, 1, 0) / 3.0f;
    }

    /// <summary>
    /// SoftMask is a component that can be added to UI elements for masking the children. It works
    /// like a standard Unity's <see cref="Mask"/> but supports alpha.
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("UI/Soft Mask", 14)]
    [RequireComponent(typeof(RectTransform))]
    [HelpURL("https://docs.google.com/document/d/1w8ENeeE_wi_DSpCyU34voUJIOk9o3J8gDDAyOja_9yE")]
    public class SoftMask : UIBehaviour, ICanvasRaycastFilter, ISoftMask
    {
        //
        // How it works:
        //
        // SoftMask overrides Shader used by child elements. To do it, SoftMask spawns invisible 
        // SoftMaskable components on them on the fly. SoftMaskable implements IMaterialOverride,
        // which allows it to override the shader that performs actual rendering. Use of
        // IMaterialOverride is transparent to the user: a material assigned to Graphic in the 
        // inspector is left untouched.
        //
        // Management of SoftMaskables is fully automated. SoftMaskables are kept on the child
        // objects while any SoftMask parent present. When something changes and SoftMask parent
        // no longer exists, SoftMaskable is destroyed automatically. So, a user of SoftMask
        // doesn't have to worry about any component changes in the hierarchy.
        //
        // The replacement shader samples the mask texture and multiply the resulted color 
        // accordingly. SoftMask has the predefined replacement for Unity's default UI shader 
        // (and its ETC1-version in Unity 5.4+). So, when SoftMask 'sees' a material that uses a
        // known shader, it overrides shader by the predefined one. If SoftMask encounters a
        // material with an unknown shader, it can't do anything reasonable (because it doesn't know
        // what that shader should do). In such a case, SoftMask will not work and a warning will
        // be displayed in Console. If you want SoftMask to work with a custom shader, you can
        // manually add support to this shader. For reference how to do it, see
        // CustomWithSoftMask.shader from included samples.
        //
        // All replacements are cached in SoftMask instances. By default Unity draws UI with a
        // very small amount of material instances (they are spawned one per masking/clipping layer),
        // so, SoftMask creates a relatively small amount of overrides.
        //

        [SerializeField]
        private Shader m_DefaultShader = null;

        [SerializeField]
        private MaskSource m_Source = MaskSource.Graphic;

        [SerializeField]
        private RectTransform m_SeparateMask = null;

        [SerializeField]
        private Sprite m_Sprite = null;

        [SerializeField]
        private BorderMode m_SpriteBorderMode = BorderMode.Simple;

        [SerializeField]
        private Texture2D m_Texture = null;

        [SerializeField]
        private Rect m_TextureUVRect = DefaultUVRect;

        [SerializeField]
        private Color m_ChannelWeights = MaskChannel.alpha;

        [SerializeField]
        private float m_RaycastThreshold = 0.0f;
        
        [SerializeField]
        private bool m_SelfMask = false;

        private SoftMaskMaterialReplacements m_Materials;
        private MaterialParameters m_Parameters;
        private Sprite m_LastUsedSprite;
        private bool m_MaskingWasEnabled;
        private bool m_Destroyed;
        private bool m_Dirty;

        // Cached components
        private RectTransform m_MaskTransform;
        private Graphic m_Graphic;
        private Canvas m_Canvas;

        public SoftMask()
        {
            m_Materials = new SoftMaskMaterialReplacements(Replace, m => m_Parameters.Apply(m));
        }

        /// <summary>
        /// Source of the mask's image.
        /// </summary>
        [Serializable]
        public enum MaskSource
        {
            /// <summary>
            /// The mask image should be taken from the Graphic component of the containing 
            /// GameObject. Only Image and RawImage components are supported. If there is no
            /// appropriate Graphic on the GameObject, a solid rectangle of the RectTransform
            /// dimensions will be used.
            /// </summary>
            Graphic,
            /// <summary>
            /// The mask image should be taken from an explicitly specified Sprite. When this mode
            /// is used, spriteBorderMode can also be set to determine how to process Sprite's
            /// borders. If the sprite isn't set, a solid rectangle of the RectTransform dimensions 
            /// will be used. This mode is analogous to using an Image with according sprite and 
            /// type set.
            /// </summary>
            Sprite,
            /// <summary>
            /// The mask image should be taken from an explicitly specified Texture2D. When this
            /// mode is used, textureUVRect can also be set to determine what part of the texture
            /// should be used. If the texture isn't set, a solid rectangle of the RectTransform
            /// dimensions will be used. This mode is analogous to using a RawImage with according 
            /// texture and uvRect set.
            /// </summary>
            Texture
        }

        /// <summary>
        /// How Sprite's borders should be processed. It is a reduced set of Image.Type values.
        /// </summary>
        [Serializable]
        public enum BorderMode
        {
            /// <summary>
            /// Sprite should be drawn as a whole, ignoring any borders set. It works the
            /// same way as Unity's Image.Type.Simple.
            /// </summary>
            Simple,
            /// <summary>
            /// Sprite borders should be stretched when the drawn image is larger that the
            /// source. It works the same way as Unity's Image.Type.Sliced.
            /// </summary>
            Sliced,
            /// <summary>
            /// The same as Sliced, but border fragments will be repeated instead of
            /// stretched. It works the same way as Unity's Image.Type.Tiled.
            /// </summary>
            Tiled
        }

        /// <summary>
        /// Errors encountered during SoftMask diagnostics. Mostly intended to use in Unity Editor.
        /// </summary>
        [Flags]
        [Serializable]
        public enum Errors
        {
            NoError = 0,
            UnsupportedShaders = 1 << 0,
            NestedMasks = 1 << 1,
            TightPackedSprite = 1 << 2,
            AlphaSplitSprite = 1 << 3,
            UnsupportedImageType = 1 << 4
        }

        /// <summary>
        /// Specifies a Shader that should be used as a replacement of the Unity's default UI
        /// shader. If you add SoftMask in play-time by AddComponent(), you should set 
        /// this property manually.
        /// </summary>
        public Shader defaultShader
        {
            get { return m_DefaultShader; }
            set { SetShader(ref m_DefaultShader, value); }
        }

        /// <summary>
        /// Determines from where the mask image should be taken.
        /// </summary>
        public MaskSource source
        {
            get { return m_Source; }
            set { if (m_Source != value) Set(ref m_Source, value); }
        }

        /// <summary>
        /// Specifies a RectTransform that should be used as a mask. It allows to separate 
        /// a mask from a masking hierarchy root, which simplifies creation of moving or 
        /// sliding masks. When null, the RectTransform of the current object will be used.
        /// Default value is null.
        /// </summary>
        public RectTransform separateMask
        {
            get { return m_SeparateMask; }
            set
            {
                if (m_SeparateMask != value)
                {
                    Set(ref m_SeparateMask, value);
                    // We should search them again
                    m_Graphic = null;
                    m_MaskTransform = null;
                }
            }
        }

        /// <summary>
        /// Specifies a Sprite that should be used as the mask image. This property takes
        /// effect only when the source is MaskSource.Sprite.
        /// </summary>
        public Sprite sprite
        {
            get { return m_Sprite; }
            set { if (m_Sprite != value) Set(ref m_Sprite, value); }
        }

        /// <summary>
        /// Specifies the draw mode of sprite borders. This property takes effect only when the
        /// source is MaskSource.Sprite.
        /// </summary>
        public BorderMode spriteBorderMode
        {
            get { return m_SpriteBorderMode; }
            set { if (m_SpriteBorderMode != value) Set(ref m_SpriteBorderMode, value); }
        }

        /// <summary>
        /// Specifies a Texture2D that should be used as the mask image. This property takes
        /// effect only when the source is MaskSource.Texture.
        /// </summary>
        public Texture2D texture
        {
            get { return m_Texture; }
            set { if (m_Texture != value) Set(ref m_Texture, value); }
        }

        /// <summary>
        /// Specifies an UV rectangle defining the image part, that should be used as 
        /// the mask image. This property takes effect only when the source is MaskSource.Texture.
        /// A value is set in normalized coordinates. The default value is (0, 0, 1, 1), which means
        /// that the whole texture is used.
        /// </summary>
        public Rect textureUVRect
        {
            get { return m_TextureUVRect; }
            set { if (m_TextureUVRect != value) Set(ref m_TextureUVRect, value); }
        }

        /// <summary>
        /// Specifies weights of the color channels of the mask. The color sampled from the mask 
        /// texture is multiplied by this value, after what all components are summed up together.
        /// That is, the final mask value is calculated as:
        ///     color = `pixel-from-mask` * channelWeights
        ///     value = color.r + color.g + color.b + color.a
        /// The `value` is a number by which the resulting pixel's alpha is multiplied. As you
        /// can see, the result value isn't normalized, so, you should account it while defining
        /// custom values for this property.
        /// Static class MaskChannel contains some useful predefined values. You can use they
        /// as example of how mask calculation works.
        /// The default value is MaskChannel.alpha.
        /// </summary>
        public Color channelWeights
        {
            get { return m_ChannelWeights; }
            set { if (m_ChannelWeights != value) Set(ref m_ChannelWeights, value); }
        }

        /// <summary>
        /// Specifies the minimum mask value that the point should have for an input event to pass.
        /// If the value sampled from the mask is greater or equal this value, the input event
        /// is considered 'hit'. The mask value is compared with raycastThreshold after
        /// channelWeights applied.
        /// The default value is 0, which means that any pixel belonging to RectTransform is
        /// considered in input events. If you specify the value greater than 0, the mask's 
        /// texture should be readable.
        /// Accepts values in range [0..1].
        /// </summary>
        public float raycastThreshold
        {
            get { return m_RaycastThreshold; }
            set { m_RaycastThreshold = value; }
        }
        
        public bool selfMask
        {
            get { return m_SelfMask; }
            set
            {
                m_SelfMask = value;
                SpawnMaskablesInChildren(transform);
                NotifyChildrenThatMaskMightChanged();
            }
        }

        /// <summary>
        /// Returns true if masking is currently active.
        /// </summary>
        public bool isMaskingEnabled
        {
            get { return isActiveAndEnabled && canvas; }
        }

        /// <summary>
        /// Checks for errors and returns them as flags. It is used in the editor to determine
        /// which warnings should be displayed.
        /// </summary>
        public Errors PollErrors() { return new Diagnostics(this).PollErrors(); }

        // ICanvasRaycastFilter
        public bool IsRaycastLocationValid(Vector2 sp, Camera cam)
        {
            Vector2 localPos;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(maskTransform, sp, cam, out localPos)) return false;
            if (!Mathr.Inside(localPos, LocalMaskRect(Vector4.zero))) return false;
            if (!m_Parameters.texture) return true;
            if (m_RaycastThreshold <= 0.0f) return true;
            float mask;
            if (!m_Parameters.SampleMask(localPos, out mask))
            {
                Debug.LogError("raycastThreshold greater than 0 can't be used on SoftMask whose texture cannot be read.", this);
                return true;
            }
            return mask >= m_RaycastThreshold;
        }

        protected override void Start()
        {
            base.Start();
            if (m_DefaultShader == null || UIMaterials.IsDefaultShader(m_DefaultShader))
            {
                m_DefaultShader = UIMaterials.GetDefaultShader();
            }

            WarnIfDefaultShaderIsNotSet();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SpawnMaskablesInChildren(transform);
            FindGraphic();
            if (isMaskingEnabled)
            {
                UpdateMask();
            }
            NotifyChildrenThatMaskMightChanged();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_Graphic)
            {
                m_Graphic.UnregisterDirtyVerticesCallback(OnGraphicDirty);
                m_Graphic.UnregisterDirtyMaterialCallback(OnGraphicDirty);
                m_Graphic = null;
            }
            NotifyChildrenThatMaskMightChanged();
            DestroyMaterials();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_Destroyed = true;
            NotifyChildrenThatMaskMightChanged();
        }

        protected virtual void LateUpdate()
        {
            var maskingEnabled = isMaskingEnabled;
            if (maskingEnabled)
            {
                if (m_MaskingWasEnabled != maskingEnabled)
                    SpawnMaskablesInChildren(transform);
                var prevGraphic = m_Graphic;
                FindGraphic();
                if (maskTransform.hasChanged || m_Dirty || !ReferenceEquals(m_Graphic, prevGraphic))
                    UpdateMask();
            }
            m_MaskingWasEnabled = maskingEnabled;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            m_Dirty = true;
        }

        protected override void OnDidApplyAnimationProperties()
        {
            base.OnDidApplyAnimationProperties();
            m_Dirty = true;
        }

#if UNITY_EDITOR
        private bool m_LastSelfMask = false;
        protected override void OnValidate()
        {
            base.OnValidate();
            m_Dirty = true;
            m_MaskTransform = null;
            m_Graphic = null;
            if (m_LastSelfMask != m_SelfMask)
            {
                UnityEditor.EditorApplication.delayCall+=()=>
                {
                    SpawnMaskablesInChildren(transform);
                    NotifyChildrenThatMaskMightChanged();
                };
                m_LastSelfMask = m_SelfMask;
            }
            
        }
#endif

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            m_Canvas = null;
            m_Dirty = true;
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            m_Canvas = null;
            m_Dirty = true;
            NotifyChildrenThatMaskMightChanged();
        }

        void OnTransformChildrenChanged()
        {
            SpawnMaskablesInChildren(transform);
        }

        static readonly Rect DefaultUVRect = new Rect(0, 0, 1, 1);

        RectTransform maskTransform
        {
            get
            {
                return
                    m_MaskTransform
                        ? m_MaskTransform
                        : (m_MaskTransform = m_SeparateMask ? m_SeparateMask : GetComponent<RectTransform>());
            }
        }

        Canvas canvas
        {
            get { return m_Canvas ? m_Canvas : (m_Canvas = NearestEnabledCanvas()); }
        }

        bool isBasedOnGraphic { get { return m_Source == MaskSource.Graphic; } }

        bool ISoftMask.isAlive { get { return this && !m_Destroyed; } }

        Material ISoftMask.GetReplacement(Material original)
        {
            Assert.IsTrue(isActiveAndEnabled);
            return m_Materials.Get(original);
        }

        void ISoftMask.ReleaseReplacement(Material replacement)
        {
            m_Materials.Release(replacement);
        }

        void ISoftMask.ApplyParameters(Material renderMaterial)
        {
            m_Parameters.Apply(renderMaterial);
        }

        void ISoftMask.UpdateTransformChildren(Transform transform)
        {
            SpawnMaskablesInChildren(transform);
        }

        void OnGraphicDirty()
        {
            if (isBasedOnGraphic)
                m_Dirty = true;
        }

        void FindGraphic()
        {
            if (!m_Graphic)
            {
                m_Graphic = maskTransform.GetComponent<Graphic>();
                if (m_Graphic)
                {
                    m_Graphic.RegisterDirtyVerticesCallback(OnGraphicDirty);
                    m_Graphic.RegisterDirtyMaterialCallback(OnGraphicDirty);
                }
            }
        }

        Canvas NearestEnabledCanvas()
        {
            // It's a rare operation, so I do not optimize it with static lists
            var canvases = GetComponentsInParent<Canvas>(false);
            for (int i = 0; i < canvases.Length; ++i)
                if (canvases[i].isActiveAndEnabled)
                    return canvases[i];
            return null;
        }

        void UpdateMask()
        {
            Assert.IsTrue(isMaskingEnabled);
            CalculateMaskParameters();
            m_Materials.ApplyAll();
            ForEachChildMaskable(x => x.ApplyMaterialParameters());
            maskTransform.hasChanged = false;
            m_Dirty = false;
        }

        void SpawnMaskablesInChildren(Transform root)
        {
            if (m_SelfMask)
            {
                if (!GetComponent<SoftMaskable>())
                    gameObject.AddComponent<SoftMaskable>();
            }

            for (int i = 0; i < root.childCount; ++i)
            {
                var child = root.GetChild(i);
                if (!child.GetComponent<SoftMaskable>())
                    child.gameObject.AddComponent<SoftMaskable>();
            }
        }

        void InvalidateChildren()
        {
            ForEachChildMaskable(x => x.Invalidate());
        }

        void NotifyChildrenThatMaskMightChanged()
        {
            ForEachChildMaskable(x => x.MaskMightChanged());
        }

        void ForEachChildMaskable(Action<SoftMaskable> f)
        {
            transform.GetComponentsInChildren(s_Maskables);
            for (int i = 0; i < s_Maskables.Count; ++i)
            {
                var maskable = s_Maskables[i];
                if (maskable)
                {
                    f(maskable);
                }
            }
        }

        void DestroyMaterials()
        {
            m_Materials.DestroyAllAndClear();
        }

        Material Replace(Material original)
        {
            Material mat = null;

            if (original == null || UIMaterials.IsDefaultShader(original.shader) || original.SupportsSoftMask())
            {
                //默认UIShader已经确保支持
                mat = new Material(original);
                mat.name += " - softmask";
            }
            else if (original.shader != null && original.shader.name.StartsWith("TextMeshPro/"))
            {
                Shader replacement = Shader.Find("Soft Mask/" + original.shader.name);
                if (replacement != null)
                {
                    mat = new Material(original)
                    {
                        shader = replacement
                    };
                    mat.name += " - softmask";
                }
            }

            return mat;
        }

        void CalculateMaskParameters()
        {
            switch (m_Source)
            {
                case MaskSource.Graphic:
                    if (m_Graphic is Image)
                        CalculateImageBased((Image)m_Graphic);
                    else if (m_Graphic is RawImage)
                        CalculateRawImageBased((RawImage)m_Graphic);
                    else
                        CalculateSolidFill();
                    break;
                case MaskSource.Sprite:
                    CalculateSpriteBased(m_Sprite, m_SpriteBorderMode);
                    break;
                case MaskSource.Texture:
                    CalculateTextureBased(m_Texture, m_TextureUVRect);
                    break;
                default:
                    Debug.LogErrorFormat("Unknown MaskSource: {0}", m_Source);
                    CalculateSolidFill();
                    break;
            }
        }

        BorderMode ToBorderMode(Image.Type imageType)
        {
            switch (imageType)
            {
                case Image.Type.Simple: return BorderMode.Simple;
                case Image.Type.Sliced: return BorderMode.Sliced;
                case Image.Type.Tiled: return BorderMode.Tiled;
                default:
                    Debug.LogErrorFormat(
                        this,
                        "SoftMask doesn't support image type {0}. Image type Simple will be used.",
                        imageType);
                    return BorderMode.Simple;
            }
        }

        void CalculateImageBased(Image image)
        {
            Assert.IsNotNull(image);
            CalculateSpriteBased(image.sprite, ToBorderMode(image.type));
        }

        void CalculateRawImageBased(RawImage image)
        {
            Assert.IsNotNull(image);
            CalculateTextureBased(image.texture, image.uvRect);
        }

        void CalculateSpriteBased(Sprite sprite, BorderMode borderMode)
        {
            var lastSprite = m_LastUsedSprite;
            m_LastUsedSprite = sprite;
            var spriteErrors = Diagnostics.CheckSprite(sprite);
            if (spriteErrors != Errors.NoError)
            {
                if (lastSprite != sprite)
                    WarnSpriteErrors(spriteErrors);
                CalculateSolidFill();
                return;
            }
            if (!sprite)
            {
                CalculateSolidFill();
                return;
            }
            FillCommonParameters();
            var spriteRect = Mathr.Move(Mathr.ToVector(sprite.rect), sprite.textureRect.position - sprite.rect.position - sprite.textureRectOffset);
            var textureRect = Mathr.ToVector(sprite.textureRect);
            var textureBorder = Mathr.BorderOf(spriteRect, textureRect);
            var textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
            var fullMaskRect = LocalMaskRect(Vector4.zero);
            m_Parameters.maskRectUV = Mathr.Div(textureRect, textureSize);
            if (borderMode == BorderMode.Simple)
            {
                var textureRectInFullRect = Mathr.Div(textureBorder, Mathr.Size(spriteRect));
                m_Parameters.maskRect = Mathr.ApplyBorder(fullMaskRect, Mathr.Mul(textureRectInFullRect, Mathr.Size(fullMaskRect)));
            }
            else
            {
                m_Parameters.maskRect = Mathr.ApplyBorder(fullMaskRect, textureBorder * GraphicToCanvasScale(sprite));
                var fullMaskRectUV = Mathr.Div(spriteRect, textureSize);
                var adjustedBorder = AdjustBorders(sprite.border * GraphicToCanvasScale(sprite), fullMaskRect);
                m_Parameters.maskBorder = LocalMaskRect(adjustedBorder);
                m_Parameters.maskBorderUV = Mathr.ApplyBorder(fullMaskRectUV, Mathr.Div(sprite.border, textureSize));
            }
            m_Parameters.texture = sprite.texture;
            m_Parameters.borderMode = borderMode;
            if (borderMode == BorderMode.Tiled)
                m_Parameters.tileRepeat = MaskRepeat(sprite, m_Parameters.maskBorder);
        }

        static Vector4 AdjustBorders(Vector4 border, Vector4 rect)
        {
            // Copied from Unity's Image.
            var size = Mathr.Size(rect);
            for (int axis = 0; axis <= 1; axis++)
            {
                // If the rect is smaller than the combined borders, then there's not room for
                // the borders at their normal size. In order to avoid artefacts with overlapping
                // borders, we scale the borders down to fit.
                float combinedBorders = border[axis] + border[axis + 2];
                if (size[axis] < combinedBorders && combinedBorders != 0)
                {
                    float borderScaleRatio = size[axis] / combinedBorders;
                    border[axis] *= borderScaleRatio;
                    border[axis + 2] *= borderScaleRatio;
                }
            }
            return border;
        }

        void CalculateTextureBased(Texture texture, Rect uvRect)
        {
            FillCommonParameters();
            m_Parameters.maskRect = LocalMaskRect(Vector4.zero);
            m_Parameters.maskRectUV = Mathr.ToVector(uvRect);
            m_Parameters.texture = texture;
            m_Parameters.borderMode = BorderMode.Simple;
        }

        void CalculateSolidFill()
        {
            CalculateTextureBased(null, DefaultUVRect);
        }

        void FillCommonParameters()
        {
            m_Parameters.worldToMask = WorldToMask();
            m_Parameters.maskChannelWeights = m_ChannelWeights;
        }

        float GraphicToCanvasScale(Sprite sprite)
        {
            var canvasPPU = canvas ? canvas.referencePixelsPerUnit : 100;
            var maskPPU = sprite ? sprite.pixelsPerUnit : 100;
            return canvasPPU / maskPPU;
        }

        Matrix4x4 WorldToMask()
        {
            return maskTransform.worldToLocalMatrix * canvas.rootCanvas.transform.localToWorldMatrix;
        }

        Vector4 LocalMaskRect(Vector4 border)
        {
            return Mathr.ApplyBorder(Mathr.ToVector(maskTransform.rect), border);
        }

        Vector2 MaskRepeat(Sprite sprite, Vector4 centralPart)
        {
            var textureCenter = Mathr.ApplyBorder(Mathr.ToVector(sprite.textureRect), sprite.border);
            return Mathr.Div(Mathr.Size(centralPart) * GraphicToCanvasScale(sprite), Mathr.Size(textureCenter));
        }

        void WarnIfDefaultShaderIsNotSet()
        {
            if (!m_DefaultShader)
                Debug.LogWarning("SoftMask may not work because its defaultShader is not set", this);
        }

        void WarnSpriteErrors(Errors errors)
        {
            if ((errors & Errors.TightPackedSprite) != 0)
                Debug.LogError("SoftMask doesn't support tight packed sprites", this);
            if ((errors & Errors.AlphaSplitSprite) != 0)
                Debug.LogError("SoftMask doesn't support sprites with an alpha split texture", this);
        }

        void Set<T>(ref T field, T value)
        {
            field = value;
            m_Dirty = true;
        }

        void SetShader(ref Shader field, Shader value, bool warnIfNotSet = true)
        {
            if (field != value)
            {
                field = value;
                if (warnIfNotSet)
                    WarnIfDefaultShaderIsNotSet();
                DestroyMaterials();
                InvalidateChildren();
            }
        }

        static readonly List<SoftMask> s_Masks = new List<SoftMask>();
        static readonly List<SoftMaskable> s_Maskables = new List<SoftMaskable>();

        // Various operations on a Rect represented as Vector4. 
        // In Vector4 Rect is stored as (xMin, yMin, xMax, yMax).
        static class Mathr
        {
            public static Vector4 ToVector(Rect r) { return new Vector4(r.xMin, r.yMin, r.xMax, r.yMax); }
            public static Vector4 Div(Vector4 v, Vector2 s) { return new Vector4(v.x / s.x, v.y / s.y, v.z / s.x, v.w / s.y); }
            public static Vector2 Div(Vector2 v, Vector2 s) { return new Vector2(v.x / s.x, v.y / s.y); }
            public static Vector4 Mul(Vector4 v, Vector2 s) { return new Vector4(v.x * s.x, v.y * s.y, v.z * s.x, v.w * s.y); }
            public static Vector2 Size(Vector4 r) { return new Vector2(r.z - r.x, r.w - r.y); }
            public static Vector4 Move(Vector4 v, Vector2 o) { return new Vector4(v.x + o.x, v.y + o.y, v.z + o.x, v.w + o.y); }

            public static Vector4 BorderOf(Vector4 outer, Vector4 inner)
            {
                return new Vector4(inner.x - outer.x, inner.y - outer.y, outer.z - inner.z, outer.w - inner.w);
            }

            public static Vector4 ApplyBorder(Vector4 v, Vector4 b)
            {
                return new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w);
            }

            public static Vector2 Min(Vector4 r) { return new Vector2(r.x, r.y); }
            public static Vector2 Max(Vector4 r) { return new Vector2(r.z, r.w); }

            public static Vector2 Remap(Vector2 c, Vector4 r1, Vector4 r2)
            {
                var r1size = Max(r1) - Min(r1);
                var r2size = Max(r2) - Min(r2);
                return Vector2.Scale(Div((c - Min(r1)), r1size), r2size) + Min(r2);
            }

            public static bool Inside(Vector2 v, Vector4 r)
            {
                return v.x >= r.x && v.y >= r.y && v.x <= r.z && v.y <= r.w;
            }
        }

        struct MaterialParameters
        {
            public Vector4 maskRect;
            public Vector4 maskBorder;
            public Vector4 maskRectUV;
            public Vector4 maskBorderUV;
            public Vector2 tileRepeat;
            public Color maskChannelWeights;
            public Matrix4x4 worldToMask;
            public Texture texture;
            public BorderMode borderMode;

            public Texture activeTexture { get { return texture ? texture : Texture2D.whiteTexture; } }

            public bool SampleMask(Vector2 localPos, out float mask)
            {
                var uv = XY2UV(localPos);
                try
                {
                    if (texture is RenderTexture)
                    {
                        //Todo：修正使用RT时的动态遮罩交互，直接转Texture2D消耗不可接受，异步回传未验证
                        mask = 1;
                    }
                    else
                    {
                        mask = MaskValue((texture as Texture2D).GetPixelBilinear(uv.x, uv.y));
                    }
                    return true;
                }
                catch (UnityException)
                {
                    mask = 0;
                    return false;
                }
            }

            public void Apply(Material mat)
            {
                if (mat != null)
                {
                    mat.SetTexture(Ids.SoftMask, activeTexture);
                    mat.SetVector(Ids.SoftMask_Rect, maskRect);
                    mat.SetVector(Ids.SoftMask_UVRect, maskRectUV);
                    mat.SetColor(Ids.SoftMask_ChannelWeights, maskChannelWeights);
                    mat.SetMatrix(Ids.SoftMask_WorldToMask, worldToMask);
                    mat.EnableKeyword("SOFTMASK_SIMPLE", borderMode == BorderMode.Simple);
                    mat.EnableKeyword("SOFTMASK_SLICED", borderMode == BorderMode.Sliced);
                    mat.EnableKeyword("SOFTMASK_TILED", borderMode == BorderMode.Tiled);
                    if (borderMode != BorderMode.Simple)
                    {
                        mat.SetVector(Ids.SoftMask_BorderRect, maskBorder);
                        mat.SetVector(Ids.SoftMask_UVBorderRect, maskBorderUV);
                        if (borderMode == BorderMode.Tiled)
                        {
                            mat.SetVector(Ids.SoftMask_TileRepeat, tileRepeat);
                        }
                    }
                }
            }

            // Next functions performs the same logic as functions from SoftMask.cginc. 
            // They implemented it a bit different way, because there is no such convenient
            // vector operations in Unity/C# and conditions are much cheaper here.

            Vector2 XY2UV(Vector2 localPos)
            {
                switch (borderMode)
                {
                    case BorderMode.Simple: return MapSimple(localPos);
                    case BorderMode.Sliced: return MapBorder(localPos, repeat: false);
                    case BorderMode.Tiled: return MapBorder(localPos, repeat: true);
                    default:
                        Debug.LogError("Unknown BorderMode");
                        return MapSimple(localPos);
                }
            }

            Vector2 MapSimple(Vector2 localPos)
            {
                return Mathr.Remap(localPos, maskRect, maskRectUV);
            }

            Vector2 MapBorder(Vector2 localPos, bool repeat)
            {
                return
                    new Vector2(
                        Inset(
                            localPos.x,
                            maskRect.x, maskBorder.x, maskBorder.z, maskRect.z,
                            maskRectUV.x, maskBorderUV.x, maskBorderUV.z, maskRectUV.z,
                            repeat ? tileRepeat.x : 1),
                        Inset(
                            localPos.y,
                            maskRect.y, maskBorder.y, maskBorder.w, maskRect.w,
                            maskRectUV.y, maskBorderUV.y, maskBorderUV.w, maskRectUV.w,
                            repeat ? tileRepeat.y : 1));
            }

            float Inset(float v, float x1, float x2, float u1, float u2, float repeat = 1)
            {
                var w = (x2 - x1);
                return Mathf.Lerp(u1, u2, w != 0.0f ? Frac((v - x1) / w * repeat) : 0.0f);
            }

            float Inset(float v, float x1, float x2, float x3, float x4, float u1, float u2, float u3, float u4, float repeat = 1)
            {
                if (v < x2)
                    return Inset(v, x1, x2, u1, u2);
                else if (v < x3)
                    return Inset(v, x2, x3, u2, u3, repeat);
                else
                    return Inset(v, x3, x4, u3, u4);
            }

            float Frac(float v) { return v - Mathf.Floor(v); }

            float MaskValue(Color mask)
            {
                var value = mask * maskChannelWeights;
                return value.a + value.r + value.g + value.b;
            }

            static class Ids
            {
                public static readonly int SoftMask = Shader.PropertyToID("_SoftMask");
                public static readonly int SoftMask_Rect = Shader.PropertyToID("_SoftMask_Rect");
                public static readonly int SoftMask_UVRect = Shader.PropertyToID("_SoftMask_UVRect");
                public static readonly int SoftMask_ChannelWeights = Shader.PropertyToID("_SoftMask_ChannelWeights");
                public static readonly int SoftMask_WorldToMask = Shader.PropertyToID("_SoftMask_WorldToMask");
                public static readonly int SoftMask_BorderRect = Shader.PropertyToID("_SoftMask_BorderRect");
                public static readonly int SoftMask_UVBorderRect = Shader.PropertyToID("_SoftMask_UVBorderRect");
                public static readonly int SoftMask_TileRepeat = Shader.PropertyToID("_SoftMask_TileRepeat");
            }
        }

        struct Diagnostics
        {
            SoftMask m_SoftMask;

            public Diagnostics(SoftMask softMask) { m_SoftMask = softMask; }

            public Errors PollErrors()
            {
                var softMask = m_SoftMask; // for use in lambda
                var result = Errors.NoError;
                softMask.GetComponentsInChildren(s_Maskables);
                if (s_Maskables.Any(m => ReferenceEquals(m.mask, softMask) && m.shaderIsNotSupported))
                    result |= Errors.UnsupportedShaders;
                if (ThereAreNestedMasks())
                    result |= Errors.NestedMasks;
                result |= CheckSprite(sprite);
                result |= CheckImage();
                return result;
            }

            public static Errors CheckSprite(Sprite sprite)
            {
                var result = Errors.NoError;
                if (!sprite) return result;
                if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight)
                    result |= Errors.TightPackedSprite;
                if (sprite.associatedAlphaSplitTexture)
                    result |= Errors.AlphaSplitSprite;
                return result;
            }

            Image image
            {
                get { return m_SoftMask.m_Graphic as Image; }
            }

            Sprite sprite
            {
                get
                {
                    switch (m_SoftMask.source)
                    {
                        case MaskSource.Sprite: return m_SoftMask.m_Sprite;
                        case MaskSource.Graphic: return this.image ? this.image.sprite : null;
                        default: return null;
                    }
                }
            }

            bool ThereAreNestedMasks()
            {
                var softMask = m_SoftMask; // for use in lambda
                var result = false;
                softMask.GetComponentsInParent(false, s_Masks);
                result |= s_Masks.Any(x => AreCompeting(softMask, x));
                softMask.GetComponentsInChildren(false, s_Masks);
                result |= s_Masks.Any(x => AreCompeting(softMask, x));
                return result;
            }

            Errors CheckImage()
            {
                var result = Errors.NoError;
                if (!m_SoftMask.isBasedOnGraphic) return result;
                if (image && image.type == Image.Type.Filled)
                    result |= Errors.UnsupportedImageType;
                return result;
            }

            static bool AreCompeting(SoftMask softMask, SoftMask other)
            {
                Assert.IsNotNull(other);
                return softMask.isMaskingEnabled
                    && softMask != other
                    && other.isMaskingEnabled
                    && softMask.canvas.rootCanvas == other.canvas.rootCanvas
                    && !Child(softMask, other).canvas.overrideSorting;
            }

            static T Child<T>(T first, T second) where T : Component
            {
                Assert.IsNotNull(first);
                Assert.IsNotNull(second);
                return first.transform.IsChildOf(second.transform) ? first : second;
            }
        }
    }
}
