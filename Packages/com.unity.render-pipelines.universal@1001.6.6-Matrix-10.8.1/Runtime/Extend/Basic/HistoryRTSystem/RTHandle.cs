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
    public delegate float RTResizeDelegate(float scale, RTHandle rtHandle);
    public class RTHandle
    {
        public enum ResizeType
        {
            Disable,
            Automatic,
            Manual
        }

        private ResizeType m_ResizeType = ResizeType.Automatic;
        private RenderTexture m_RT = null;
        private RenderTextureDescriptor m_RTDescriptor;
        private RTResizeDelegate m_RTResizeDelegate = null;
        private int m_OriginWidth = 0;
        private int m_OriginHeight = 0;
        private bool m_Inited = false;
        private string m_Name = string.Empty;

        public bool inited
        {
            set
            {
                m_Inited = value;
            }
            get
            {
                return m_Inited;
            }
        }

        public string name
        {
            get => m_Name;
            set
            {
                m_Name = value;
                if (m_RT != null)
                {
                    m_RT.name = CoreUtils.GetRenderTargetAutoName(m_RT.width, m_RT.height, m_RT.depth, m_RT.graphicsFormat, m_Name);
                }
            }
        }

        public RenderTexture rt
        {
            get
            {
                return m_RT;
            }
        }
        public RenderTextureDescriptor descriptor
        {
            get
            {
                return m_RTDescriptor;
            }
            set
            {
                m_RTDescriptor = value;
            }
        }

        public static implicit operator RenderTexture(RTHandle handle)
        {
            return handle.rt;
        }

        public RTHandle(RenderTextureDescriptor renderTextureDescriptor, RTResizeDelegate rtResizeDelegate)
        {
            m_RTDescriptor = renderTextureDescriptor;
            m_RTDescriptor.useDynamicScale = false;
            m_ResizeType = ResizeType.Manual;
            m_RTResizeDelegate = rtResizeDelegate;
            m_OriginWidth = m_RTDescriptor.width;
            m_OriginHeight = m_RTDescriptor.height;
            Alloc();
        }

        public RTHandle(RenderTextureDescriptor renderTextureDescriptor, bool useAutomaticResize = false)
        {
            m_RTDescriptor = renderTextureDescriptor;
            if (useAutomaticResize)
            {
                m_RTDescriptor.useDynamicScale = DynamicResolutionController.excutedType == DynamicResolutionController.DynamicResolutionType.Hardware;
                m_ResizeType = ResizeType.Automatic;
            }
            else
            {
                m_RTDescriptor.useDynamicScale = false;
                m_ResizeType = ResizeType.Disable;
            }
            m_RTResizeDelegate = null;
            m_OriginWidth = m_RTDescriptor.width;
            m_OriginHeight = m_RTDescriptor.height;
            Alloc();
        }

        public void Alloc()
        {
            if (m_RT != null)
            {
                RenderTexture.ReleaseTemporary(m_RT);
            }
            m_RT = RenderTexture.GetTemporary(descriptor);
            m_Inited = false;
        }

        public void Release()
        {
            if (m_RT != null)
            {
                RenderTexture.ReleaseTemporary(m_RT);
            }
            m_RT = null;
        }

        public void Resize()
        {
            if (DynamicResolutionController.excutedType != DynamicResolutionController.DynamicResolutionType.Disable)
            {
                if (m_ResizeType != ResizeType.Disable)
                {
                    bool useHardware = false;
                    float scale = DynamicResolutionController.curScale;
                    if (m_ResizeType == ResizeType.Manual)
                    {
                        if (m_RTResizeDelegate != null)
                        {
                            scale = m_RTResizeDelegate(scale, this);
                        }
                    }
                    else
                    {
                        if (DynamicResolutionController.excutedType == DynamicResolutionController.DynamicResolutionType.Hardware)
                        {
                            useHardware = true;
                        }
                    }

                    bool reset = false;
                    if (useHardware)
                    {
                        if (!m_RTDescriptor.useDynamicScale || m_RTDescriptor.width != m_OriginWidth || m_RTDescriptor.height != m_OriginHeight)
                        {
                            reset = true;
                            m_RTDescriptor.useDynamicScale = true;
                            m_RTDescriptor.width = m_OriginWidth;
                            m_RTDescriptor.height = m_OriginHeight;
                        }
                    }
                    else
                    {
                        int newWidth = Mathf.FloorToInt(scale * m_OriginWidth);
                        int newHeight = Mathf.FloorToInt(scale * m_OriginHeight);
                        if (m_RTDescriptor.useDynamicScale || m_RTDescriptor.width != newWidth || m_RTDescriptor.height != newHeight)
                        {
                            reset = true;
                            m_RTDescriptor.useDynamicScale = false;
                            m_RTDescriptor.width = newWidth;
                            m_RTDescriptor.height = newHeight;
                        }
                    }
                    if (reset)
                    {
                        Alloc();
                    }
                }
            }
        }

        public void Resize(int width, int height)
        {
            if (m_RTDescriptor.width != width || m_RTDescriptor.height != height)
            {
                m_RTDescriptor.width = width;
                m_RTDescriptor.height = height;

                Alloc();
                name = m_Name;
            }
        }
    }
}
