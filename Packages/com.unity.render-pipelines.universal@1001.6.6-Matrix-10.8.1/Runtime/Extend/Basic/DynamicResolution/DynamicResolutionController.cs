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
    public static partial class DynamicResolutionController
    {
        public delegate float PerformDynamicResolution();
        private static readonly int SHADER_TEXELSIZE_SCALE = Shader.PropertyToID("_GLOBAL_TEXELSIZE_SCALE");
        
        public enum DynamicResolutionType
        {
            Disable = 0,
            Auto,//优先使用Hardware，再使用Software
            Hardware,
            Software
        }

        private static UniversalRenderPipelineAsset asset
        {
            get => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        }

        private static float m_CurScale = float.MaxValue;
        private static Camera m_CurCamera = null;
        private static PerformDynamicResolution m_PerformDynamicResolution = null;
        public static void SetPerformDynamicResolution(PerformDynamicResolution action)
        {
            Reset();
            m_PerformDynamicResolution = action;
        }
        public static void ClearPerformDynamicResolution()
        {
            Reset();
            m_PerformDynamicResolution = null;
        }
        public static bool IsManualPerformDynamicResolution()
        {
            return m_PerformDynamicResolution != null;
        }

        // public static void SetResolution(int width, int height)
        // {
        //     float widthScale = width * 1.0f / Screen.width;
        //     float heightScale = height * 1.0f / Screen.height;
        //
        //     float finalScale = Mathf.Min(widthScale, heightScale);
        //    // SetResolution(finalScale);
        // }

        private static DynamicResolutionType m_DynamicResolutionType = DynamicResolutionType.Software;
        public static DynamicResolutionType dynamicResolutionType
        {
            get
            {
                return m_DynamicResolutionType;
            }
            set
            {
                if (value != m_DynamicResolutionType)
                {
                    m_DynamicResolutionType = value;
                    Reset();
                }
            }
        }

        private static int m_HardwareSupported = -1;
        private static bool HardwareSupported()
        {
            if (m_HardwareSupported == -1)
            {
                m_HardwareSupported = 0;
                switch (Application.platform)
                {
                    case RuntimePlatform.WindowsEditor:
                    case RuntimePlatform.WindowsPlayer:
                        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12)
                        {
                            m_HardwareSupported = 1;
                        }
                        break;
                    case RuntimePlatform.PS4:
                    case RuntimePlatform.XboxOne:
                    case RuntimePlatform.Switch:
                        m_HardwareSupported = 1;
                        break;
                    case RuntimePlatform.tvOS:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.OSXEditor:
                    case RuntimePlatform.OSXPlayer:
                        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                        {
                            m_HardwareSupported = 1;
                        }
                        break;
                    case RuntimePlatform.Android:
                        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan)
                        {
                            m_HardwareSupported = 1;
                        }
                        break;
                }
            }

            return m_HardwareSupported == 1;
        }

        public static DynamicResolutionType excutedType
        {
            get
            {
                if (m_DynamicResolutionType == DynamicResolutionType.Auto)
                {
                    if (HardwareSupported())
                    {
                        return DynamicResolutionType.Hardware;
                    }
                    return DynamicResolutionType.Software;
                }
                else if (m_DynamicResolutionType == DynamicResolutionType.Hardware)
                {
                    if (HardwareSupported())
                    {
                        return DynamicResolutionType.Hardware;
                    }
                }
                else if (m_DynamicResolutionType == DynamicResolutionType.Software)
                {
                    return DynamicResolutionType.Software;
                }

                return DynamicResolutionType.Disable;
            }
        }

        private static float m_CurTexelSizeScale = float.MaxValue;
        public static float texelSizeScale
        {
            get
            {
                if (m_CurCamera != null && m_CurCamera.targetTexture == null)
                {
                    if (excutedType == DynamicResolutionType.Hardware)
                    {
                        return curScale;
                    }
                }
                return 1;
            }
        }

        public static float curScale
        {
            get
            {
                return m_CurScale == float.MaxValue ? 1 : m_CurScale;
            }
        }

        public static int screenWidth
        {
            get
            {
                if (excutedType == DynamicResolutionType.Disable)
                {
                    return Screen.width;
                }
                return (int)(Screen.width * curScale);
            }
        }

        public static int screenHeight
        {
            get
            {
                if (excutedType == DynamicResolutionType.Disable)
                {
                    return Screen.height;
                }
                return (int)(Screen.height * curScale);
            }
        }

        private static void SetScale(float value)
        {
            if (excutedType != DynamicResolutionType.Disable)
            {
                if (m_CurScale != value)
                {
                    m_CurScale = value;
                    if (excutedType == DynamicResolutionType.Hardware)
                    {
                        ScalableBufferManager.ResizeBuffers(m_CurScale, m_CurScale);
                    }
                    else if (excutedType == DynamicResolutionType.Software)
                    {
                        asset.renderScale = value;
                    }
                }
            }
        }

        internal static void Update(CameraData cameraData, CommandBuffer cmd)
        {
            dynamicResolutionType = asset.dynamicResolutionType;
            if (m_DynamicResolutionType != DynamicResolutionType.Disable)
            {
#if UNITY_EDITOR
                if (cameraData.renderType == CameraRenderType.Base && cameraData.cameraType == CameraType.Game && !cameraData.camera.name.Equals("Preview Camera"))

#else
                if (cameraData.renderType == CameraRenderType.Base && cameraData.cameraType == CameraType.Game)

#endif
                {

                    m_CurCamera = cameraData.camera;
                    if (m_PerformDynamicResolution != null)
                    {
                        SetScale(m_PerformDynamicResolution());
                    }
                    else
                    {
                        AutomaticScale();
                    }
                    if (m_CurTexelSizeScale != texelSizeScale)
                    {
                        m_CurTexelSizeScale = texelSizeScale;
                        cmd.SetGlobalVector(SHADER_TEXELSIZE_SCALE, new Vector4(1 / m_CurTexelSizeScale, 1 / m_CurTexelSizeScale, m_CurTexelSizeScale, m_CurTexelSizeScale));
                    }

                }
                DynamicResolutionController.CalculateFPS();
            }
        }

        internal static void Reset()
        {
            if (HardwareSupported())
            {
                ScalableBufferManager.ResizeBuffers(1, 1);
            }
            //asset.renderScale = 1;    // YangFan:会导致打包或者运行时RenderScale被强制切成1，暂时注释掉
            m_CurScale = float.MaxValue;
            m_CurTexelSizeScale = float.MaxValue;
            m_CurCamera = null;
            Shader.SetGlobalVector(SHADER_TEXELSIZE_SCALE, Vector4.one);
        }
    }
}
