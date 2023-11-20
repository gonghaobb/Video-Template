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
    public class MaskPipelineEncoder : IPipelineEncoder
    {
        private uint m_CurPiplineMaskValue = 0;
        public uint pipelineMaskValue
        {
            get => m_CurPiplineMaskValue;
            set => m_CurPiplineMaskValue = value;
        }
        
        private const int RENDERPASS_BITS = 0;
        private void SetRenderPass(PipelineState.RenderPassType passType)
        {
            m_CurPiplineMaskValue |= ((uint)0x1 << (int)passType) << RENDERPASS_BITS;
        }
        private bool GetRenderPass(PipelineState.RenderPassType passType)
        {
            uint value = m_CurPiplineMaskValue & (((uint)0x1 << (int)passType) << RENDERPASS_BITS);
            return value > 0;
        }

        private const int EYEBUFFER_BITS = 15;
        private void SetEyeBufferSize(float scale)
        {
            uint value;
            if (scale <= 1f)
            {
                value = 0x0;
            }
            else if (scale <= 1.1f)
            {
                value = 0x1;
            }
            else if (scale <= 1.2f)
            {
                value = 0x2;
            }
            else if (scale <= 1.25f)
            {
                value = 0x3;
            }
            else if (scale <= 1.3f)
            {
                value = 0x4;
            }
            else if (scale <= 1.35f)
            {
                value = 0x5;
            }
            else if (scale <= 1.4f)
            {
                value = 0x6;
            }
            else
            {
                value = 0x7;
            }

            m_CurPiplineMaskValue |= value << EYEBUFFER_BITS;
        }
        private float GetEyeBufferSize()
        {
            uint value = (m_CurPiplineMaskValue >> EYEBUFFER_BITS) & 0x7;
            if (value == 0)
            {
                return 1.0f;
            }
            else if (value == 1)
            {
                return 1.1f;
            }
            else if (value == 2)
            {
                return 1.2f;
            }
            else if (value == 3)
            {
                return 1.25f;
            }
            else if (value == 4)
            {
                return 1.3f;
            }
            else if (value == 5)
            {
                return 1.35f;
            }
            else if (value == 6)
            {
                return 1.4f;
            }
            else
            {
                return 1.5f;
            }
        }

        private const int MASS_BITS = 18;
        private void SetMSAALevel(int level)
        {
            uint value;
            if (level == 1)
            {
                value = 0x0;
            }
            else if (level == 2)
            {
                value = 0x1;
            }
            else if (level == 4)
            {
                value = 0x2;
            }
            else
            {
                value = 0x3;
            }
            
            m_CurPiplineMaskValue |= value << MASS_BITS;
        }
        private int GetMSAALevel()
        {
            uint value = (m_CurPiplineMaskValue >> MASS_BITS) & 0x3;
            if (value == 0)
            {
                return 1;
            }
            else if (value == 1)
            {
                return 2;
            }
            else if (value == 2)
            {
                return 4;
            }
            else
            {
                return 8;
            }
        }
        
        private const int FFR_BITS = 20;
        private void SetFFRLevel(TextureFoveatedFeatureQuality level)
        {
            uint value = 0;
            switch (level)
            {
                case TextureFoveatedFeatureQuality.Low:
                    value = 0x1;
                    break;
                case TextureFoveatedFeatureQuality.Medium:
                    value = 0x2;
                    break;
                case TextureFoveatedFeatureQuality.High:
                    value = 0x3;
                    break;
                case TextureFoveatedFeatureQuality.TopHigh:
                    value = 0x4;
                    break;
            }
            m_CurPiplineMaskValue |= value << FFR_BITS;
        }
        private TextureFoveatedFeatureQuality GetFFRLevel()
        {
            uint value = (m_CurPiplineMaskValue >> FFR_BITS) & 0x7;
            if (value == 0)
            {
                return TextureFoveatedFeatureQuality.Close;
            }
            else if (value == 1)
            {
                return TextureFoveatedFeatureQuality.Low;
            }
            else if (value == 2)
            {
                return TextureFoveatedFeatureQuality.Medium;
            }
            else if (value == 3)
            {
                return TextureFoveatedFeatureQuality.High;
            }
            else if (value == 4)
            {
                return TextureFoveatedFeatureQuality.TopHigh;
            }
            return TextureFoveatedFeatureQuality.Close;
        }
        
        private enum FeatureFlag
        {
            Subsampledlayout = 23,
            SRPBatch = 24,
            HDR = 25,
            RenderFeature = 26,
            OverlayCamera = 27,
        }

        private void SetFeatureFlag(FeatureFlag flag)
        {
            m_CurPiplineMaskValue |= (uint)0x1 << (int)flag;
        }
        private bool GetFeatureFlag(FeatureFlag flag)
        {
            uint value = m_CurPiplineMaskValue & ((uint)0x1 << (int)flag);
            return value > 0;
        }
        
        private const int SUBPASS_BITS = 28;
        private void SetSubPassState(PipelineState.SubPassState subPassState)
        {
            uint value = 0x4;
            switch (subPassState)
            {
                case PipelineState.SubPassState.Open:
                    value = 0x0;
                    break;
                case PipelineState.SubPassState.Useless:
                    value = 0x1;
                    break;
                case PipelineState.SubPassState.AssetClose:
                    value = 0x2;
                    break;
                case PipelineState.SubPassState.CustomPass:
                    value = 0x3;
                    break;
            }
            
            m_CurPiplineMaskValue |= value<< SUBPASS_BITS;
        }
        private PipelineState.SubPassState GetSubPassState()
        {
            uint value = (m_CurPiplineMaskValue >> SUBPASS_BITS) & 0x7;
            
            if (value == 0)
            {
                return PipelineState.SubPassState.Open;
            }
            else if (value == 1)
            {
                return PipelineState.SubPassState.Useless;
            }
            else if (value == 2)
            {
                return PipelineState.SubPassState.AssetClose;
            }
            else if (value == 3)
            {
                return PipelineState.SubPassState.CustomPass;
            }

            return PipelineState.SubPassState.Other;
        }
        
        private void Clear()
        {
            m_CurPiplineMaskValue = 0x0;
        }

        public void Printf()
        {
          //  Debug.Log("PipelineStateMask : " + m_CurPiplineMaskValue + "," + Time.renderedFrameCount);
        }

        public bool Equals(IPipelineEncoder other)
        {
            if (other is MaskPipelineEncoder)
            {
                return m_CurPiplineMaskValue == (other as MaskPipelineEncoder).pipelineMaskValue;
            }

            return false;
        }

        public void Encode(ref PipelineState pipeLineState)
        {
            Clear();
            for (int i = 0; i < pipeLineState.renderPassList.Count; ++i)
            {
                SetRenderPass(pipeLineState.renderPassList[i]);
            }
            
            SetEyeBufferSize(pipeLineState.eyeBufferSize);
            SetMSAALevel(pipeLineState.msaaLevel);
            SetFFRLevel(pipeLineState.ffrLevel);
            
            if (pipeLineState.subsampledLayout)
            {
                SetFeatureFlag(FeatureFlag.Subsampledlayout);
            }
            if (pipeLineState.srpBatch)
            {
                SetFeatureFlag(FeatureFlag.SRPBatch);
            }
            if (pipeLineState.hdr)
            {
                SetFeatureFlag(FeatureFlag.HDR);
            }
            if (pipeLineState.useRenderFeature)
            {
                SetFeatureFlag(FeatureFlag.RenderFeature);
            }
            if (pipeLineState.useOverlayCamera)
            {
                SetFeatureFlag(FeatureFlag.OverlayCamera);
            }

            SetSubPassState(pipeLineState.subPassState);
        }

        public void Decode(ref PipelineState pipeLineState)
        {
            pipeLineState.Clear();

            for (int i = 0, imax = (int)PipelineState.RenderPassType.Count; i < imax; i++)
            {
                PipelineState.RenderPassType curState = (PipelineState.RenderPassType)i;
                if (GetRenderPass(curState))
                {
                    pipeLineState.AddRenderPass(curState);
                }
            }

            pipeLineState.eyeBufferSize = GetEyeBufferSize();
            pipeLineState.msaaLevel = GetMSAALevel();
            pipeLineState.ffrLevel = GetFFRLevel();

            pipeLineState.subsampledLayout = GetFeatureFlag(FeatureFlag.Subsampledlayout);
            pipeLineState.srpBatch = GetFeatureFlag(FeatureFlag.SRPBatch);
            pipeLineState.hdr = GetFeatureFlag(FeatureFlag.HDR);
            pipeLineState.useRenderFeature = GetFeatureFlag(FeatureFlag.RenderFeature);
            pipeLineState.useOverlayCamera = GetFeatureFlag(FeatureFlag.OverlayCamera);

            pipeLineState.subPassState = GetSubPassState();
        }
    }
}
