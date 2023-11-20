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
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public delegate int CustomCommonRequireFlagDelegate();
    public delegate void CustomSetupAction(RenderTextureDescriptor cameraTargetDescriptor);
    public delegate void CustomConfigureAction(CustomPass pass, CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor);
    public delegate void CustomExecuteAction(CustomPass pass, ScriptableRenderContext context, ref RenderingData renderingData);
    public delegate void CustomFrameCleanupAction(CustomPass pass, CommandBuffer cmd);

    public static class ForwardRendererExtend
    {
        // Use Swap Full Screen RenderTexture To Optimize Performance
        public static bool m_UseSwapFullScreenRT = false;

        // 切换画质花屏尝试修复测试
        private static bool m_RefreshColorGradingLutPass = false;
        public static bool refreshColorGradingLutPass {
            set {
                m_RefreshColorGradingLutPass = value;
            }
            get {
                return m_RefreshColorGradingLutPass;
            }
        }

        private static RenderTargetHandle m_ActiveCameraColorAttachment;
        private static RenderTargetHandle m_ActiveCameraDepthAttachment;
        private static RenderTargetHandle m_FullScreenTempRT;
        private static int m_CurActiveColorRT = 0;//0 - m_ActiveCameraColorAttachment, 1 - _CameraColorTextureSwapped
        private static RenderTextureDescriptor m_FullScreenTempRTDescriptor;
        private static bool m_FullScreenTempRTCreated = false;

        public const int REQUIRE_DEPTH_TEXTURE = 0x1 << 0;
        public const int REQUIRE_DEPTH_PREPASS = 0x1 << 1;
        public const int REQUIRE_OPAQUE_TEXTURE = 0x1 << 2;
        public const int REQUIRE_TRANSPARENT_TEXTURE = 0x1 << 3;
        public const int REQUIRE_CREATE_COLOR_TEXTURE = 0x1 << 5;
        public const int REQUIRE_DEPTH_SETCIL_IN_POST = 0x1 << 7;
        public const int SUPPORT_SUBPASS = 0x1 << 8;
        public const int REQUIRE_FLAG_COUNT = 7;

        private static Dictionary<Camera, List<CustomCommonRequireFlagDelegate>> m_CustomCommonRequireFlagDict = new Dictionary<Camera, List<CustomCommonRequireFlagDelegate>>();
        private static Dictionary<Camera, List<CustomPass>> m_CustomPassDict = new Dictionary<Camera, List<CustomPass>>();
        private static List<CustomPass> m_EmptyPassList = new List<CustomPass>();

        public static void RegisterRequiredDelegate(Camera camera, CustomCommonRequireFlagDelegate flagDelegate)
        {
            if (camera != null && flagDelegate != null)
            {
                List<CustomCommonRequireFlagDelegate> requireFlagList = null;
                if (!m_CustomCommonRequireFlagDict.TryGetValue(camera, out requireFlagList))
                {
                    requireFlagList = new List<CustomCommonRequireFlagDelegate>();
                    m_CustomCommonRequireFlagDict.Add(camera, requireFlagList);
                }
                if (!requireFlagList.Contains(flagDelegate))
                {
                    requireFlagList.Add(flagDelegate);
                }
            }
        }

        public static void UnRegisterRequiredDelegate(Camera camera, CustomCommonRequireFlagDelegate flagDelegate)
        {
            if (camera != null && flagDelegate != null)
            {
                List<CustomCommonRequireFlagDelegate> requireFlagList = null;
                if (m_CustomCommonRequireFlagDict.TryGetValue(camera, out requireFlagList))
                {
                    for (int i = 0; i < requireFlagList.Count; ++i)
                    {
                        if (requireFlagList[i] == flagDelegate)
                        {
                            requireFlagList.RemoveAt(i);
                            return;
                        }
                    }
                }
            }
        }

        public static void AddCustomPass(Camera camera, CustomPass userDefinedPass)
        {
            if (camera == null || userDefinedPass == null) {
                if(Application.isPlaying)
                    Debug.LogError("AddCustomPass Failed, Camera or CustomPass is null");
                return;
            }
            List<CustomPass> passList = null;
            if (!m_CustomPassDict.TryGetValue(camera, out passList))
            {
                passList = new List<CustomPass>();
                m_CustomPassDict.Add(camera, passList);
            }
            passList.Add(userDefinedPass);
        }

        public static void RemoveCustomPass(Camera camera, CustomPass userDefinedPass)
        {
            if (camera == null || userDefinedPass == null)
            {
                if(Application.isPlaying)
                    Debug.LogError("RemoveCustomPass Failed, Camera or CustomPass is null");
                return;
            }
            List<CustomPass> passList = null;
            if (m_CustomPassDict.TryGetValue(camera, out passList))
            {
                passList.Remove(userDefinedPass);
            }
        }

        public static List<CustomPass> GetCustomPassList(Camera camera)
        {
            List<CustomPass> passList = null;
            if (m_CustomPassDict.TryGetValue(camera, out passList))
            {
                return passList;
            }
            return m_EmptyPassList;
        }

        public static void CopyCameraCustomPassesAndFlag(Camera from, Camera to)
        {
            m_CustomPassDict.TryGetValue(from, out List<CustomPass> passList);
            if (passList != null)
            {
                m_CustomPassDict.Add(to , passList);
            }

            m_CustomCommonRequireFlagDict.TryGetValue(from, out List<CustomCommonRequireFlagDelegate> flagList);
            if (flagList != null)
            {
                m_CustomCommonRequireFlagDict.Add(to , flagList);
            }
        }
        
        public static bool GetFlag(int flag, int compare)
        {
            if ((flag & compare) > 0)
            {
                return true;
            }
            return false;
        }

        public static int GetRequirementFlag(Camera camera, bool isAll = false)
        {
            int flag = 0;
            if (isAll)
            {
                flag = -1;
            }
            List<CustomPass> passList = null;
            if (m_CustomPassDict.TryGetValue(camera, out passList))
            {
                for (int i = 0; i < passList.Count; ++i)
                {
                    if (isAll)
                    {
                        flag &= passList[i].RequireFlag();
                    }
                    else
                    {
                        flag |= passList[i].RequireFlag();
                    }
                }
            }

            List<CustomCommonRequireFlagDelegate> requireFlagList = null;
            if (m_CustomCommonRequireFlagDict.TryGetValue(camera, out requireFlagList))
            {
                for (int i = 0; i < requireFlagList.Count; ++i)
                {
                    if (requireFlagList[i] != null)
                    {
                        if (isAll)
                        {
                            flag &= requireFlagList[i]();
                        }
                        else
                        {
                            flag |= requireFlagList[i]();
                        }
                    }
                    else
                    {
                        requireFlagList.RemoveAt(i);
                    }
                }
            }

            return flag;
        }

        public static void Setup(Camera camera, RenderTextureDescriptor cameraTargetDescriptor)
        {
            List<CustomPass> passList = null;
            if (m_CustomPassDict.TryGetValue(camera, out passList))
            {
                for (int i = 0; i < passList.Count; ++i)
                {
                    passList[i].Setup(cameraTargetDescriptor);
                }
            }
        }

        public static void InitFullScreenTempRT(RenderTextureDescriptor renderTextureDescriptor, RenderTargetHandle activeCameraColorAttachment, RenderTargetHandle activeCameraDepthAttachment)
        {
            m_ActiveCameraColorAttachment = activeCameraColorAttachment;
            m_ActiveCameraDepthAttachment = activeCameraDepthAttachment;
            m_FullScreenTempRTDescriptor = renderTextureDescriptor;
            m_FullScreenTempRTDescriptor.depthBufferBits = activeCameraDepthAttachment == RenderTargetHandle.CameraTarget ? 32 : 0;
            m_FullScreenTempRTCreated = false;
            m_CurActiveColorRT = 0;
        }

        public static int GetFullScreenTempRT(CommandBuffer cmd)
        {
            if (!m_FullScreenTempRTCreated)
            {
                m_FullScreenTempRTCreated = true;
                m_FullScreenTempRT.Init("_CameraColorTextureSwapped");
                cmd.GetTemporaryRT(m_FullScreenTempRT.id, m_FullScreenTempRTDescriptor, FilterMode.Bilinear);
            }

            if (m_UseSwapFullScreenRT)
            {
                if (m_CurActiveColorRT == 0)
                {
                    return m_FullScreenTempRT.id;
                }
                else
                {
                    return m_ActiveCameraColorAttachment.id;
                }
            }

            return m_FullScreenTempRT.id;
        }

        public static RenderTargetHandle GetActiveColorRenderTargetHandle()
        {
            if (m_CurActiveColorRT == 0)
            {
                return m_ActiveCameraColorAttachment;
            }
            return m_FullScreenTempRT;
        }

        public static RenderTextureDescriptor GetActiveColorTextureDescriptor()
        {
            return m_FullScreenTempRTDescriptor;
        }

        public static RenderTargetHandle GetActiveDepthRenderTargetHandle()
        {
            return m_ActiveCameraDepthAttachment;
        }

        public static int GetActiveColorRenderTargetID()
        {
            if (m_CurActiveColorRT == 0)
            {
                return m_ActiveCameraColorAttachment.id;
            }
            return m_FullScreenTempRT.id;
        }

        public static int GetActiveDepthRenderTargetID()
        {
            return m_ActiveCameraDepthAttachment.id;
        }

        public static void SwapFullScreenRT(CommandBuffer cmd)
        {
            if (m_FullScreenTempRTCreated)
            {
                m_CurActiveColorRT = 1 - m_CurActiveColorRT;
                if (m_CurActiveColorRT == 0)
                {
                    cmd.SetRenderTarget(m_ActiveCameraColorAttachment.id, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, m_ActiveCameraDepthAttachment.id, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                }
                else
                {
                    cmd.SetRenderTarget(m_FullScreenTempRT.id, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, m_ActiveCameraDepthAttachment.id, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                }
            }
        }

        public static void UninitFullScreenTempRT(CommandBuffer cmd)
        {
            if (m_FullScreenTempRTCreated)
            {
                cmd.ReleaseTemporaryRT(m_FullScreenTempRT.id);
            }
        }

        public static void SetSinglePassActive(CameraData data, CommandBuffer cmd, bool isActive)
        {
            if (data.xr.enabled)
            {
                if (isActive)
                {
                    data.xr.StartSinglePass(cmd);
                }
                else
                {
                    data.xr.StopSinglePass(cmd);
                }
            }
        }

        public static bool DrawRenderersIsValid(ScriptableRenderContext context, CullingResults cullingResults, ref DrawingSettings drawingSettings, ref FilteringSettings filteringSettings)
        {
            #if PICO_VIDEO_VR_SUBPASS
            bool result = context.DrawRenderersIsValid(cullingResults, ref drawingSettings, ref filteringSettings);
            return result;
            #endif
            
            return true;
        }
    }
}