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

using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    public enum SubPassType
    {
        RenderToFinalColor,
        RenderToSwapColor,
        RenderToFinalColorWithPreviousResult,
        RenderToSwapColorWithPreviousResult
    }
    
    public static class SubPassManager
    {
        public static int SUBPASS_ATTACH_BEGIN_COLOR = int.MaxValue;
        public static int SUBPASS_ATTACH_FINAL_COLOR = int.MaxValue;
        public static int SUBPASS_ATTACH_SWAP1_COLOR = int.MaxValue;
        public static int SUBPASS_ATTACH_SWAP2_COLOR = int.MaxValue;
        public static int SUBPASS_ATTACH_DEPTH = int.MaxValue;

        private static int s_CurSubPassSwap;
        private static int s_CurSubPassInput;
        
        private static bool s_IsInsideRenderPass = false;
        private static bool s_IsInsideSubPass = false;
        private static bool s_AllowSubPass = true;//当前帧是否允许执行SubPass模式
        private static bool s_NeedSubPass = false;//当前帧是否有模块请求需求SubPass模式
        public static bool needSubPass
        {
            get => s_NeedSubPass;
        }
        private static Camera s_CurCamera = null;

        public static bool isInsideRenderPass
        {
            get => s_IsInsideRenderPass;
        }

        public static bool supported
        {
            get
            {
#if PICO_VIDEO_VR_SUBPASS
                if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan &&
                    UniversalRenderPipeline.asset.GetOptimizedRenderPipelineFeatureActivated(OptimizedRenderPipelineFeature.SubPass) && s_AllowSubPass)
                {
                    return true; 
                }
#endif
                return false;
            }
        }
        
        public static void Init(Camera curCamera)
        {
            s_IsInsideRenderPass = false;
            s_IsInsideSubPass = false;
            s_AllowSubPass = true;
            s_NeedSubPass = false;
            s_CurCamera = curCamera;
        }

        public static void NeedSubPass()
        {
            s_NeedSubPass = true;
        }

        public static void DisableSubPass(string errorMsg = null)
        {
            if (!string.IsNullOrEmpty(errorMsg))
            {
                //Debug.Log($"{s_CurCamera?.name} DisableSubPass : {errorMsg}");
            }
            s_AllowSubPass = false;
        }

        public static void BeginRenderPass(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!supported)
            {
                return;
            }
            if (!s_NeedSubPass)
            {
                return;
            }

            if (!s_IsInsideRenderPass)
            {
                var clearColor =
                    CoreUtils.ConvertSRGBToActiveColorSpace(renderingData.cameraData.camera.backgroundColor);
                var isMultiView = renderingData.cameraData.xr.singlePassEnabled;
                
                var backColorTexture = ForwardRendererExtend.GetActiveColorRenderTargetHandle();
                var backColorDescriptor = ForwardRendererExtend.GetActiveColorTextureDescriptor();
               
                //var start = new AttachmentDescriptor(backColorDescriptor.colorFormat);
                var swap1 = new AttachmentDescriptor(backColorDescriptor.colorFormat);
                var swap2 = new AttachmentDescriptor(backColorDescriptor.colorFormat);
                var final = new AttachmentDescriptor(backColorDescriptor.colorFormat);
                var depth = new AttachmentDescriptor(RenderTextureFormat.Depth);
                depth.ConfigureClear(new Color(), 1.0f, 0);
#if PICO_VIDEO_VR_SUBPASS
                depth.isMultiView = isMultiView;
                final.isMultiView = isMultiView;
                swap1.isMultiView = isMultiView;
                swap2.isMultiView = isMultiView;
#endif
#if PICO_VIDEO_VR_SUBPASS2
                swap1.useFoveatedImange = UniversalRenderPipeline.asset.enableTextureFoveatedFeature;
                swap2.useFoveatedImange = UniversalRenderPipeline.asset.enableTextureFoveatedFeature; 
#endif
                if (backColorDescriptor.msaaSamples > 1)
                {
                    final.ConfigureResolveTarget(new RenderTargetIdentifier(backColorTexture.Identifier(), 0, CubemapFace.Unknown, -1));
                    #if PICO_VIDEO_VR_SUBPASS2
                    final.useFoveatedImange = UniversalRenderPipeline.asset.enableTextureFoveatedFeature;
                    #endif
                    final.loadStoreTarget =
                        new RenderTargetIdentifier(BuiltinRenderTextureType.None, 0, CubemapFace.Unknown, -1);
                }
                else
                {
                    final.ConfigureTarget(new RenderTargetIdentifier(backColorTexture.Identifier(), 0, CubemapFace.Unknown, -1), false, true);
                }
                swap1.ConfigureClear(clearColor);
                NativeArray<AttachmentDescriptor> attachments;
                // if (false)
                // {
                //     attachments = new NativeArray<AttachmentDescriptor>(3, Allocator.Temp);
                //     attachments[0] = depth;
                //     attachments[1] = final;
                //     attachments[2] = swap1;
                //     SUBPASS_ATTACH_SWAP1_COLOR = 1;
                //     SUBPASS_ATTACH_SWAP2_COLOR = 2;
                //     SUBPASS_ATTACH_FINAL_COLOR = 1;
                //     SUBPASS_ATTACH_DEPTH = 0;
                // }
                // else
                // {
                    attachments = new NativeArray<AttachmentDescriptor>(4, Allocator.Temp);
                    attachments[0] = depth;
                    attachments[1] = final;
                    attachments[2] = swap1;
                    attachments[3] = swap2;
                    SUBPASS_ATTACH_SWAP1_COLOR = 2;
                    SUBPASS_ATTACH_SWAP2_COLOR = 3;
                    SUBPASS_ATTACH_FINAL_COLOR = 1;
                    SUBPASS_ATTACH_DEPTH = 0;
                //}

                s_CurSubPassInput = SUBPASS_ATTACH_BEGIN_COLOR;
                s_CurSubPassSwap = SUBPASS_ATTACH_SWAP1_COLOR;

                context.BeginRenderPass(backColorDescriptor.width, backColorDescriptor.height, backColorDescriptor.msaaSamples, attachments, SUBPASS_ATTACH_DEPTH);
                s_IsInsideRenderPass = true;
                
                CommandBuffer cmd = CommandBufferPool.Get();
                cmd.SetViewport(new Rect(0, 0, backColorDescriptor.width, backColorDescriptor.height));
                cmd.DisableShaderKeyword("_SubPassMSAA1");
                cmd.DisableShaderKeyword("_SubPassMSAA2");
                cmd.DisableShaderKeyword("_SubPassMSAA4");
                if (backColorDescriptor.msaaSamples == 1)
                {
                    cmd.EnableShaderKeyword("_SubPassMSAA1");
                }
                if (backColorDescriptor.msaaSamples == 2)
                {
                    cmd.EnableShaderKeyword("_SubPassMSAA2");
                }
                else if (backColorDescriptor.msaaSamples == 4)
                {
                    cmd.EnableShaderKeyword("_SubPassMSAA4");
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        public static void BeginSubPass(ScriptableRenderContext context, SubPassType subPassType)
        {
            if (s_IsInsideRenderPass)
            {
                if (s_IsInsideSubPass)
                {
                    context.EndSubPass();
                }
                var outputBuffers = new NativeArray<int>(1, Allocator.Temp);
                var inputBuffers = new NativeArray<int>(1, Allocator.Temp);
                switch (subPassType)
                {
                    case SubPassType.RenderToFinalColor:
                        outputBuffers[0] = SUBPASS_ATTACH_FINAL_COLOR;
                        context.BeginSubPass(outputBuffers);
                        break;
                    case SubPassType.RenderToFinalColorWithPreviousResult:
                        outputBuffers[0] = SUBPASS_ATTACH_FINAL_COLOR;
                        inputBuffers[0] = s_CurSubPassInput;
                        context.BeginSubPass(outputBuffers, inputBuffers);
                        break;
                    case SubPassType.RenderToSwapColor:
                        outputBuffers[0] = s_CurSubPassSwap;
                        s_CurSubPassInput = s_CurSubPassSwap;
                        s_CurSubPassSwap = 1 - s_CurSubPassSwap + SUBPASS_ATTACH_SWAP1_COLOR * 2;
                        context.BeginSubPass(outputBuffers);
                        break;
                    case SubPassType.RenderToSwapColorWithPreviousResult:
                        outputBuffers[0] = s_CurSubPassSwap;
                        inputBuffers[0] = s_CurSubPassInput;
                        s_CurSubPassInput = s_CurSubPassSwap;
                        s_CurSubPassSwap = 1 - s_CurSubPassSwap + SUBPASS_ATTACH_SWAP1_COLOR * 2;
                        context.BeginSubPass(outputBuffers, inputBuffers);
                        break;
                }
                
                s_IsInsideSubPass = true;
            }
        }

        public static void EndRenderPass(ScriptableRenderContext context)
        {
            if (s_IsInsideRenderPass)
            {
                if (s_IsInsideSubPass)
                {
                    context.EndSubPass();
                }
                
                context.EndRenderPass();
                s_IsInsideRenderPass = false;
                s_IsInsideSubPass = false;
            }
        }
    }
}