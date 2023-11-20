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
    public class CustomPass : ScriptableRenderPass
    {
        private UserDefinedPass m_UserDefinedPass = null;
        private CustomCommonRequireFlagDelegate m_CustomCommonRequireFlagDelegate = null;
        private CustomSetupAction m_SetupAction = null;
        private CustomConfigureAction m_ConfigureAction = null;
        private CustomExecuteAction m_ExcuteAction = null;
        private CustomFrameCleanupAction m_FrameCleanupAction = null;

        public CustomPass(RenderPassEvent renderEvent, UserDefinedPass userDefinedPass)
        {
            renderPassEvent = renderEvent;
            m_UserDefinedPass = userDefinedPass;
        }

        public void SetActions(CustomCommonRequireFlagDelegate customCommonRequireFlagDelegate,
                               CustomSetupAction customSetupAction,
                               CustomConfigureAction customConfigureAction,
                               CustomExecuteAction customExecuteAction,
                               CustomFrameCleanupAction customFrameCleanupAction)
        {
            m_CustomCommonRequireFlagDelegate = customCommonRequireFlagDelegate;
            m_SetupAction = customSetupAction;
            m_ConfigureAction = customConfigureAction;
            m_ExcuteAction = customExecuteAction;
            m_FrameCleanupAction = customFrameCleanupAction;
        }

        public void ClearActions()
        {
            m_CustomCommonRequireFlagDelegate = null;
            m_SetupAction = null;
            m_ConfigureAction = null;
            m_ExcuteAction = null;
            m_FrameCleanupAction = null;
        }

        public int RequireFlag()
        {
            if (m_CustomCommonRequireFlagDelegate != null)
            {
                return m_CustomCommonRequireFlagDelegate();
            }
            return 0;
        }

        public UserDefinedPass GetUserDefinedPass()
        {
            return m_UserDefinedPass;
        }

        public void Setup(RenderTextureDescriptor cameraTargetDescriptor)
        {
            if (m_SetupAction != null)
            {
                m_SetupAction(cameraTargetDescriptor);
            }
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            if (m_ConfigureAction != null)
            {
                m_ConfigureAction(this, cmd, cameraTextureDescriptor);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_ExcuteAction != null)
            {
                m_ExcuteAction(this, context, ref renderingData);
            }
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
            {
                throw new ArgumentNullException("cmd");
            }
            if (m_FrameCleanupAction != null)
            {
                m_FrameCleanupAction(this, cmd);
            }
        }
    }
}