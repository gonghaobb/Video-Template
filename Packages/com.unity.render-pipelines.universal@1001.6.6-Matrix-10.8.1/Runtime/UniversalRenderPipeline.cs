using System;
using Unity.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Rendering.Universal;
#endif
using UnityEngine.Scripting.APIUpdating;
using Lightmapping = UnityEngine.Experimental.GlobalIllumination.Lightmapping;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;

namespace UnityEngine.Rendering.LWRP
{
    [Obsolete("LWRP -> Universal (UnityUpgradable) -> UnityEngine.Rendering.Universal.UniversalRenderPipeline", true)]
    public class LightweightRenderPipeline
    {
        public LightweightRenderPipeline(LightweightRenderPipelineAsset asset)
        {
        }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public sealed partial class UniversalRenderPipeline : RenderPipeline
    {
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        
        public enum MaximumEyeBufferRenderingInactiveReasonType
        {
            Succeed = 0,
            RenderFeature = 1 << 0,
            HDR = 1 << 1,
            PostProcessing = 1 << 2,
            OtherReason = 1 << 3,
        }
        
        private static int s_MaximumEyeBufferRenderingInactiveReason = 0;

        public static int maximumEyeBufferRenderingInactiveReason
        {
            get
            {
                return s_MaximumEyeBufferRenderingInactiveReason;
            }
            set
            {
                s_MaximumEyeBufferRenderingInactiveReason = value;
            }
        }
#endif
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End

        
        public const string k_ShaderTagName = "UniversalPipeline";

        private static class Profiling
        {
            private static Dictionary<int, ProfilingSampler> s_HashSamplerCache = new Dictionary<int, ProfilingSampler>();
            public static readonly ProfilingSampler unknownSampler = new ProfilingSampler("Unknown");

            // Specialization for camera loop to avoid allocations.
            public static ProfilingSampler TryGetOrAddCameraSampler(Camera camera)
            {
                #if UNIVERSAL_PROFILING_NO_ALLOC
                    return unknownSampler;
                #else
                    ProfilingSampler ps = null;
                    int cameraId = camera.GetHashCode();
                    bool exists = s_HashSamplerCache.TryGetValue(cameraId, out ps);
                    if (!exists)
                    {
                        // NOTE: camera.name allocates!
                        ps = new ProfilingSampler( $"{nameof(UniversalRenderPipeline)}.{nameof(RenderSingleCamera)}: {camera.name}");
                        s_HashSamplerCache.Add(cameraId, ps);
                    }
                    return ps;
                #endif
            }

            public static class Pipeline
            {
                // TODO: Would be better to add Profiling name hooks into RenderPipeline.cs, requires changes outside of Universal.
#if UNITY_2021_1_OR_NEWER
                public static readonly ProfilingSampler beginContextRendering  = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(BeginContextRendering)}");
                public static readonly ProfilingSampler endContextRendering    = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(EndContextRendering)}");
#else
                public static readonly ProfilingSampler beginFrameRendering  = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(BeginFrameRendering)}");
                public static readonly ProfilingSampler endFrameRendering    = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(EndFrameRendering)}");
#endif
                public static readonly ProfilingSampler beginCameraRendering = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(BeginCameraRendering)}");
                public static readonly ProfilingSampler endCameraRendering   = new ProfilingSampler($"{nameof(RenderPipeline)}.{nameof(EndCameraRendering)}");

                const string k_Name = nameof(UniversalRenderPipeline);
                public static readonly ProfilingSampler initializeCameraData           = new ProfilingSampler($"{k_Name}.{nameof(InitializeCameraData)}");
                public static readonly ProfilingSampler initializeStackedCameraData    = new ProfilingSampler($"{k_Name}.{nameof(InitializeStackedCameraData)}");
                public static readonly ProfilingSampler initializeAdditionalCameraData = new ProfilingSampler($"{k_Name}.{nameof(InitializeAdditionalCameraData)}");
                public static readonly ProfilingSampler initializeRenderingData        = new ProfilingSampler($"{k_Name}.{nameof(InitializeRenderingData)}");
                public static readonly ProfilingSampler initializeShadowData           = new ProfilingSampler($"{k_Name}.{nameof(InitializeShadowData)}");
                public static readonly ProfilingSampler initializeLightData            = new ProfilingSampler($"{k_Name}.{nameof(InitializeLightData)}");
                public static readonly ProfilingSampler getPerObjectLightFlags         = new ProfilingSampler($"{k_Name}.{nameof(GetPerObjectLightFlags)}");
                public static readonly ProfilingSampler getMainLightIndex              = new ProfilingSampler($"{k_Name}.{nameof(GetMainLightIndex)}");
                public static readonly ProfilingSampler setupPerFrameShaderConstants   = new ProfilingSampler($"{k_Name}.{nameof(SetupPerFrameShaderConstants)}");

                public static class Renderer
                {
                    const string k_Name = nameof(ScriptableRenderer);
                    public static readonly ProfilingSampler setupCullingParameters = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderer.SetupCullingParameters)}");
                    public static readonly ProfilingSampler setup                  = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderer.Setup)}");
                };

                public static class Context
                {
                    const string k_Name = nameof(Context);
                    public static readonly ProfilingSampler submit = new ProfilingSampler($"{k_Name}.{nameof(ScriptableRenderContext.Submit)}");
                };

                public static class XR
                {
                    public static readonly ProfilingSampler mirrorView = new ProfilingSampler("XR Mirror View");
                };
            };
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        internal static XRSystem m_XRSystem = new XRSystem();
#endif

        public static float maxShadowBias
        {
            get => 10.0f;
        }

        public static float minRenderScale
        {
            get => 0.1f;
        }

        public static float maxRenderScale
        {
            get => 2.0f;
        }

        // Amount of Lights that can be shaded per object (in the for loop in the shader)
        public static int maxPerObjectLights
        {
            // No support to bitfield mask and int[] in gles2. Can't index fast more than 4 lights.
            // Check Lighting.hlsl for more details.
            get => (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2) ? 4 : 8;
        }

        // These limits have to match same limits in Input.hlsl
        const int k_MaxVisibleAdditionalLightsMobileShaderLevelLessThan45 = 16;
        const int k_MaxVisibleAdditionalLightsMobile    = 32;
        const int k_MaxVisibleAdditionalLightsNonMobile = 256;
        public static int maxVisibleAdditionalLights
        {
            get
            {
                bool isMobile = Application.isMobilePlatform;
                if (isMobile && (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && Graphics.minOpenGLESVersion <= OpenGLESVersion.OpenGLES30)))
                    return k_MaxVisibleAdditionalLightsMobileShaderLevelLessThan45;

                // GLES can be selected as platform on Windows (not a mobile platform) but uniform buffer size so we must use a low light count.
                return (isMobile || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                        ? k_MaxVisibleAdditionalLightsMobile : k_MaxVisibleAdditionalLightsNonMobile;
            }
        }

        public UniversalRenderPipeline(UniversalRenderPipelineAsset asset)
        {
            SetSupportedRenderingFeatures();

            // In QualitySettings.antiAliasing disabled state uses value 0, where in URP 1
            int qualitySettingsMsaaSampleCount = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
            bool msaaSampleCountNeedsUpdate = qualitySettingsMsaaSampleCount != asset.msaaSampleCount;

            // Let engine know we have MSAA on for cases where we support MSAA backbuffer
            if (msaaSampleCountNeedsUpdate)
            {
                QualitySettings.antiAliasing = asset.msaaSampleCount;
#if ENABLE_VR && ENABLE_XR_MODULE
                XRSystem.UpdateMSAALevel(asset.msaaSampleCount);
#endif
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            XRSystem.UpdateRenderScale(asset.renderScale);
#endif
            // For compatibility reasons we also match old LightweightPipeline tag.
            Shader.globalRenderPipeline = "UniversalPipeline,LightweightPipeline";

            Lightmapping.SetDelegate(lightsDelegate);

            CameraCaptureBridge.enabled = true;

            RenderingUtils.ClearSystemInfoCache();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            Shader.globalRenderPipeline = "";
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures();
            ShaderData.instance.Dispose();
            DeferredShaderData.instance.Dispose();

#if ENABLE_VR && ENABLE_XR_MODULE
            m_XRSystem?.Dispose();
#endif

#if UNITY_EDITOR
            SceneViewDrawMode.ResetDrawMode();
#endif
            Lightmapping.ResetDelegate();
            CameraCaptureBridge.enabled = false;
        }

#if UNITY_2021_1_OR_NEWER
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
        {
            Render(renderContext, new List<Camera>(cameras));
        }
#endif

#if UNITY_2021_1_OR_NEWER
        protected override void Render(ScriptableRenderContext renderContext, List<Camera> cameras)
#else
        protected override void Render(ScriptableRenderContext renderContext, Camera[] cameras)
#endif
        {
            //PicoVideo;OptimizedRenderPipeline;YangFan;Begin
            asset.SetXRTextureModifyRequest();
            //PicoVideo;OptimizedRenderPipeline;YangFan;Begin
            // TODO: Would be better to add Profiling name hooks into RenderPipelineManager.
            // C#8 feature, only in >= 2020.2
            using var profScope = new ProfilingScope(null, ProfilingSampler.Get(URPProfileId.UniversalRenderTotal));

#if UNITY_2021_1_OR_NEWER
            using (new ProfilingScope(null, Profiling.Pipeline.beginContextRendering))
            {
                BeginContextRendering(renderContext, cameras);
            }
#else
            using(new ProfilingScope(null, Profiling.Pipeline.beginFrameRendering))
            {
                BeginFrameRendering(renderContext, cameras);
            }
#endif

            GraphicsSettings.lightsUseLinearIntensity = (QualitySettings.activeColorSpace == ColorSpace.Linear);
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;
            SetupPerFrameShaderConstants();
#if ENABLE_VR && ENABLE_XR_MODULE
            // Update XR MSAA level per frame.
            XRSystem.UpdateMSAALevel(asset.msaaSampleCount);
#endif


            SortCameras(cameras);
#if UNITY_2021_1_OR_NEWER
            for (int i = 0; i < cameras.Count; ++i)
#else
            for (int i = 0; i < cameras.Length; ++i)
#endif
            {
                var camera = cameras[i];
                if (IsGameCamera(camera))
                {
                    RenderCameraStack(renderContext, camera);
                }
                else
                {
                    using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
                    {
                        BeginCameraRendering(renderContext, camera);
                    }
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                VFX.VFXManager.PrepareCamera(camera);
#endif
                    UpdateVolumeFramework(camera, null);

                    RenderSingleCamera(renderContext, camera);

                    using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
                    {
                        EndCameraRendering(renderContext, camera);
                    }
                }
            }
#if UNITY_2021_1_OR_NEWER
            using (new ProfilingScope(null, Profiling.Pipeline.endContextRendering))
            {
                EndContextRendering(renderContext, cameras);
            }
#else
            using(new ProfilingScope(null, Profiling.Pipeline.endFrameRendering))
            {
                EndFrameRendering(renderContext, cameras);
            }
#endif
        }

        /// <summary>
        /// Standalone camera rendering. Use this to render procedural cameras.
        /// This method doesn't call <c>BeginCameraRendering</c> and <c>EndCameraRendering</c> callbacks.
        /// </summary>
        /// <param name="context">Render context used to record commands during execution.</param>
        /// <param name="camera">Camera to render.</param>
        /// <seealso cref="ScriptableRenderContext"/>
        public static void RenderSingleCamera(ScriptableRenderContext context, Camera camera)
        {
            UniversalAdditionalCameraData additionalCameraData = null;
            if (IsGameCamera(camera))
                camera.gameObject.TryGetComponent(out additionalCameraData);

            if (additionalCameraData != null && additionalCameraData.renderType != CameraRenderType.Base)
            {
                Debug.LogWarning("Only Base cameras can be rendered with standalone RenderSingleCamera. Camera will be skipped.");
                return;
            }

            InitializeCameraData(camera, additionalCameraData, true, out var cameraData);
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            if (asset.useAdaptivePerformance)
                ApplyAdaptivePerformance(ref cameraData);
#endif
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
            RenderSingleCamera(context, cameraData, cameraData.postProcessEnabled, camera == Camera.main);
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End

        }

        static bool TryGetCullingParameters(CameraData cameraData, out ScriptableCullingParameters cullingParams)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                cullingParams = cameraData.xr.cullingParams;

                // Sync the FOV on the camera to match the projection from the XR device
                if (!cameraData.camera.usePhysicalProperties)
                    cameraData.camera.fieldOfView = Mathf.Rad2Deg * Mathf.Atan(1.0f / cullingParams.stereoProjectionMatrix.m11) * 2.0f;

                return true;
            }
#endif

            return cameraData.camera.TryGetCullingParameters(false, out cullingParams);
        }

        /// <summary>
        /// Renders a single camera. This method will do culling, setup and execution of the renderer.
        /// </summary>
        /// <param name="context">Render context used to record commands during execution.</param>
        /// <param name="cameraData">Camera rendering data. This might contain data inherited from a base camera.</param>
        /// <param name="anyPostProcessingEnabled">True if at least one camera has post-processing enabled in the stack, false otherwise.</param>
        /// <param name="isMainCamera">Is this camera belong to main camera</param>
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
        static void RenderSingleCamera(ScriptableRenderContext context, CameraData cameraData, bool anyPostProcessingEnabled, bool isMainCamera)
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
        {
            Camera camera = cameraData.camera;
            var renderer = cameraData.renderer;
            if (renderer == null)
            {
                Debug.LogWarning(string.Format("Trying to render {0} with an invalid renderer. Camera rendering will be skipped.", camera.name));
                return;
            }

            if (!TryGetCullingParameters(cameraData, out var cullingParameters))
                return;

            ScriptableRenderer.current = renderer;
            bool isSceneViewCamera = cameraData.isSceneViewCamera;

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            // The named CommandBuffer will close its "profiling scope" on execution.
            // That will orphan ProfilingScope markers as the named CommandBuffer markers are their parents.
            // Resulting in following pattern:
            // exec(cmd.start, scope.start, cmd.end) and exec(cmd.start, scope.end, cmd.end)
            CommandBuffer cmd = CommandBufferPool.Get();

            // TODO: move skybox code from C++ to URP in order to remove the call to context.Submit() inside DrawSkyboxPass
            // Until then, we can't use nested profiling scopes with XR multipass
            CommandBuffer cmdScope = cameraData.xr.enabled ? null : cmd;

            ProfilingSampler sampler = Profiling.TryGetOrAddCameraSampler(camera);
            using (new ProfilingScope(cmdScope, sampler)) // Enqueues a "BeginSample" command into the CommandBuffer cmd
            {
                DynamicResolutionController.Update(cameraData, cmd);//PicoVideo;DynamicResolution;Ernst
                renderer.Clear(cameraData.renderType);

                using (new ProfilingScope( cmd, Profiling.Pipeline.Renderer.setupCullingParameters))
                {
                    renderer.SetupCullingParameters(ref cullingParameters, ref cameraData);
                }

                context.ExecuteCommandBuffer(cmd); // Send all the commands enqueued so far in the CommandBuffer cmd, to the ScriptableRenderContext context
                cmd.Clear();

#if UNITY_EDITOR
                // Emit scene view UI
                if (isSceneViewCamera)
                {
                    ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
                }
#endif

                var cullResults = context.Cull(ref cullingParameters);
                
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
                InitializeRenderingData(asset, ref cameraData, ref cullResults, anyPostProcessingEnabled, isMainCamera,
                    out var renderingData);
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
                if (asset.useAdaptivePerformance)
                    ApplyAdaptivePerformance(ref renderingData);
#endif

                using (new ProfilingScope(cmd, Profiling.Pipeline.Renderer.setup))
                {
                    renderer.Setup(context, ref renderingData);
                }

                // Timing scope inside
                renderer.Execute(context, ref renderingData);

            } // When ProfilingSample goes out of scope, an "EndSample" command is enqueued into CommandBuffer cmd

            cameraData.xr.EndCamera(cmd, cameraData);
            context.ExecuteCommandBuffer(cmd); // Sends to ScriptableRenderContext all the commands enqueued since cmd.Clear, i.e the "EndSample" command
            CommandBufferPool.Release(cmd);

            using (new ProfilingScope(cmd, Profiling.Pipeline.Context.submit))
            {
                context.Submit(); // Actually execute the commands that we previously sent to the ScriptableRenderContext context
            }

            ScriptableRenderer.current = null;
        }

        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
        static bool DoAllCamerasNeedOffScreen(ScriptableRenderer renderer, Camera baseCamera)
        {
            bool renderFeatureEnabled = false;
            if (renderer != null)
            {
                var rendererFeatures = renderer.rendererFeatures;
                if (rendererFeatures != null)
                {
                    for (int i = 0; i < rendererFeatures.Count; ++i)
                    {
                        if (rendererFeatures[i].isActive)
                        {
                            renderFeatureEnabled = true;
                            break;
                        }
                    }    
                }    
            }
            var hdr = baseCamera.allowHDR && asset.supportsHDR;
            bool massResolved = UniversalRenderPipeline.asset.msaaSampleCount > 1 && ForwardRenderer.PlatformRequiresExplicitMsaaResolve();
            int extendRequirementFlag = ForwardRendererExtend.GetRequirementFlag(baseCamera);
            return renderFeatureEnabled || hdr ||
                   ForwardRendererExtend.GetFlag(extendRequirementFlag,
                       ForwardRendererExtend.REQUIRE_CREATE_COLOR_TEXTURE) ||
                   ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_OPAQUE_TEXTURE) || ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_TRANSPARENT_TEXTURE) || massResolved;
        }
        
        static bool DoCameraNeedOffScreen(UniversalAdditionalCameraData cameraData)
        {
            int extendRequirementFlag = ForwardRendererExtend.GetRequirementFlag(cameraData.camera);
            bool massResolved = UniversalRenderPipeline.asset.msaaSampleCount > 1 && ForwardRenderer.PlatformRequiresExplicitMsaaResolve();
            return cameraData.renderPostProcessing ||
                   ForwardRendererExtend.GetFlag(extendRequirementFlag,
                       ForwardRendererExtend.REQUIRE_CREATE_COLOR_TEXTURE) ||
                   ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_OPAQUE_TEXTURE) || ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_TRANSPARENT_TEXTURE) || massResolved;
        }
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
        
        /// <summary>
        // Renders a camera stack. This method calls RenderSingleCamera for each valid camera in the stack.
        // The last camera resolves the final target to screen.
        /// </summary>
        /// <param name="context">Render context used to record commands during execution.</param>
        /// <param name="camera">Camera to render.</param>
        static void RenderCameraStack(ScriptableRenderContext context, Camera baseCamera)
        {
            using var profScope = new ProfilingScope(null, ProfilingSampler.Get(URPProfileId.RenderCameraStack));

            baseCamera.TryGetComponent<UniversalAdditionalCameraData>(out var baseCameraAdditionalData);

            // Overlay cameras will be rendered stacked while rendering base cameras
            if (baseCameraAdditionalData != null && baseCameraAdditionalData.renderType == CameraRenderType.Overlay)
                return;

            // renderer contains a stack if it has additional data and the renderer supports stacking
            var renderer = baseCameraAdditionalData?.scriptableRenderer;
            bool supportsCameraStacking = renderer != null && renderer.supportedRenderingFeatures.cameraStacking;
            List<Camera> cameraStack = (supportsCameraStacking) ? baseCameraAdditionalData?.cameraStack : null;

            bool anyPostProcessingEnabled = baseCameraAdditionalData != null && baseCameraAdditionalData.renderPostProcessing;

            // We need to know the last active camera in the stack to be able to resolve
            // rendering to screen when rendering it. The last camera in the stack is not
            // necessarily the last active one as it users might disable it.
            int lastActiveOverlayCameraIndex = -1;
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
            // if base camera enables postprocess, lastActiveOffScreenIndex = -1
            // if not, lastActiveOffScreenIndex = -2
            int lastActiveOffScreenIndex = -2;
            if (anyPostProcessingEnabled || DoAllCamerasNeedOffScreen(renderer, baseCamera))
            {
                lastActiveOffScreenIndex = -1;
            }
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
            if (cameraStack != null)
            {
                var baseCameraRendererType = baseCameraAdditionalData?.scriptableRenderer.GetType();
                bool shouldUpdateCameraStack = false;

                for (int i = 0; i < cameraStack.Count; ++i)
                {
                    Camera currCamera = cameraStack[i];
                    if (currCamera == null)
                    {
                        shouldUpdateCameraStack = true;
                        continue;
                    }

                    if (currCamera.isActiveAndEnabled)
                    {
                        currCamera.TryGetComponent<UniversalAdditionalCameraData>(out var data);

                        if (data == null || data.renderType != CameraRenderType.Overlay)
                        {
                            Debug.LogWarning(string.Format("Stack can only contain Overlay cameras. {0} will skip rendering.", currCamera.name));
                            continue;
                        }

                        var currCameraRendererType = data?.scriptableRenderer.GetType();
                        if (currCameraRendererType != baseCameraRendererType)
                        {
                            var renderer2DType = typeof(Experimental.Rendering.Universal.Renderer2D);
                            if (currCameraRendererType != renderer2DType && baseCameraRendererType != renderer2DType)
                            {
                                Debug.LogWarning(string.Format("Only cameras with compatible renderer types can be stacked. {0} will skip rendering", currCamera.name));
                                continue;
                            }
                        }

                        anyPostProcessingEnabled |= data.renderPostProcessing;
                        lastActiveOverlayCameraIndex = i;
                        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
                        if (DoCameraNeedOffScreen(data))
                        {
                            lastActiveOffScreenIndex = i;
                        }
                        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
                    }
                }
                if(shouldUpdateCameraStack)
                {
                    baseCameraAdditionalData.UpdateCameraStack();
                }
            }

            // Post-processing not supported in GLES2.
            anyPostProcessingEnabled &= SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            bool isStackedRendering = lastActiveOverlayCameraIndex != -1;
            using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
            {
                BeginCameraRendering(context, baseCamera);
            }

            // Update volumeframework before initializing additional camera data
            UpdateVolumeFramework(baseCamera, baseCameraAdditionalData);
            InitializeCameraData(baseCamera, baseCameraAdditionalData, !isStackedRendering, out var baseCameraData);
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
            baseCameraData.cameraIndexInStack = -1;
            baseCameraData.lastCameraIndexWhoNeedOffScreen = lastActiveOffScreenIndex;
            baseCameraData.includeOverlay = false;
            if (cameraStack != null && cameraStack.Count > 0)
            {
                for (int i = 0; i < cameraStack.Count; ++i)
                {
                    if (cameraStack[i].enabled)
                    {
                        baseCameraData.includeOverlay = true;
                        break;
                    }
                }
            }
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
#if ENABLE_VR && ENABLE_XR_MODULE
            var originalTargetDesc = baseCameraData.cameraTargetDescriptor;
            var xrActive = false;
            var xrPasses = m_XRSystem.SetupFrame(baseCameraData);
            foreach (XRPass xrPass in xrPasses)
            {
                baseCameraData.xr = xrPass;

                // XRTODO: remove isStereoEnabled in 2021.x
#pragma warning disable 0618
                baseCameraData.isStereoEnabled = xrPass.enabled;
#pragma warning restore 0618

                if (baseCameraData.xr.enabled)
                {
                    xrActive = true;
                    // Helper function for updating cameraData with xrPass Data
                    m_XRSystem.UpdateCameraData(ref baseCameraData, baseCameraData.xr);

                    // Update volume manager to use baseCamera's settings for XR multipass rendering.
                    if (baseCameraData.xr.multipassId > 0)
                    {
                        UpdateVolumeFramework(baseCamera, baseCameraAdditionalData);
                    }
					m_XRSystem.BeginLateLatching(baseCamera, xrPass);
                }
#endif

#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                VFX.VFXManager.PrepareCamera(baseCamera);
#endif
#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
                if (asset.useAdaptivePerformance)
                    ApplyAdaptivePerformance(ref baseCameraData);
#endif
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
                RenderSingleCamera(context, baseCameraData, anyPostProcessingEnabled, baseCamera == Camera.main);
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End

                using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
                {
                    EndCameraRendering(context, baseCamera);
                }
#if ENABLE_VR && ENABLE_XR_MODULE
                m_XRSystem.EndLateLatching(baseCamera, xrPass);
#endif
                if (isStackedRendering)
                {
                    for (int i = 0; i < cameraStack.Count; ++i)
                    {
                        var currCamera = cameraStack[i];
                        if (!currCamera.isActiveAndEnabled)
                            continue;

                        currCamera.TryGetComponent<UniversalAdditionalCameraData>(out var currCameraData);
                        // Camera is overlay and enabled
                        if (currCameraData != null)
                        {
                            // Copy base settings from base camera data and initialize initialize remaining specific settings for this camera type.
                            CameraData overlayCameraData = baseCameraData;
                            bool lastCamera = i == lastActiveOverlayCameraIndex;
                            
                            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
                            overlayCameraData.cameraIndexInStack = i;
                            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End

                            using (new ProfilingScope(null, Profiling.Pipeline.beginCameraRendering))
                            {
                                BeginCameraRendering(context, currCamera);
                            }
#if VISUAL_EFFECT_GRAPH_0_0_1_OR_NEWER
                            //It should be called before culling to prepare material. When there isn't any VisualEffect component, this method has no effect.
                            VFX.VFXManager.PrepareCamera(currCamera);
#endif
                            UpdateVolumeFramework(currCamera, currCameraData);
                            InitializeAdditionalCameraData(currCamera, currCameraData, lastCamera, ref overlayCameraData);
#if ENABLE_VR && ENABLE_XR_MODULE
                            if (baseCameraData.xr.enabled)
                                m_XRSystem.UpdateFromCamera(ref overlayCameraData.xr, overlayCameraData);
#endif
                            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
                            RenderSingleCamera(context, overlayCameraData, anyPostProcessingEnabled, baseCamera == Camera.main);
                            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
                            
                            using (new ProfilingScope(null, Profiling.Pipeline.endCameraRendering))
                            {
                                EndCameraRendering(context, currCamera);
                            }
                        }
                    }
                }

#if ENABLE_VR && ENABLE_XR_MODULE
                if (baseCameraData.xr.enabled)
                    baseCameraData.cameraTargetDescriptor = originalTargetDesc;
            }

            if (xrActive)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, Profiling.Pipeline.XR.mirrorView))
                {
                    m_XRSystem.RenderMirrorView(cmd, baseCamera);
                }

                context.ExecuteCommandBuffer(cmd);
                context.Submit();
                CommandBufferPool.Release(cmd);
            }

            m_XRSystem.ReleaseFrame();
#endif
        }

        static void UpdateVolumeFramework(Camera camera, UniversalAdditionalCameraData additionalCameraData)
        {
            using var profScope = new ProfilingScope(null, ProfilingSampler.Get(URPProfileId.UpdateVolumeFramework));

            // We update the volume framework for:
            // * All cameras in the editor when not in playmode
            // * scene cameras
            // * cameras with update mode set to EveryFrame
            // * cameras with update mode set to UsePipelineSettings and the URP Asset set to EveryFrame
            bool shouldUpdate = camera.cameraType == CameraType.SceneView;
            shouldUpdate |= additionalCameraData != null && additionalCameraData.requiresVolumeFrameworkUpdate;

            #if UNITY_EDITOR
            shouldUpdate |= Application.isPlaying == false;
            #endif

            // When we have volume updates per-frame disabled...
            if (!shouldUpdate && additionalCameraData)
            {
                // Create a local volume stack and cache the state if it's null
                if (additionalCameraData.volumeStack == null)
                {
                    camera.UpdateVolumeStack(additionalCameraData);
                }

                VolumeManager.instance.stack = additionalCameraData.volumeStack;
                return;
            }

            // When we want to update the volumes every frame...

            // We destroy the volumeStack in the additional camera data, if present, to make sure
            // it gets recreated and initialized if the update mode gets later changed to ViaScripting...
            if (additionalCameraData && additionalCameraData.volumeStack != null)
            {
                camera.DestroyVolumeStack(additionalCameraData);
            }

            // Get the mask + trigger and update the stack
            camera.GetVolumeLayerMaskAndTrigger(additionalCameraData, out LayerMask layerMask, out Transform trigger);
            VolumeManager.instance.ResetMainStack();
            VolumeManager.instance.Update(trigger, layerMask);
        }

        static bool CheckPostProcessForDepth(in CameraData cameraData)
        {
            if (!cameraData.postProcessEnabled)
                return false;

            if (cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                return true;

            var stack = VolumeManager.instance.stack;

            if (stack.GetComponent<DepthOfField>().IsActive())
                return true;

            if (stack.GetComponent<MotionBlur>().IsActive())
                return true;

            return false;
        }

        static void SetSupportedRenderingFeatures()
        {
#if UNITY_EDITOR
            SupportedRenderingFeatures.active = new SupportedRenderingFeatures()
            {
                reflectionProbeModes = SupportedRenderingFeatures.ReflectionProbeModes.None,
                defaultMixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive,
                mixedLightingModes = SupportedRenderingFeatures.LightmapMixedBakeModes.Subtractive | SupportedRenderingFeatures.LightmapMixedBakeModes.IndirectOnly | SupportedRenderingFeatures.LightmapMixedBakeModes.Shadowmask,
                lightmapBakeTypes = LightmapBakeType.Baked | LightmapBakeType.Mixed,
                lightmapsModes = LightmapsMode.CombinedDirectional | LightmapsMode.NonDirectional,
                lightProbeProxyVolumes = false,
                motionVectors = false,
                receiveShadows = false,
                reflectionProbes = true,
                particleSystemInstancing = true
            };
            SceneViewDrawMode.SetupDrawMode();
#endif
        }

        static void InitializeCameraData(Camera camera, UniversalAdditionalCameraData additionalCameraData, bool resolveFinalTarget, out CameraData cameraData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeCameraData);

            cameraData = new CameraData();
            InitializeStackedCameraData(camera, additionalCameraData, ref cameraData);
            InitializeAdditionalCameraData(camera, additionalCameraData, resolveFinalTarget, ref cameraData);

            ///////////////////////////////////////////////////////////////////
            // Descriptor settings                                            /
            ///////////////////////////////////////////////////////////////////

            var renderer = additionalCameraData?.scriptableRenderer;
            bool rendererSupportsMSAA = renderer != null && renderer.supportedRenderingFeatures.msaa;

            int msaaSamples = 1;
            if (camera.allowMSAA && asset.msaaSampleCount > 1 && rendererSupportsMSAA)
                msaaSamples = (camera.targetTexture != null) ? camera.targetTexture.antiAliasing : asset.msaaSampleCount;
#if ENABLE_VR && ENABLE_XR_MODULE
            // Use XR's MSAA if camera is XR camera. XR MSAA needs special handle here because it is not per Camera.
            // Multiple cameras could render into the same XR display and they should share the same MSAA level.
            if (cameraData.xrRendering)
                msaaSamples = XRSystem.GetMSAALevel();
#endif

            bool needsAlphaChannel = Graphics.preserveFramebufferAlpha;
            cameraData.cameraTargetDescriptor = CreateRenderTextureDescriptor(camera, cameraData.renderScale,
                cameraData.isHdrEnabled, msaaSamples, needsAlphaChannel, cameraData.requiresOpaqueTexture);
        }

        /// <summary>
        /// Initialize camera data settings common for all cameras in the stack. Overlay cameras will inherit
        /// settings from base camera.
        /// </summary>
        /// <param name="baseCamera">Base camera to inherit settings from.</param>
        /// <param name="baseAdditionalCameraData">Component that contains additional base camera data.</param>
        /// <param name="cameraData">Camera data to initialize setttings.</param>
        static void InitializeStackedCameraData(Camera baseCamera, UniversalAdditionalCameraData baseAdditionalCameraData, ref CameraData cameraData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeStackedCameraData);

            var settings = asset;
            cameraData.targetTexture = baseCamera.targetTexture;
            cameraData.cameraType = baseCamera.cameraType;
            bool isSceneViewCamera = cameraData.isSceneViewCamera;

            ///////////////////////////////////////////////////////////////////
            // Environment and Post-processing settings                       /
            ///////////////////////////////////////////////////////////////////
            if (isSceneViewCamera)
            {
                cameraData.volumeLayerMask = 1; // "Default"
                cameraData.volumeTrigger = null;
                cameraData.isStopNaNEnabled = false;
                cameraData.isDitheringEnabled = false;
                cameraData.antialiasing = AntialiasingMode.None;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
#if ENABLE_VR && ENABLE_XR_MODULE
                cameraData.xrRendering = false;
#endif
            }
            else if (baseAdditionalCameraData != null)
            {
                cameraData.volumeLayerMask = baseAdditionalCameraData.volumeLayerMask;
                cameraData.volumeTrigger = baseAdditionalCameraData.volumeTrigger == null ? baseCamera.transform : baseAdditionalCameraData.volumeTrigger;
                cameraData.isStopNaNEnabled = baseAdditionalCameraData.stopNaN && SystemInfo.graphicsShaderLevel >= 35;
                cameraData.isDitheringEnabled = baseAdditionalCameraData.dithering;
                cameraData.antialiasing = baseAdditionalCameraData.antialiasing;
                cameraData.antialiasingQuality = baseAdditionalCameraData.antialiasingQuality;
#if ENABLE_VR && ENABLE_XR_MODULE
                cameraData.xrRendering = baseAdditionalCameraData.allowXRRendering && m_XRSystem.RefreshXrSdk();
#endif
            }
            else
            {
                cameraData.volumeLayerMask = 1; // "Default"
                cameraData.volumeTrigger = null;
                cameraData.isStopNaNEnabled = false;
                cameraData.isDitheringEnabled = false;
                cameraData.antialiasing = AntialiasingMode.None;
                cameraData.antialiasingQuality = AntialiasingQuality.High;
#if ENABLE_VR && ENABLE_XR_MODULE
                cameraData.xrRendering = m_XRSystem.RefreshXrSdk();
#endif
            }

            ///////////////////////////////////////////////////////////////////
            // Settings that control output of the camera                     /
            ///////////////////////////////////////////////////////////////////

            cameraData.isHdrEnabled = baseCamera.allowHDR && settings.supportsHDR;

            Rect cameraRect = baseCamera.rect;
            cameraData.pixelRect = baseCamera.pixelRect;
            cameraData.pixelWidth = baseCamera.pixelWidth;
            cameraData.pixelHeight = baseCamera.pixelHeight;
            cameraData.aspectRatio = (float)cameraData.pixelWidth / (float)cameraData.pixelHeight;
            cameraData.isDefaultViewport = (!(Math.Abs(cameraRect.x) > 0.0f || Math.Abs(cameraRect.y) > 0.0f ||
                Math.Abs(cameraRect.width) < 1.0f || Math.Abs(cameraRect.height) < 1.0f));

            // Discard variations lesser than kRenderScaleThreshold.
            // Scale is only enabled for gameview.
            const float kRenderScaleThreshold = 0.05f;
            cameraData.renderScale = (Mathf.Abs(1.0f - settings.renderScale) < kRenderScaleThreshold) ? 1.0f : settings.renderScale;

            //PicoVideo;FSR;ZhengLingFeng;Begin
            // Convert the upscaling filter selection from the pipeline asset into an image upscaling filter
            cameraData.upscalingFilter = ResolveUpscalingFilterSelection(new Vector2(cameraData.pixelWidth, cameraData.pixelHeight), cameraData.renderScale, settings.upscalingFilter);

            if (cameraData.renderScale > 1.0f)
            {
                cameraData.imageScalingMode = FSRUtils.ImageScalingMode.Downscaling;
            }
            else if ((cameraData.renderScale < 1.0f) || (cameraData.upscalingFilter == FSRUtils.ImageUpscalingFilter.FSR))
            {
                // When FSR is enabled, we still consider 100% render scale an upscaling operation.
                // This allows us to run the FSR shader passes all the time since they improve visual quality even at 100% scale.

                cameraData.imageScalingMode = FSRUtils.ImageScalingMode.Upscaling;
            }
            else
            {
                cameraData.imageScalingMode = FSRUtils.ImageScalingMode.None;
            }

            cameraData.fsrOverrideSharpness = settings.fsrOverrideSharpness;
            cameraData.fsrSharpness = settings.fsrSharpness;
            //PicoVideo;FSR;ZhengLingFeng;End
            
#if ENABLE_VR && ENABLE_XR_MODULE
            cameraData.xr = m_XRSystem.emptyPass;
            XRSystem.UpdateRenderScale(cameraData.renderScale);
#else
            cameraData.xr = XRPass.emptyPass;
#endif

            var commonOpaqueFlags = SortingCriteria.CommonOpaque;
            var noFrontToBackOpaqueFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;
            bool hasHSRGPU = SystemInfo.hasHiddenSurfaceRemovalOnGPU;
            bool canSkipFrontToBackSorting = (baseCamera.opaqueSortMode == OpaqueSortMode.Default && hasHSRGPU) || baseCamera.opaqueSortMode == OpaqueSortMode.NoDistanceSort;

            cameraData.defaultOpaqueSortFlags = canSkipFrontToBackSorting ? noFrontToBackOpaqueFlags : commonOpaqueFlags;
            cameraData.captureActions = CameraCaptureBridge.GetCaptureActions(baseCamera);
        }

        /// <summary>
        /// Initialize settings that can be different for each camera in the stack.
        /// </summary>
        /// <param name="camera">Camera to initialize settings from.</param>
        /// <param name="additionalCameraData">Additional camera data component to initialize settings from.</param>
        /// <param name="resolveFinalTarget">True if this is the last camera in the stack and rendering should resolve to camera target.</param>
        /// <param name="cameraData">Settings to be initilized.</param>
        static void InitializeAdditionalCameraData(Camera camera, UniversalAdditionalCameraData additionalCameraData, bool resolveFinalTarget, ref CameraData cameraData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeAdditionalCameraData);

            var settings = asset;
            cameraData.camera = camera;

            bool anyShadowsEnabled = settings.supportsMainLightShadows || settings.supportsAdditionalLightShadows;
            cameraData.maxShadowDistance = Mathf.Min(settings.shadowDistance, camera.farClipPlane);
            cameraData.maxShadowDistance = (anyShadowsEnabled && cameraData.maxShadowDistance >= camera.nearClipPlane) ? cameraData.maxShadowDistance : 0.0f;

            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            if (isSceneViewCamera)
            {
                cameraData.renderType = CameraRenderType.Base;
                cameraData.clearDepth = true;
                cameraData.postProcessEnabled = CoreUtils.ArePostProcessesEnabled(camera);
                cameraData.requiresDepthTexture = settings.supportsCameraDepthTexture;
                cameraData.requiresOpaqueTexture = settings.supportsCameraOpaqueTexture;
                cameraData.renderer = asset.scriptableRenderer;
            }
            else if (additionalCameraData != null)
            {
                cameraData.renderType = additionalCameraData.renderType;
                cameraData.clearDepth = (additionalCameraData.renderType != CameraRenderType.Base) ? additionalCameraData.clearDepth : true;
                cameraData.postProcessEnabled = additionalCameraData.renderPostProcessing;
                cameraData.maxShadowDistance = (additionalCameraData.renderShadows) ? cameraData.maxShadowDistance : 0.0f;
                cameraData.requiresDepthTexture = additionalCameraData.requiresDepthTexture;
                cameraData.requiresOpaqueTexture = additionalCameraData.requiresColorTexture;
                cameraData.renderer = additionalCameraData.scriptableRenderer;
                cameraData.rtDynamicResolutionType = additionalCameraData.m_RTDynamicResolutionType;//PicoVideo;DynamicResolution;Ernst
            }
            else
            {
                cameraData.renderType = CameraRenderType.Base;
                cameraData.clearDepth = true;
                cameraData.postProcessEnabled = false;
                cameraData.requiresDepthTexture = settings.supportsCameraDepthTexture;
                cameraData.requiresOpaqueTexture = settings.supportsCameraOpaqueTexture;
                cameraData.renderer = asset.scriptableRenderer;
            }

            // Disables post if GLes2
            cameraData.postProcessEnabled &= SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2;

            cameraData.requiresDepthTexture |= isSceneViewCamera || CheckPostProcessForDepth(cameraData);
            cameraData.resolveFinalTarget = resolveFinalTarget;

            // Disable depth and color copy. We should add it in the renderer instead to avoid performance pitfalls
            // of camera stacking breaking render pass execution implicitly.
            bool isOverlayCamera = (cameraData.renderType == CameraRenderType.Overlay);
            if (isOverlayCamera)
            {
                cameraData.requiresDepthTexture = false;
                cameraData.requiresOpaqueTexture = false;
            }
            
            //PicoVideo;DynamicResolution;Ernst;Begin
            if (cameraData.rtDynamicResolutionType != RTDynamicResolutionType.UseDynamicResolution)
            {
                cameraData.cameraTargetDescriptor.useDynamicScale = false;
                cameraData.cameraTargetDescriptor.width = (int)((float)camera.pixelWidth);
                cameraData.cameraTargetDescriptor.height = (int)((float)camera.pixelHeight);
            }
            //PicoVideo;DynamicResolution;Ernst;End

            Matrix4x4 projectionMatrix = camera.projectionMatrix;

            // Overlay cameras inherit viewport from base.
            // If the viewport is different between them we might need to patch the projection to adjust aspect ratio
            // matrix to prevent squishing when rendering objects in overlay cameras.
            if (isOverlayCamera && !camera.orthographic && cameraData.pixelRect != camera.pixelRect)
            {
                // m00 = (cotangent / aspect), therefore m00 * aspect gives us cotangent.
                float cotangent = camera.projectionMatrix.m00 * camera.aspect;

                // Get new m00 by dividing by base camera aspectRatio.
                float newCotangent = cotangent / cameraData.aspectRatio;
                projectionMatrix.m00 = newCotangent;
            }

            cameraData.SetViewAndProjectionMatrix(camera.worldToCameraMatrix, projectionMatrix);
        }

        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
        static void InitializeRenderingData(UniversalRenderPipelineAsset settings, ref CameraData cameraData, ref CullingResults cullResults,
            bool anyPostProcessingEnabled, bool isMainCamera, out RenderingData renderingData)
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeRenderingData);

            var visibleLights = cullResults.visibleLights;

            int mainLightIndex = GetMainLightIndex(settings, visibleLights);
            bool mainLightCastShadows = false;
            bool additionalLightsCastShadows = false;

            if (cameraData.maxShadowDistance > 0.0f)
            {
                mainLightCastShadows = (mainLightIndex != -1 && visibleLights[mainLightIndex].light != null &&
                                        visibleLights[mainLightIndex].light.shadows != LightShadows.None);

                // If additional lights are shaded per-pixel they cannot cast shadows
                if (settings.additionalLightsRenderingMode == LightRenderingMode.PerPixel)
                {
                    for (int i = 0; i < visibleLights.Length; ++i)
                    {
                        if (i == mainLightIndex)
                            continue;

                        Light light = visibleLights[i].light;

                        // UniversalRP doesn't support additional directional lights or point light shadows yet
                        if (visibleLights[i].lightType == LightType.Spot && light != null && light.shadows != LightShadows.None)
                        {
                            additionalLightsCastShadows = true;
                            break;
                        }
                    }
                }
            }

            renderingData.cullResults = cullResults;
            renderingData.cameraData = cameraData;
            InitializeLightData(settings, visibleLights, mainLightIndex, out renderingData.lightData);
            InitializeShadowData(settings, visibleLights, mainLightCastShadows, additionalLightsCastShadows && !renderingData.lightData.shadeAdditionalLightsPerVertex, out renderingData.shadowData);
            InitializePostProcessingData(settings, out renderingData.postProcessingData);
            renderingData.supportsDynamicBatching = settings.supportsDynamicBatching;
            renderingData.perObjectData = GetPerObjectLightFlags(renderingData.lightData.additionalLightsCount);
            renderingData.postProcessingEnabled = anyPostProcessingEnabled;
            
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
            renderingData.maximumEyeBufferRenderingEnabled = settings.GetOptimizedRenderPipelineFeatureActivated(OptimizedRenderPipelineFeature.MaximumEyeBufferRendering) && !renderingData.cameraData.includeOverlay;
            renderingData.isMainCamera = isMainCamera;
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End

        }

        static void InitializeShadowData(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, bool mainLightCastShadows, bool additionalLightsCastShadows, out ShadowData shadowData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeShadowData);

            m_ShadowBiasData.Clear();

            for (int i = 0; i < visibleLights.Length; ++i)
            {
                Light light = visibleLights[i].light;
                UniversalAdditionalLightData data = null;
                if (light != null)
                {
                    light.gameObject.TryGetComponent(out data);
                }

                if (data && !data.usePipelineSettings)
                    m_ShadowBiasData.Add(new Vector4(light.shadowBias, light.shadowNormalBias, 0.0f, 0.0f));
                else
                    m_ShadowBiasData.Add(new Vector4(settings.shadowDepthBias, settings.shadowNormalBias, 0.0f, 0.0f));
            }

            shadowData.bias = m_ShadowBiasData;
            shadowData.supportsMainLightShadows = SystemInfo.supportsShadows && settings.supportsMainLightShadows && mainLightCastShadows;

            // We no longer use screen space shadows in URP.
            // This change allows us to have particles & transparent objects receive shadows.
            shadowData.requiresScreenSpaceShadowResolve = false;

            shadowData.mainLightShadowCascadesCount = settings.shadowCascadeCount;
            shadowData.mainLightShadowmapWidth = settings.mainLightShadowmapResolution;
            shadowData.mainLightShadowmapHeight = settings.mainLightShadowmapResolution;

            switch (shadowData.mainLightShadowCascadesCount)
            {
                case 1:
                    shadowData.mainLightShadowCascadesSplit = new Vector3(1.0f, 0.0f, 0.0f);
                    break;

                case 2:
                    shadowData.mainLightShadowCascadesSplit = new Vector3(settings.cascade2Split, 1.0f, 0.0f);
                    break;

                case 3:
                    shadowData.mainLightShadowCascadesSplit = new Vector3(settings.cascade3Split.x, settings.cascade3Split.y, 0.0f);
                    break;

                default:
                    shadowData.mainLightShadowCascadesSplit = settings.cascade4Split;
                    break;
            }

            shadowData.supportsAdditionalLightShadows = SystemInfo.supportsShadows && settings.supportsAdditionalLightShadows && additionalLightsCastShadows;
            shadowData.additionalLightsShadowmapWidth = shadowData.additionalLightsShadowmapHeight = settings.additionalLightsShadowmapResolution;
            shadowData.supportsSoftShadows = settings.supportsSoftShadows && (shadowData.supportsMainLightShadows || shadowData.supportsAdditionalLightShadows);
            shadowData.shadowmapDepthBufferBits = 16;
        }

        static void InitializePostProcessingData(UniversalRenderPipelineAsset settings, out PostProcessingData postProcessingData)
        {
            postProcessingData.gradingMode = settings.supportsHDR
                ? settings.colorGradingMode
                : ColorGradingMode.LowDynamicRange;

            postProcessingData.lutSize = settings.colorGradingLutSize;
        }

        static void InitializeLightData(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights, int mainLightIndex, out LightData lightData)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.initializeLightData);

            int maxPerObjectAdditionalLights = UniversalRenderPipeline.maxPerObjectLights;
            int maxVisibleAdditionalLights = UniversalRenderPipeline.maxVisibleAdditionalLights;

            lightData.mainLightIndex = mainLightIndex;

            if (settings.additionalLightsRenderingMode != LightRenderingMode.Disabled)
            {
                lightData.additionalLightsCount =
                    Math.Min((mainLightIndex != -1) ? visibleLights.Length - 1 : visibleLights.Length,
                        maxVisibleAdditionalLights);
                lightData.maxPerObjectAdditionalLightsCount = Math.Min(settings.maxAdditionalLightsCount, maxPerObjectAdditionalLights);
            }
            else
            {
                lightData.additionalLightsCount = 0;
                lightData.maxPerObjectAdditionalLightsCount = 0;
            }

            lightData.shadeAdditionalLightsPerVertex = settings.additionalLightsRenderingMode == LightRenderingMode.PerVertex;
            lightData.visibleLights = visibleLights;
            lightData.supportsMixedLighting = settings.supportsMixedLighting;
        }

        static PerObjectData GetPerObjectLightFlags(int additionalLightsCount)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.getPerObjectLightFlags);

            var configuration = PerObjectData.ReflectionProbes | PerObjectData.Lightmaps | PerObjectData.LightProbe | PerObjectData.LightData | PerObjectData.OcclusionProbe | PerObjectData.ShadowMask;

            if (additionalLightsCount > 0)
            {
                configuration |= PerObjectData.LightData;

                // In this case we also need per-object indices (unity_LightIndices)
                if (!RenderingUtils.useStructuredBuffer)
                    configuration |= PerObjectData.LightIndices;
            }

            return configuration;
        }

        // Main Light is always a directional light
        static int GetMainLightIndex(UniversalRenderPipelineAsset settings, NativeArray<VisibleLight> visibleLights)
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.getMainLightIndex);

            int totalVisibleLights = visibleLights.Length;

            if (totalVisibleLights == 0 || settings.mainLightRenderingMode != LightRenderingMode.PerPixel)
                return -1;

            Light sunLight = RenderSettings.sun;
            int brightestDirectionalLightIndex = -1;
            float brightestLightIntensity = 0.0f;
            for (int i = 0; i < totalVisibleLights; ++i)
            {
                VisibleLight currVisibleLight = visibleLights[i];
                Light currLight = currVisibleLight.light;

                // Particle system lights have the light property as null. We sort lights so all particles lights
                // come last. Therefore, if first light is particle light then all lights are particle lights.
                // In this case we either have no main light or already found it.
                if (currLight == null)
                    break;

                if (currVisibleLight.lightType == LightType.Directional)
                {
                    // Sun source needs be a directional light
                    if (currLight == sunLight)
                        return i;

                    // In case no sun light is present we will return the brightest directional light
                    if (currLight.intensity > brightestLightIntensity)
                    {
                        brightestLightIntensity = currLight.intensity;
                        brightestDirectionalLightIndex = i;
                    }
                }
            }

            return brightestDirectionalLightIndex;
        }
        
        //PicoVideo;FoveatedFeature;YangFan;Begin
        public static bool fixedFoveatedRenderingEnabled
        {
            get
            {
                if (asset == null || !asset.enableTextureFoveatedFeature)
                {
                    return false;
                }
            
                return asset.currentTextureFoveatedFeatureQuality != TextureFoveatedFeatureQuality.Close;
            }
        }

        public static bool eyeTrackingFoveatedRenderingEnabled
        {
            get
            {
                if (asset == null || !asset.enableTextureFoveatedFeature)
                {
                    return false;
                }
            
                if (asset.currentEyeTrackingTextureFoveatedFeatureQuality == EyeTrackingTextureFoveatedFeatureQuality.Default)
                {
                    return fixedFoveatedRenderingEnabled;
                }
                else
                {
                    return asset.currentEyeTrackingTextureFoveatedFeatureQuality != EyeTrackingTextureFoveatedFeatureQuality.Close;
                }
            }
        }
        
        private static Vector2 s_FoveatedRenderingFocalPoint = Vector2.zero;

        internal static void SetEyeTrackingFocalPoint(ref RenderingData renderingData)
        {
            s_FoveatedRenderingFocalPoint = Vector2.zero;
            if (eyeTrackingFoveatedRenderingEnabled && XRRenderUtils.instance.IsSupportedEyeTracking())
            {
                s_FoveatedRenderingFocalPoint = XRRenderUtils.instance.GetFoveatedFocalPoint(renderingData.cameraData.GetProjectionMatrix().decomposeProjection);
            }
        }

        internal static void SetOffscreenRenderingState(bool isCreateCameraColorTexture)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2)
            {
                s_OffscreenRendering = isCreateCameraColorTexture;
            }
            else
            {
                s_OffscreenRendering = false;
            }
        }
        
#if PICO_VIDEO_VRS_EXTEND1_SUPPORTED
        private static HashSet<int> s_FoveatedRenderingTextureIdHash = new HashSet<int>();
#endif
        
#if PICO_VIDEO_VRS_EXTEND2_SUPPORTED
        private static HashSet<int> s_SubsampledLayoutFoveatedRenderingTextureIdHash = new HashSet<int>();
#endif

        public static bool IsEnableFoveatedRendering(int textureId)
        {
#if UNITY_EDITOR || !PLATFORM_ANDROID || !PICO_VIDEO_VRS_EXTEND2_SUPPORTED
            return false;
#else
            if (!asset.enableTextureFoveatedFeature)
            {
                return false;
            }
            
            return s_FoveatedRenderingTextureIdHash.Contains(textureId);
#endif
        }

        public static bool IsEnableFoveatedRenderingAndSubsampledLayout(int textureID)
        {
#if UNITY_EDITOR || !PLATFORM_ANDROID || !PICO_VIDEO_VRS_EXTEND2_SUPPORTED
            return false;
#else
            if (!asset.enableSubsampledLayout)
            {
                return false;
            }
            
            return IsEnableFoveatedRendering(textureID) && s_SubsampledLayoutFoveatedRenderingTextureIdHash.Contains(textureID);
#endif
        }
        
        private static bool s_FrameBufferFoveatedRenderingEnabled = false;
        private static bool s_FrameBufferSubsampledLayoutEnabled = false;
        private static float s_RestoreRenderScale = -1;
        private static bool s_OffscreenRendering = false;

        private static bool TryGetFoveatedRenderingParameters(out TextureFoveatedParameters parameters)
        {
            parameters = null;
            if (asset == null)
            {
                return false;
            }

            if (fixedFoveatedRenderingEnabled || eyeTrackingFoveatedRenderingEnabled)
            {
                parameters = eyeTrackingFoveatedRenderingEnabled ? asset.currentEyeTrackingTextureFoveatedParameters : asset.currentTextureFoveatedParameters;
                return true;
            }

            return false;
        }
        
        internal static void SetFrameBufferFoveatedRendering()
        {
            if (s_RestoreRenderScale > 0)
            {
                asset.renderScale = s_RestoreRenderScale;
                s_RestoreRenderScale = -1;
            }
            
            int level = -1; // -1 : None | 0 : Low | 1 : Medium | 2 : High | 3 : TopHigh
            if (asset != null && (fixedFoveatedRenderingEnabled || eyeTrackingFoveatedRenderingEnabled))
            {
                if (eyeTrackingFoveatedRenderingEnabled && asset.currentEyeTrackingTextureFoveatedFeatureQuality != EyeTrackingTextureFoveatedFeatureQuality.Default)
                {
                    level = (int) asset.currentEyeTrackingTextureFoveatedFeatureQuality;
                }
                else
                {
                    level = Mathf.Clamp((int) asset.currentTextureFoveatedFeatureQuality, 0, 3);
                }
            }

            bool needRecreateEyeBuffer = false;
            if (s_FrameBufferFoveatedRenderingEnabled && level == -1)
            {
                XRRenderUtils.instance.SetSystemFoveatedFeature(level);
                needRecreateEyeBuffer = true;
                s_FrameBufferFoveatedRenderingEnabled = false;
            }
            
            bool subsampledLayoutEnabled = asset.enableSubsampledLayout;
            if (s_FrameBufferSubsampledLayoutEnabled && !subsampledLayoutEnabled)
            {
                XRRenderUtils.instance.SetSystemSubsampledLayout(false);
                needRecreateEyeBuffer = true;
                s_FrameBufferSubsampledLayoutEnabled = false;
            }

            if (needRecreateEyeBuffer)
            {
                s_RestoreRenderScale = asset.renderScale;
                asset.renderScale = asset.renderScale > 0.11F ? asset.renderScale - 0.01F : asset.renderScale + 0.01F;
            }
            
            if (s_OffscreenRendering)
            {
                XRRenderUtils.instance.SetSystemFoveatedFeature(-1);
                XRRenderUtils.instance.SetSystemSubsampledLayout(false);
                return;
            }
            
            XRRenderUtils.instance.SetSystemFoveatedFeature(level);
            XRRenderUtils.instance.SetSystemSubsampledLayout(subsampledLayoutEnabled);
            
            if (level >= 0)
            {
                s_FrameBufferFoveatedRenderingEnabled = true;
            }

            if (subsampledLayoutEnabled)
            {
                s_FrameBufferSubsampledLayoutEnabled = true;
            }
        }
        
        private static void RecreateColorTextureIfNeed(CommandBuffer cmd, int textureID,
            ref RenderTextureDescriptor desc, bool isUrpDefaultCameraColorTexture = false)
        {
#if PICO_VIDEO_VRS_EXTEND1_SUPPORTED
            bool forceDestroyColorTexture = false;
            bool foveatedRenderingDisabled = !asset.enableTextureFoveatedFeature || (!fixedFoveatedRenderingEnabled && !eyeTrackingFoveatedRenderingEnabled);
            if (isUrpDefaultCameraColorTexture)
            {
                foveatedRenderingDisabled |= !CameraColorTextureFoveatedRenderingSupported();
            }
            if (s_FoveatedRenderingTextureIdHash.Contains(textureID) && foveatedRenderingDisabled)
            {
                forceDestroyColorTexture = true;
                s_FoveatedRenderingTextureIdHash.Remove(textureID);
            }
                
    #if PICO_VIDEO_VRS_EXTEND2_SUPPORTED
            bool subsampledLayoutDisabled = !asset.enableSubsampledLayout;
            if (isUrpDefaultCameraColorTexture)
            {
                subsampledLayoutDisabled |= !CameraColorTextureSubsampledLayoutSupported();
            }
            if (s_SubsampledLayoutFoveatedRenderingTextureIdHash.Contains(textureID) && subsampledLayoutDisabled)
            {
                forceDestroyColorTexture = true;
                s_SubsampledLayoutFoveatedRenderingTextureIdHash.Remove(textureID);
            }
    #endif
            if (forceDestroyColorTexture)
            {
                cmd.ReleaseTemporaryRT(textureID, true);
                cmd.GetTemporaryRT(textureID, desc, FilterMode.Bilinear);
            }
#endif
        }
        
        internal static bool cameraColorTextureAsRenderTargetAfterSampling = false;
        
        private static bool CameraColorTextureFoveatedRenderingSupported()
        {
            // 对于_CameraColorTexture来说，FoveatedRendering不支持的原因如下：
            // 1.非离屏渲染（SDK未提供subsampled接口）
            return s_OffscreenRendering;
        }

        private static bool CameraColorTextureSubsampledLayoutSupported()
        {
#if !PICO_VIDEO_VRS_EXTEND2_SUPPORTED
            return false;
#endif
            // 对于_CameraColorTexture来说，SubsampledLayout不支持的原因如下（自定义开启SubsampledLayout的贴图需要自己根据功能判断）：
            // 1.存在未支持的后期（景深尚未支持）
            // 2.在第一次采样读取后还存在写入的操作 cameraColorTextureAsRenderTargetAfterSampling
            // 3.不支持FR（SubsampledLayout强依赖于FR）
            if (!CameraColorTextureFoveatedRenderingSupported() || cameraColorTextureAsRenderTargetAfterSampling)
            {
                return false;
            }
            
            VolumeStack stack = VolumeManager.instance.stack;
            DepthOfField dof = stack.GetComponent<DepthOfField>();
            if (dof != null && dof.IsActive())
            {
                return false;
            }
            return true;
        }

        public static void SetTextureFoveatedRendering(CommandBuffer cmd, int textureID, ref RenderTextureDescriptor desc, bool isUrpDefaultCameraColorTexture = false)
        {
#if PICO_VIDEO_VRS_SUPPORTED            
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3 && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES2)
            {
                return;
            }
            
            RecreateColorTextureIfNeed(cmd, textureID, ref desc, isUrpDefaultCameraColorTexture);
            if (isUrpDefaultCameraColorTexture && !CameraColorTextureFoveatedRenderingSupported())
            {
                return;
            }
            
            bool subsampledLayoutEnabled = asset.enableSubsampledLayout;
            if (isUrpDefaultCameraColorTexture)
            {
                subsampledLayoutEnabled &= CameraColorTextureSubsampledLayoutSupported();
            }

            bool foveatedRenderingEnabled = false;
            if (TryGetFoveatedRenderingParameters(out TextureFoveatedParameters param))
            {
    #if PICO_VIDEO_VRS_EXTEND2_SUPPORTED
                cmd.EnableTexFoveatedFeature(textureID, param.foveationMinimum, subsampledLayoutEnabled);
    #else
                cmd.EnableTexFoveatedFeature(textureID, param.foveationMinimum);
    #endif
                cmd.SetTexFoveatedParameters(textureID, 0, s_FoveatedRenderingFocalPoint.x, s_FoveatedRenderingFocalPoint.y, param.foveationGainX, param.foveationGainY, param.foveationArea);
                foveatedRenderingEnabled = true;
            }
            
    #if PICO_VIDEO_VRS_EXTEND1_SUPPORTED
            if (foveatedRenderingEnabled)
            {
                s_FoveatedRenderingTextureIdHash.Add(textureID);
            }
    #endif
    #if PICO_VIDEO_VRS_EXTEND2_SUPPORTED
            if (foveatedRenderingEnabled && subsampledLayoutEnabled)
            {
                s_SubsampledLayoutFoveatedRenderingTextureIdHash.Add(textureID);
            }
    #endif       
#endif
        }
        //PicoVideo;FoveatedFeature;YangFan;End

        static void SetupPerFrameShaderConstants()
        {
            using var profScope = new ProfilingScope(null, Profiling.Pipeline.setupPerFrameShaderConstants);

            // When glossy reflections are OFF in the shader we set a constant color to use as indirect specular
            SphericalHarmonicsL2 ambientSH = RenderSettings.ambientProbe;
            Color linearGlossyEnvColor = new Color(ambientSH[0, 0], ambientSH[1, 0], ambientSH[2, 0]) * RenderSettings.reflectionIntensity;
            Color glossyEnvColor = CoreUtils.ConvertLinearToActiveColorSpace(linearGlossyEnvColor);
            Shader.SetGlobalVector(ShaderPropertyId.glossyEnvironmentColor, glossyEnvColor);

            // Ambient
            Shader.SetGlobalVector(ShaderPropertyId.ambientSkyColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientSkyColor));
            Shader.SetGlobalVector(ShaderPropertyId.ambientEquatorColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientEquatorColor));
            Shader.SetGlobalVector(ShaderPropertyId.ambientGroundColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.ambientGroundColor));

            // Used when subtractive mode is selected
            Shader.SetGlobalVector(ShaderPropertyId.subtractiveShadowColor, CoreUtils.ConvertSRGBToActiveColorSpace(RenderSettings.subtractiveShadowColor));

            // Required for 2D Unlit Shadergraph master node as it doesn't currently support hidden properties.
            Shader.SetGlobalColor(ShaderPropertyId.rendererColor, Color.white);

            //PicoVideo;LightMode;XiaoPengCheng;Begin
            Shader.SetGlobalColor(ShaderPropertyId.globalAdjustColor, asset.globalAdjustColor);
            //PicoVideo;LightMode;XiaoPengCheng;End

            //PicoVideo;UIDarkenMode;ZhouShaoyang;Begin
            Shader.SetGlobalColor(ShaderPropertyId.globalDarkenColor, asset.globalDarkenColor);
            //PicoVideo;UIDarkenMode;ZhouShaoyang;End
        }

        //PicoVideo;FSR;ZhengLingFeng;Begin
        /// <summary>
        /// Returns the best supported image upscaling filter based on the provided upscaling filter selection
        /// </summary>
        /// <param name="imageSize">Size of the final image</param>
        /// <param name="renderScale">Scale being applied to the final image size</param>
        /// <param name="selection">Upscaling filter selected by the user</param>
        /// <returns>Either the original filter provided, or the best replacement available</returns>
        static FSRUtils.ImageUpscalingFilter ResolveUpscalingFilterSelection(Vector2 imageSize, float renderScale, UpscalingFilterSelection selection)
        {
            // By default we just use linear filtering since it's the most compatible choice
            FSRUtils.ImageUpscalingFilter filter = FSRUtils.ImageUpscalingFilter.Linear;

            // Fall back to the automatic filter if FSR was selected, but isn't supported on the current platform
            if ((selection == UpscalingFilterSelection.FSR) && !FSRUtils.IsSupported())
            {
                selection = UpscalingFilterSelection.Auto;
            }

            switch (selection)
            {
                case UpscalingFilterSelection.Auto:
                {
                    // The user selected "auto" for their upscaling filter so we should attempt to choose the best filter
                    // for the current situation. When the current resolution and render scale are compatible with integer
                    // scaling we use the point sampling filter. Otherwise we just use the default filter (linear).
                    float pixelScale = (1.0f / renderScale);
                    bool isIntegerScale = Mathf.Approximately((pixelScale - Mathf.Floor(pixelScale)), 0.0f);

                    if (isIntegerScale)
                    {
                        float widthScale = (imageSize.x / pixelScale);
                        float heightScale = (imageSize.y / pixelScale);

                        bool isImageCompatible = (Mathf.Approximately((widthScale - Mathf.Floor(widthScale)), 0.0f) &&
                                                  Mathf.Approximately((heightScale - Mathf.Floor(heightScale)), 0.0f));

                        if (isImageCompatible)
                        {
                            filter = FSRUtils.ImageUpscalingFilter.Point;
                        }
                    }

                    break;
                }

                case UpscalingFilterSelection.Linear:
                {
                    // Do nothing since linear is already the default

                    break;
                }

                case UpscalingFilterSelection.Point:
                {
                    filter = FSRUtils.ImageUpscalingFilter.Point;

                    break;
                }

                case UpscalingFilterSelection.FSR:
                {
                    filter = FSRUtils.ImageUpscalingFilter.FSR;

                    break;
                }
            }

            return filter;
        }
        //PicoVideo;FSR;ZhengLingFeng;End

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
        static void ApplyAdaptivePerformance(ref CameraData cameraData)
        {
            var noFrontToBackOpaqueFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;
            if (AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipFrontToBackSorting)
                cameraData.defaultOpaqueSortFlags = noFrontToBackOpaqueFlags;

            var MaxShadowDistanceMultiplier = AdaptivePerformance.AdaptivePerformanceRenderSettings.MaxShadowDistanceMultiplier;
            cameraData.maxShadowDistance *= MaxShadowDistanceMultiplier;

            var RenderScaleMultiplier = AdaptivePerformance.AdaptivePerformanceRenderSettings.RenderScaleMultiplier;
            cameraData.renderScale *= RenderScaleMultiplier;

            // TODO
            if (!cameraData.xr.enabled)
            {
                cameraData.cameraTargetDescriptor.width = (int)(cameraData.camera.pixelWidth * cameraData.renderScale);
                cameraData.cameraTargetDescriptor.height = (int)(cameraData.camera.pixelHeight * cameraData.renderScale);
            }

            var antialiasingQualityIndex = (int)cameraData.antialiasingQuality - AdaptivePerformance.AdaptivePerformanceRenderSettings.AntiAliasingQualityBias;
            if (antialiasingQualityIndex < 0)
                cameraData.antialiasing = AntialiasingMode.None;
            cameraData.antialiasingQuality = (AntialiasingQuality)Mathf.Clamp(antialiasingQualityIndex, (int)AntialiasingQuality.Low, (int)AntialiasingQuality.High);
        }
        static void ApplyAdaptivePerformance(ref RenderingData renderingData)
        {
            if (AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipDynamicBatching)
                renderingData.supportsDynamicBatching = false;

            var MainLightShadowmapResolutionMultiplier = AdaptivePerformance.AdaptivePerformanceRenderSettings.MainLightShadowmapResolutionMultiplier;
            renderingData.shadowData.mainLightShadowmapWidth = (int)(renderingData.shadowData.mainLightShadowmapWidth * MainLightShadowmapResolutionMultiplier);
            renderingData.shadowData.mainLightShadowmapHeight = (int)(renderingData.shadowData.mainLightShadowmapHeight * MainLightShadowmapResolutionMultiplier);

            var MainLightShadowCascadesCountBias = AdaptivePerformance.AdaptivePerformanceRenderSettings.MainLightShadowCascadesCountBias;
            renderingData.shadowData.mainLightShadowCascadesCount = Mathf.Clamp(renderingData.shadowData.mainLightShadowCascadesCount - MainLightShadowCascadesCountBias, 0, 4);

            var shadowQualityIndex = AdaptivePerformance.AdaptivePerformanceRenderSettings.ShadowQualityBias;
            for (int i = 0; i < shadowQualityIndex; i++)
            {
                if (renderingData.shadowData.supportsSoftShadows)
                {
                    renderingData.shadowData.supportsSoftShadows = false;
                    continue;
                }

                if (renderingData.shadowData.supportsAdditionalLightShadows)
                {
                    renderingData.shadowData.supportsAdditionalLightShadows = false;
                    continue;
                }

                if (renderingData.shadowData.supportsMainLightShadows)
                {
                    renderingData.shadowData.supportsMainLightShadows = false;
                    continue;
                }

                break;
            }

            if (AdaptivePerformance.AdaptivePerformanceRenderSettings.LutBias >= 1 && renderingData.postProcessingData.lutSize == 32)
                renderingData.postProcessingData.lutSize = 16;
        }
#endif
    }
}
