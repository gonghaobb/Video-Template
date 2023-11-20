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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace Matrix.EcosystemSimulate
{
    public class SunShaftsLightPass : UserDefinedPass
    {
        private const string m_ProfilerTag = "SunShafts";

        private SunLightManager m_SunShaftsLight;
        private Material m_BlitMaterial;
        private RenderTargetHandle m_SourceRT;
        private RenderTargetHandle m_SourceTempRT;

        private RenderTargetHandle m_SunShaftsRT;
        private RenderTargetHandle m_TempRT;

        public SunShaftsLightPass(SunLightManager voluetricLight, Camera camera, bool enablePass = true) 
            : base(RenderPassEvent.BeforeRenderingPostProcessing, camera, enablePass)
        {
            m_SunShaftsLight = voluetricLight;
            renderCamera = camera;
         
            if (m_SunShaftsLight.m_SunShaftsShader == null)
            {
                m_SunShaftsLight.m_SunShaftsShader =
                    Shader.Find("PicoVideo/EcosystemSimulate/SunLightSimulate/SunShaftsLight");
            }

            if(m_SunShaftsLight.m_SunShaftsShader == null)
            {
                Debugger.LogError("SunShaftsLight : SunShaftsLight Shader Load Failed!");
                return;
            }
            m_BlitMaterial = new Material(m_SunShaftsLight.m_SunShaftsShader);
            
            m_SunShaftsRT.Init("_SunShaftsTex");
            m_TempRT.Init("_TempTex");
        }

        public override void Configure(CustomPass pass, CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor opaqueDesc = cameraTextureDescriptor;
            cmd.GetTemporaryRT(m_SourceTempRT.id, opaqueDesc, FilterMode.Point);

            // m_SourceTempRT.id = ForwardRendererExtend.GetFullScreenTempRT(cmd);
            opaqueDesc.colorFormat = RenderTextureFormat.BGRA32;
            opaqueDesc.width = Screen.width / m_SunShaftsLight.m_DownSample;
            opaqueDesc.height = Screen.height / m_SunShaftsLight.m_DownSample;
            opaqueDesc.depthBufferBits = 0;   // opengl必须设置,要不然读取不到值
            cmd.GetTemporaryRT(m_SunShaftsRT.id, opaqueDesc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(m_TempRT.id, opaqueDesc, FilterMode.Point);
        }

        public override int RequireFlag()
        {
            if (renderCamera != null && renderCamera.cullingMask == 0)
                return 0;

            return ForwardRendererExtend.REQUIRE_DEPTH_TEXTURE;
        }

        public override void Execute(CustomPass pass, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_SunShaftsLight == null)
            {
                enable = false;
                Debugger.LogError("SunShaftsLightPass : m_SunShaftsLight 为空，已自动关闭 SunShaftsLightPass");
                return;
            }

            if (m_SunShaftsLight.m_MainLight == null || m_SunShaftsLight.m_MainCamera == null)
            {
                Debugger.LogError("SunShaftsLightPass : Main Camera 或者 Main Light 为空，已自动关闭 SunShaftsLight");
                m_SunShaftsLight.enableSunLight = false;
                return;
            }

            if (m_BlitMaterial == null)
            {
                enable = false;
                Debugger.LogError("SunShaftsLightPass : m_BlitMaterial 为空，已自动关闭 SunShaftsLightPass");
                return;
            }

            // 太阳位置=摄像机位置-太阳方向*视距
            Vector3 dir = m_SunShaftsLight.m_SunLightForward;
            if (dir == Vector3.zero)
            {
                dir = m_SunShaftsLight.m_MainLight.transform.forward;
            }
            Vector3 sunWPos = renderCamera.transform.position - dir * renderCamera.farClipPlane;
            Vector3 sunScreenPos = renderCamera.WorldToViewportPoint(sunWPos);
            if (sunScreenPos.z < 0.0f && m_SunShaftsLight.m_EnableViewDirOptimize)
            {
                return;
            }

            if (!renderingData.cameraData.postProcessEnabled)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilerTag);
            m_SourceRT = ForwardRendererExtend.GetActiveColorRenderTargetHandle();

            // pass 0
            Vector4 sunColor = new Vector4(m_SunShaftsLight.m_ShaftsColor.r,
                                    m_SunShaftsLight.m_ShaftsColor.g,
                                    m_SunShaftsLight.m_ShaftsColor.b,
                                    m_SunShaftsLight.m_ShaftsColor.a)
                                * m_SunShaftsLight.m_SunShaftIntensity * m_SunShaftsLight.m_MainLight.intensity;
            cmd.SetGlobalVector("_SunColor", sunColor);

            cmd.SetGlobalVector("_SunPosition", new Vector4(sunScreenPos.x, sunScreenPos.y, sunScreenPos.z, m_SunShaftsLight.m_SunRadius));
            cmd.SetGlobalVector("_SunThreshold", m_SunShaftsLight.m_SunThreshold);
            
            cmd.SetGlobalTexture("_GlobalMainTex", m_SourceRT.id);
            cmd.SetRenderTarget(new RenderTargetIdentifier(m_SunShaftsRT.Identifier(), 0, CubemapFace.Unknown, -1),
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, 0, MeshTopology.Quads, 4, 1, null);
            //cmd.Blit(m_SourceRT.id, m_SunShaftsRT.Identifier(), m_BlitMaterial, 0);

            // pass 1
            float ofs = m_SunShaftsLight.m_BlurRadius * (1f / 768f);
            cmd.SetGlobalVector("_BlurRadius4", new Vector4(ofs, ofs, 0.0f, 0.0f));
            for (int index = 0; index < m_SunShaftsLight.m_BlurIterations; index++)
            {
                cmd.SetGlobalTexture("_GlobalMainTex", m_SunShaftsRT.id);
                cmd.SetRenderTarget(new RenderTargetIdentifier(m_TempRT.Identifier(), 0, CubemapFace.Unknown, -1),
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, 1, MeshTopology.Quads, 4, 1, null);
                //cmd.Blit(m_SunShaftsRT.Identifier(), m_TempRT.Identifier(), m_BlitMaterial, 1);

                ofs = m_SunShaftsLight.m_BlurRadius * (((index * 2.0f + 1.0f) * 6.0f)) / 768.0f;
                cmd.SetGlobalVector("_BlurRadius4", new Vector4(ofs, ofs, 0.0f, 0.0f));
                
                cmd.SetGlobalTexture("_GlobalMainTex", m_TempRT.id);
                cmd.SetRenderTarget(new RenderTargetIdentifier(m_SunShaftsRT.Identifier(), 0, CubemapFace.Unknown, -1),
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, 1, MeshTopology.Quads, 4, 1, null);
                //cmd.Blit(m_TempRT.Identifier(), m_SunShaftsRT.Identifier(), m_BlitMaterial, 1);

                ofs = m_SunShaftsLight.m_BlurRadius * (((index * 2.0f + 2.0f) * 6.0f)) / 768.0f;
                cmd.SetGlobalVector("_BlurRadius4", new Vector4(ofs, ofs, 0.0f, 0.0f));
            }

            // pass 2
            if (ForwardRendererExtend.m_UseSwapFullScreenRT)
            {
                cmd.Blit(m_SourceRT.Identifier(), m_SourceTempRT.Identifier(), m_BlitMaterial,
                    (m_SunShaftsLight.m_ScreenBlendMode == SunLightManager.ShaftsScreenBlendMode.Screen) ? 3 : 2);
                ForwardRendererExtend.SwapFullScreenRT(cmd);
            }
            else
            {
                cmd.CopyTexture(m_SourceRT.Identifier(), m_SourceTempRT.Identifier());
                cmd.SetGlobalTexture("_GlobalMainTex", m_SourceTempRT.id);
                cmd.SetRenderTarget(new RenderTargetIdentifier(m_SourceRT.Identifier(), 0, CubemapFace.Unknown, -1),
                    RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, (m_SunShaftsLight.m_ScreenBlendMode == SunLightManager.ShaftsScreenBlendMode.Screen) ? 3 : 2, MeshTopology.Quads, 4, 1, null);

                //cmd.Blit(m_SourceRT.Identifier(), m_SourceTempRT.Identifier());
                //cmd.Blit(m_SourceTempRT.Identifier(), m_SourceRT.Identifier(), m_BlitMaterial,
                //     (m_SunShaftsLight.m_ScreenBlendMode == SunLightManager.ShaftsScreenBlendMode.Screen) ? 3 : 2);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CustomPass pass, CommandBuffer cmd)
        {
            base.FrameCleanup(pass, cmd);
            cmd.ReleaseTemporaryRT(m_SunShaftsRT.id);
            cmd.ReleaseTemporaryRT(m_TempRT.id);
        }

        public override void Release()
        {
            base.Release();
            if (m_BlitMaterial != null)
            {
                m_BlitMaterial = null;
            }
        }
    } 
}
