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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

namespace Matrix.EcosystemSimulate
{
    public class PostEffectPass : UserDefinedPass
    {
        private PostEffectManager m_Manager = null;
        private Material m_FullScreenMaterial = null;
        private Vector4 m_SourceSize = Vector4.zero;
        private ProfilingSampler m_ProfilingSampler;
        private ProfilingSampler m_ProfilingSamplerFullScreen;
        private static readonly ShaderTagId s_ShaderTagId = new ShaderTagId("UberPostEffect");

        private static List<PostEffectScript> s_PostEffectScriptList = new List<PostEffectScript>();
        private static readonly int s_SourceSize = Shader.PropertyToID("_SourceSize");

        public PostEffectPass(PostEffectManager manager, Camera camera, bool enablePass = true)
            : base(RenderPassEvent.BeforeRenderingPostProcessing, camera, enablePass)
        {
            m_Manager = manager;
            if (m_Manager != null && m_Manager.postEffectShader != null)
            {
                m_FullScreenMaterial = new Material(m_Manager.postEffectShader);
                m_FullScreenMaterial.EnableKeyword("_FULL_SCREEN");
            }

            m_ProfilingSampler = new ProfilingSampler("UberEffectPost");
            m_ProfilingSamplerFullScreen = new ProfilingSampler("UberEffectPostFullScreen");
        }

        public override int RequireFlag()
        {
            if (renderCamera != null && renderCamera.cullingMask == 0)
            {
                return 0;
            }

            //只在需要的时候请求半透明颜色贴图
            if (IsFullScreenPostEffectEnable() || IsRendererPostEffectEnable())
            {
                return ForwardRendererExtend.REQUIRE_TRANSPARENT_TEXTURE | ForwardRendererExtend.REQUIRE_CREATE_COLOR_TEXTURE;
            }

            return 0;
        }

        public override void Configure(CustomPass pass, CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            
        }

        public override void Execute(CustomPass pass, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            if (IsFullScreenPostEffectEnable() || IsRendererPostEffectEnable())
            {
                RenderTextureDescriptor renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
                float width = renderTextureDescriptor.width;
                float height = renderTextureDescriptor.height;
                if (renderTextureDescriptor.useDynamicScale)
                {
                    width *= ScalableBufferManager.widthScaleFactor;
                    height *= ScalableBufferManager.heightScaleFactor;
                }
                m_SourceSize = new Vector4(width, height, 1.0f / width, 1.0f / height);
                cmd.SetGlobalVector(s_SourceSize, m_SourceSize);
                context.ExecuteCommandBuffer(cmd);
            }

            if (IsRendererPostEffectEnable())
            {
                cmd.Clear();

                //cmd.SetRenderTarget(ForwardRendererExtend.GetActiveColorRenderTargetID(), RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                //    ForwardRendererExtend.GetActiveDepthRenderTargetID(), RenderBufferLoadAction.Load, RenderBufferStoreAction.DontCare);
                //context.ExecuteCommandBuffer(cmd);
                DrawPostEffectRenderer(context, ref renderingData);
            }

            if (IsFullScreenPostEffectEnable())
            {
                SetFullScreenPostEffect();
                DrawFullScreenPostEffect(context, ref renderingData);
            }
            CommandBufferPool.Release(cmd);
        }

        public void DrawPostEffectRenderer(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                SortingSettings sortingSettings = new SortingSettings(renderCamera) { criteria = SortingCriteria.CommonOpaque};
                DrawingSettings drawingSettings = new DrawingSettings(s_ShaderTagId, sortingSettings);
                FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
                //RenderStateBlock stateBlock = new RenderStateBlock(RenderStateMask.Depth);
                //stateBlock.depthState = new DepthState(true, CompareFunction.LessEqual);

                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings/*, ref stateBlock*/);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void DrawFullScreenPostEffect(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSamplerFullScreen))
            {
                cmd.DrawProcedural(Matrix4x4.identity, m_FullScreenMaterial, 0, MeshTopology.Quads, 4, 1);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private bool IsFullScreenPostEffectEnable()
        {
            if (m_Manager != null && m_Manager.enable && m_FullScreenMaterial != null)
            {
                return m_Manager.enableScreenDistortion || m_Manager.enableKawaseBlur || m_Manager.enableGrainyBlur ||
                       m_Manager.enableGlitchImageBlock || m_Manager.enableGlitchScreenShake || m_Manager.enableScreenDissolve;
            }

            return false;
        }

        private void SetFullScreenPostEffect()
        {
            if (m_FullScreenMaterial == null)
            {
                return;
            }

            if (m_Manager.enableScreenDistortion)
            {
                m_FullScreenMaterial.EnableKeyword("_SCREEN_DISTORTION");
                m_FullScreenMaterial.SetTexture("_ScreenDistortionTexture", m_Manager.screenDistortionTexture);
                m_FullScreenMaterial.SetTextureScale("_ScreenDistortionTexture", m_Manager.screenDistortionTextureScale);
                m_FullScreenMaterial.SetTextureOffset("_ScreenDistortionTexture", m_Manager.screenDistortionTextureOffset);
                m_FullScreenMaterial.SetFloat("_ScreenDistortionU", m_Manager.screenDistortionU);
                m_FullScreenMaterial.SetFloat("_ScreenDistortionV", m_Manager.screenDistortionV);
                m_FullScreenMaterial.SetFloat("_ScreenDistortStrength", m_Manager.screenDistortStrength);
            }
            else
            {
                m_FullScreenMaterial.DisableKeyword("_SCREEN_DISTORTION");
            }

            if (m_Manager.enableKawaseBlur)
            {
                m_FullScreenMaterial.EnableKeyword("_KAWASE_BLUR");
                m_FullScreenMaterial.SetFloat("_KawaseBlurRadius", m_Manager.kawaseBlurRadius);
                m_FullScreenMaterial.SetFloat("_KawaseBlurIteration", m_Manager.kawaseBlurIteration);
                m_FullScreenMaterial.SetFloat("_KawaseBlurEdgeWeakDistance", m_Manager.kawaseBlurEdgeWeakDistance);
            }
            else
            {
                m_FullScreenMaterial.DisableKeyword("_KAWASE_BLUR");
            }

            if (m_Manager.enableGrainyBlur)
            {
                m_FullScreenMaterial.EnableKeyword("_GRAINY_BLUR");
                m_FullScreenMaterial.SetFloat("_GrainyBlurRadius", m_Manager.grainyRadius);
                m_FullScreenMaterial.SetFloat("_GrainyBlurIteration", m_Manager.grainyBlurIteration);
                m_FullScreenMaterial.SetFloat("_GrainyBlurEdgeWeakDistance", m_Manager.grainyBlurEdgeWeakDistance);
            }
            else
            {
                m_FullScreenMaterial.DisableKeyword("_GRAINY_BLUR");
            }

            if (m_Manager.enableGlitchImageBlock)
            {
                m_FullScreenMaterial.EnableKeyword("_GLITCH_IMAGE_BLOCK");
                m_FullScreenMaterial.SetFloat("_GlitchImageBlockSpeed", m_Manager.glitchImageBlockSpeed);
                m_FullScreenMaterial.SetFloat("_GlitchImageBlockSize", m_Manager.glitchImageBlockSize);
                m_FullScreenMaterial.SetFloat("_GlitchImageBlockMaxRGBSplitX", m_Manager.glitchImageBlockMaxRGBSplitX);
                m_FullScreenMaterial.SetFloat("_GlitchImageBlockMaxRGBSplitY", m_Manager.glitchImageBlockMaxRGBSplitY);
            }
            else
            {
                m_FullScreenMaterial.DisableKeyword("_GLITCH_IMAGE_BLOCK");
            }

            if (m_Manager.enableGlitchScreenShake)
            {
                m_FullScreenMaterial.EnableKeyword("_GLITCH_SCREEN_SHAKE");
                m_FullScreenMaterial.SetFloat("_GlitchScreenShakeIndensityX", m_Manager.glitchScreenShakeIndensityX);
                m_FullScreenMaterial.SetFloat("_GlitchScreenShakeIndensityY", m_Manager.glitchScreenShakeIndensityY);
            }
            else
            {
                m_FullScreenMaterial.DisableKeyword("_GLITCH_SCREEN_SHAKE");
            }

            if (m_Manager.enableScreenDissolve)
            {
                m_FullScreenMaterial.EnableKeyword("_SCREEN_DISSOLVE");
                m_FullScreenMaterial.SetTexture("_DissolveTex", m_Manager.dissolveTexture);
                m_FullScreenMaterial.SetTextureScale("_DissolveTex", m_Manager.dissolveTextureScale);
                m_FullScreenMaterial.SetTextureOffset("_DissolveTex", m_Manager.dissolveTextureOffset);
                m_FullScreenMaterial.SetFloat("_DissolveWidth", m_Manager.dissolveWidth);
                m_FullScreenMaterial.SetFloat("_DissolveProcess", m_Manager.dissolveProcess);
                m_FullScreenMaterial.SetVector("_DissolveEdgeColor", m_Manager.dissolveEdgeColor);
                m_Manager.dissolveBackgroundColor.a = Application.isEditor ? m_Manager.dissolveBackgroundColor.a : 0;
                m_FullScreenMaterial.SetVector("_DissolveBackgroundColor", m_Manager.dissolveBackgroundColor);
                m_FullScreenMaterial.SetFloat("_DissolveHardEdge", m_Manager.dissolveHardEdge ? 1 : 0);
                m_FullScreenMaterial.SetFloat("_InvertDissolve", m_Manager.invertDissolve ? 1 : 0);
                m_FullScreenMaterial.SetVector("_lensDistortionParams",
                    new Vector4(m_Manager.lensDistortionStrength, m_Manager.lensDistortionIntensity, 0,
                        m_Manager.lensDistortionRange));
            }
            else
            {
                m_FullScreenMaterial.DisableKeyword("_SCREEN_DISSOLVE");
            }
        }

        private bool IsRendererPostEffectEnable()
        {
            return m_Manager.enable && s_PostEffectScriptList.Count > 0;
        }

        public static void AddPostEffectScript(PostEffectScript postEffectScript)
        {
            if (!s_PostEffectScriptList.Contains(postEffectScript))
            {
                s_PostEffectScriptList.Add(postEffectScript);
            }
        }

        public static void RemovePostEffectScript(PostEffectScript postEffectScript)
        {
            s_PostEffectScriptList.Remove(postEffectScript);
        }
        
        public override void Release()
        {
            base.Release();

            if (m_FullScreenMaterial != null)
            {
                m_FullScreenMaterial = null;
            }
        }
    }
}