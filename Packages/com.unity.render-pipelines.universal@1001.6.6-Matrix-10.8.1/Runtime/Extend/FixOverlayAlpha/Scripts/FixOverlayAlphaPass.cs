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
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class FixOverlayAlphaPass : UserDefinedPass
    {
        private ProfilingSampler m_ProfilingSampler;
        private static Material s_FixAlphaMaterial;
        private static readonly int s_StencilRef = Shader.PropertyToID("_StencilRef");

        public FixOverlayAlphaPass(Camera camera, bool enable = true) : base(
            RenderPassEvent.AfterRenderingTransparents, camera, enable)
        {
            m_ProfilingSampler = new ProfilingSampler("FixOverlayAlphaPass");
            if (s_FixAlphaMaterial == null)
            {
                s_FixAlphaMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Hidden/FixOverlayAlpha"));
            }
        }

        public override void Configure(CustomPass pass, CommandBuffer cmd,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        public override void Execute(CustomPass pass, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.renderType != CameraRenderType.Base || s_FixAlphaMaterial == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                s_FixAlphaMaterial.SetFloat(s_StencilRef, FixOverlayAlphaManager.instance.stencilRef);
                cmd.DrawProcedural(Matrix4x4.identity, s_FixAlphaMaterial, 0, MeshTopology.Quads, 4, 1, null);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void Release()
        {
            base.Release();
            if (s_FixAlphaMaterial != null)
            {
                s_FixAlphaMaterial = null;
            }
        }
    }
}