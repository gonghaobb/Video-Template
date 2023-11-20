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

namespace UnityEngine.Rendering.Universal
{
    public static class PipelineAnalyze
    {
        private static PipelineState s_PipelineState = new PipelineState();
        private static IPipelineEncoder s_CurMaskPipelineEncoder = new MaskPipelineEncoder();
        private static IPipelineEncoder s_LastMaskPipelineEncoder = new MaskPipelineEncoder();
        private static bool s_Inited = false;
        private static float s_LastPrintfTime = 0;
        private static float s_IntervalTime = 10;   //单位(s)
        
#if MATRIX_UNIT_TEST
        private static PipelineState s_UniteTestPipelineState = new PipelineState();
#endif
        
        public static void InitPipelineState()
        {
            s_PipelineState.Clear();
        }

        public static void FinishPipelineState()
        {
            if (!s_Inited)
            {
                s_Inited = true;
                s_LastMaskPipelineEncoder.Encode(ref s_PipelineState);
                s_LastMaskPipelineEncoder.Printf();
            }
            else
            {
                s_CurMaskPipelineEncoder.Encode(ref s_PipelineState);
                if (!s_LastMaskPipelineEncoder.Equals(s_CurMaskPipelineEncoder))
                {
                    (s_LastMaskPipelineEncoder, s_CurMaskPipelineEncoder) = (s_CurMaskPipelineEncoder, s_LastMaskPipelineEncoder);
                    s_LastMaskPipelineEncoder.Printf();
                    //s_PipelineState.Printf();
                    
#if MATRIX_UNIT_TEST
                    s_UniteTestPipelineState.Clear();
                    s_LastMaskPipelineEncoder.Decode(ref s_UniteTestPipelineState);
                    if (!s_UniteTestPipelineState.Equals(s_PipelineState))
                    {
                        UnitTestCore.Failed($"PiplelineAnalyze encode or decode error");
                        s_PipelineState.Printf();
                        s_UniteTestPipelineState.Printf();
                    }
#endif
                }
                else
                {
                    TryPrintf();
                }
            }
        }

        public static void TryPrintf()
        {
            var curTime = Time.realtimeSinceStartup;
            if (curTime - s_LastPrintfTime > s_IntervalTime)
            {
                s_LastPrintfTime = curTime;
                s_LastMaskPipelineEncoder.Printf();
            }
        }

        public static void AddRenderPass(PipelineState.RenderPassType renderPass)
        {
            s_PipelineState.AddRenderPass(renderPass);
        }

        public static void SetEyeBufferSize(float value)
        {
            s_PipelineState.eyeBufferSize = value;
        }

        public static void SetMSAALevel(int value)
        {
            s_PipelineState.msaaLevel = value;
        }

        public static void SetFFRLevel(TextureFoveatedFeatureQuality value)
        {
            s_PipelineState.ffrLevel = value;
        }

        public static void EnableSubsampledLayout()
        {
            s_PipelineState.subsampledLayout = true;
        }
        
        public static void EnableSRPBatch()
        {
            s_PipelineState.srpBatch = true;
        }
        
        public static void EnableHDR()
        {
            s_PipelineState.hdr = true;
        }
        
        public static void EnableRenderFeature()
        {
            s_PipelineState.useRenderFeature = true;
        }
        
        public static void EnableOverlayCamera()
        {
            s_PipelineState.useOverlayCamera = true;
        }

        public static void SetSubPassState(bool isOpen, bool isUseless, bool isAssetClose, bool isCustomPassUnSupport)
        {
            s_PipelineState.SetSubPassState(isOpen, isUseless, isAssetClose, isCustomPassUnSupport);
        }
    }
}