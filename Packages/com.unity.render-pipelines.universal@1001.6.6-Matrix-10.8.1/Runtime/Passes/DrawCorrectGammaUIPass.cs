using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal.Internal
{
    public class DrawCorrectGammaUIPass : ScriptableRenderPass
    {
        private RenderTargetHandle m_Source;
        private RenderTargetHandle m_Depth;
        private RenderTargetHandle m_TempBlit;
        private RenderStateBlock m_RenderStateBlock;
        private Material m_BlitMaterial;
        private Material m_BlitInSubPassMaterial;
        private FilteringSettings m_FilteringSettings;
        private ShaderTagId m_ShaderTagId;
        private ProfilingSampler m_DrawUIProfilingSampler;
        private ProfilingSampler m_FirstFixingProfilingSampler;
        private ProfilingSampler m_FinalFixingProfilingSampler;
        private static readonly int s_DrawObjectPassDataPropID = Shader.PropertyToID("_DrawObjectPassData");
        private const string RENDER_IN_GAMMA = "RENDER_IN_GAMMA";
        private LayerMask m_CurLayerMask;

        public DrawCorrectGammaUIPass(RenderPassEvent evt, LayerMask layerMask, Material blitMaterial, Material blitInSubPassMaterial,
            StencilState stencilState, int stencilReference)
        {
            renderPassEvent = evt;
            m_DrawUIProfilingSampler = new ProfilingSampler("DrawCustomUI");
            m_FirstFixingProfilingSampler = new ProfilingSampler("LinearToSRGB");
            m_FinalFixingProfilingSampler = new ProfilingSampler("SRGBToLinear");
            m_ShaderTagId = new ShaderTagId("SRPDefaultUnlit");
            m_BlitMaterial = blitMaterial;
            m_BlitInSubPassMaterial = blitInSubPassMaterial;
            m_TempBlit.Init("_CopyCameraBufferInGammaSpace");

            m_FilteringSettings = new FilteringSettings(RenderQueueRange.transparent, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilReference = stencilReference;
                m_RenderStateBlock.mask = RenderStateMask.Stencil;
                m_RenderStateBlock.stencilState = stencilState;
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (!SubPassManager.supported)
            {
                cameraTextureDescriptor.depthBufferBits = 1;
                cameraTextureDescriptor.memoryless = cameraTextureDescriptor.msaaSamples > 1 ? RenderTextureMemoryless.MSAA : RenderTextureMemoryless.None;
                cmd.GetTemporaryRT(m_TempBlit.id, cameraTextureDescriptor);
            }
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (!SubPassManager.supported)
            {
                cmd.ReleaseTemporaryRT(m_TempBlit.id);
            }
        }

        public bool DrawRendersIsValid(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var sortFlags = SortingCriteria.CommonTransparent;
            var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
            var filterSettings = m_FilteringSettings;
            return ForwardRendererExtend.DrawRenderersIsValid(context, renderingData.cullResults, ref drawSettings, ref filterSettings);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            if (SubPassManager.isInsideRenderPass)
            {
                SubPassManager.BeginSubPass(context, SubPassType.RenderToSwapColorWithPreviousResult);
                cmd.EnableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitInSubPassMaterial, 0, MeshTopology.Quads, 4, 1, null);
                cmd.DisableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
                cmd.EnableShaderKeyword(RENDER_IN_GAMMA);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                var sortFlags = SortingCriteria.CommonTransparent;
                var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                var filterSettings = m_FilteringSettings;
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings,
                    ref m_RenderStateBlock);
                cmd.DisableShaderKeyword(RENDER_IN_GAMMA);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                
                SubPassManager.BeginSubPass(context, SubPassType.RenderToFinalColorWithPreviousResult);
                cmd.EnableShaderKeyword(ShaderKeywordStrings.SRGBToLinearConversion);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitInSubPassMaterial, 0, MeshTopology.Quads, 4, 1, null);
                cmd.DisableShaderKeyword(ShaderKeywordStrings.SRGBToLinearConversion);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
            else
            {
                m_Source = ForwardRendererExtend.GetActiveColorRenderTargetHandle();
                m_Depth = ForwardRendererExtend.GetActiveDepthRenderTargetHandle();
                if (!renderingData.cameraData.xr.enabled)
                {
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.UseDrawProcedural);
                }
                using (new ProfilingScope(cmd, m_FirstFixingProfilingSampler))
                {
                    // cmd.DisableShaderKeyword("_SubPassMSAA2");
                    // cmd.DisableShaderKeyword("_SubPassMSAA4");
                    // if (renderingData.cameraData.cameraTargetDescriptor.msaaSamples == 2)
                    // {
                    //     cmd.EnableShaderKeyword("_SubPassMSAA2");
                    // }
                    // else if (renderingData.cameraData.cameraTargetDescriptor.msaaSamples == 4)
                    // {
                    //     cmd.EnableShaderKeyword("_SubPassMSAA4");
                    // }
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
                    cmd.SetGlobalTexture(ShaderPropertyId.sourceTex, m_Source.Identifier());
                    Vector4 scaleBias = new Vector4(1, 1, 0, 0);
                    Vector4 scaleBiasRt = new Vector4(1, 1, 0, 0);
                    cmd.SetGlobalVector(ShaderPropertyId.scaleBias, scaleBias);
                    cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBiasRt);
                    //cmd.SetGlobalTexture("_CameraDepthTexture", new RenderTargetIdentifier(m_Depth.Identifier(), 0, CubemapFace.Unknown, -1));
                    cmd.SetRenderTarget(new RenderTargetIdentifier(m_TempBlit.Identifier(), 0, CubemapFace.Unknown, -1), RenderBufferLoadAction.DontCare, renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1 ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store, 
                             new RenderTargetIdentifier(m_Depth.Identifier(), 0, CubemapFace.Unknown, -1), RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
                    cmd.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, 0, MeshTopology.Quads, 4, 1, null);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.LinearToSRGBConversion);
                }
                
                using (new ProfilingScope(cmd, m_DrawUIProfilingSampler))
                {
                    cmd.EnableShaderKeyword(RENDER_IN_GAMMA);
                    cmd.SetGlobalVector(s_DrawObjectPassDataPropID, Vector4.zero);
                
                    float flipSign = (renderingData.cameraData.IsCameraProjectionMatrixFlipped()) ? -1.0f : 1.0f;
                    Vector4 scaleBias = (flipSign < 0.0f)
                        ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                        : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                    cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBias);
                
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                
                    Camera camera = renderingData.cameraData.camera;
                    var sortFlags = SortingCriteria.CommonTransparent;
                    var drawSettings = CreateDrawingSettings(m_ShaderTagId, ref renderingData, sortFlags);
                    var filterSettings = m_FilteringSettings;
                
                    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSettings,
                        ref m_RenderStateBlock);
                    RenderingUtils.RenderObjectsWithError(context, ref renderingData.cullResults, camera, filterSettings,
                        SortingCriteria.None);
                    cmd.DisableShaderKeyword(RENDER_IN_GAMMA);
                }
                
                using (new ProfilingScope(cmd, m_FinalFixingProfilingSampler))
                {
                    cmd.EnableShaderKeyword(ShaderKeywordStrings.SRGBToLinearConversion);
                    //RenderingUtils.Blit(cmd, m_TempBlit.Identifier(), m_Source.Identifier(), m_BlitMaterial, 0, true, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    cmd.SetGlobalTexture(ShaderPropertyId.sourceTex, m_TempBlit.Identifier());
                    cmd.SetRenderTarget(new RenderTargetIdentifier(m_Source.Identifier(), 0, CubemapFace.Unknown, -1), RenderBufferLoadAction.DontCare, renderingData.cameraData.cameraTargetDescriptor.msaaSamples > 1 ? RenderBufferStoreAction.Resolve : RenderBufferStoreAction.Store,
                        new RenderTargetIdentifier(m_Depth.Identifier(), 0, CubemapFace.Unknown, -1), RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
                    cmd.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, 0, MeshTopology.Quads, 4, 1, null);
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.SRGBToLinearConversion);
                }

                if (!renderingData.cameraData.xr.enabled)
                {
                    cmd.DisableShaderKeyword(ShaderKeywordStrings.UseDrawProcedural);
                }
                context.ExecuteCommandBuffer(cmd);
            }
            
            CommandBufferPool.Release(cmd);
        }
    }
}
