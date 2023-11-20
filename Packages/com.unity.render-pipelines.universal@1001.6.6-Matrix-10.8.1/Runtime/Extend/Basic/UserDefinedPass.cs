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

using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public abstract class UserDefinedPass
    {
        private CustomPass m_CustomPass = null;
        private Camera m_Camera = null;
        public Camera renderCamera
        {
            get => m_Camera;
            set
            {
                if (value != m_Camera)
                {
                    ForwardRendererExtend.RemoveCustomPass(m_Camera, m_CustomPass);
                    m_Camera = value;
                    ForwardRendererExtend.AddCustomPass(m_Camera, m_CustomPass);
                }
            }
        }

        private bool m_Enable = false;
        public bool enable
        {
            get => m_Enable;
            set
            {
                if (value != m_Enable)
                {
                    m_Enable = value;
                    if (m_Enable)
                    {
                        ForwardRendererExtend.AddCustomPass(m_Camera, m_CustomPass);
                    }
                    else
                    {
                        ForwardRendererExtend.RemoveCustomPass(m_Camera, m_CustomPass);
                    }
                }
            }
        }

        public UserDefinedPass(RenderPassEvent renderEvent, Camera camera, bool enablePass = true)
        {
            m_Camera = camera;
            m_CustomPass = new CustomPass(renderEvent, this);
            m_CustomPass.SetActions(RequireFlag,
                                    /*RequireDepthTexture, RequireDepthPrepass, RequireCreateColorTexture, RequireTransparentDepthTexture, RequireTransparentColorTexture, RequireTransparentColorTextureBeforePostProcess,*/
                                    Setup, Configure, Execute, FrameCleanup);
            if (enablePass)
            {
                ForwardRendererExtend.AddCustomPass(m_Camera, m_CustomPass);
            }
            m_Enable = enablePass;
        }

        public virtual void Release()
        {
            m_CustomPass.ClearActions();
            if (m_Enable)
            {
                ForwardRendererExtend.RemoveCustomPass(m_Camera, m_CustomPass);
            }
        }

        public virtual int RequireFlag()
        {
            return 0;
        }

        public virtual void Setup(RenderTextureDescriptor cameraTargetDescriptor)
        {

        }

        public abstract void Configure(CustomPass pass, CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor);
        public abstract void Execute(CustomPass pass, ScriptableRenderContext context, ref RenderingData renderingData);

        public virtual void FrameCleanup(CustomPass pass, CommandBuffer cmd)
        {

        }
    }
}

