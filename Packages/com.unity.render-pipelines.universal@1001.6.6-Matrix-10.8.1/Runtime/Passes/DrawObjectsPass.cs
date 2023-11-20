using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Draw  objects into the given color and depth target
    ///
    /// You can use this pass to render objects that have a material and/or shader
    /// with the pass names UniversalForward or SRPDefaultUnlit.
    /// </summary>
    public class DrawObjectsPass : ScriptableRenderPass
    {
        public enum PassType
        {
            Opaque,
            Transparent,
            BeforePostOpaque,
            BeforePostTransparent,
            OverlayOpaque,
            OverlayTransparent
        }
        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        string m_ProfilerTag;
        ProfilingSampler m_ProfilingSampler;
        PassType m_PassType;

        static readonly int s_DrawObjectPassDataPropID = Shader.PropertyToID("_DrawObjectPassData");
        //PicoVideo;SubPass;Ernst;Begin
        private LayerMask m_CurLayerMask;
        private bool m_ClearDepth = false;
        //PicoVideo;SubPass;Ernst;End

        public DrawObjectsPass(string profilerTag, ShaderTagId[] shaderTagIds, PassType passType, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
        {
            base.profilingSampler = new ProfilingSampler(nameof(DrawObjectsPass));

            m_ProfilerTag = profilerTag;
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            foreach (ShaderTagId sid in shaderTagIds)
                m_ShaderTagIdList.Add(sid);
            renderPassEvent = evt;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_PassType = passType;

            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilReference = stencilReference;
                m_RenderStateBlock.mask = RenderStateMask.Stencil;
                m_RenderStateBlock.stencilState = stencilState;
            }
            
            //PicoVideo;SubPass;Ernst;Begin
            m_CurLayerMask = layerMask;
            //PicoVideo;SubPass;Ernst;End
        }

        public DrawObjectsPass(string profilerTag, PassType passType, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
            : this(profilerTag,
                new ShaderTagId[] { new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly"), new ShaderTagId("LightweightForward")},
                passType, evt, renderQueueRange, layerMask, stencilState, stencilReference)
        {}

        internal DrawObjectsPass(URPProfileId profileId, PassType passType, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
            : this(profileId.GetType().Name, passType, evt, renderQueueRange, layerMask, stencilState, stencilReference)
        {
            m_ProfilingSampler = ProfilingSampler.Get(profileId);
        }
        
        //PicoVideo;SubPass;Ernst;Begin
        public void ResetLayerMask(LayerMask newLayerMask)
        {
            if (newLayerMask != m_CurLayerMask)
            {
                m_CurLayerMask = newLayerMask;
                if (m_PassType == PassType.Opaque)
                {
                    m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, m_CurLayerMask);
                }
                else if (m_PassType == PassType.Transparent)
                {
                    m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent, m_CurLayerMask);
                }
                else if (m_PassType == PassType.BeforePostOpaque)
                {
                    m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, m_CurLayerMask);
                }
                else if (m_PassType == PassType.BeforePostTransparent)
                {
                    m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent, m_CurLayerMask);
                }
                else if (m_PassType == PassType.OverlayOpaque)
                {
                    m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque, m_CurLayerMask);
                }
                else if (m_PassType == PassType.OverlayTransparent)
                {
                    m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent, m_CurLayerMask);
                }
            }
        }

        public override void EarlyConfigure(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            base.EarlyConfigure(context, ref renderingData);
            if (m_PassType == PassType.Opaque)
            {
                SubPassManager.BeginRenderPass(context, ref renderingData);
            }
        }

        public void ClearDepth(bool isClear)
        {
            m_ClearDepth = isClear;
        }
        //PicoVideo;SubPass;Ernst;End

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //PicoVideo;SubPass;Ernst;Begin
            if (m_PassType == PassType.Opaque)
            {
                SubPassManager.BeginSubPass(context, SubPassType.RenderToSwapColor);
            }

            //PicoVideo;SubPass;Ernst;End
            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            if (m_CurLayerMask != 0)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    if (m_ClearDepth)
                    {
                        cmd.ClearRenderTarget(true, false, Color.black);
                    }
                    
                    // Global render pass data containing various settings.
                    // x,y,z are currently unused
                    // w is used for knowing whether the object is opaque(1) or alpha blended(0)
                    Vector4 drawObjectPassData =
                        new Vector4(0.0f, 0.0f, 0.0f, (m_PassType == PassType.Opaque || m_PassType == PassType.BeforePostOpaque || m_PassType == PassType.OverlayOpaque) ? 1.0f : 0.0f);
                    cmd.SetGlobalVector(s_DrawObjectPassDataPropID, drawObjectPassData);

                    // scaleBias.x = flipSign
                    // scaleBias.y = scale
                    // scaleBias.z = bias
                    // scaleBias.w = unused
                    float flipSign = (renderingData.cameraData.IsCameraProjectionMatrixFlipped()) ? -1.0f : 1.0f;
                    Vector4 scaleBias = (flipSign < 0.0f)
                        ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                        : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                    cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBias);
                    
#if ENABLE_VR && ENABLE_XR_MODULE
                    var cameraData = renderingData.cameraData;
                    bool updateMatrices = false;
                    if (cameraData.xr.enabled)
                    {
                        if (m_PassType == PassType.OverlayOpaque || m_PassType == PassType.OverlayTransparent)
                        {
                            if (cameraData.xr.isLateLatchEnabled)
                            {
                                cameraData.xr.canMarkLateLatch = true;
                            }
                            cameraData.xr.UpdateGPUViewAndProjectionMatrices(cmd, ref cameraData, false);
                        }
                        else if (cameraData.xr.isLateLatchEnabled && cameraData.targetTexture == null)
                        {
                            cameraData.xr.MarkLateLatchShaderProperties(cmd, ref cameraData);
                        }
                    }
#endif      
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    Camera camera = renderingData.cameraData.camera;
                    var sortFlags = (m_PassType == PassType.Opaque || m_PassType == PassType.BeforePostOpaque || m_PassType == PassType.OverlayOpaque)
                        ? renderingData.cameraData.defaultOpaqueSortFlags
                        : SortingCriteria.CommonTransparent;
                    var drawSettings = CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortFlags);
                    var filterSettings = m_FilteringSettings;

#if UNITY_EDITOR
                    // When rendering the preview camera, we want the layer mask to be forced to Everything
                    if (renderingData.cameraData.isPreviewCamera)
                    {
                        filterSettings.layerMask = -1;
                    }
#endif

                    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings,
                        ref m_RenderStateBlock);

                    // Render objects that did not match any shader pass with error shader
                    RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera,
                        filterSettings, SortingCriteria.None);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            if (m_PassType == PassType.BeforePostTransparent)
            {
                SubPassManager.EndRenderPass(context);
            }
        }
    }
}
