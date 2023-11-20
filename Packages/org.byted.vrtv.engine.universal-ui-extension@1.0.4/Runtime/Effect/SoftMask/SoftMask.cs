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
using UnityEngine.EventSystems;
using UnityEngine.Scripting;
using UnityEngine.Sprites;
using UnityEngine.UI;

[assembly: AlwaysLinkAssembly]
[assembly: Preserve]
namespace Matrix.UniversalUIExtension
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("UniversalUIEffect/SoftMask", 10)]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Graphic))]
    public class SoftMask : UIBehaviour, ICanvasRaycastFilter
    {
        protected SoftMask()
        {
            m_Parameters = new MaterialParameters();
            m_MaterialDistributor = new MaterialDistributor(Replace, m_Parameters.Apply);
        }

        private enum BorderMode
        {
            Simple,
            Sliced,
            Tiled,
            RadialFilled
        }

        private struct SourceParameters
        {
            public Graphic graphic;
            public Sprite sprite;
            public BorderMode spriteBorderMode;
            public float spritePixelsPerUnit;
            public Texture texture;
            public Rect textureUVRect;
        }

        private readonly MaterialParameters m_Parameters = null;
        private readonly MaterialDistributor m_MaterialDistributor = null;

        internal MaterialDistributor materialDistributor
        {
            get { return m_MaterialDistributor; }
        }

        private static readonly Rect s_DefaultUVRect = new Rect(0, 0, 1, 1);

        [NonSerialized]
        private Material m_MaskMaterial;

        [NonSerialized]
        private RectTransform m_RectTransform;

        public RectTransform rectTransform
        {
            get { return m_RectTransform ? m_RectTransform : m_RectTransform = GetComponent<RectTransform>(); }
        }

        [NonSerialized]
        private Graphic m_Graphic;

        /// <summary>
        /// The graphic associated with the SoftMask.
        /// </summary>
        public Graphic graphic
        {
            get { return m_Graphic ? m_Graphic : m_Graphic = GetComponent<Graphic>(); }
        }

        [SerializeField]
        private bool m_ShowMaskGraphic = true;

        /// <summary>
        /// Show the graphic that is associated with the SoftMask render area.
        /// </summary>
        public bool showMaskGraphic
        {
            get { return m_ShowMaskGraphic; }
            set
            {
                if (m_ShowMaskGraphic == value)
                    return;

                m_ShowMaskGraphic = value;
                if (graphic != null)
                    graphic.SetMaterialDirty();
            }
        }

        private IndexedSet<SoftMaskModifier> softMaskModifierList
        {
            get
            {
                if (m_SoftMaskModifierList == null)
                {
                    m_SoftMaskModifierList = new IndexedSet<SoftMaskModifier>();
                }

                return m_SoftMaskModifierList;
            }
        }

        private bool m_IsDestroyed = false;

        public bool isDestroyed
        {
            get { return m_IsDestroyed; }
        }

        public bool isMasking
        {
            get { return isActiveAndEnabled && !isDestroyed && m_RootCanvas != null; }
        }

        private Canvas m_RootCanvas;
        private Matrix4x4 m_LastWorldMatrix = Matrix4x4.identity;
        private bool m_IsMaskDirty = true;
        private IndexedSet<SoftMaskModifier> m_SoftMaskModifierList = null;

        internal void RegisterModifier(SoftMaskModifier modifier)
        {
            softMaskModifierList.AddUnique(modifier);
            modifier.SetMaterialDirty();
        }

        internal void UnregisterModifier(SoftMaskModifier modifier)
        {
            softMaskModifierList.Remove(modifier);
        }

        internal void SetChildModifierDirty()
        {
            for (int i = softMaskModifierList.Count - 1; i >= 0; i--)
            {
                softMaskModifierList[i].SetMaterialDirty();
            }
        }

        internal void SetChildModifierRebindToParent()
        {
            for (int i = softMaskModifierList.Count - 1; i >= 0; i--)
            {
                softMaskModifierList[i].RebindParentSoftMask();
            }
        }


        private void SetChildModifierApplyParameters()
        {
            for (int i = softMaskModifierList.Count - 1; i >= 0; i--)
            {
                softMaskModifierList[i].ApplyParameters();
            }
        }

        internal void DestroyAllChildModifier(bool force)
        {
            for (int i = softMaskModifierList.Count - 1; i >= 0; i--)
            {
                SoftMaskModifier modifier = softMaskModifierList[i];
                if (force || modifier.autoDestroy)
                {
                    modifier.SelfDestroy();
                }
            }
        }

        public static void DeployModifierInChildren(SoftMask softmask, Transform root)
        {
            List<Graphic> results = ListPool<Graphic>.Get();
            root.GetComponentsInChildren(true, results);
            for (int i = results.Count - 1; i >= 0; i--)
            {
                Graphic childGraphic = results[i];
                if (childGraphic == softmask.graphic)
                {
                    continue;
                }

                if (!childGraphic.TryGetComponent(out SoftMaskModifier modifier))
                {
                    modifier = childGraphic.gameObject.AddComponent<SoftMaskModifier>();
                    modifier.autoDestroy = true;
                    modifier.SetMaterialDirty();
                }
                else
                {
                    modifier.RebindParentSoftMask();
                }
            }

            ListPool<Graphic>.Release(results);
        }

        /// <summary>
        /// Notify SoftMask to refresh
        /// </summary>
        public void SetMaskDirty()
        {
            m_IsMaskDirty = true;
        }

        protected override void Start()
        {
            base.Start();
            m_RootCanvas = UIUtilities.FindRootSortOverrideCanvas(transform, true);
            if (m_RootCanvas != null)
            {
                m_RootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                if (m_RootCanvas.rootCanvas != null)
                {
                    m_RootCanvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                }
            }
            DeployModifierInChildren(this, transform);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_IsMaskDirty = true;
            Canvas.preWillRenderCanvases -= UpdateMask;
            Canvas.preWillRenderCanvases += UpdateMask;
            graphic.RegisterDirtyMaterialCallback(SetMaskDirty);
            SetChildModifierDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Canvas.preWillRenderCanvases -= UpdateMask;
            graphic.UnregisterDirtyMaterialCallback(SetMaskDirty);
            SetChildModifierDirty();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_IsDestroyed = true;
            SetChildModifierRebindToParent();
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            m_RootCanvas = UIUtilities.FindRootSortOverrideCanvas(transform, true);
            if (m_RootCanvas != null)
            {
                m_RootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                if (m_RootCanvas.rootCanvas != null)
                {
                    m_RootCanvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                }
            }
            m_IsMaskDirty = true;
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            m_IsMaskDirty = true;
        }

        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            DeployModifierInChildren(this, transform);
            m_IsMaskDirty = true;
        }

        private void OnTransformChildrenChanged()
        {
            DeployModifierInChildren(this, transform);
            m_IsMaskDirty = true;
        }

        private void UpdateMask()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            graphic.canvasRenderer.cull = !m_ShowMaskGraphic;

            Matrix4x4 localToWorldMatrix = transform.localToWorldMatrix;
            if (!MathUtilities.TRSApproximately(m_LastWorldMatrix, localToWorldMatrix, 0.0000001f))
            {
                m_IsMaskDirty = true;
                m_LastWorldMatrix = localToWorldMatrix;
            }

            if (m_IsMaskDirty)
            {
                CalculateMaskParameters();
                SetChildModifierApplyParameters();
                m_IsMaskDirty = false;
            }
        }

        private void CalculateMaskParameters()
        {
            SourceParameters sourceParams = DeduceSourceParameters();
            m_Parameters.Revert();
            if (sourceParams.sprite)
            {
                CalculateSpriteBased(sourceParams.sprite, sourceParams.spriteBorderMode,
                    sourceParams.spritePixelsPerUnit);
            }
            else if (sourceParams.texture)
            {
                CalculateTextureBased(sourceParams.texture, sourceParams.textureUVRect);
            }
            else
            {
                CalculateSolidFill();
            }

            if (sourceParams.graphic is IRounded roundedGraphic)
            {
                roundedGraphic.GetRoundedParameters(out m_Parameters.roundedRadius, out m_Parameters.roundedRatio);
                m_Parameters.roundedRect = LocalMaskRect(Vector4.zero);
            }
        }

        private void CalculateSpriteBased(Sprite sprite, BorderMode borderMode, float spritePixelsPerUnit)
        {
            m_Parameters.worldToMask = WorldToMask();
            Vector4 inner = DataUtility.GetInnerUV(sprite);
            Vector4 outer = DataUtility.GetOuterUV(sprite);
            Vector4 padding = DataUtility.GetPadding(sprite);
            Vector4 fullMaskRect = LocalMaskRect(Vector4.zero);
            m_Parameters.maskRectUV = outer;
            Image image = graphic as Image;
            Vector2 spriteSize = sprite.rect.size;
            if (borderMode == BorderMode.Simple)
            {
                Vector4 normalizedPadding = MathUtilities.Div(padding, spriteSize);
                if (image.type == Image.Type.Filled)
                {
                    if (image.fillMethod == Image.FillMethod.Horizontal)
                    {
                        float imageFillAmount = image.fillAmount;
                        switch ((Image.OriginHorizontal)image.fillOrigin)
                        {
                            case Image.OriginHorizontal.Left:
                                fullMaskRect.z -= rectTransform.rect.width * (1 - imageFillAmount);
                                m_Parameters.maskRectUV.z -= (outer.z - outer.x) * (1 - imageFillAmount);
                                spriteSize.x *= imageFillAmount;
                                break;
                            case Image.OriginHorizontal.Right:
                                fullMaskRect.x += rectTransform.rect.width * (1 - imageFillAmount);
                                m_Parameters.maskRectUV.x += (outer.z - outer.x) * (1 - imageFillAmount);
                                spriteSize.x *= imageFillAmount;
                                break;
                            default:
                                break;
                        }
                    }
                    else if (image.fillMethod == Image.FillMethod.Vertical)
                    {
                        float imageFillAmount = image.fillAmount;
                        switch ((Image.OriginVertical)image.fillOrigin)
                        {
                            case Image.OriginVertical.Bottom:
                                fullMaskRect.w -= rectTransform.rect.height * (1 - imageFillAmount);
                                m_Parameters.maskRectUV.w -= (outer.w - outer.y) * (1 - imageFillAmount);
                                spriteSize.y *= imageFillAmount;
                                break;
                            case Image.OriginVertical.Top:
                                fullMaskRect.y += rectTransform.rect.height * (1 - imageFillAmount);
                                m_Parameters.maskRectUV.y += (outer.w - outer.y) * (1 - imageFillAmount);
                                spriteSize.y *= imageFillAmount;
                                break;
                            default:
                                break;
                        }
                    }
                }

                m_Parameters.maskRect =
                    MathUtilities.ApplyBorder(fullMaskRect,
                        MathUtilities.Mul(normalizedPadding, MathUtilities.Size(fullMaskRect)));

                if (image.preserveAspect)
                {
                    m_Parameters.maskRect = PreserveSpriteAspectRatio(m_Parameters.maskRect, spriteSize);
                }
            }
            else if (borderMode == BorderMode.RadialFilled)
            {
                float imageFillAmount = image.fillAmount;
                float radialFillAmount = 0;
                Vector2 radialFillUVCenter = Vector2.zero;
                Vector2 size = new Vector2(outer.z - outer.x, outer.w - outer.y);
                Vector2 radialFillUVRatio = new Vector2(size.y / size.x, 1f);
                int radialFillStartAngle = 0;
                switch (image.fillMethod)
                {
                    case Image.FillMethod.Horizontal:
                        switch ((Image.OriginHorizontal)image.fillOrigin)
                        {
                            case Image.OriginHorizontal.Left:
                                fullMaskRect.z = fullMaskRect.z * imageFillAmount;
                                break;
                            case Image.OriginHorizontal.Right:
                                fullMaskRect.z = fullMaskRect.z * imageFillAmount;
                                fullMaskRect.x += fullMaskRect.z * (1 - imageFillAmount);
                                break;
                            default:
                                break;
                        }

                        break;
                    case Image.FillMethod.Vertical:
                        break;
                    case Image.FillMethod.Radial90:
                        radialFillAmount = image.fillClockwise ? (1 - imageFillAmount / 4) : (imageFillAmount / 4 - 1);
                        switch ((Image.Origin90)image.fillOrigin)
                        {
                            case Image.Origin90.BottomLeft:
                                radialFillUVCenter = new Vector2(outer.x, outer.y);
                                radialFillStartAngle = image.fillClockwise ? 0 : 90;
                                break;
                            case Image.Origin90.TopLeft:
                                radialFillUVCenter = new Vector2(outer.x, outer.w);
                                radialFillStartAngle = image.fillClockwise ? 90 : 180;
                                break;
                            case Image.Origin90.TopRight:
                                radialFillUVCenter = new Vector2(outer.z, outer.w);
                                radialFillStartAngle = image.fillClockwise ? 180 : 270;
                                break;
                            case Image.Origin90.BottomRight:
                                radialFillUVCenter = new Vector2(outer.z, outer.y);
                                radialFillStartAngle = image.fillClockwise ? 270 : 360;
                                break;
                            default:
                                break;
                        }

                        break;
                    case Image.FillMethod.Radial180:
                        radialFillAmount = image.fillClockwise ? (1 - imageFillAmount / 2) : (imageFillAmount / 2 - 1);
                        switch ((Image.Origin180)image.fillOrigin)
                        {
                            case Image.Origin180.Bottom:
                                radialFillUVCenter =
                                    (new Vector2(outer.x, outer.y) + new Vector2(outer.z, outer.y)) / 2;
                                radialFillStartAngle = image.fillClockwise ? 270 : 90;
                                radialFillUVRatio *= new Vector2(2, 1);
                                break;
                            case Image.Origin180.Left:
                                radialFillUVCenter =
                                    (new Vector2(outer.x, outer.y) + new Vector2(outer.x, outer.w)) / 2;
                                radialFillStartAngle = image.fillClockwise ? 0 : 180;
                                radialFillUVRatio *= new Vector2(1, 2);
                                break;
                            case Image.Origin180.Top:
                                radialFillUVCenter =
                                    (new Vector2(outer.x, outer.w) + new Vector2(outer.z, outer.w)) / 2;
                                radialFillStartAngle = image.fillClockwise ? 90 : 270;
                                radialFillUVRatio *= new Vector2(2, 1);
                                break;
                            case Image.Origin180.Right:
                                radialFillUVCenter =
                                    (new Vector2(outer.z, outer.y) + new Vector2(outer.z, outer.w)) / 2;
                                radialFillStartAngle = image.fillClockwise ? 180 : 0;
                                radialFillUVRatio *= new Vector2(1, 2);
                                break;
                            default:
                                break;
                        }

                        break;
                    case Image.FillMethod.Radial360:
                        radialFillAmount = image.fillClockwise ? 1 - imageFillAmount : imageFillAmount - 1;
                        radialFillUVCenter = (new Vector2(outer.x, outer.y) + new Vector2(outer.z, outer.w)) / 2;
                        switch ((Image.Origin360)image.fillOrigin)
                        {
                            case Image.Origin360.Bottom:
                                radialFillStartAngle = 180;
                                break;
                            case Image.Origin360.Right:
                                radialFillStartAngle = 90;
                                break;
                            case Image.Origin360.Top:
                                radialFillStartAngle = 0;
                                break;
                            case Image.Origin360.Left:
                                radialFillStartAngle = 270;
                                break;
                            default:
                                break;
                        }

                        break;
                    default:
                        break;
                }

                m_Parameters.radialFillAmount = radialFillAmount;
                m_Parameters.radialFillStartAngle = radialFillStartAngle / 360f;
                m_Parameters.radialFillUVCenter = radialFillUVCenter;
                m_Parameters.radialFillUVRatio = radialFillUVRatio;
                Vector4 normalizedPadding = MathUtilities.Div(padding, spriteSize);
                m_Parameters.maskRect =
                    MathUtilities.ApplyBorder(fullMaskRect,
                        MathUtilities.Mul(normalizedPadding, MathUtilities.Size(fullMaskRect)));

                if (image.preserveAspect)
                {
                    m_Parameters.maskRect = PreserveSpriteAspectRatio(m_Parameters.maskRect, spriteSize);
                }
            }
            else
            {
                float spriteToCanvasScale = SpriteToCanvasScale(spritePixelsPerUnit);
                m_Parameters.maskRect = MathUtilities.ApplyBorder(fullMaskRect, padding * spriteToCanvasScale);
                Vector4 adjustedBorder = AdjustBorders(sprite.border * spriteToCanvasScale, fullMaskRect);
                m_Parameters.maskBorder = LocalMaskRect(adjustedBorder);
                m_Parameters.maskBorderUV = inner;
            }

            m_Parameters.texture = sprite.texture;
            m_Parameters.borderMode = borderMode;
            if (borderMode == BorderMode.Tiled)
                m_Parameters.tileRepeat = MaskRepeat(sprite, spritePixelsPerUnit, m_Parameters.maskBorder);
        }

        private static Vector4 AdjustBorders(Vector4 border, Vector4 rect)
        {
            Vector2 size = MathUtilities.Size(rect);
            for (int axis = 0; axis <= 1; axis++)
            {
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

        private Vector4 PreserveSpriteAspectRatio(Vector4 rect, Vector2 spriteSize)
        {
            float spriteRatio = spriteSize.x / spriteSize.y;
            float rectRatio = (rect.z - rect.x) / (rect.w - rect.y);
            if (spriteRatio > rectRatio)
            {
                float scale = rectRatio / spriteRatio;
                return new Vector4(rect.x, rect.y * scale, rect.z, rect.w * scale);
            }
            else
            {
                float scale = spriteRatio / rectRatio;
                return new Vector4(rect.x * scale, rect.y, rect.z * scale, rect.w);
            }
        }

        private float SpriteToCanvasScale(float spritePixelsPerUnit)
        {
            float canvasPixelsPerUnit = m_RootCanvas ? m_RootCanvas.referencePixelsPerUnit : 100;
            return canvasPixelsPerUnit / spritePixelsPerUnit;
        }

        private void CalculateTextureBased(Texture texture, Rect uvRect)
        {
            m_Parameters.worldToMask = WorldToMask();
            m_Parameters.maskRect = LocalMaskRect(Vector4.zero);
            m_Parameters.maskRectUV = MathUtilities.ToVector(uvRect);
            m_Parameters.texture = texture;
            m_Parameters.borderMode = BorderMode.Simple;
        }

        private void CalculateSolidFill()
        {
            CalculateTextureBased(null, s_DefaultUVRect);
        }

        private Matrix4x4 WorldToMask()
        {
            if (m_RootCanvas != null)
            {
                return transform.worldToLocalMatrix * m_RootCanvas.rootCanvas.transform.localToWorldMatrix;
            }

            return Matrix4x4.identity;
        }

        private Vector4 LocalMaskRect(Vector4 border)
        {
            return MathUtilities.ApplyBorder(MathUtilities.ToVector(rectTransform.rect), border);
        }

        private Vector2 MaskRepeat(Sprite sprite, float spritePixelsPerUnit, Vector4 centralPart)
        {
            Vector4 textureCenter = MathUtilities.ApplyBorder(MathUtilities.ToVector(sprite.rect), sprite.border);
            return MathUtilities.Div(MathUtilities.Size(centralPart) * SpriteToCanvasScale(spritePixelsPerUnit),
                MathUtilities.Size(textureCenter));
        }
        
        private SourceParameters DeduceSourceParameters()
        {
            SourceParameters result = new SourceParameters();
            result.graphic = graphic;
            if (graphic is Image image)
            {
                Sprite sprite = image.sprite;
                result.sprite = sprite;
                result.spriteBorderMode = GetBorderMode(image);
                if (sprite)
                {
                    switch (result.spriteBorderMode)
                    {
                        case BorderMode.Tiled:
                            if (sprite.border == Vector4.zero)
                            {
                                result.spritePixelsPerUnit = sprite.pixelsPerUnit * (1 / image.pixelsPerUnitMultiplier);
                            }

                            break;
                        default:
                            result.spritePixelsPerUnit = sprite.pixelsPerUnit * image.pixelsPerUnitMultiplier;
                            break;
                    }

                    result.texture = sprite.texture;
                }
                else
                    result.spritePixelsPerUnit = 100f;
            }
            else if (graphic is RawImage rawImage)
            {
                result.texture = rawImage.texture;
                result.textureUVRect = rawImage.uvRect;
            }

            return result;
        }

        private BorderMode GetBorderMode(Image image)
        {
            Image.Type type = image.type;
            switch (type)
            {
                case Image.Type.Simple:
                    return BorderMode.Simple;
                case Image.Type.Sliced:
                    return BorderMode.Sliced;
                case Image.Type.Tiled:
                    return BorderMode.Tiled;
                case Image.Type.Filled:
                    return image.fillMethod == Image.FillMethod.Horizontal ||
                           image.fillMethod == Image.FillMethod.Vertical
                        ? BorderMode.Simple
                        : BorderMode.RadialFilled;
                default:
                    return BorderMode.Simple;
            }
        }

        public virtual bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
        {
            return !isActiveAndEnabled ||
                   RectTransformUtility.RectangleContainsScreenPoint(rectTransform, sp, eventCamera);
        }

        private Material Replace(Material original)
        {
            Material material = DefaultUIEffect.Replace(original);
            material.name += " - SoftMask";
            return material;
        }

        private class MaterialParameters
        {
            public Vector4 maskRect = Vector4.zero;
            public Vector4 maskBorder = Vector4.zero;
            public Vector4 maskRectUV = Vector4.zero;
            public Vector4 maskBorderUV = Vector4.zero;
            public Vector2 tileRepeat = Vector2.zero;
            public Matrix4x4 worldToMask = Matrix4x4.identity;
            public Texture texture = null;
            public BorderMode borderMode = BorderMode.Simple;

            public float roundedRadius = 0;
            public float roundedRatio = 0;
            public Vector4 roundedRect = Vector4.zero;

            public float radialFillAmount = 1f;
            public float radialFillStartAngle = 0f;
            public Vector2 radialFillUVCenter = Vector2.zero;
            public Vector2 radialFillUVRatio = Vector2.zero;

            private const string SOFTMASK_SIMPLE = "SOFTMASK_SIMPLE";
            private const string SOFTMASK_SLICED = "SOFTMASK_SLICED";
            private const string SOFTMASK_TILED = "SOFTMASK_TILED";
            private const string SOFTMASK_RADIALFILLED = "SOFTMASK_RADIALFILLED";
            private static readonly int s_SoftMask = Shader.PropertyToID("_SoftMask");
            private static readonly int s_SoftMask_Rect = Shader.PropertyToID("_SoftMask_Rect");
            private static readonly int s_SoftMask_UVRect = Shader.PropertyToID("_SoftMask_UVRect");
            private static readonly int s_SoftMask_WorldToMask = Shader.PropertyToID("_SoftMask_WorldToMask");
            private static readonly int s_SoftMask_BorderRect = Shader.PropertyToID("_SoftMask_BorderRect");
            private static readonly int s_SoftMask_UVBorderRect = Shader.PropertyToID("_SoftMask_UVBorderRect");
            private static readonly int s_SoftMask_TileRepeat = Shader.PropertyToID("_SoftMask_TileRepeat");
            private static readonly int s_SoftMask_ChannelWeights = Shader.PropertyToID("_SoftMask_ChannelWeights");
            private static readonly int s_RadialFillAmount = Shader.PropertyToID("_RadialFillAmount");
            private static readonly int s_RadialFillStartAngle = Shader.PropertyToID("_RadialFillStartAngle");
            private static readonly int s_RadialFillUVCenter = Shader.PropertyToID("_RadialFillUVCenter");
            private static readonly int s_RadialFillUVRatio = Shader.PropertyToID("_RadialFillUVRatio");
            private static readonly int s_RoundedRadius = Shader.PropertyToID("_RoundedRadius");
            private static readonly int s_RoundedRatio = Shader.PropertyToID("_RoundedRatio");
            private static readonly int s_RoundedRect = Shader.PropertyToID("_RoundedRect");

            public void Revert()
            {
                maskRect = Vector4.zero;
                maskBorder = Vector4.zero;
                maskRectUV = Vector4.zero;
                maskBorderUV = Vector4.zero;
                tileRepeat = Vector2.zero;
                worldToMask = Matrix4x4.identity;
                texture = null;
                borderMode = BorderMode.Simple;
                roundedRadius = 0;
                roundedRatio = 0;
                roundedRect = Vector4.zero;
                radialFillAmount = 0;
                radialFillStartAngle = 0;
                radialFillUVCenter = Vector2.zero;
                radialFillUVRatio = Vector2.zero;
            }

            private Texture activeTexture
            {
                get { return texture ? texture : Texture2D.whiteTexture; }
            }

            public void Apply(Material material)
            {
                material.SetKeywordEnable(SOFTMASK_SIMPLE, borderMode == BorderMode.Simple);
                material.SetKeywordEnable(SOFTMASK_SLICED, borderMode == BorderMode.Sliced);
                material.SetKeywordEnable(SOFTMASK_TILED, borderMode == BorderMode.Tiled);
                material.SetKeywordEnable(SOFTMASK_RADIALFILLED, borderMode == BorderMode.RadialFilled);
                material.SetTexture(s_SoftMask, activeTexture);
                material.SetVector(s_SoftMask_Rect, maskRect);
                material.SetFloat(s_RoundedRadius, roundedRadius);
                material.SetFloat(s_RoundedRatio, roundedRatio);
                material.SetVector(s_RoundedRect, roundedRect);
                material.SetVector(s_SoftMask_UVRect, maskRectUV);
                material.SetVector(s_SoftMask_ChannelWeights, new Vector4(0,0,0,1));
                material.SetMatrix(s_SoftMask_WorldToMask, worldToMask);
                if (borderMode != BorderMode.Simple)
                {
                    material.SetVector(s_SoftMask_BorderRect, maskBorder);
                    material.SetVector(s_SoftMask_UVBorderRect, maskBorderUV);
                    if (borderMode == BorderMode.Tiled)
                    {
                        material.SetVector(s_SoftMask_TileRepeat, tileRepeat);
                    }

                    if (borderMode == BorderMode.RadialFilled)
                    {
                        material.SetFloat(s_RadialFillAmount, radialFillAmount);
                        material.SetFloat(s_RadialFillStartAngle, radialFillStartAngle);
                        material.SetVector(s_RadialFillUVCenter, radialFillUVCenter);
                        material.SetVector(s_RadialFillUVRatio, radialFillUVRatio);
                    }
                }
            }
        }
    }
}