using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Draw  motion vectors into the given color and depth target. Both come from the Oculus runtime.
    ///
    /// This will render objects that have a material and/or shader with the pass name "MotionVectors".
    /// </summary>
    public class MotionVectorPass : ScriptableRenderPass
    {
        private FilteringSettings m_FilteringSettings;
        private ProfilingSampler m_ProfilingSampler;
        private bool m_UseExtenalTexture;
        private static int s_MotionVectorTextureID = Shader.PropertyToID("_CameraMVTexture");

        private RenderTargetIdentifier m_MotionVectorColorIdentifier;
        private RenderTargetIdentifier m_MotionVectorDepthIdentifier;

        public MotionVectorPass(string profilerTag, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
        {
            base.profilingSampler = new ProfilingSampler(nameof(MotionVectorPass));
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            renderPassEvent = evt;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
        }

        internal MotionVectorPass(URPProfileId profileId, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
            : this(profileId.GetType().Name, opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference)
        {
            m_ProfilingSampler = ProfilingSampler.Get(profileId);
        }

        public void Setup(RenderTargetIdentifier motionVecColorIdentifier, RenderTargetIdentifier motionVecDepthIdentifier, bool useExtenalTexture)
        {
            m_MotionVectorColorIdentifier = motionVecColorIdentifier;
            m_MotionVectorDepthIdentifier = motionVecDepthIdentifier;
            m_UseExtenalTexture = useExtenalTexture;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            if (m_UseExtenalTexture)
            {
                RenderTextureDescriptor xrMotionVectorDesc = renderingData.cameraData.cameraTargetDescriptor;
                xrMotionVectorDesc.depthBufferBits = 24;
                int downSampling = (int)UniversalRenderPipeline.asset.motionVectorDownsampling;
                xrMotionVectorDesc.width /= downSampling;
                xrMotionVectorDesc.height /= downSampling;
                cmd.GetTemporaryRT(s_MotionVectorTextureID, xrMotionVectorDesc);
                if (renderingData.cameraData.xr.enabled)
                {
                    RenderTargetIdentifier motionVectionIdentifiler =
                        new RenderTargetIdentifier(s_MotionVectorTextureID, 0, CubemapFace.Unknown, -1);
                    ConfigureTarget(motionVectionIdentifiler);
                }
                else
                {
                    ConfigureTarget(s_MotionVectorTextureID, m_MotionVectorDepthIdentifier);
                }
            }
            else
            {
                ConfigureTarget(m_MotionVectorColorIdentifier, m_MotionVectorDepthIdentifier);
            }
            ConfigureClear(ClearFlag.All, Color.black);

        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                Camera camera = renderingData.cameraData.camera;
                var filterSettings = m_FilteringSettings;
                
                var drawSettings = CreateDrawingSettings(new ShaderTagId("MotionVectors"), ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                drawSettings.perObjectData = PerObjectData.MotionVectors;
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings);
                if (!m_UseExtenalTexture)
                {
                    cmd.SetGlobalTexture(s_MotionVectorTextureID, m_MotionVectorColorIdentifier);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
            if (m_UseExtenalTexture)
            {
                cmd.ReleaseTemporaryRT(s_MotionVectorTextureID);
            }
        }
    }
}
