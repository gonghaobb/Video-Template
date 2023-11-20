using System;
using System.Collections.Generic;
using UnityEngine.Scripting.APIUpdating;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using System.IO;
using UnityEditorInternal;
#endif
using System.ComponentModel;
using System.Linq;

namespace UnityEngine.Rendering.LWRP
{
    [Obsolete("LWRP -> Universal (UnityUpgradable) -> UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset", true)]
    public class LightweightRenderPipelineAsset
    {
    }
}


namespace UnityEngine.Rendering.Universal
{
    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum ShadowQuality
    {
        Disabled,
        HardShadows,
        SoftShadows,
    }

    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum ShadowResolution
    {
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum MsaaQuality
    {
        Disabled = 1,
        _2x = 2,
        _4x = 4,
        _8x = 8
    }

    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum Downsampling
    {
        None,
        _2xBilinear,
        _4xBox,
        _4xBilinear
    }

    internal enum DefaultMaterialType
    {
        Standard,
        Particle,
        Terrain,
        Sprite,
        UnityBuiltinDefault
    }

    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum LightRenderingMode
    {
        Disabled = 0,
        PerVertex = 2,
        PerPixel = 1,
    }

    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum ShaderVariantLogLevel
    {
        Disabled,
        OnlyUniversalRPShaders,
        AllShaders,
    }

    [Obsolete("PipelineDebugLevel is unused and has no effect.", false)]
    public enum PipelineDebugLevel
    {
        Disabled,
        Profiling,
    }

    [MovedFrom("UnityEngine.Rendering.LWRP")] public enum RendererType
    {
        Custom,
        ForwardRenderer,
        _2DRenderer,
    }

    public enum ColorGradingMode
    {
        LowDynamicRange,
        HighDynamicRange
    }
    
    //PicoVideo;AppSW;Ernst;Begin
    public enum CommonDownsampling
    {
        None = 1,
        _2x = 2,
        _4x = 4
    }
    //PicoVideo;AppSW;Ernst;End
    
    //PicoVideo;FSR;ZhengLingFeng;Begin
    public enum UpscalingFilterSelection
    {
        [InspectorName("Automatic")] Auto,
        [InspectorName("Bilinear")] Linear,
        [InspectorName("Nearest-Neighbor")] Point,
        [InspectorName("FidelityFX Super Resolution 1.0")] FSR,
    }
    //PicoVideo;FSR;ZhengLingFeng;End

    //PicoVideo;FoveatedFeature;YangFan;Begin
    public enum TextureFoveatedFeatureQuality
    {
        Low = 0, 
        Medium = 1,
        High = 2, 
        TopHigh = 3,
        Custom = 4,
        [Obsolete("'None' has been deprecated. Use 'Close' instead.")]
        None = 5,
        Close = 5
    }
    
    [Obsolete("'DynamicTextureFoveatedFeatureQuality' has been deprecated. Use 'EyeTrackingTextureFoveatedFeatureQuality' instead.")]
    public enum DynamicTextureFoveatedFeatureQuality
    {
        Low = 0, 
        Medium = 1,
        High = 2, 
        TopHigh = 3,
        [Obsolete("'None' has been deprecated. Use 'Close' instead.")]
        None = 4,
        Close = 4,
        Default = 5
    }
    
    public enum EyeTrackingTextureFoveatedFeatureQuality
    {
        Low = 0, 
        Medium = 1,
        High = 2, 
        TopHigh = 3,
        Close = 4,
        Default = 5
    }

    [Serializable]
    public class TextureFoveatedParameters
    {
        public float focalPointX;
        public float focalPointY;
        public float foveationGainX;
        public float foveationGainY;
        public float foveationArea;
        public float foveationMinimum;

        public TextureFoveatedParameters(float focalPointX, float focalPointY, float foveationGainX,
            float foveationGainY, float foveationArea, float foveationMinimum)
        {
            this.focalPointX = focalPointX;
            this.focalPointY = focalPointY;
            this.foveationGainX = foveationGainX;
            this.foveationGainY = foveationGainY;
            this.foveationArea = foveationArea;
            this.foveationMinimum = foveationMinimum;
        }
        
        public TextureFoveatedParameters(TextureFoveatedParameters parameters)
        {
            this.focalPointX = parameters.focalPointX;
            this.focalPointY = parameters.focalPointY;
            this.foveationGainX = parameters.foveationGainX;
            this.foveationGainY = parameters.foveationGainY;
            this.foveationArea = parameters.foveationArea;
            this.foveationMinimum = parameters.foveationMinimum;
        }
        
        public static List<TextureFoveatedParameters> s_DefaultParametersList = new List<TextureFoveatedParameters>()
        {
            new TextureFoveatedParameters(0 , 0 , 8.86335f ,  10.07531f , 31 , 0.25f) ,
            new TextureFoveatedParameters(0 , 0 , 11.23511f , 11.20824f , 25.8f , 0.25f) ,
            new TextureFoveatedParameters(0 , 0 , 13.74341f , 14.03977f , 25.8f , 0.25f) ,
            new TextureFoveatedParameters(0 , 0 , 13.74341f , 14.03977f , 25.8f , 0.125f) ,
        };

        public static TextureFoveatedParameters s_DefaultNoneParameters =
            new TextureFoveatedParameters(0, 0, 0, 0, 0, 1);
    }
    //PicoVideo;FoveatedFeature;YangFan;End

    /// <summary>
    /// Defines if Unity discards or stores the render targets of the DrawObjects Passes. Selecting the Store option significantly increases the memory bandwidth on mobile and tile-based GPUs.
    /// </summary>
    public enum StoreActionsOptimization
    {
        /// <summary>Unity uses the Discard option by default, and falls back to the Store option if it detects any injected Passes.</summary>
        Auto,
        /// <summary>Unity discards the render targets of render Passes that are not reused later (lower memory bandwidth).</summary>
        Discard,
        /// <summary>Unity stores all render targets of each Pass (higher memory bandwidth).</summary>
        Store
    }
    
    //PicoVideo;OptimizedRenderPipeline;YangFan;Begin
    public enum OptimizedRenderPipelineFeature
    {
        MaximumEyeBufferRendering = 1 << 0,
        MemorylessMSAA = 1 << 1,
        SubPass = 1 << 2,
    }
    //PicoVideo;OptimizedRenderPipeline;YangFan;End

    /// <summary>
    /// Defines the update frequency for the Volume Framework.
    /// </summary>
    public enum VolumeFrameworkUpdateMode
    {
        [InspectorName("Every Frame")]
        EveryFrame = 0,
        [InspectorName("Via Scripting")]
        ViaScripting = 1,
        [InspectorName("Use Pipeline Settings")]
        UsePipelineSettings = 2,
    }
    
    //PicoVideo;Debug;Ernst;Begin
    public enum DebugMode
    {
        [InspectorName("Disable")]
        Disable = 0,
        [InspectorName("Montion Vector")]
        MotionVector = 1,
        [InspectorName("Depth Texture")]
        DepthTexture = 2,
        [InspectorName("Opaque Color Texture")]
        OpaqueColorTexture = 3,
        [InspectorName("Transparent Color Texture")]
        TransparentColorTexture = 4,
    }
    //PicoVideo;Debug;Ernst;End

    //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;Begin
    [Serializable]
    public class CustomMainLightShadowRange
    {
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;
        public float minZ;
        public float maxZ;
    }
    //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;End

    [ExcludeFromPreset]
    public partial class UniversalRenderPipelineAsset : RenderPipelineAsset, ISerializationCallbackReceiver
    {
        Shader m_DefaultShader;
        ScriptableRenderer[] m_Renderers = new ScriptableRenderer[1];

        // Default values set when a new UniversalRenderPipeline asset is created
        [SerializeField] int k_AssetVersion = 5;
        [SerializeField] int k_AssetPreviousVersion = 5;

        // Deprecated settings for upgrading sakes
        [SerializeField] RendererType m_RendererType = RendererType.ForwardRenderer;
        [EditorBrowsable(EditorBrowsableState.Never)]
        [SerializeField] internal ScriptableRendererData m_RendererData = null;

        // Renderer settings
        [SerializeField] internal ScriptableRendererData[] m_RendererDataList = new ScriptableRendererData[1];
        [SerializeField] internal int m_DefaultRendererIndex = 0;

        // General settings
        [SerializeField] bool m_RequireDepthTexture = false;
        [SerializeField] bool m_RequireOpaqueTexture = false;
        [SerializeField] Downsampling m_OpaqueDownsampling = Downsampling._2xBilinear;
        //PicoVideo;Basic;Ernst;Begin
        [SerializeField] bool m_RequireTransparentTexture = false;
        //PicoVideo;Basic;Ernst;End
        [SerializeField] bool m_SupportsTerrainHoles = true;
        [SerializeField] StoreActionsOptimization m_StoreActionsOptimization = StoreActionsOptimization.Auto;
        //PicoVideo;AppSW;Ernst;Begin
        [SerializeField] bool m_RequireMotionVector = false;
        [SerializeField] private CommonDownsampling m_MotionVectorDownsampling = CommonDownsampling._2x;
        //PicoVidoe;AppSW;Ernst;End

        // Quality settings
        [SerializeField] bool m_SupportsHDR = true;
        [SerializeField] MsaaQuality m_MSAA = MsaaQuality.Disabled;
        [SerializeField] float m_RenderScale = 1.0f;
        // TODO: Shader Quality Tiers

        //PicoVideo;FSR;ZhengLingFeng;Begin
        [SerializeField] UpscalingFilterSelection m_UpscalingFilter = UpscalingFilterSelection.Auto;
        [SerializeField] bool m_FsrOverrideSharpness = false;
        [SerializeField] float m_FsrSharpness = FSRUtils.kDefaultSharpnessLinear;
        //PicoVideo;FSR;ZhengLingFeng;End

        //PicoVideo;DynamicResolution;Ernst;Begin
        [SerializeField] DynamicResolutionController.DynamicResolutionType m_DynamicResolutionType = DynamicResolutionController.DynamicResolutionType.Disable;
        [SerializeField] float m_AutomaticFPSTarget = 30f;
        [SerializeField] float m_AutomaticScaleMin = 0.8f;
        [SerializeField] float m_AutomaticScaleMax = 1.0f;
        //PicoVideo;DynamicResolution;Ernst;End
     
        //PicoVideo;FoveatedFeature;YangFan;Begin
        [SerializeField] private bool m_EnableTextureFoveatedFeature = false;
        [SerializeField]
        private TextureFoveatedFeatureQuality m_TextureFoveatedFeatureQuality = TextureFoveatedFeatureQuality.Medium;
        [SerializeField]
        private EyeTrackingTextureFoveatedFeatureQuality m_EyeTrackingTextureFoveatedFeatureQuality = EyeTrackingTextureFoveatedFeatureQuality.Close;
        [SerializeField] 
        private bool m_EnableTextureFoveatedSubsampledLayout = false;
        [SerializeField]
        private TextureFoveatedParameters m_CustomTextureFoveatedParameters = new TextureFoveatedParameters(TextureFoveatedParameters.s_DefaultParametersList[(int)TextureFoveatedFeatureQuality.TopHigh]);

        public bool enableTextureFoveatedFeature
        {
            get => m_EnableTextureFoveatedFeature && XRRenderUtils.instance.IsXRDevice();
            set
            {
                m_EnableTextureFoveatedFeature = value;
                XRRenderUtils.instance.SetEyeTrackingEnabled(UniversalRenderPipeline.eyeTrackingFoveatedRenderingEnabled);
            }
        }

        public TextureFoveatedFeatureQuality currentTextureFoveatedFeatureQuality
        {
            get => m_TextureFoveatedFeatureQuality;
            set
            {
                m_TextureFoveatedFeatureQuality = value;
                XRRenderUtils.instance.SetEyeTrackingEnabled(UniversalRenderPipeline.eyeTrackingFoveatedRenderingEnabled);
            }
        }

        public TextureFoveatedParameters currentTextureFoveatedParameters
        {
            get
            {
                if (m_TextureFoveatedFeatureQuality == TextureFoveatedFeatureQuality.Custom)
                {
                    return m_CustomTextureFoveatedParameters;
                }

                if (m_TextureFoveatedFeatureQuality == TextureFoveatedFeatureQuality.Close)
                {
                    return TextureFoveatedParameters.s_DefaultNoneParameters;
                }

                return TextureFoveatedParameters.s_DefaultParametersList[(int) m_TextureFoveatedFeatureQuality];
            }
        }

        [Obsolete("'currentDynamicTextureFoveatedParameters' has been deprecated. Use 'currentEyeTrackingTextureFoveatedParameters' instead.")]
        public TextureFoveatedParameters currentDynamicTextureFoveatedParameters => currentEyeTrackingTextureFoveatedParameters;

        public TextureFoveatedParameters currentEyeTrackingTextureFoveatedParameters
        {
            get
            {
                if (m_EyeTrackingTextureFoveatedFeatureQuality == EyeTrackingTextureFoveatedFeatureQuality.Default)
                {
                    return currentTextureFoveatedParameters;
                }

                if (m_EyeTrackingTextureFoveatedFeatureQuality == EyeTrackingTextureFoveatedFeatureQuality.Close)
                {
                    return TextureFoveatedParameters.s_DefaultNoneParameters;
                }
                
                return TextureFoveatedParameters.s_DefaultParametersList[(int) m_EyeTrackingTextureFoveatedFeatureQuality];
            }
        }

        [Obsolete("'currentDynamicTextureFoveatedFeatureQuality' has been deprecated. Use 'currentEyeTrackingTextureFoveatedFeatureQuality' instead.")]
        public DynamicTextureFoveatedFeatureQuality currentDynamicTextureFoveatedFeatureQuality
        {
            get => (DynamicTextureFoveatedFeatureQuality) currentEyeTrackingTextureFoveatedFeatureQuality;
            set => currentEyeTrackingTextureFoveatedFeatureQuality = (EyeTrackingTextureFoveatedFeatureQuality) value;
        }
        
        public EyeTrackingTextureFoveatedFeatureQuality currentEyeTrackingTextureFoveatedFeatureQuality
        {
            get => m_EyeTrackingTextureFoveatedFeatureQuality;
            set
            {
                m_EyeTrackingTextureFoveatedFeatureQuality = value;
                XRRenderUtils.instance.SetEyeTrackingEnabled(UniversalRenderPipeline.eyeTrackingFoveatedRenderingEnabled);
            }
        }
        
        public bool enableSubsampledLayout
        {
            get
            {
                if (!XRRenderUtils.instance.IsSupportedSubsampledLayout())
                {
                    return false;
                }
                return m_EnableTextureFoveatedSubsampledLayout;
            }
            set => m_EnableTextureFoveatedSubsampledLayout = value;
        }
        //PicoVideo;FoveatedFeature;YangFan;End
        
        //PicoVideo;Debug;Ernst;Begin
        public DebugMode debugMode
        {
            get => m_DebugMode;
            set => m_DebugMode = value;
        }
        //PicoVideo;Debug;Ernst;End
        
        //PicoVideo;EditorUIColorAdjustment;WuJunLin;Begin
        public bool enableUIColorAdjustment
        {
            get
            {
                if (QualitySettings.activeColorSpace == ColorSpace.Linear &&
                    UniversalRenderPipeline.asset.colorAdjustUILayerMask != 0)
                {
                    if (Application.isMobilePlatform && !Application.isEditor && SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)//OpenGLES下使用FrameBufferFetch解决
                    {
                        return false;
                    }
                    return m_EnableUIColorAdjustment;
                }

                return false;
            }
            set { m_EnableUIColorAdjustment = value; }
        }

        public LayerMask colorAdjustUILayerMask
        {
            get { return m_ColorAdjustUILayerMask; }
            set { m_ColorAdjustUILayerMask = value; }
        }
        //PicoVideo;EditorUIColorAdjustment;WuJunLin;End
        

        // Main directional light Settings
        [SerializeField] LightRenderingMode m_MainLightRenderingMode = LightRenderingMode.PerPixel;
        [SerializeField] bool m_MainLightShadowsSupported = true;
        [SerializeField] ShadowResolution m_MainLightShadowmapResolution = ShadowResolution._2048;

        //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;Begin
        [SerializeField] bool m_EnableCustomMainLightShadowRange = false;
        [SerializeField] CustomMainLightShadowRange m_CustomMainLightShadowRange = new CustomMainLightShadowRange();
        [SerializeField] bool m_ResetCustomMainLightShadowRange = false;
        [SerializeField] bool m_DebugCustomMainLightShadowRange = false;
        //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;End

        // Additional lights settings
        [SerializeField] LightRenderingMode m_AdditionalLightsRenderingMode = LightRenderingMode.PerPixel;
        [SerializeField] int m_AdditionalLightsPerObjectLimit = 4;
        [SerializeField] bool m_AdditionalLightShadowsSupported = false;
        [SerializeField] ShadowResolution m_AdditionalLightsShadowmapResolution = ShadowResolution._512;

        //PicoVideo;LightMode;YangFan;Begin
        [SerializeField][Range(0.0f, 1.0f)] float m_BakeGIIntensityMultiplier = 1;   // 控制烘焙GI强度
        [SerializeField] bool m_DirSpecularLightingmapEnable = false;
        [SerializeField][Range(0.0f, 3.0f)] float m_DirSpecularLightingingmapMultiplier = 1.0f;
        //PicoVideo;LightMode;YangFan;End

        //PicoVideo;LightMode;XiaoPengCheng;Begin
        [SerializeField] Color m_GlobalAdjustColor = Color.white;
        //PicoVideo;LightMode;XiaoPengCheng;End

        //PicoVideo;UIDarkenMode;ZhouShaoyang;Begin
        [SerializeField] Color m_GlobalDarkenColor = Color.white;
        //PicoVideo;UIDarkenMode;ZhouShaoyang;End

        // Shadows Settings
        [SerializeField] float m_ShadowDistance = 50.0f;
        [SerializeField] int m_ShadowCascadeCount = 1;
        [SerializeField] float m_Cascade2Split = 0.25f;
        [SerializeField] Vector2 m_Cascade3Split = new Vector2(0.1f, 0.3f);
        [SerializeField] Vector3 m_Cascade4Split = new Vector3(0.067f, 0.2f, 0.467f);
        [SerializeField] float m_ShadowDepthBias = 1.0f;
        [SerializeField] float m_ShadowNormalBias = 1.0f;
        [SerializeField] bool m_SoftShadowsSupported = false;

        // Advanced settings
        [SerializeField] bool m_UseSRPBatcher = true;
        [SerializeField] bool m_SupportsDynamicBatching = false;
        [SerializeField] bool m_MixedLightingSupported = true;
        
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
        [SerializeField] bool m_OptimizedRenderPipelineEnabled = false;
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
        
        //PicoVideo;OptimizedRenderPipeline;YangFan;Begin
        [SerializeField] private int m_OptimizedRenderPipelineFeature = -1;

        public bool GetOptimizedRenderPipelineFeatureActivated(OptimizedRenderPipelineFeature feature)
        {
            return m_OptimizedRenderPipelineEnabled && (m_OptimizedRenderPipelineFeature & (int)feature) != 0;
        }

        public void SetOptimizedRenderPipelineFeatureActivated(OptimizedRenderPipelineFeature feature, bool active)
        {
            if (active)
            {
                m_OptimizedRenderPipelineFeature |= (int) feature;
            }
            else
            {
                m_OptimizedRenderPipelineFeature &= ~(int) feature;
            }
        }
        //PicoVideo;OptimizedRenderPipeline;YangFan;End
        
        [SerializeField][Obsolete] PipelineDebugLevel m_DebugLevel;

        // Adaptive performance settings
        [SerializeField] bool m_UseAdaptivePerformance = true;

        // Post-processing settings
        [SerializeField] ColorGradingMode m_ColorGradingMode = ColorGradingMode.LowDynamicRange;
        [SerializeField] int m_ColorGradingLutSize = 32;

        // Deprecated settings
        [SerializeField] ShadowQuality m_ShadowType = ShadowQuality.HardShadows;
        [SerializeField] bool m_LocalShadowsSupported = false;
        [SerializeField] ShadowResolution m_LocalShadowsAtlasResolution = ShadowResolution._256;
        [SerializeField] int m_MaxPixelLights = 0;
        [SerializeField] ShadowResolution m_ShadowAtlasResolution = ShadowResolution._256;

        [SerializeField] ShaderVariantLogLevel m_ShaderVariantLogLevel = ShaderVariantLogLevel.Disabled;
        [SerializeField] VolumeFrameworkUpdateMode m_VolumeFrameworkUpdateMode = VolumeFrameworkUpdateMode.EveryFrame;
        //PicoVideo;Debug;Ernst;Begin
        [SerializeField] DebugMode m_DebugMode = DebugMode.Disable;
        //PicoVideo;Debug;Ernst;End
        
        //PicoVideo;EditorUIColorAdjustment;WuJunLin;Begin
        [SerializeField] bool m_EnableUIColorAdjustment = true;
        [SerializeField] LayerMask m_ColorAdjustUILayerMask = 32;
        //PicoVideo;EditorUIColorAdjustment;WuJunLin;End
        
        // Note: A lut size of 16^3 is barely usable with the HDR grading mode. 32 should be the
        // minimum, the lut being encoded in log. Lower sizes would work better with an additional
        // 1D shaper lut but for now we'll keep it simple.
        public const int k_MinLutSize = 16;
        public const int k_MaxLutSize = 65;

        internal const int k_ShadowCascadeMinCount = 1;
        internal const int k_ShadowCascadeMaxCount = 4;

#if UNITY_EDITOR
        [NonSerialized]
        internal UniversalRenderPipelineEditorResources m_EditorResourcesAsset;

        public static readonly string packagePath = "Packages/com.unity.render-pipelines.universal";
        public static readonly string editorResourcesGUID = "a3d8d823eedde654bb4c11a1cfaf1abb";

        public static UniversalRenderPipelineAsset Create(ScriptableRendererData rendererData = null)
        {
            // Create Universal RP Asset
            var instance = CreateInstance<UniversalRenderPipelineAsset>();
            if (rendererData != null)
                instance.m_RendererDataList[0] = rendererData;
            else
                instance.m_RendererDataList[0] = CreateInstance<ForwardRendererData>();

            // Initialize default Renderer
            instance.m_EditorResourcesAsset = instance.editorResources;

            return instance;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812")]
        internal class CreateUniversalPipelineAsset : EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                //Create asset
                AssetDatabase.CreateAsset(Create(CreateRendererAsset(pathName, RendererType.ForwardRenderer)), pathName);
            }
        }

        [MenuItem("Assets/Create/Rendering/Universal Render Pipeline/Pipeline Asset (Forward Renderer)", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateUniversalPipeline()
        {
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, CreateInstance<CreateUniversalPipelineAsset>(),
                "UniversalRenderPipelineAsset.asset", null, null);
        }

        static ScriptableRendererData CreateRendererAsset(string path, RendererType type, bool relativePath = true)
        {
            ScriptableRendererData data = CreateRendererData(type);
            string dataPath;
            if (relativePath)
                dataPath =
                    $"{Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path))}_Renderer{Path.GetExtension(path)}";
            else
                dataPath = path;
            AssetDatabase.CreateAsset(data, dataPath);
            return data;
        }

        static ScriptableRendererData CreateRendererData(RendererType type)
        {
            switch (type)
            {
                case RendererType.ForwardRenderer:
                    return CreateInstance<ForwardRendererData>();
                // 2D renderer is experimental
                case RendererType._2DRenderer:
                    return CreateInstance<Experimental.Rendering.Universal.Renderer2DData>();
                // Forward Renderer is the fallback renderer that works on all platforms
                default:
                    return CreateInstance<ForwardRendererData>();
            }
        }

        //[MenuItem("Assets/Create/Rendering/Universal Pipeline Editor Resources", priority = CoreUtils.assetCreateMenuPriority1)]
        static void CreateUniversalPipelineEditorResources()
        {
            var instance = CreateInstance<UniversalRenderPipelineEditorResources>();
            ResourceReloader.ReloadAllNullIn(instance, packagePath);
            AssetDatabase.CreateAsset(instance, string.Format("Assets/{0}.asset", typeof(UniversalRenderPipelineEditorResources).Name));
        }

        UniversalRenderPipelineEditorResources editorResources
        {
            get
            {
                if (m_EditorResourcesAsset != null && !m_EditorResourcesAsset.Equals(null))
                    return m_EditorResourcesAsset;

                string resourcePath = AssetDatabase.GUIDToAssetPath(editorResourcesGUID);
                var objs = InternalEditorUtility.LoadSerializedFileAndForget(resourcePath);
                m_EditorResourcesAsset = objs != null && objs.Length > 0 ? objs.First() as UniversalRenderPipelineEditorResources : null;
                return m_EditorResourcesAsset;
            }
        }
#endif

        public ScriptableRendererData LoadBuiltinRendererData(RendererType type = RendererType.ForwardRenderer)
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            return m_RendererDataList[0] =
                CreateRendererAsset("Assets/ForwardRenderer.asset", type, false);
#else
            m_RendererDataList[0] = null;
            return m_RendererDataList[0];
#endif
        }

        protected override RenderPipeline CreatePipeline()
        {
            if (m_RendererDataList == null)
                m_RendererDataList = new ScriptableRendererData[1];

            // If no default data we can't create pipeline instance
            if (m_RendererDataList[m_DefaultRendererIndex] == null)
            {
                // If previous version and current version are miss-matched then we are waiting for the upgrader to kick in
                    return null;

                // Debug.LogError(
                //     $"Default Renderer is missing, make sure there is a Renderer assigned as the default on the current Universal RP asset:{UniversalRenderPipeline.asset.name}",
                //     this);
                // return null;
            }

            CreateRenderers();
            return new UniversalRenderPipeline(this);
        }

        void DestroyRenderers()
        {
            if (m_Renderers == null)
                return;

            for (int i = 0; i < m_Renderers.Length; i++)
                DestroyRenderer(ref m_Renderers[i]);
        }

        void DestroyRenderer(ref ScriptableRenderer renderer)
        {
            if (renderer != null)
            {
                renderer.Dispose();
                renderer = null;
            }
        }

        protected override void OnValidate()
        {
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnValidate();
        }

        protected override void OnDisable()
        {
            DestroyRenderers();

            // This will call RenderPipelineManager.CleanupRenderPipeline that in turn disposes the render pipeline instance and
            // assign pipeline asset reference to null
            base.OnDisable();
        }

        void CreateRenderers()
        {
            DestroyRenderers();

            if (m_Renderers == null || m_Renderers.Length != m_RendererDataList.Length)
                m_Renderers = new ScriptableRenderer[m_RendererDataList.Length];

            for (int i = 0; i < m_RendererDataList.Length; ++i)
            {
                if (m_RendererDataList[i] != null)
                    m_Renderers[i] = m_RendererDataList[i].InternalCreateRenderer();
            }
        }

        Material GetMaterial(DefaultMaterialType materialType)
        {
#if UNITY_EDITOR
            if (scriptableRendererData == null || editorResources == null)
                return null;

            var material = scriptableRendererData.GetDefaultMaterial(materialType);
            if (material != null)
                return material;

            switch (materialType)
            {
                case DefaultMaterialType.Standard:
                    return editorResources.materials.lit;

                case DefaultMaterialType.Particle:
                    return editorResources.materials.particleLit;

                case DefaultMaterialType.Terrain:
                    return editorResources.materials.terrainLit;

                // Unity Builtin Default
                default:
                    return null;
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// Returns the default renderer being used by this pipeline.
        /// </summary>
        public ScriptableRenderer scriptableRenderer
        {
            get
            {
                if (m_RendererDataList?.Length > m_DefaultRendererIndex && m_RendererDataList[m_DefaultRendererIndex] == null)
                {
                    Debug.LogError("Default renderer is missing from the current Pipeline Asset.", this);
                    return null;
                }

                if (scriptableRendererData.isInvalidated || m_Renderers[m_DefaultRendererIndex] == null)
                {
                    DestroyRenderer(ref m_Renderers[m_DefaultRendererIndex]);
                    m_Renderers[m_DefaultRendererIndex] = scriptableRendererData.InternalCreateRenderer();
                }

                return m_Renderers[m_DefaultRendererIndex];
            }
        }

        /// <summary>
        /// Returns a renderer from the current pipeline asset
        /// </summary>
        /// <param name="index">Index to the renderer. If invalid index is passed, the default renderer is returned instead.</param>
        /// <returns></returns>
        public ScriptableRenderer GetRenderer(int index)
        {
            if (index == -1)
                index = m_DefaultRendererIndex;

            if (index >= m_RendererDataList.Length || index < 0 || m_RendererDataList[index] == null)
            {
                Debug.LogWarning(
                    $"Renderer at index {index.ToString()} is missing, falling back to Default Renderer {m_RendererDataList[m_DefaultRendererIndex].name}",
                    this);
                index = m_DefaultRendererIndex;
            }

            // RendererData list differs from RendererList. Create RendererList.
            if (m_Renderers == null || m_Renderers.Length < m_RendererDataList.Length)
                CreateRenderers();

            // This renderer data is outdated or invalid, we recreate the renderer
            // so we construct all render passes with the updated data
            if (m_RendererDataList[index].isInvalidated || m_Renderers[index] == null)
            {
                DestroyRenderer(ref m_Renderers[index]);
                m_Renderers[index] = m_RendererDataList[index].InternalCreateRenderer();
            }

            return m_Renderers[index];
        }

        internal ScriptableRendererData scriptableRendererData
        {
            get
            {
                if (m_RendererDataList[m_DefaultRendererIndex] == null)
                    CreatePipeline();

                return m_RendererDataList[m_DefaultRendererIndex];
            }
        }

#if UNITY_EDITOR
        internal GUIContent[] rendererDisplayList
        {
            get
            {
                GUIContent[] list = new GUIContent[m_RendererDataList.Length + 1];
                list[0] = new GUIContent($"Default Renderer ({RendererDataDisplayName(m_RendererDataList[m_DefaultRendererIndex])})");

                for (var i = 1; i < list.Length; i++)
                {
                    list[i] = new GUIContent($"{(i - 1).ToString()}: {RendererDataDisplayName(m_RendererDataList[i - 1])}");
                }
                return list;
            }
        }

        string RendererDataDisplayName(ScriptableRendererData data)
        {
            if (data != null)
                return data.name;

            return "NULL (Missing RendererData)";
        }

#endif
        
        //PicoVideo;OptimizedRenderPipeline;YangFan;Begin
        private enum XRTextureModifyRequestType
        {
            RenderScale,
            MSAA
        }

        private class XRTextureModifyRequest
        {
            internal XRTextureModifyRequestType type;
            internal float renderscale;
            internal MsaaQuality msaaQuality;
        }

        private Queue<XRTextureModifyRequest> m_ModifyRequestQueue = new Queue<XRTextureModifyRequest>();
        private const int XR_TEXTURE_MODIFY_DELAY = 1;
        private int m_XRTextureModifyDelayRemain = 1;
        private int m_LastModifyFrameIndex = -1;

        internal void SetXRTextureModifyRequest()
        {
            if (m_LastModifyFrameIndex == Time.frameCount || m_ModifyRequestQueue == null || m_ModifyRequestQueue.Count == 0)
            {
                return;
            }
            
            m_LastModifyFrameIndex = Time.frameCount;
            
            if (m_XRTextureModifyDelayRemain > 0)
            {
                m_XRTextureModifyDelayRemain -= 1;
                return;
            }

            XRTextureModifyRequest frontModifyRequest = m_ModifyRequestQueue.Dequeue();
            if (frontModifyRequest.type == XRTextureModifyRequestType.RenderScale)
            {
                Debug.Log($"SetXRTextureModifyRequest : Modify RenderScale To {frontModifyRequest.renderscale:F2}");
                m_RenderScale = frontModifyRequest.renderscale;
            }
            else
            {
                Debug.Log($"SetXRTextureModifyRequest : Modify MSAA To {frontModifyRequest.msaaQuality}");
                m_MSAA = frontModifyRequest.msaaQuality;
            }
            
            m_XRTextureModifyDelayRemain = XR_TEXTURE_MODIFY_DELAY;
        }
        //PicoVideo;OptimizedRenderPipeline;YangFan;End
        
        internal int[] rendererIndexList
        {
            get
            {
                int[] list = new int[m_RendererDataList.Length + 1];
                for (int i = 0; i < list.Length; i++)
                {
                    list[i] = i - 1;
                }
                return list;
            }
        }

        public bool supportsCameraDepthTexture
        {
            get { return m_RequireDepthTexture; }
            set { m_RequireDepthTexture = value; }
        }

        public bool supportsCameraOpaqueTexture
        {
            get { return m_RequireOpaqueTexture; }
            set { m_RequireOpaqueTexture = value; }
        }

        public Downsampling opaqueDownsampling
        {
            get { return m_OpaqueDownsampling; }
        }
        
        //PicoVideo;Basic;Ernst;Begin
        public bool supportsCameraTransparentTexture
        {
            get { return m_RequireTransparentTexture; }
            set { m_RequireTransparentTexture = value; }
        }
        //PicoVideo;Basic;Ernst;End

        public bool supportsTerrainHoles
        {
            get { return m_SupportsTerrainHoles; }
        }
        
        //PicoVideo;AppSW;Ernst;Begin
        public bool supportsMotionVector
        {
            get => m_RequireMotionVector;
            set => m_RequireMotionVector = value;
        }
        public CommonDownsampling motionVectorDownsampling
        {
            get => m_MotionVectorDownsampling;
            set => m_MotionVectorDownsampling = value;
        }
        //PicoVideo;AppSW;Ernst;End

        /// <summary>
        /// Returns the active store action optimization value.
        /// </summary>
        /// <returns>Returns the active store action optimization value.</returns>
        public StoreActionsOptimization storeActionsOptimization
        {
            get { return m_StoreActionsOptimization; }
            set { m_StoreActionsOptimization = value; }
        }

        public bool supportsHDR
        {
            get { return m_SupportsHDR; }
            set { m_SupportsHDR = value; }
        }

        public int msaaSampleCount
        {
            get { return (int)m_MSAA; }
            set
            {
                MsaaQuality newMsaa = (MsaaQuality)value;
                if (m_MSAA == newMsaa)
                {
                    return;
                }
                
                m_ModifyRequestQueue.Enqueue(new XRTextureModifyRequest()
                {
                    type = XRTextureModifyRequestType.MSAA,
                    msaaQuality = newMsaa
                });
            }
        }

        public float renderScale
        {
            get { return m_RenderScale; }
            set
            {
                float newRenderScale = ValidateRenderScale(value);
                if (Math.Abs(m_RenderScale - newRenderScale) < 0.01F)
                {
                    return;
                }
                
                m_ModifyRequestQueue.Enqueue(new XRTextureModifyRequest()
                {
                    type = XRTextureModifyRequestType.RenderScale,
                    renderscale = newRenderScale
                });
            }
        }

        //PicoVideo;FSR;ZhengLingFeng;Begin
        /// <summary>
        /// Returns the upscaling filter desired by the user
        /// Note: Filter selections differ from actual filters in that they may include "meta-filters" such as
        ///       "Automatic" which resolve to an actual filter at a later time.
        /// </summary>
        public UpscalingFilterSelection upscalingFilter
        {
            get { return m_UpscalingFilter; }
            set { m_UpscalingFilter = value; }
        }

        /// <summary>
        /// If this property is set to true, the value from the fsrSharpness property will control the intensity of the
        /// sharpening filter associated with FidelityFX Super Resolution.
        /// </summary>
        public bool fsrOverrideSharpness
        {
            get { return m_FsrOverrideSharpness; }
            set { m_FsrOverrideSharpness = value; }
        }

        /// <summary>
        /// Controls the intensity of the sharpening filter associated with FidelityFX Super Resolution.
        /// A value of 1.0 produces maximum sharpness while a value of 0.0 disables the sharpening filter entirely.
        ///
        /// Note: This value only has an effect when the fsrOverrideSharpness property is set to true.
        /// </summary>
        public float fsrSharpness
        {
            get { return m_FsrSharpness; }
            set { m_FsrSharpness = value; }
        }
        //PicoVideo;FSR;ZhengLingFeng;End

        //PicoVideo;DynamicResolution;Ernst;Begin
        public float automaticScaleMin
        {
            get { return m_AutomaticScaleMin; }
            set { m_AutomaticScaleMin = value; }
        }

        public float automaticScaleMax
        {
            get { return m_AutomaticScaleMax; }
            set { m_AutomaticScaleMax = value; }
        }
        public float automaticFPSTarget
        {
            get { return m_AutomaticFPSTarget; }
            set { m_AutomaticFPSTarget = value; }
        }
        public DynamicResolutionController.DynamicResolutionType dynamicResolutionType
        {
            get { return m_DynamicResolutionType; }
            set { m_DynamicResolutionType = value; }
        }
        //PicoVideo;DynamicResolution;Ernst;End

        public LightRenderingMode mainLightRenderingMode
        {
            get { return m_MainLightRenderingMode; }
        }

        public bool supportsMainLightShadows
        {
            get { return m_MainLightShadowsSupported; }
        }

        //PicoVideo;LightMode;YangFan;Begin
        public float bakeGIIntensityMultiplier
        {
            get => m_BakeGIIntensityMultiplier;
            set => m_BakeGIIntensityMultiplier = value;
        }
        public bool dirSpecularLightingmapEnable
        {
            get => m_DirSpecularLightingmapEnable;
            set => m_DirSpecularLightingmapEnable = value;
        }
        public float dirSpecularLightingingmapMultiplier
        {
            get => m_DirSpecularLightingingmapMultiplier;
            set => m_DirSpecularLightingingmapMultiplier = value;
        }
        //PicoVideo;LightMode;YangFan;End

        //PicoVideo;LightMode;XiaoPengCheng;Begin
        public Color globalAdjustColor
        {
            get { return m_GlobalAdjustColor; }
            set { m_GlobalAdjustColor = value; }
        }
        //PicoVideo;LightMode;XiaoPengCheng;End
        
        //PicoVideo;UIDarkenMode;ZhouShaoyang;Begin
        public Color globalDarkenColor
        {
            get => m_GlobalDarkenColor;
            set => m_GlobalDarkenColor = value; 
        }
        //PicoVideo;UIDarkenMode;ZhouShaoyang;End

        public int mainLightShadowmapResolution
        {
            get { return (int)m_MainLightShadowmapResolution; }
        }

        //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;Begin
        public bool enableCustomMainLightShadowRange
        {
            get { return m_EnableCustomMainLightShadowRange; }
            set { m_EnableCustomMainLightShadowRange = value; }
        }

        public CustomMainLightShadowRange customMainLightShadowRange
        {
            get { return m_CustomMainLightShadowRange; }
            set { m_CustomMainLightShadowRange = value; }
        }

        public bool resetCustomMainLightShadowRange
        {
            get { return m_ResetCustomMainLightShadowRange; }
            set { m_ResetCustomMainLightShadowRange = value; }
        }

        public bool debugCustomMainLightShadowRange
        {
            get { return m_DebugCustomMainLightShadowRange; }
        }
        //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;End

        public LightRenderingMode additionalLightsRenderingMode
        {
            get { return m_AdditionalLightsRenderingMode; }
        }

        public int maxAdditionalLightsCount
        {
            get { return m_AdditionalLightsPerObjectLimit; }
            set { m_AdditionalLightsPerObjectLimit = ValidatePerObjectLights(value); }
        }

        public bool supportsAdditionalLightShadows
        {
            get { return m_AdditionalLightShadowsSupported; }
        }

        public int additionalLightsShadowmapResolution
        {
            get { return (int)m_AdditionalLightsShadowmapResolution; }
        }

        /// <summary>
        /// Controls the maximum distance at which shadows are visible.
        /// </summary>
        public float shadowDistance
        {
            get { return m_ShadowDistance; }
            set { m_ShadowDistance = Mathf.Max(0.0f, value); }
        }

        /// <summary>
        /// Returns the number of shadow cascades.
        /// </summary>
        public int shadowCascadeCount
        {
            get { return m_ShadowCascadeCount; }
            set
            {
                if (value < k_ShadowCascadeMinCount || value > k_ShadowCascadeMaxCount)
                {
                    throw new ArgumentException($"Value ({value}) needs to be between {k_ShadowCascadeMinCount} and {k_ShadowCascadeMaxCount}.");
                }
                m_ShadowCascadeCount = value;
            }
        }

        /// <summary>
        /// Returns the split value.
        /// </summary>
        /// <returns>Returns a Float with the split value.</returns>
        public float cascade2Split
        {
            get { return m_Cascade2Split; }
        }

        /// <summary>
        /// Returns the split values.
        /// </summary>
        /// <returns>Returns a Vector2 with the split values.</returns>
        public Vector2 cascade3Split
        {
            get { return m_Cascade3Split; }
        }

        /// <summary>
        /// Returns the split values.
        /// </summary>
        /// <returns>Returns a Vector3 with the split values.</returns>
        public Vector3 cascade4Split
        {
            get { return m_Cascade4Split; }
        }

        /// <summary>
        /// The Shadow Depth Bias, controls the offset of the lit pixels.
        /// </summary>
        public float shadowDepthBias
        {
            get { return m_ShadowDepthBias; }
            set { m_ShadowDepthBias = ValidateShadowBias(value); }
        }

        /// <summary>
        /// Controls the distance at which the shadow casting surfaces are shrunk along the surface normal.
        /// </summary>
        public float shadowNormalBias
        {
            get { return m_ShadowNormalBias; }
            set { m_ShadowNormalBias = ValidateShadowBias(value); }
        }

        /// <summary>
        /// Returns true Soft Shadows are supported, false otherwise.
        /// </summary>
        public bool supportsSoftShadows
        {
            get { return m_SoftShadowsSupported; }
        }

        public bool supportsDynamicBatching
        {
            get { return m_SupportsDynamicBatching; }
            set { m_SupportsDynamicBatching = value; }
        }

        public bool supportsMixedLighting
        {
            get { return m_MixedLightingSupported; }
        }

        public ShaderVariantLogLevel shaderVariantLogLevel
        {
            get { return m_ShaderVariantLogLevel; }
            set { m_ShaderVariantLogLevel = value; }
        }

        /// <summary>
        /// Returns the selected update mode for volumes.
        /// </summary>
        public VolumeFrameworkUpdateMode volumeFrameworkUpdateMode => m_VolumeFrameworkUpdateMode;

        [Obsolete("PipelineDebugLevel is deprecated. Calling debugLevel is not necessary.", false)]
        public PipelineDebugLevel debugLevel
        {
            get => PipelineDebugLevel.Disabled ;
        }
        
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
        public bool optimizedRenderPipelineEnabled
        {
            get
            {
                return m_OptimizedRenderPipelineEnabled;
            }
            set
            {
                m_OptimizedRenderPipelineEnabled = value;
            }
        }
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End

        public bool useSRPBatcher
        {
            get { return m_UseSRPBatcher; }
            set { m_UseSRPBatcher = value; }
        }

        public ColorGradingMode colorGradingMode
        {
            get { return m_ColorGradingMode; }
            set { m_ColorGradingMode = value; }
        }

        public int colorGradingLutSize
        {
            get { return m_ColorGradingLutSize; }
            set { m_ColorGradingLutSize = Mathf.Clamp(value, k_MinLutSize, k_MaxLutSize); }
        }

       /// <summary>
       /// Set to true to allow Adaptive performance to modify graphics quality settings during runtime.
       /// Only applicable when Adaptive performance package is available.
       /// </summary>
        public bool useAdaptivePerformance
        {
            get { return m_UseAdaptivePerformance; }
            set { m_UseAdaptivePerformance = value; }
        }

        public override Material defaultMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Standard); }
        }

        public override Material defaultParticleMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        public override Material defaultLineMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Particle); }
        }

        public override Material defaultTerrainMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Terrain); }
        }

        public override Material defaultUIMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        public override Material defaultUIOverdrawMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        public override Material defaultUIETC1SupportedMaterial
        {
            get { return GetMaterial(DefaultMaterialType.UnityBuiltinDefault); }
        }

        public override Material default2DMaterial
        {
            get { return GetMaterial(DefaultMaterialType.Sprite); }
        }

        public override Shader defaultShader
        {
            get
            {
#if UNITY_EDITOR
                // TODO: When importing project, AssetPreviewUpdater:CreatePreviewForAsset will be called multiple time
                // which in turns calls this property to get the default shader.
                // The property should never return null as, when null, it loads the data using AssetDatabase.LoadAssetAtPath.
                // However it seems there's an issue that LoadAssetAtPath will not load the asset in some cases. so adding the null check
                // here to fix template tests.
                if (scriptableRendererData != null)
                {
                    Shader defaultShader = scriptableRendererData.GetDefaultShader();
                    if (defaultShader != null)
                        return defaultShader;
                }

                if (m_DefaultShader == null)
                {
                    string path = AssetDatabase.GUIDToAssetPath(ShaderUtils.GetShaderGUID(ShaderPathID.Lit));
                    m_DefaultShader  = AssetDatabase.LoadAssetAtPath<Shader>(path);
                }
#endif

                if (m_DefaultShader == null)
                    m_DefaultShader = Shader.Find(ShaderUtils.GetShaderPath(ShaderPathID.Lit));

                return m_DefaultShader;
            }
        }

#if UNITY_EDITOR
        public override Shader autodeskInteractiveShader
        {
            get { return editorResources?.shaders.autodeskInteractivePS; }
        }

        public override Shader autodeskInteractiveTransparentShader
        {
            get { return editorResources?.shaders.autodeskInteractiveTransparentPS; }
        }

        public override Shader autodeskInteractiveMaskedShader
        {
            get { return editorResources?.shaders.autodeskInteractiveMaskedPS; }
        }

        public override Shader terrainDetailLitShader
        {
            get { return editorResources?.shaders.terrainDetailLitPS; }
        }

        public override Shader terrainDetailGrassShader
        {
            get { return editorResources?.shaders.terrainDetailGrassPS; }
        }

        public override Shader terrainDetailGrassBillboardShader
        {
            get { return editorResources?.shaders.terrainDetailGrassBillboardPS; }
        }

        public override Shader defaultSpeedTree7Shader
        {
            get { return editorResources?.shaders.defaultSpeedTree7PS; }
        }

        public override Shader defaultSpeedTree8Shader
        {
            get { return editorResources?.shaders.defaultSpeedTree8PS; }
        }
#endif

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            if (k_AssetVersion < 3)
            {
                m_SoftShadowsSupported = (m_ShadowType == ShadowQuality.SoftShadows);
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 3;
            }

            if (k_AssetVersion < 4)
            {
                m_AdditionalLightShadowsSupported = m_LocalShadowsSupported;
                m_AdditionalLightsShadowmapResolution = m_LocalShadowsAtlasResolution;
                m_AdditionalLightsPerObjectLimit = m_MaxPixelLights;
                m_MainLightShadowmapResolution = m_ShadowAtlasResolution;
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 4;
            }

            if (k_AssetVersion < 5)
            {
                if (m_RendererType == RendererType.Custom)
                {
                    m_RendererDataList[0] = m_RendererData;
                }
                k_AssetPreviousVersion = k_AssetVersion;
                k_AssetVersion = 5;
            }

            if (k_AssetVersion < 6)
            {
#pragma warning disable 618 // Obsolete warning
                // Adding an upgrade here so that if it was previously set to 2 it meant 4 cascades.
                // So adding a 3rd cascade shifted this value up 1.
                int value = (int)m_ShadowCascades;
                if (value == 2)
                {
                    m_ShadowCascadeCount = 4;
                }
                else
                {
                    m_ShadowCascadeCount = value + 1;
                }
                k_AssetVersion = 6;
#pragma warning restore 618 // Obsolete warning
            }


#if UNITY_EDITOR
            if (k_AssetPreviousVersion != k_AssetVersion)
            {
                EditorApplication.delayCall += () => UpgradeAsset(this);
            }
#endif
        }

#if UNITY_EDITOR
        static void UpgradeAsset(UniversalRenderPipelineAsset asset)
        {
            if(asset.k_AssetPreviousVersion < 5)
            {
                if (asset.m_RendererType == RendererType.ForwardRenderer)
                {
                    var data = AssetDatabase.LoadAssetAtPath<ForwardRendererData>("Assets/ForwardRenderer.asset");
                    if (data)
                    {
                        asset.m_RendererDataList[0] = data;
                    }
                    else
                    {
                        asset.LoadBuiltinRendererData();
                    }
                    asset.m_RendererData = null; // Clears the old renderer
                }

                asset.k_AssetPreviousVersion = 5;
            }
        }
#endif

        float ValidateShadowBias(float value)
        {
            return Mathf.Max(0.0f, Mathf.Min(value, UniversalRenderPipeline.maxShadowBias));
        }

        int ValidatePerObjectLights(int value)
        {
            return System.Math.Max(0, System.Math.Min(value, UniversalRenderPipeline.maxPerObjectLights));
        }

        float ValidateRenderScale(float value)
        {
            return Mathf.Max(UniversalRenderPipeline.minRenderScale, Mathf.Min(value, UniversalRenderPipeline.maxRenderScale));
        }

        /// <summary>
        /// Check to see if the RendererData list contains valid RendererData references.
        /// </summary>
        /// <param name="partial">This bool controls whether to test against all or any, if false then there has to be no invalid RendererData</param>
        /// <returns></returns>
        internal bool ValidateRendererDataList(bool partial = false)
        {
            var emptyEntries = 0;
            for (int i = 0; i < m_RendererDataList.Length; i++) emptyEntries += ValidateRendererData(i) ? 0 : 1;
            if (partial)
                return emptyEntries == 0;
            return emptyEntries != m_RendererDataList.Length;
        }

        internal bool ValidateRendererData(int index)
        {
            // Check to see if you are asking for the default renderer
            if (index == -1) index = m_DefaultRendererIndex;
            return index < m_RendererDataList.Length ? m_RendererDataList[index] != null : false;
        }
    }
}
