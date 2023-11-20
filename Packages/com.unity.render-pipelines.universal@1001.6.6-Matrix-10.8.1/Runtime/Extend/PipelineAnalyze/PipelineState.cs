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
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public class PipelineState
    {
        public enum RenderPassType
        {
            DepthOnlyPass = 0,
            ColorGradingLutPass,
            MainLightShadowCasterPass,
            AdditionalLightsShadowCasterPass,
            CopyDepthPass,
            CopyColorPass_Opaque,
            CopyColorPass_Transparent,
            PostProcessPass_Post,
            PostProcessPass_FinalBlit,
            DrawCorrectGammaUIPass,
            MotionVectorPass,
            DebugPass,
            Count = 12,
        }
        
        public enum SubPassState
        {
            Open,
            Useless,
            AssetClose,
            CustomPass,
            OverlayCamera,
            RenderFeature,
            Other
        }
        
        private List<RenderPassType> m_RenderPassList = new List<RenderPassType>();
        public List<RenderPassType> renderPassList
        {
            get => m_RenderPassList;
        }
        
        private float m_EyeBufferSize;
        public float eyeBufferSize
        {
            get => m_EyeBufferSize;
            set
            {
                if (value <= 1f)
                {
                    m_EyeBufferSize = 1f;
                }
                else if (value <= 1.1f)
                {
                    m_EyeBufferSize = 1.1f;
                }
                else if (value <= 1.2f)
                {
                    m_EyeBufferSize = 1.2f;
                }
                else if (value <= 1.25f)
                {
                    m_EyeBufferSize = 1.25f;
                }
                else if (value <= 1.3f)
                {
                    m_EyeBufferSize = 1.3f;
                }
                else if (value <= 1.35f)
                {
                    m_EyeBufferSize = 1.35f;
                }
                else if (value <= 1.4f)
                {
                    m_EyeBufferSize = 1.4f;
                }
                else
                {
                    m_EyeBufferSize = 1.5f;
                }
            }
        }
        
        private int m_MSAALevel;
        public int msaaLevel
        {
            get => m_MSAALevel;
            set
            {
                if (value <= 1)
                {
                    m_MSAALevel = 1;
                }
                else if (value <= 2)
                {
                    m_MSAALevel = 2;
                }
                else if (value <= 4)
                {
                    m_MSAALevel = 4;
                }
                else
                {
                    m_MSAALevel = 8;
                }
            }
        }
        
        private TextureFoveatedFeatureQuality m_FFRLevel;
        public TextureFoveatedFeatureQuality ffrLevel
        {
            get => m_FFRLevel;
            set => m_FFRLevel = value;
        }
        
        private bool m_SubsampledLayout;
        public bool subsampledLayout
        {
            get => m_SubsampledLayout;
            set => m_SubsampledLayout = value;
        }
        
        private bool m_SRPBatch;
        public bool srpBatch
        {
            get => m_SRPBatch;
            set => m_SRPBatch = value;
        }
        
        private bool m_HDR;
        public bool hdr
        {
            get => m_HDR;
            set => m_HDR = value;
        }
        
        private bool m_UseRenderFeature;
        public bool useRenderFeature
        {
            get => m_UseRenderFeature;
            set => m_UseRenderFeature = value; 
        }
        
        private bool m_UseOverlayCamera;
        public bool useOverlayCamera
        {
            get => m_UseOverlayCamera;
            set => m_UseOverlayCamera = value;
        }
        
        private SubPassState m_SubPassState;
        public SubPassState subPassState
        {
            get => m_SubPassState;
            set
            {
                if (value == SubPassState.Other)
                {
                    if (m_UseOverlayCamera)
                    {
                        m_SubPassState = SubPassState.OverlayCamera;
                    }
                    else if (m_UseRenderFeature)
                    {
                        m_SubPassState = SubPassState.RenderFeature;
                    }
                    else
                    {
                        m_SubPassState = value;
                    }
                }
                else
                {
                    m_SubPassState = value;
                }
            }
        }

        public void Clear()
        {
            m_RenderPassList.Clear();
            m_EyeBufferSize = 0;
            m_MSAALevel = 0;
            m_FFRLevel = TextureFoveatedFeatureQuality.Close;
            m_SubsampledLayout = false;
            m_SRPBatch = false;
            m_HDR = false;
            m_UseRenderFeature = false;
            m_UseOverlayCamera = false;
            m_SubPassState = SubPassState.AssetClose;
        }

        public void AddRenderPass(RenderPassType renderPass)
        {
            m_RenderPassList.Add(renderPass);
        }

        public void SetSubPassState(bool isOpen, bool isUseless, bool isAssetClose, bool isCustomPassUnSupport)
        {
            if (isOpen)
            {
                m_SubPassState = SubPassState.Open;
            }
            else if (isUseless)
            {
                m_SubPassState = SubPassState.Useless;
            }
            else if (isAssetClose)
            {
                m_SubPassState = SubPassState.AssetClose;
            }
            else if (isCustomPassUnSupport)
            {
                m_SubPassState = SubPassState.CustomPass;
            }
            else
            {
                if (m_UseOverlayCamera)
                {
                    m_SubPassState = SubPassState.OverlayCamera;
                }
                else if (m_UseRenderFeature)
                {
                    m_SubPassState = SubPassState.RenderFeature;
                }
                else
                {
                    m_SubPassState = SubPassState.Other;
                }
            }
        }

        public bool Equals(PipelineState other)
        {
            if (m_RenderPassList.Count != other.renderPassList.Count)
            {
                return false;
            }

            for (int i = 0; i < m_RenderPassList.Count; ++i)
            {
                if (!m_RenderPassList.Contains(other.renderPassList[i]))
                {
                    return false;
                }
            }

            if (m_EyeBufferSize != other.eyeBufferSize)
            {
                return false;
            }

            if (m_MSAALevel != other.msaaLevel)
            {
                return false;
            }

            if (m_FFRLevel != other.ffrLevel)
            {
                return false;
            }

            if (m_SubsampledLayout != other.subsampledLayout)
            {
                return false;
            }

            if (m_SRPBatch != other.srpBatch)
            {
                return false;
            }

            if (m_HDR != other.hdr)
            {
                return false;
            }

            if (m_UseRenderFeature != other.useRenderFeature)
            {
                return false;
            }

            if (m_UseOverlayCamera != other.useOverlayCamera)
            {
                return false;
            }

            if (m_SubPassState != other.subPassState)
            {
                return false;
            }
            
            return true;
        }

        public void Printf()
        {
            Debug.Log("-------------------- Pipeline State Begin --------------------");
            Debug.Log("");
            Debug.Log($"------ RenderPass({m_RenderPassList.Count}) ------");
            for (int i = 0; i < m_RenderPassList.Count; ++i)
            {
                Debug.Log($"({i}), {m_RenderPassList[i]}");
            }
            Debug.Log("");
            Debug.Log($"EyeBuffer:{m_EyeBufferSize}");
            Debug.Log($"MSAA:{m_MSAALevel}");
            Debug.Log($"FFR:{m_FFRLevel}");
            Debug.Log($"SubsampledLayout:{m_SubsampledLayout}");
            Debug.Log($"SRPBatch:{m_SRPBatch}");
            Debug.Log($"HDR:{m_HDR}");
            Debug.Log($"RenderFeature:{m_UseRenderFeature}");
            Debug.Log($"OverlayCamera:{m_UseOverlayCamera}");
            Debug.Log($"SubPassState:{m_SubPassState}");
            Debug.Log("");
            Debug.Log("-------------------- Pipeline State End --------------------");
        }
    }
}
