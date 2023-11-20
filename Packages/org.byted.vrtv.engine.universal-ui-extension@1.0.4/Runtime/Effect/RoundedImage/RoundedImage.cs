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
using UnityEngine.UI;

namespace Matrix.UniversalUIExtension
{
    [AddComponentMenu("UniversalUIEffect/RoundedImage", 0)]
    public class RoundedImage : Image, IMeshModifier, IRounded
    {
        private static readonly MaterialDistributor s_MaterialDistributor = MaterialDistributor.identity;
        protected RoundedImage()
        {
            useLegacyMeshGeneration = false;
        }

        [SerializeField]
        [Min(0f)]
        private float m_CornerRadius = 0;

        /// <summary>
        /// The Radius of the Corner
        /// </summary>
        public float cornerRadius
        {
            get { return m_CornerRadius; }
            set
            {
                m_CornerRadius = value;
                SetVerticesDirty();
            }
        }

        [SerializeField]
        [Range(0f, 1f)]
        private float m_CornerSmoothing = 0;

        /// <summary>
        /// The Smoothing of the Corner, 0.6 is Similar to IOS
        /// </summary>
        public float cornerSmoothing
        {
            get { return m_CornerSmoothing; }
            set
            {
                m_CornerSmoothing = value;
                SetVerticesDirty();
            }
        }

        /// <summary>
        /// Returns the default material for the RoundedImage.
        /// </summary>
        public override Material defaultMaterial
        {
            get { return DefaultUIEffect.defaultUniversalUIEffectMaterial; }
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

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (canvas != null)
            {
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                if (canvas.rootCanvas != null)
                {
                    canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                }
            }
        }
        
#if UNITY_EDITOR
        private void Update()
        {
            if (canvas != null)
            {
                canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                if (canvas.rootCanvas != null)
                {
                    canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord2;
                }
            }
        }
#endif

        public override Material GetModifiedMaterial(Material baseMaterial)
        {
            return s_MaterialDistributor.Get( base.GetModifiedMaterial(defaultMaterial));
        }

        public void ModifyMesh(Mesh mesh)
        {
        }

        public void ModifyMesh(VertexHelper vh)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            var verts = ListPool<UIVertex>.Get();
            vh.GetUIVertexStream(verts);

            CalcRoundedCorner(ref verts);

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
            ListPool<UIVertex>.Release(verts);
        }

        public void GetRoundedParameters(out float cornersRatio, out float aspectRatio)
        {
            Rect rect = rectTransform.rect;
            cornersRatio = m_CornerRadius * 2 <= rect.width ? m_CornerRadius / rect.width : 0.5f;
            cornersRatio = rect.width * cornersRatio > 0.5f * rect.height
                ? 0.5f * rect.height / rect.width
                : cornersRatio;
            aspectRatio = rect.height / rect.width;
        }

        private void CalcRoundedCorner(ref List<UIVertex> verts)
        {
            GetRoundedParameters(out float cornersRatio, out float aspectRatio);
            Rect adjustedRect = GetPixelAdjustedRect();
            for (int i = 0; i < verts.Count; i++)
            {
                UIVertex uiVertex = verts[i];
                uiVertex.uv2 = new Vector4(
                    cornersRatio,
                    aspectRatio,
                    (uiVertex.position.x - adjustedRect.x) / adjustedRect.width,
                    (uiVertex.position.y - adjustedRect.y) / adjustedRect.height);
                verts[i] = uiVertex;
            }
        }
    }
}