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

namespace UnityEngine.Rendering.Universal.Internal
{
    public class DynamicResolutionPass : ScriptableRenderPass
    {
        private const string m_ProfilerTag = "Dynamic Resolution Translate";
        private static readonly ProfilingSampler m_ProfilingSampler = new ProfilingSampler(m_ProfilerTag);

        private RenderTextureDescriptor m_ColorTextureDescriptor;

        private RenderTargetHandle m_SrcColorRenderTarget;
        private RenderTargetHandle m_SrcDepthRenderTarget;

        private RenderTargetHandle m_DstColorRenderTarget;
        private RenderTargetHandle m_DstDepthRenderTarget;

        private bool m_ExcuteDepth = false;

        private Material m_Material = null;
        private int m_ScaleBiasId = Shader.PropertyToID("_ScaleBiasRT");

        public DynamicResolutionPass(RenderPassEvent evt, Material material)
        {
            renderPassEvent = evt;
            m_Material = material;
        }

        public void Setup(RenderTextureDescriptor colorTextureDescriptor, RenderTargetHandle srcColorRenderTarget, RenderTargetHandle srcDepthRenderTarget,
                          bool excuteDepth,
                          RenderTargetHandle dstColorRenderTarget, RenderTargetHandle dstDepthRenderTarget)
        {
            m_SrcColorRenderTarget = srcColorRenderTarget;
            m_SrcDepthRenderTarget = srcDepthRenderTarget;

            m_DstColorRenderTarget = dstColorRenderTarget;
            m_DstDepthRenderTarget = dstDepthRenderTarget;

            m_ExcuteDepth = excuteDepth;
            m_ColorTextureDescriptor = colorTextureDescriptor;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                //Copy Color Texture
                cmd.SetRenderTarget(m_SrcColorRenderTarget.Identifier(), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.Blit(m_SrcColorRenderTarget.id, m_DstColorRenderTarget.id);
                cmd.ReleaseTemporaryRT(m_SrcColorRenderTarget.id);

                //Copy Depth Texture
                if (m_ExcuteDepth)
                {
                    cmd.SetRenderTarget(m_DstDepthRenderTarget.Identifier(), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                    CameraData cameraData = renderingData.cameraData;
                    switch (renderingData.cameraData.cameraTargetDescriptor.msaaSamples)
                    {
                        case 8:
                            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                            cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                            break;

                        case 4:
                            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                            cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                            break;

                        case 2:
                            cmd.EnableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                            break;

                        default:
                            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa2);
                            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa4);
                            cmd.DisableShaderKeyword(ShaderKeywordStrings.DepthMsaa8);
                            break;
                    }

                    cmd.SetGlobalTexture("_CameraDepthAttachment", m_SrcDepthRenderTarget.Identifier());

                    float flipSign = (cameraData.IsCameraProjectionMatrixFlipped()) ? -1.0f : 1.0f;
                    Vector4 scaleBias = (flipSign < 0.0f) ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f) : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                    cmd.SetGlobalVector(m_ScaleBiasId, scaleBias);
                    cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material);
                }
                cmd.ReleaseTemporaryRT(m_SrcDepthRenderTarget.id);

                ForwardRendererExtend.UninitFullScreenTempRT(cmd);
                ForwardRendererExtend.InitFullScreenTempRT(m_ColorTextureDescriptor, m_DstColorRenderTarget, m_ExcuteDepth ? m_DstDepthRenderTarget : m_SrcDepthRenderTarget);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }
        }
    }
}
