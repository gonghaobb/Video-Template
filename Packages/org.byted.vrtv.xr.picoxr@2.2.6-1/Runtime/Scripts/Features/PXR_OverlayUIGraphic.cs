using System;
using Unity.XR.PXR;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace UnityEngine.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Overlay UI Graphic")]
    public class PXR_OverlayUIGraphic : Graphic
    {
        protected PXR_OverlayUIGraphic()
        {
            useLegacyMeshGeneration = false;
        }

        private static Material s_OverlayMaterial = null;
        private static Rect s_UVRect = new Rect(0f, 0f, 1f, 1f);
        private static readonly int s_SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int s_DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int s_SrcAlphaBlend = Shader.PropertyToID("_SrcAlphaBlend");
        private static readonly int s_DstAlphaBlend = Shader.PropertyToID("_DstAlphaBlend");
        private static readonly int s_AlphaBlendOp = Shader.PropertyToID("_AlphaBlendOp");

        public PXR_OverLay overlay = null;
        public Vector2Int overlaySize = Vector2Int.zero;
        public bool fixedLengthMode = false;

        [Min(0f)]
        public float roundedCornersRadius = 0;

        [Min(0f)]
        public float softEdgeLength = 0;

        [Range(0f, 0.5f)]
        public float roundedCornersRatio = 0;

        [Range(0f, 0.5f)]
        public float softEdgeRatio = 0;

        public override Material materialForRendering
        {
            get
            {
                if (s_OverlayMaterial == null)
                {
                    s_OverlayMaterial = new Material(Shader.Find("PXR_SDK/PXR_UnderlayHole_UI"));
#if UNITY_EDITOR
                    s_OverlayMaterial.SetFloat(s_SrcBlend, (float)BlendMode.One);
                    s_OverlayMaterial.SetFloat(s_DstBlend, (float)BlendMode.OneMinusSrcAlpha);
                    s_OverlayMaterial.SetFloat(s_SrcAlphaBlend,(float)BlendMode.One);
                    s_OverlayMaterial.SetFloat(s_DstAlphaBlend,(float)BlendMode.One);
                    s_OverlayMaterial.SetFloat(s_AlphaBlendOp, (float)BlendOp.ReverseSubtract);
#else
                    s_OverlayMaterial.SetFloat(s_SrcBlend,(float)BlendMode.Zero);
                    s_OverlayMaterial.SetFloat(s_DstBlend,(float)BlendMode.OneMinusSrcAlpha);
                    s_OverlayMaterial.SetFloat(s_SrcAlphaBlend,(float)BlendMode.One);
                    s_OverlayMaterial.SetFloat(s_DstAlphaBlend,(float)BlendMode.One);
                    s_OverlayMaterial.SetFloat(s_AlphaBlendOp, (float)BlendOp.ReverseSubtract);
#endif
                }

                return s_OverlayMaterial;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (canvas != null)
            {
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                if (canvas.rootCanvas != null)
                {
                    canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                }
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = GetPixelAdjustedRect();
            var v = new Vector4(r.x, r.y, r.x + r.width, r.y + r.height);
            var rect = rectTransform.rect;
            Color32 color32 = color;
            float cornersRatio = roundedCornersRatio;
            float edgeRatio = softEdgeRatio;
            if (fixedLengthMode)
            {
                cornersRatio = roundedCornersRadius * 2 <= rect.width ? roundedCornersRadius / rect.width : 0.5f;
                edgeRatio = softEdgeLength / rect.width;
            }

            cornersRatio = rect.width * cornersRatio > 0.5f * rect.height
                ? 0.5f * rect.height / rect.width
                : cornersRatio;

            Vector4 holeSetting = new Vector4(
                cornersRatio,
                edgeRatio,
                rect.height / rect.width,
                0);
            Vector4 defaultTangent = new Vector4(1.0f, 0.0f, 0.0f, -1.0f);
            Vector3 defaultNormal = Vector3.back;
            vh.AddVert(new Vector3(v.x, v.y), color32, new Vector2(s_UVRect.xMin, s_UVRect.yMin), Vector4.zero,
                holeSetting
                , Vector4.zero, defaultNormal, defaultTangent);
            vh.AddVert(new Vector3(v.x, v.w), color32, new Vector2(s_UVRect.xMin, s_UVRect.yMax), Vector4.zero,
                holeSetting
                , Vector4.zero, defaultNormal, defaultTangent);
            vh.AddVert(new Vector3(v.z, v.w), color32, new Vector2(s_UVRect.xMax, s_UVRect.yMax), Vector4.zero,
                holeSetting
                , Vector4.zero, defaultNormal, defaultTangent);
            vh.AddVert(new Vector3(v.z, v.y), color32, new Vector2(s_UVRect.xMax, s_UVRect.yMin), Vector4.zero,
                holeSetting
                , Vector4.zero, defaultNormal, defaultTangent);

            vh.AddTriangle(0, 1, 2);
            vh.AddTriangle(2, 3, 0);
        }

        protected override void OnDidApplyAnimationProperties()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            SetMaterialDirty();
            SetVerticesDirty();
        }
    }
}