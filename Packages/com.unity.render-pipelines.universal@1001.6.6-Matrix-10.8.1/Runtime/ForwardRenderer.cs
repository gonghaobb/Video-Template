using UnityEngine.Rendering.Universal.Internal;
using System.Reflection;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Rendering modes for Universal renderer.
    /// </summary>
    public enum RenderingMode
    {
        /// <summary>Render all objects and lighting in one pass, with a hard limit on the number of lights that can be applied on an object.</summary>
        Forward,
        /// <summary>Render all objects first in a g-buffer pass, then apply all lighting in a separate pass using deferred shading.</summary>
        Deferred
    };

    /// <summary>
    /// Default renderer for Universal RP.
    /// This renderer is supported on all Universal RP supported platforms.
    /// It uses a classic forward rendering strategy with per-object light culling.
    /// </summary>
    public sealed class ForwardRenderer : ScriptableRenderer
    {
        const int k_DepthStencilBufferBits = 32;

        private static class Profiling
        {
            private const string k_Name = nameof(ForwardRenderer);
            public static readonly ProfilingSampler createCameraRenderTarget = new ProfilingSampler($"{k_Name}.{nameof(CreateCameraRenderTarget)}");
        }

        // Rendering mode setup from UI.
        internal RenderingMode renderingMode { get { return RenderingMode.Forward;  } }
        // Actual rendering mode, which may be different (ex: wireframe rendering, harware not capable of deferred rendering).
        internal RenderingMode actualRenderingMode { get { return GL.wireframe || m_DeferredLights == null || !m_DeferredLights.IsRuntimeSupportedThisFrame()  ? RenderingMode.Forward : this.renderingMode; } }
        internal bool accurateGbufferNormals { get { return m_DeferredLights != null ? m_DeferredLights.AccurateGbufferNormals : false; } }
        ColorGradingLutPass m_ColorGradingLutPass;
        DepthOnlyPass m_DepthPrepass;
        DepthNormalOnlyPass m_DepthNormalPrepass;
        MainLightShadowCasterPass m_MainLightShadowCasterPass;
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;
        GBufferPass m_GBufferPass;
        CopyDepthPass m_GBufferCopyDepthPass;
        TileDepthRangePass m_TileDepthRangePass;
        TileDepthRangePass m_TileDepthRangeExtraPass; // TODO use subpass API to hide this pass
        DeferredPass m_DeferredPass;
        DrawObjectsPass m_RenderOpaqueForwardOnlyPass;
        DrawObjectsPass m_RenderOpaqueForwardPass;
        DrawObjectsPass m_RenderBeforePostOpaqueForwardPass;
        DrawObjectsPass m_RenderOverlayOpaqueForwardPass;
        DrawSkyboxPass m_DrawSkyboxPass;
        CopyDepthPass m_CopyDepthPass;
        CopyColorPass m_CopyColorPass;
        //PicoVideo;Basic;Ernst;Begin
        CopyColorPass m_CopyTransparentColorPass;
        const string TRANSPARENT_COLOR_NAME = "_CameraTransparentTexture";
        //PicoVideo;Basic;Ernst;End
        TransparentSettingsPass m_TransparentSettingsPass;
        DrawObjectsPass m_RenderTransparentForwardPass;
        DrawObjectsPass m_RenderBeforePostTransparentForwardPass;
        DrawObjectsPass m_RenderOverlayTransparentForwardPass;
        InvokeOnRenderObjectCallbackPass m_OnRenderObjectCallbackPass;
        PostProcessPass m_PostProcessPass;
        PostProcessPass m_FinalPostProcessPass;
        FinalBlitPass m_FinalBlitPass;
        CapturePass m_CapturePass;
#if ENABLE_VR && ENABLE_XR_MODULE
        XROcclusionMeshPass m_XROcclusionMeshPass;
        CopyDepthPass m_XRCopyDepthPass;
#endif
#if UNITY_EDITOR
        SceneViewDepthCopyPass m_SceneViewDepthCopyPass;
#endif
        DrawCorrectGammaUIPass m_DrawCorrectGammaUIPass; //PicoVideo;EditorUIColorAdjustment;WuJunLin

        //PicoVideo;DynamicResolution;Ernst;Begin
        DynamicResolutionPass m_DynamicResolutionPass;
        //PicoVideo;DynamicResolution;Ernst;End

        //PicoVideo;AppSW;Ernst;Begin
        MotionVectorPass m_MotionVectorPass;
        //PicoVideo;AppSW;Ernst;End
        //PicoVideo;Debug;Ernst;Begin
        DebugPass m_DebugPass;
        //PicoVideo;Debug;Ernst;End

        RenderTargetHandle m_ActiveCameraColorAttachment;
        RenderTargetHandle m_ActiveCameraDepthAttachment;
        RenderTargetHandle m_CameraColorAttachment;
        RenderTargetHandle m_CameraDepthAttachment;
        RenderTargetHandle m_DepthTexture;
        RenderTargetHandle m_NormalsTexture;
        RenderTargetHandle[] m_GBufferHandles;
        RenderTargetHandle m_OpaqueColor;
        RenderTargetHandle m_AfterPostProcessColor;
        RenderTargetHandle m_ColorGradingLut;
        // For tiled-deferred shading.
        RenderTargetHandle m_DepthInfoTexture;
        RenderTargetHandle m_TileDepthInfoTexture;

        ForwardLights m_ForwardLights;
        DeferredLights m_DeferredLights;
#pragma warning disable 414
        RenderingMode m_RenderingMode;
#pragma warning restore 414
        StencilState m_DefaultStencilState;

        Material m_BlitMaterial;
        //PicoVideo;SubPass;Ernst;Begin
        Material m_BlitInSubPassMaterial;
        //PicoVideo;SubPass;Ernst;End
        Material m_CopyDepthMaterial;
        Material m_SamplingMaterial;
        Material m_ScreenspaceShadowsMaterial;
        Material m_TileDepthInfoMaterial;
        Material m_TileDeferredMaterial;
        Material m_StencilDeferredMaterial;

        //PicoVideo;FoveatedFeature;YangFan;Begin
        private static bool s_FoveationImageIsDirty = false;
        private static bool s_OnFocus = false;
        private static void OnFocusLost()
        {
            s_OnFocus = false;
        }
        
        private static void OnFocusAcquired()
        {
            s_FoveationImageIsDirty = true;
            s_OnFocus = true;
        }
        //PicoVideo;FoveatedFeature;YangFan;End
        
        //PicoVideo;SubPass;Ernst;Begin
        ForwardRendererData m_CurRendererData = null;
        //PicoVideo;SubPass;Ernst;End

        public ForwardRenderer(ForwardRendererData data) : base(data)
        {
            //PicoVideo;SubPass;Ernst;Begin
            m_CurRendererData = data;
            //PicoVideo;SubPass;Ernst;End
#if ENABLE_VR && ENABLE_XR_MODULE
            UniversalRenderPipeline.m_XRSystem.InitializeXRSystemData(data.xrSystemData);
#endif

            m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitPS);
            //PicoVideo;SubPass;Ernst;Begin
            m_BlitInSubPassMaterial = CoreUtils.CreateEngineMaterial(data.shaders.blitPSInSubPass);
            //PicoVideo;SubPass;Ernst;End
            m_CopyDepthMaterial = CoreUtils.CreateEngineMaterial(data.shaders.copyDepthPS);
            m_SamplingMaterial = CoreUtils.CreateEngineMaterial(data.shaders.samplingPS);
            m_ScreenspaceShadowsMaterial = CoreUtils.CreateEngineMaterial(data.shaders.screenSpaceShadowPS);
            //m_TileDepthInfoMaterial = CoreUtils.CreateEngineMaterial(data.shaders.tileDepthInfoPS);
            //m_TileDeferredMaterial = CoreUtils.CreateEngineMaterial(data.shaders.tileDeferredPS);
            m_StencilDeferredMaterial = CoreUtils.CreateEngineMaterial(data.shaders.stencilDeferredPS);

            StencilStateData stencilData = data.defaultStencilState;
            m_DefaultStencilState = StencilState.defaultValue;
            m_DefaultStencilState.enabled = stencilData.overrideStencilState;
            m_DefaultStencilState.SetCompareFunction(stencilData.stencilCompareFunction);
            m_DefaultStencilState.SetPassOperation(stencilData.passOperation);
            m_DefaultStencilState.SetFailOperation(stencilData.failOperation);
            m_DefaultStencilState.SetZFailOperation(stencilData.zFailOperation);

            m_ForwardLights = new ForwardLights();
            //m_DeferredLights.LightCulling = data.lightCulling;
            this.m_RenderingMode = RenderingMode.Forward;

            // Note: Since all custom render passes inject first and we have stable sort,
            // we inject the builtin passes in the before events.
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
#if ENABLE_VR && ENABLE_XR_MODULE
            m_XROcclusionMeshPass = new XROcclusionMeshPass(RenderPassEvent.BeforeRenderingOpaques);
            // Schedule XR copydepth right after m_FinalBlitPass(AfterRendering + 1)
            m_XRCopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRendering + 2, m_CopyDepthMaterial);
#endif
            m_DepthPrepass = new DepthOnlyPass(RenderPassEvent.BeforeRenderingPrepasses, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_DepthNormalPrepass = new DepthNormalOnlyPass(RenderPassEvent.BeforeRenderingPrepasses, RenderQueueRange.opaque, data.opaqueLayerMask);
            m_ColorGradingLutPass = new ColorGradingLutPass(RenderPassEvent.BeforeRenderingPrepasses, data.postProcessData);

            if (this.renderingMode == RenderingMode.Deferred)
            {
                m_DeferredLights = new DeferredLights(m_TileDepthInfoMaterial, m_TileDeferredMaterial, m_StencilDeferredMaterial);
                m_DeferredLights.AccurateGbufferNormals = data.accurateGbufferNormals;
                //m_DeferredLights.TiledDeferredShading = data.tiledDeferredShading;
                m_DeferredLights.TiledDeferredShading = false;
                UniversalRenderPipelineAsset urpAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;

                m_GBufferPass = new GBufferPass(RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference, m_DeferredLights);
                // Forward-only pass only runs if deferred renderer is enabled.
                // It allows specific materials to be rendered in a forward-like pass.
                // We render both gbuffer pass and forward-only pass before the deferred lighting pass so we can minimize copies of depth buffer and
                // benefits from some depth rejection.
                // - If a material can be rendered either forward or deferred, then it should declare a UniversalForward and a UniversalGBuffer pass.
                // - If a material cannot be lit in deferred (unlit, bakedLit, special material such as hair, skin shader), then it should declare UniversalForwardOnly pass
                // - Legacy materials have unamed pass, which is implicitely renamed as SRPDefaultUnlit. In that case, they are considered forward-only too.
                // TO declare a material with unnamed pass and UniversalForward/UniversalForwardOnly pass is an ERROR, as the material will be rendered twice.
                StencilState forwardOnlyStencilState = DeferredLights.OverwriteStencil(m_DefaultStencilState, (int)StencilUsage.MaterialMask);
                ShaderTagId[] forwardOnlyShaderTagIds = new ShaderTagId[] {
                    new ShaderTagId("UniversalForwardOnly"),
                    new ShaderTagId("SRPDefaultUnlit"), // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
                    new ShaderTagId("LightweightForward") // Legacy shaders (do not have a gbuffer pass) are considered forward-only for backward compatibility
                };
                int forwardOnlyStencilRef = stencilData.stencilReference | (int)StencilUsage.MaterialUnlit;
                m_RenderOpaqueForwardOnlyPass = new DrawObjectsPass("Render Opaques Forward Only", forwardOnlyShaderTagIds, DrawObjectsPass.PassType.Opaque, RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, data.opaqueLayerMask, forwardOnlyStencilState, forwardOnlyStencilRef);
                m_GBufferCopyDepthPass = new CopyDepthPass(RenderPassEvent.BeforeRenderingOpaques + 2, m_CopyDepthMaterial);
                m_TileDepthRangePass = new TileDepthRangePass(RenderPassEvent.BeforeRenderingOpaques + 3, m_DeferredLights, 0);
                m_TileDepthRangeExtraPass = new TileDepthRangePass(RenderPassEvent.BeforeRenderingOpaques + 4, m_DeferredLights, 1);
                m_DeferredPass = new DeferredPass(RenderPassEvent.BeforeRenderingOpaques + 5, m_DeferredLights);
            }

            //PicoVideo;AppSW;Ernst;Begin
            m_MotionVectorPass = new MotionVectorPass(URPProfileId.DrawMVOpaqueObjects, true, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            //PicoVideo;AppSW;Ernst;End
            //PicoVideo;Debug;Ernst;Begin
            m_DebugPass = new DebugPass(RenderPassEvent.AfterRendering, data.postProcessData);
            //PicoVideo;Debug;Ernst;End

            // Always create this pass even in deferred because we use it for wireframe rendering in the Editor or offscreen depth texture rendering.
            m_RenderOpaqueForwardPass = new DrawObjectsPass(URPProfileId.DrawOpaqueObjects, DrawObjectsPass.PassType.Opaque, RenderPassEvent.BeforeRenderingOpaques, RenderQueueRange.opaque, data.opaqueLayerMask, m_DefaultStencilState, stencilData.stencilReference);
            m_RenderBeforePostOpaqueForwardPass = new DrawObjectsPass(URPProfileId.DrawBeforePostOpaqueObjects, DrawObjectsPass.PassType.BeforePostOpaque, RenderPassEvent.BeforeRenderingPostProcessing - 3, RenderQueueRange.opaque, 0, m_DefaultStencilState, stencilData.stencilReference);
            m_RenderOverlayOpaqueForwardPass = new DrawObjectsPass(URPProfileId.DrawOverlayOpaqueObjects, DrawObjectsPass.PassType.OverlayOpaque, RenderPassEvent.AfterRendering + 2, RenderQueueRange.opaque, 0, m_DefaultStencilState, stencilData.stencilReference);

            m_CopyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingSkybox, m_CopyDepthMaterial);
            m_DrawSkyboxPass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingSkybox);
            m_CopyColorPass = new CopyColorPass(RenderPassEvent.AfterRenderingSkybox, m_SamplingMaterial, m_BlitMaterial);
            //PicoVideo;Basic;Ernst;Begin
            m_CopyTransparentColorPass = new CopyColorPass(RenderPassEvent.AfterRenderingTransparents + 10, m_SamplingMaterial, m_BlitMaterial);
            //PicoVideo;Basic;Ernst;End
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (!UniversalRenderPipeline.asset.useAdaptivePerformance || AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipTransparentObjects == false)
#endif
            {
                m_TransparentSettingsPass = new TransparentSettingsPass(RenderPassEvent.BeforeRenderingTransparents, data.shadowTransparentReceive);
                //PicoVideo;EditorUIColorAdjustment;WuJunLin;Begin
                int layerMask =
                    UniversalRenderPipeline.asset.enableUIColorAdjustment
                        ? data.transparentLayerMask ^ UniversalRenderPipeline.asset.colorAdjustUILayerMask
                        : (int)data.transparentLayerMask;
                m_RenderTransparentForwardPass = new DrawObjectsPass(URPProfileId.DrawTransparentObjects, DrawObjectsPass.PassType.Transparent, RenderPassEvent.BeforeRenderingTransparents, RenderQueueRange.transparent, layerMask, m_DefaultStencilState, stencilData.stencilReference);
                m_DrawCorrectGammaUIPass = new DrawCorrectGammaUIPass(
                        RenderPassEvent.AfterRenderingTransparents + 1,
                        UniversalRenderPipeline.asset.colorAdjustUILayerMask, m_BlitMaterial, m_BlitInSubPassMaterial, m_DefaultStencilState,
                        stencilData.stencilReference);
                //PicoVideo;EditorUIColorAdjustment;WuJunLin;End
            }
            m_RenderBeforePostTransparentForwardPass = new DrawObjectsPass(URPProfileId.DrawBeforePostTransparentObjects, DrawObjectsPass.PassType.BeforePostTransparent,
                RenderPassEvent.BeforeRenderingPostProcessing - 2, RenderQueueRange.transparent, 0, m_DefaultStencilState,
                stencilData.stencilReference);
            m_RenderOverlayTransparentForwardPass = new DrawObjectsPass(URPProfileId.DrawOverlayTransparentObjects, DrawObjectsPass.PassType.OverlayTransparent,
                RenderPassEvent.AfterRendering + 3, RenderQueueRange.transparent, 0, m_DefaultStencilState,
                stencilData.stencilReference);
            m_OnRenderObjectCallbackPass = new InvokeOnRenderObjectCallbackPass(RenderPassEvent.BeforeRenderingPostProcessing);
            m_PostProcessPass = new PostProcessPass(RenderPassEvent.BeforeRenderingPostProcessing, data.postProcessData, m_BlitMaterial);
            m_FinalPostProcessPass = new PostProcessPass(RenderPassEvent.AfterRendering + 1, data.postProcessData, m_BlitMaterial);
            m_CapturePass = new CapturePass(RenderPassEvent.AfterRendering);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, m_BlitMaterial);

#if UNITY_EDITOR
            m_SceneViewDepthCopyPass = new SceneViewDepthCopyPass(RenderPassEvent.AfterRendering + 9, m_CopyDepthMaterial);
#endif

            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            m_CameraColorAttachment.Init("_CameraColorTexture");
            m_CameraDepthAttachment.Init("_CameraDepthAttachment");
            m_DepthTexture.Init("_CameraDepthTexture");
            m_NormalsTexture.Init("_CameraNormalsTexture");
            if (this.renderingMode == RenderingMode.Deferred)
            {
                m_GBufferHandles = new RenderTargetHandle[(int)DeferredLights.GBufferHandles.Count];
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.DepthAsColor].Init("_GBufferDepthAsColor");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.Albedo].Init("_GBuffer0");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.SpecularMetallic].Init("_GBuffer1");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.NormalSmoothness].Init("_GBuffer2");
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.Lighting] = new RenderTargetHandle();
                m_GBufferHandles[(int)DeferredLights.GBufferHandles.ShadowMask].Init("_GBuffer4");
            }
            m_OpaqueColor.Init("_CameraOpaqueTexture");
            m_AfterPostProcessColor.Init("_AfterPostProcessTexture");
            m_ColorGradingLut.Init("_InternalGradingLut");
            m_DepthInfoTexture.Init("_DepthInfoTexture");
            m_TileDepthInfoTexture.Init("_TileDepthInfoTexture");

            supportedRenderingFeatures = new RenderingFeatures()
            {
                cameraStacking = true,
            };

            if (this.renderingMode == RenderingMode.Deferred)
            {
                unsupportedGraphicsDeviceTypes = new GraphicsDeviceType[] {
                    GraphicsDeviceType.OpenGLCore,
                    GraphicsDeviceType.OpenGLES2,
                    GraphicsDeviceType.OpenGLES3
                };
            }

            //PicoVideo;DynamicResolution;Ernst;Begin
            m_DynamicResolutionPass = new DynamicResolutionPass(RenderPassEvent.BeforeRenderingPrepasses, m_CopyDepthMaterial);
            //PicoVideo;DynamicResolution;Ernst;End
            
            //PicoVideo;FoveatedFeature;YangFan;Begin
            XRRenderUtils.instance.RegisterFocusAcquiredCallback(OnFocusAcquired);
            XRRenderUtils.instance.RegisterFocusLostCallback(OnFocusLost);
            //PicoVideo;FoveatedFeature;YangFan;End
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            //PicoVideo;FoveatedFeature;YangFan;Begin
            XRRenderUtils.instance.UnregisterFocusAcquiredCallback(OnFocusAcquired);
            XRRenderUtils.instance.UnregisterFocusLostCallback(OnFocusLost);
            //PicoVideo;FoveatedFeature;YangFan;End
            
            // always dispose unmanaged resources
            m_PostProcessPass.Cleanup();
            m_FinalPostProcessPass.Cleanup();
            m_ColorGradingLutPass.Cleanup();

            CoreUtils.Destroy(m_BlitMaterial);
            CoreUtils.Destroy(m_CopyDepthMaterial);
            CoreUtils.Destroy(m_SamplingMaterial);
            CoreUtils.Destroy(m_ScreenspaceShadowsMaterial);
            CoreUtils.Destroy(m_TileDepthInfoMaterial);
            CoreUtils.Destroy(m_TileDeferredMaterial);
            CoreUtils.Destroy(m_StencilDeferredMaterial);
        }

        /// <inheritdoc />
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            bool needTransparencyPass = !UniversalRenderPipeline.asset.useAdaptivePerformance || !AdaptivePerformance.AdaptivePerformanceRenderSettings.SkipTransparentObjects;
#endif
            Camera camera = renderingData.cameraData.camera;
            ref CameraData cameraData = ref renderingData.cameraData;
            UniversalAdditionalCameraData additionalCameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;

            // Special path for depth only offscreen cameras. Only write opaques + transparents.
            bool isOffscreenDepthTexture = cameraData.targetTexture != null && cameraData.targetTexture.format == RenderTextureFormat.Depth;
            if (isOffscreenDepthTexture)
            {
                ConfigureCameraTarget(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget);
                AddRenderPasses(ref renderingData);
                EnqueuePass(m_RenderOpaqueForwardPass);

                // TODO: Do we need to inject transparents and skybox when rendering depth only camera? They don't write to depth.
                EnqueuePass(m_DrawSkyboxPass);
#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
                if (!needTransparencyPass)
                    return;
#endif
                EnqueuePass(m_RenderTransparentForwardPass);
                return;
            }

            bool usePipelineAnalyze = camera == Camera.main &&
                                      renderingData.cameraData.cameraType == CameraType.Game &&
                                      renderingData.cameraData.renderType == CameraRenderType.Base;
            if (usePipelineAnalyze)
            {
                PipelineAnalyze.InitPipelineState();
            }

            //PicoVideo;Basic;Ernst;Begin
            RenderPassInputSummary renderPassInputs = GetRenderPassInputs(ref renderingData);
            int extendRequirementFlag = ForwardRendererExtend.GetRequirementFlag(camera);
            int extendSupportFlag = ForwardRendererExtend.GetRequirementFlag(camera, true);
            bool depthStencilInPost = false;
            var stack = VolumeManager.instance.stack;
            var bloom = stack.GetComponent<Bloom>();
            if (bloom.IsActive() && bloom.maskMode.value == BloomMaskMode.DepthStencil_Forward || bloom.maskMode.value == BloomMaskMode.DepthStencil_Reverse)
            {
                depthStencilInPost = true;
            }
            depthStencilInPost |= ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_DEPTH_SETCIL_IN_POST);
            //PicoVideo;Basic;Ernst;End
            
            //PicoVideo;SubPass;Ernst;Begin
            SubPassManager.Init(camera);
            if (cameraData.cameraType != CameraType.Game)
            {
                SubPassManager.DisableSubPass();
            }
            if (!ForwardRendererExtend.GetFlag(extendSupportFlag, ForwardRendererExtend.SUPPORT_SUBPASS))
            {
                SubPassManager.DisableSubPass("Custom not surpport.");
            }
            if (renderingData.cameraData.includeOverlay || renderingData.cameraData.renderType == CameraRenderType.Overlay)
            {
                SubPassManager.DisableSubPass("Contains overlay camera.");
            }
            //PicoVideo;SubPass;Ernst;End

            if (m_DeferredLights != null)
                m_DeferredLights.ResolveMixedLightingMode(ref renderingData);

            // Assign the camera color target early in case it is needed during AddRenderPasses.
            bool isPreviewCamera = cameraData.isPreviewCamera;
            bool isRunningHololens = false;
#if ENABLE_VR && ENABLE_VR_MODULE
            isRunningHololens = UniversalRenderPipeline.IsRunningHololens(cameraData);
#endif
            
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
            bool createColorTexture = false;
            bool renderFeatureEnabled = false;
            for (int i = 0; i < rendererFeatures.Count; ++i)
            {
                if (rendererFeatures[i].isActive)
                {
                    renderFeatureEnabled = true;
                    break;
                }
            }
            if (renderingData.maximumEyeBufferRenderingEnabled && renderingData.isMainCamera)
            {
                createColorTexture = (renderFeatureEnabled && !isRunningHololens) && !isPreviewCamera;    
            }
            else
            {
                createColorTexture = (rendererFeatures.Count != 0 && !isRunningHololens) && !isPreviewCamera;
            }
            if (renderFeatureEnabled)
            {
                SubPassManager.DisableSubPass("RenderFeature not surpport.");
            }
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
            
            if (createColorTexture)
            {
                m_ActiveCameraColorAttachment = m_CameraColorAttachment;
                var activeColorRenderTargetId = m_ActiveCameraColorAttachment.Identifier();
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled) activeColorRenderTargetId = new RenderTargetIdentifier(activeColorRenderTargetId, 0, CubemapFace.Unknown, -1);
#endif
                ConfigureCameraColorTarget(activeColorRenderTargetId);
            }

            // Add render passes and gather the input requirements
            isCameraColorTargetValid = true;
            AddRenderPasses(ref renderingData);
            isCameraColorTargetValid = false;

            //PicoVideo;Basic;Ernst;Begin
            depthStencilInPost |= renderPassInputs.requireDepthStencilInPost;
            //PicoVideo;Basic;Ernst;End

            // Should apply post-processing after rendering this camera?
            bool applyPostProcessing = cameraData.postProcessEnabled;

            // There's at least a camera in the camera stack that applies post-processing
            bool anyPostProcessing = renderingData.postProcessingEnabled;

            // TODO: We could cache and generate the LUT before rendering the stack
            bool generateColorGradingLUT = cameraData.postProcessEnabled;
            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            bool requiresDepthTexture = cameraData.requiresDepthTexture || renderPassInputs.requiresDepthTexture || this.actualRenderingMode == RenderingMode.Deferred;
            //PicoVideo;Basic;Ernst;Begin
            requiresDepthTexture |= ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_DEPTH_TEXTURE);
            //PicoVideo;Basic;Ernst;End

            bool mainLightShadows = m_MainLightShadowCasterPass.Setup(ref renderingData);
            bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);
            bool transparentsNeedSettingsPass = m_TransparentSettingsPass.Setup(ref renderingData);

            // Depth prepass is generated in the following cases:
            // - If game or offscreen camera requires it we check if we can copy the depth from the rendering opaques pass and use that instead.
            // - Scene or preview cameras always require a depth texture. We do a depth pre-pass to simplify it and it shouldn't matter much for editor.
            // - Render passes require it
            bool requiresDepthPrepass = requiresDepthTexture && !CanCopyDepth(ref renderingData.cameraData);
            requiresDepthPrepass |= isSceneViewCamera;
            requiresDepthPrepass |= isPreviewCamera;
            requiresDepthPrepass |= renderPassInputs.requiresDepthPrepass;
            requiresDepthPrepass |= renderPassInputs.requiresNormalsTexture;
            //PicoVideo;Basic;Ernst;Begin
            requiresDepthPrepass |= ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_DEPTH_PREPASS);
            //PicoVideo;Basic;Ernst;End

            // The copying of depth should normally happen after rendering opaques.
            // But if we only require it for post processing or the scene camera then we do it after rendering transparent objects
            m_CopyDepthPass.renderPassEvent = (!requiresDepthTexture && (applyPostProcessing || isSceneViewCamera)) ? RenderPassEvent.AfterRenderingTransparents : RenderPassEvent.AfterRenderingOpaques;
            
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
            createColorTexture |= RequiresIntermediateColorTexture(ref cameraData, ref renderingData);
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End

            createColorTexture |= renderPassInputs.requiresColorTexture;
            //PicoVideo;Basic;Ernst;Begin
            createColorTexture |= renderPassInputs.requresCreateColorTexture;
            createColorTexture |= ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_CREATE_COLOR_TEXTURE);
            createColorTexture |= ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_OPAQUE_TEXTURE);
            createColorTexture |= ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_TRANSPARENT_TEXTURE);
            //PicoVideo;Basic;Ernst;End
            createColorTexture &= !isPreviewCamera;

            // TODO: There's an issue in multiview and depth copy pass. Atm forcing a depth prepass on XR until we have a proper fix.
            if (cameraData.xr.enabled && requiresDepthTexture)
                requiresDepthPrepass = true;

            // If camera requires depth and there's no depth pre-pass we create a depth texture that can be read later by effect requiring it.
            // When deferred renderer is enabled, we must always create a depth texture and CANNOT use BuiltinRenderTextureType.CameraTarget. This is to get
            // around a bug where during gbuffer pass (MRT pass), the camera depth attachment is correctly bound, but during
            // deferred pass ("camera color" + "camera depth"), the implicit depth surface of "camera color" is used instead of "camera depth",
            // because BuiltinRenderTextureType.CameraTarget for depth means there is no explicit depth attachment...
            //PicoVideo;Basic;Ernst;Begin
            bool createDepthTexture = (cameraData.requiresDepthTexture | ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_DEPTH_TEXTURE)) && !requiresDepthPrepass;
            createDepthTexture |= depthStencilInPost;
            //PicoVideo;Basic;Ernst;End
            
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
            if (renderingData.maximumEyeBufferRenderingEnabled && renderingData.isMainCamera)
            {
                createDepthTexture |= cameraData.cameraIndexInStack <= cameraData.lastCameraIndexWhoNeedOffScreen;
            }
            else
            {
                createDepthTexture |= (cameraData.renderType == CameraRenderType.Base && !cameraData.resolveFinalTarget);    
            }
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
            
            // Deferred renderer always need to access depth buffer.
            createDepthTexture |= this.actualRenderingMode == RenderingMode.Deferred;
#if ENABLE_VR && ENABLE_XR_MODULE
            //if (cameraData.xr.enabled) //PicoVideo;SubPass;Ernst
            {
                // URP can't handle msaa/size mismatch between depth RT and color RT(for now we create intermediate textures to ensure they match)
                createDepthTexture |= createColorTexture;
                createColorTexture = createDepthTexture;
            }
#endif

#if UNITY_ANDROID || UNITY_WEBGL
            if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Vulkan)
            {
                // GLES can not use render texture's depth buffer with the color buffer of the backbuffer
                // in such case we create a color texture for it too.
                createColorTexture |= createDepthTexture;
            }
#endif

            //PicoVideo;EditorUIColorAdjustment;WuJunLin;Begin
            bool curFrameNeedColorAdjustment = false;
            if (UniversalRenderPipeline.asset.enableUIColorAdjustment && renderingData.cameraData.renderType == CameraRenderType.Base)
            {
                curFrameNeedColorAdjustment =
                    m_DrawCorrectGammaUIPass.DrawRendersIsValid(context, ref renderingData);
                if (curFrameNeedColorAdjustment)
                {
                    if (camera.cameraType != CameraType.Reflection)
                    {
                        SubPassManager.NeedSubPass();
                        if (!SubPassManager.supported)
                        {
                            createColorTexture = true;
                            createDepthTexture = true;
                        }
                    }
                }
            }
            //PicoVideo;EditorUIColorAdjustment;WuJunLin;End

            // Configure all settings require to start a new camera stack (base camera only)
            if (cameraData.renderType == CameraRenderType.Base)
            {
                RenderTargetHandle cameraTargetHandle = RenderTargetHandle.GetCameraTarget(cameraData.xr);

                m_ActiveCameraColorAttachment = (createColorTexture) ? m_CameraColorAttachment : cameraTargetHandle;
                m_ActiveCameraDepthAttachment = (createDepthTexture) ? m_CameraDepthAttachment : cameraTargetHandle;

                bool intermediateRenderTexture = createColorTexture || createDepthTexture;
                //PicoVideo;SubPss;Ernst;Begin
                //启用离屏渲染后禁用eyebuffer的MSAA
                // if (intermediateRenderTexture)
                // {
                //     XRSystem.UpdateMSAALevel(1);
                // }
                //PicoVideo;SubPass;Ernst;End
                
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
#if DEVELOPMENT_BUILD || UNITY_EDITOR
                if (Camera.main == cameraData.camera && renderingData.maximumEyeBufferRenderingEnabled)
                {
                    UniversalRenderPipeline.maximumEyeBufferRenderingInactiveReason = 0;
                    if (createColorTexture)
                    {
                        for (int i = 0; i < rendererFeatures.Count; ++i)
                        {
                            if (rendererFeatures[i].isActive)
                            {
                                UniversalRenderPipeline.maximumEyeBufferRenderingInactiveReason |= (int)UniversalRenderPipeline
                                    .MaximumEyeBufferRenderingInactiveReasonType.RenderFeature;
                                break;
                            }
                        }

                        if (renderingData.postProcessingEnabled)
                        {
                            UniversalRenderPipeline.maximumEyeBufferRenderingInactiveReason |=
                                (int)UniversalRenderPipeline.MaximumEyeBufferRenderingInactiveReasonType.PostProcessing;
                        }

                        if (cameraData.isHdrEnabled)
                        {
                            UniversalRenderPipeline.maximumEyeBufferRenderingInactiveReason |=
                                (int)UniversalRenderPipeline.MaximumEyeBufferRenderingInactiveReasonType.HDR;
                        }

                        if (UniversalRenderPipeline.maximumEyeBufferRenderingInactiveReason == 0)
                        {
                            UniversalRenderPipeline.maximumEyeBufferRenderingInactiveReason |=
                                (int)UniversalRenderPipeline.MaximumEyeBufferRenderingInactiveReasonType.OtherReason;
                        }
                    }
                    
                }
#endif
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End  
                
                //PicoVideo;FoveatedFeature;YangFan;Begin
                UniversalRenderPipeline.SetEyeTrackingFocalPoint(ref renderingData);
                UniversalRenderPipeline.SetOffscreenRenderingState(createColorTexture);
                //PicoVideo;FoveatedFeature;YangFan;End
                
                // Doesn't create texture for Overlay cameras as they are already overlaying on top of created textures.
                //PicoVideo;FoveatedFeature;YangFan;Begin
                if (intermediateRenderTexture)
                    CreateCameraRenderTarget(context, ref renderingData, ref cameraTargetDescriptor, createColorTexture, createDepthTexture);
                //PicoVideo;FoveatedFeature;YangFan;End
            }
            else
            {
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
                if (renderingData.maximumEyeBufferRenderingEnabled && renderingData.isMainCamera)
                {
                    RenderTargetHandle cameraTargetHandle = RenderTargetHandle.GetCameraTarget(cameraData.xr);
                    m_ActiveCameraColorAttachment = (createColorTexture) ? m_CameraColorAttachment : cameraTargetHandle;
                    if (cameraData.lastCameraIndexWhoNeedOffScreen != -2)
                    {
                        m_ActiveCameraDepthAttachment = m_CameraDepthAttachment;
                    }
                    else
                    {
                        m_ActiveCameraDepthAttachment = cameraTargetHandle;
                    }
                }
                else
                {
                    m_ActiveCameraColorAttachment = m_CameraColorAttachment;
                    m_ActiveCameraDepthAttachment = m_CameraDepthAttachment;
                }
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
                
                //PicoVideo;DynamicResolution;Ernst;Begin
                if (cameraData.rtDynamicResolutionType != RTDynamicResolutionType.UseDynamicResolution &&
                    DynamicResolutionController.excutedType !=
                    DynamicResolutionController.DynamicResolutionType.Disable &&
                    DynamicResolutionController.curScale < 1.0)
                {
                    RenderTargetHandle newColorHandle = new RenderTargetHandle();
                    newColorHandle.id = 10000 + camera.GetInstanceID();
                    m_ActiveCameraColorAttachment = newColorHandle;

                    const string keepResolutionTexture = "Create Keep Resolution Texture";
                    CommandBuffer cmd = CommandBufferPool.Get(keepResolutionTexture);
                    var descriptor = cameraData.cameraTargetDescriptor;
                    int msaaSamples = descriptor.msaaSamples;

                    bool excuteDepth = false;

                    bool useDepthRenderBuffer = m_ActiveCameraDepthAttachment == RenderTargetHandle.CameraTarget;
                    var colorDescriptor = descriptor;
                    colorDescriptor.depthBufferBits = (useDepthRenderBuffer) ? k_DepthStencilBufferBits : 0;
                    cmd.GetTemporaryRT(m_ActiveCameraColorAttachment.id, colorDescriptor, FilterMode.Bilinear);

                    if (cameraData.rtDynamicResolutionType == RTDynamicResolutionType.KeepColorAndDepthResoluton)
                    {
                        excuteDepth = true;
                        RenderTargetHandle newDepthHandle = new RenderTargetHandle();
                        newDepthHandle.id = 20000 + camera.GetInstanceID();
                        m_ActiveCameraDepthAttachment = newDepthHandle;

                        var depthDescriptor = descriptor;
                        depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                        depthDescriptor.depthBufferBits = k_DepthStencilBufferBits;
                        cmd.GetTemporaryRT(m_ActiveCameraDepthAttachment.id, depthDescriptor, FilterMode.Point);
                    }
                    else
                    {
                        m_ActiveCameraDepthAttachment = RenderTargetHandle.CameraTarget;
                    }

                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);

                    m_DynamicResolutionPass.Setup(colorDescriptor,
                        ForwardRendererExtend.GetActiveColorRenderTargetHandle(),
                        ForwardRendererExtend.GetActiveDepthRenderTargetHandle(),
                        excuteDepth,
                        m_ActiveCameraColorAttachment, m_ActiveCameraDepthAttachment);

                    EnqueuePass(m_DynamicResolutionPass);
                }
                //PicoVideo;DynamicResolution;Ernst;End
            }

            // If a depth texture was created we necessarily need to copy it, otherwise we could have render it to a renderbuffer.
            // If deferred rendering path was selected, it has already made a copy.
            bool requiresDepthCopyPass = !requiresDepthPrepass
                                         //PicoVideo;Basic;Ernst;Begin
                                         && (renderingData.cameraData.requiresDepthTexture | ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_DEPTH_TEXTURE))
                                         //PicoVideo;Basic;Ernst;End
                                         && createDepthTexture
                                         && this.actualRenderingMode != RenderingMode.Deferred;
            bool copyColorPass = renderingData.cameraData.requiresOpaqueTexture || renderPassInputs.requiresColorTexture;
            //PicoVideo;Basic;Ernst;Begin
            copyColorPass |= ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_OPAQUE_TEXTURE);
            //PicoVideo;Basic;Ernst;End
            
            //PicoVideo;Basic;Ernst;Begin
            bool copyTransparentColorPass = UniversalRenderPipeline.asset.supportsCameraTransparentTexture;
            copyTransparentColorPass |= ForwardRendererExtend.GetFlag(extendRequirementFlag, ForwardRendererExtend.REQUIRE_TRANSPARENT_TEXTURE);
            //PicoVideo;Basic;Ernst;End

            // Assign camera targets (color and depth)
            {
                var activeColorRenderTargetId = m_ActiveCameraColorAttachment.Identifier();
                var activeDepthRenderTargetId = m_ActiveCameraDepthAttachment.Identifier();

#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    activeColorRenderTargetId = new RenderTargetIdentifier(activeColorRenderTargetId, 0, CubemapFace.Unknown, -1);
                    activeDepthRenderTargetId = new RenderTargetIdentifier(activeDepthRenderTargetId, 0, CubemapFace.Unknown, -1);
                }
#endif

                ConfigureCameraTarget(activeColorRenderTargetId, activeDepthRenderTargetId);

                //PicoVideo;Basic;Ernst;Begin
                ForwardRendererExtend.Setup(camera, cameraTargetDescriptor);
                System.Collections.Generic.List<CustomPass> customPassList = ForwardRendererExtend.GetCustomPassList(camera);
                for (int i = 0; i < customPassList.Count; ++i)
                {
                    EnqueuePass(customPassList[i]);
                }
                //PicoVideo;Basic;Ernst;End
            }

            bool hasPassesAfterPostProcessing = activeRenderPassQueue.Find(x => x.renderPassEvent == RenderPassEvent.AfterRendering) != null;
            //PicoVideo;Basic;Ernst;Begin
            if (cameraData.renderType == CameraRenderType.Base || cameraData.cameraType == CameraType.SceneView)
            {
                ForwardRendererExtend.InitFullScreenTempRT(cameraTargetDescriptor, m_ActiveCameraColorAttachment, m_ActiveCameraDepthAttachment);
            }
            //PicoVideo;Basic;Ernst;End
            if (mainLightShadows)
            {
                EnqueuePass(m_MainLightShadowCasterPass);
                if (usePipelineAnalyze)
                {
                    PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.MainLightShadowCasterPass);
                }
            }

            if (additionalLightShadows)
            {
                EnqueuePass(m_AdditionalLightsShadowCasterPass);
                if (usePipelineAnalyze)
                {
                    PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.AdditionalLightsShadowCasterPass);
                }
            }

            if (requiresDepthPrepass)
            {
                if (renderPassInputs.requiresNormalsTexture)
                {
                    m_DepthNormalPrepass.Setup(cameraTargetDescriptor, m_DepthTexture, m_NormalsTexture);
                    EnqueuePass(m_DepthNormalPrepass);
                }
                else
                {
                    m_DepthPrepass.Setup(cameraTargetDescriptor, m_DepthTexture);
                    EnqueuePass(m_DepthPrepass);
                    if (usePipelineAnalyze)
                    {
                        PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.DepthOnlyPass);
                    }
                }
            }

            if (generateColorGradingLUT)
            {
                m_ColorGradingLutPass.Setup(m_ColorGradingLut);
                EnqueuePass(m_ColorGradingLutPass);
                if (usePipelineAnalyze)
                {
                    PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.ColorGradingLutPass);
                }
            }

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.hasValidOcclusionMesh)
                EnqueuePass(m_XROcclusionMeshPass);

            //PicoVideo;AppSW;Ernst;Begin
            bool requireMotionVector = cameraData.xr.motionVectorRenderTargetValid | UniversalRenderPipeline.asset.supportsMotionVector;
            if (requireMotionVector)
            {
                if (cameraData.xr.motionVectorRenderTargetValid)
                {
                    RenderTargetHandle motionVecHandle = new RenderTargetHandle(cameraData.xr.motionVectorRenderTarget);
                    var rtMotionId = motionVecHandle.Identifier();
                    rtMotionId = new RenderTargetIdentifier(rtMotionId, 0, CubemapFace.Unknown, -1);

                    // ID is the same since a RenderTexture encapsulates all the attachments, including both color+depth.
                    m_MotionVectorPass.Setup(rtMotionId, rtMotionId, false);
                }
                else
                {
                    if (cameraData.xr.enabled)
                    {
                        m_MotionVectorPass.Setup(
                            new RenderTargetIdentifier(m_ActiveCameraColorAttachment.Identifier(), 0,
                                CubemapFace.Unknown, -1),
                            new RenderTargetIdentifier(m_ActiveCameraDepthAttachment.Identifier(), 0,
                                CubemapFace.Unknown, -1), true);
                    }
                    else
                    {
                        m_MotionVectorPass.Setup(m_ActiveCameraColorAttachment.Identifier(),
                            m_ActiveCameraDepthAttachment.Identifier(), true);
                    }
                }
                EnqueuePass(m_MotionVectorPass);
                if (usePipelineAnalyze)
                {
                    PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.MotionVectorPass);
                }
            }
            //PicoVideo;AppSW;Ernst;End
#endif
            //PicoVideo;Debug;Ernst;Begin
            if (UniversalRenderPipeline.asset.debugMode != DebugMode.Disable)
            {
                EnqueuePass(m_DebugPass);
                if (usePipelineAnalyze)
                {
                    PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.DebugPass);
                }
            }
            //PicoVideo;Debug;Ernst;End

            //PicoVideo;SubPass;Ernst;Begin
            // if (this.actualRenderingMode == RenderingMode.Deferred)
            //     EnqueueDeferred(ref renderingData, requiresDepthPrepass, mainLightShadows, additionalLightShadows);
            // else
            // {
            //PicoVideo;SubPass;Ernst;End
                // Optimized store actions are very important on tile based GPUs and have a great impact on performance.
                // if MSAA is enabled and any of the following passes need a copy of the color or depth target, make sure the MSAA'd surface is stored
                // if following passes won't use it then just resolve (the Resolve action will still store the resolved surface, but discard the MSAA'd surface, which is very expensive to store).
                RenderBufferStoreAction opaquePassColorStoreAction = RenderBufferStoreAction.Store;
                //PicoVideo;Basic;Ernst;Begin
                if (cameraTargetDescriptor.msaaSamples > 1)
                    opaquePassColorStoreAction = (copyColorPass) ? RenderBufferStoreAction.StoreAndResolve : RenderBufferStoreAction.Store;
                //PicoVideo;Basic;Ernst;End

                // make sure we store the depth only if following passes need it.
                //PicoVideo;Basic;Ernst;Begin
                RenderBufferStoreAction opaquePassDepthStoreAction = (copyColorPass || requiresDepthCopyPass) ? RenderBufferStoreAction.Store : RenderBufferStoreAction.DontCare;
                //PicoVideo;Basic;Ernst;End
                
                //PicoVideo;CustomBloom;ZhouShaoyang;Begin
                if (depthStencilInPost)
                {
                    opaquePassDepthStoreAction = RenderBufferStoreAction.Store;
                }
                //PicoVideo;CustomBloom;ZhouShaoyang;End

                //PicoVideo;SubPass;Ernst;Begin
                //m_RenderOpaqueForwardPass.ConfigureColorStoreAction(opaquePassColorStoreAction);
                //m_RenderOpaqueForwardPass.ConfigureDepthStoreAction(opaquePassDepthStoreAction);
                //PicoVideo;SubPass;Ernst;End
                if (renderingData.cameraData.renderType == CameraRenderType.Base && additionalCameraData != null)
                {
                    m_RenderOpaqueForwardPass.ResetLayerMask(m_CurRendererData.opaqueLayerMask & (~additionalCameraData.beforePostLayerMask) & (~additionalCameraData.overlayLayerMask));
                }
                EnqueuePass(m_RenderOpaqueForwardPass);
            //PicoVideo;SubPass;Ernst;Begin
            //}
            //PicoVideo;SubPass;Ernst;End

            Skybox cameraSkybox;
            cameraData.camera.TryGetComponent<Skybox>(out cameraSkybox);
            bool isOverlayCamera = cameraData.renderType == CameraRenderType.Overlay;
            if (camera.clearFlags == CameraClearFlags.Skybox && (RenderSettings.skybox != null || cameraSkybox?.material != null) && !isOverlayCamera)
                EnqueuePass(m_DrawSkyboxPass);

            if (requiresDepthCopyPass)
            {
                m_CopyDepthPass.Setup(m_ActiveCameraDepthAttachment, m_DepthTexture);
                EnqueuePass(m_CopyDepthPass);
                if (usePipelineAnalyze)
                {
                    PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.CopyDepthPass);
                }
            }

            // For Base Cameras: Set the depth texture to the far Z if we do not have a depth prepass or copy depth
            if (cameraData.renderType == CameraRenderType.Base && !requiresDepthPrepass && !requiresDepthCopyPass)
            {
                Shader.SetGlobalTexture(m_DepthTexture.id, SystemInfo.usesReversedZBuffer ? Texture2D.blackTexture : Texture2D.whiteTexture);
            }

            if (copyColorPass)
            {
                // TODO: Downsampling method should be store in the renderer instead of in the asset.
                // We need to migrate this data to renderer. For now, we query the method in the active asset.
                //PicoVideo;Basic;Ernst;Begin
                SubPassManager.DisableSubPass("OpenCopyColorPass");
                m_CopyColorPass.Setup(m_ActiveCameraColorAttachment.Identifier(), m_OpaqueColor);
                //PicoVideo;Basic;Ernst;End
                EnqueuePass(m_CopyColorPass);
                if (usePipelineAnalyze)
                {
                    PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.CopyColorPass_Opaque);
                }
            }
            
            //PicoVideo;Basic;Ernst;Begin
            if (copyTransparentColorPass)
            {
                SubPassManager.DisableSubPass("OpenCopyTransparentColorPass");
                m_CopyTransparentColorPass.Setup(m_ActiveCameraColorAttachment.Identifier(), m_OpaqueColor, TRANSPARENT_COLOR_NAME);
                EnqueuePass(m_CopyTransparentColorPass);
                if (usePipelineAnalyze)
                {
                    PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.CopyColorPass_Transparent);
                }
            }
            //PicoVideo;Basic;Ernst;End

            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
            bool lastCameraInTheStack = cameraData.resolveFinalTarget;
            if (renderingData.maximumEyeBufferRenderingEnabled)
            {
                lastCameraInTheStack = cameraData.resolveFinalTarget || cameraData.cameraIndexInStack == cameraData.lastCameraIndexWhoNeedOffScreen;
            }
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End

#if ADAPTIVE_PERFORMANCE_2_1_0_OR_NEWER
            if (needTransparencyPass)
#endif
            {
                if (transparentsNeedSettingsPass)
                {
                    EnqueuePass(m_TransparentSettingsPass);
                }

                // if this is not lastCameraInTheStack we still need to Store, since the MSAA buffer might be needed by the Overlay cameras
                RenderBufferStoreAction transparentPassColorStoreAction =
                    cameraTargetDescriptor.msaaSamples > 1 && lastCameraInTheStack
                        ? RenderBufferStoreAction.Resolve
                        : RenderBufferStoreAction.Store;
                RenderBufferStoreAction transparentPassDepthStoreAction = RenderBufferStoreAction.DontCare;

                // If CopyDepthPass pass event is scheduled on or after AfterRenderingTransparent, we will need to store the depth buffer or resolve (store for now until latest trunk has depth resolve support) it for MSAA case
                if (requiresDepthCopyPass &&
                    m_CopyDepthPass.renderPassEvent >= RenderPassEvent.AfterRenderingTransparents)
                    transparentPassDepthStoreAction = RenderBufferStoreAction.Store;
                //PicoVideo;CustomBloom;ZhouShaoyang;Begin
                if (depthStencilInPost) transparentPassDepthStoreAction = RenderBufferStoreAction.Store;
                //PicoVideo;CustomBloom;ZhouShaoyang;End
                //PicoVideo;SubPass;Ernst;Begin
                if (UniversalRenderPipeline.asset.enableUIColorAdjustment && curFrameNeedColorAdjustment &&
                    !SubPassManager.supported)
                {
                    transparentPassDepthStoreAction = RenderBufferStoreAction.Store;
                    opaquePassDepthStoreAction = transparentPassDepthStoreAction;
                }
                
                m_RenderTransparentForwardPass.ConfigureColorStoreAction(transparentPassColorStoreAction);
                m_RenderTransparentForwardPass.ConfigureDepthStoreAction(transparentPassDepthStoreAction);
                
                m_RenderBeforePostOpaqueForwardPass.ConfigureColorStoreAction(transparentPassColorStoreAction);
                m_RenderBeforePostOpaqueForwardPass.ConfigureDepthStoreAction(transparentPassDepthStoreAction);
                
                m_RenderBeforePostTransparentForwardPass.ConfigureColorStoreAction(transparentPassColorStoreAction);
                m_RenderBeforePostTransparentForwardPass.ConfigureDepthStoreAction(transparentPassDepthStoreAction);

                if (!applyPostProcessing)
                {
                    m_RenderOverlayOpaqueForwardPass.ConfigureColorStoreAction(transparentPassColorStoreAction);
                    m_RenderOverlayOpaqueForwardPass.ConfigureDepthStoreAction(transparentPassDepthStoreAction);
                
                    m_RenderOverlayTransparentForwardPass.ConfigureColorStoreAction(transparentPassColorStoreAction);
                    m_RenderOverlayTransparentForwardPass.ConfigureDepthStoreAction(transparentPassDepthStoreAction);
                }
                //PicoVideo;SubPass;Ernst;End

                //PicoVideo;SubPass;Ernst;Begin
                //根据Transparent的情况确定Opaque的情况:Opaque后面没有强制Store的情况下，Opaque采用和Transparent相同的StoreAction
                if (transparentPassColorStoreAction == RenderBufferStoreAction.Resolve &&
                    opaquePassColorStoreAction != RenderBufferStoreAction.StoreAndResolve)
                {
                    opaquePassColorStoreAction = RenderBufferStoreAction.Resolve;
                }
                
                m_RenderOpaqueForwardPass.ConfigureColorStoreAction(opaquePassColorStoreAction);
                m_RenderOpaqueForwardPass.ConfigureDepthStoreAction(opaquePassDepthStoreAction);
                m_OnRenderObjectCallbackPass.ConfigureColorStoreAction(opaquePassColorStoreAction);
                m_OnRenderObjectCallbackPass.ConfigureDepthStoreAction(opaquePassDepthStoreAction);

                m_DrawSkyboxPass.ConfigureColorStoreAction(opaquePassColorStoreAction);
                m_DrawSkyboxPass.ConfigureDepthStoreAction(opaquePassDepthStoreAction);
                //PicoVideo;SubPass;Ernst;End

                //PicoVideo;SubPass;Ernst;Begin
                int layerMask =
                    UniversalRenderPipeline.asset.enableUIColorAdjustment && curFrameNeedColorAdjustment
                        ? m_CurRendererData.transparentLayerMask ^ UniversalRenderPipeline.asset.colorAdjustUILayerMask
                        : (int)m_CurRendererData.transparentLayerMask;
                if (renderingData.cameraData.renderType == CameraRenderType.Base && additionalCameraData != null)
                {
                    layerMask &= ~additionalCameraData.beforePostLayerMask;
                    layerMask &= ~additionalCameraData.overlayLayerMask;
                }
                m_RenderTransparentForwardPass.ResetLayerMask(layerMask);
                EnqueuePass(m_RenderTransparentForwardPass);
                if (cameraData.renderType == CameraRenderType.Base)
                {
                    if (additionalCameraData != null)
                    {
                        m_RenderBeforePostOpaqueForwardPass.ClearDepth(additionalCameraData.beforePostClearDepth);
                        m_RenderBeforePostOpaqueForwardPass.ResetLayerMask(additionalCameraData.beforePostLayerMask);
                        m_RenderBeforePostTransparentForwardPass.ResetLayerMask(additionalCameraData.beforePostLayerMask);
                    }
                    EnqueuePass(m_RenderBeforePostOpaqueForwardPass);
                    EnqueuePass(m_RenderBeforePostTransparentForwardPass);
                }
                //PicoVideo;SubPass;Ernst;End
                //PicoVideo;EditorUIColorAdjustment;WuJunLin;Begin
                if (UniversalRenderPipeline.asset.enableUIColorAdjustment && (camera.cameraType != CameraType.Reflection))
                {
                    if (curFrameNeedColorAdjustment)
                    {
                        EnqueuePass(m_DrawCorrectGammaUIPass);  
                        if (usePipelineAnalyze)
                        {
                            PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.DrawCorrectGammaUIPass);
                        }
                    }
                }
                //PicoVideo;EditorUIColorAdjustment;WuJunLin;End
                
            }
            EnqueuePass(m_OnRenderObjectCallbackPass);

            bool hasCaptureActions = renderingData.cameraData.captureActions != null && lastCameraInTheStack;

            //PicoVideo;FSR;ZhengLingFeng;Begin
            // When FXAA or scaling is active, we must perform an additional pass at the end of the frame for the following reasons:
            // 1. FXAA expects to be the last shader running on the image before it's presented to the screen. Since users are allowed
            //    to add additional render passes after post processing occurs, we can't run FXAA until all of those passes complete as well.
            //    The FinalPost pass is guaranteed to execute after user authored passes so FXAA is always run inside of it.
            // 2. UberPost can only handle upscaling with linear filtering. All other filtering methods require the FinalPost pass.
            bool applyFinalPostProcessing = anyPostProcessing && lastCameraInTheStack &&
                                            ((renderingData.cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing) ||
                                             ((renderingData.cameraData.imageScalingMode == FSRUtils.ImageScalingMode.Upscaling) && (renderingData.cameraData.upscalingFilter != FSRUtils.ImageUpscalingFilter.Linear)));
            //PicoVideo;FSR;ZhengLingFeng;End

            // When post-processing is enabled we can use the stack to resolve rendering to camera target (screen or RT).
            // However when there are render passes executing after post we avoid resolving to screen so rendering continues (before sRGBConvertion etc)
            bool resolvePostProcessingToCameraTarget = !hasCaptureActions && !hasPassesAfterPostProcessing && !applyFinalPostProcessing;

            bool overlayPassNeedStore = applyPostProcessing;
            
            if (lastCameraInTheStack)
            {
                // Post-processing will resolve to final target. No need for final blit pass.
                if (applyPostProcessing)
                {
                    var destination = resolvePostProcessingToCameraTarget ? RenderTargetHandle.CameraTarget : m_AfterPostProcessColor;

                    // if resolving to screen we need to be able to perform sRGBConvertion in post-processing if necessary
                    bool doSRGBConvertion = resolvePostProcessingToCameraTarget;
					//PicoVideo;Basic;YangFan;Begin
					m_PostProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, destination, m_ActiveCameraDepthAttachment, m_ColorGradingLut, applyFinalPostProcessing, doSRGBConvertion, hasPassesAfterPostProcessing, m_ColorGradingLutPass.GetInternalLutTexture(ref renderingData));        //PicoVideo;FSR;ZhengLingFeng
                    //PicoVideo;Basic;YangFan;End
                    EnqueuePass(m_PostProcessPass);
                    if (usePipelineAnalyze)
                    {
                        PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.PostProcessPass_Post);
                    }
                }
                
                // if we applied post-processing for this camera it means current active texture is m_AfterPostProcessColor
                var sourceForFinalPass = (applyPostProcessing) ? m_AfterPostProcessColor : m_ActiveCameraColorAttachment;
                
                // Do FXAA or any other final post-processing effect that might need to run after AA.
                if (applyFinalPostProcessing)
                {
                    m_FinalPostProcessPass.SetupFinalPass(sourceForFinalPass, hasPassesAfterPostProcessing);        //PicoVideo;FSR;ZhengLingFeng
                    EnqueuePass(m_FinalPostProcessPass);
                    if (usePipelineAnalyze)
                    {
                        PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.PostProcessPass_Post);
                    }
                }

                if (renderingData.cameraData.captureActions != null)
                {
                    m_CapturePass.Setup(sourceForFinalPass);
                    EnqueuePass(m_CapturePass);
                }

                // if post-processing then we already resolved to camera target while doing post.
                // Also only do final blit if camera is not rendering to RT.
                bool cameraTargetResolved =
                    // final PP always blit to camera target
                    applyFinalPostProcessing ||
                    // no final PP but we have PP stack. In that case it blit unless there are render pass after PP
                    (applyPostProcessing && !hasPassesAfterPostProcessing) ||
                    // offscreen camera rendering to a texture, we don't need a blit pass to resolve to screen
                    m_ActiveCameraColorAttachment == RenderTargetHandle.GetCameraTarget(cameraData.xr);

                overlayPassNeedStore |= !cameraTargetResolved;
                // We need final blit to resolve to screen
                if (!cameraTargetResolved)
                {
                    m_FinalBlitPass.Setup(cameraTargetDescriptor, sourceForFinalPass);
                    EnqueuePass(m_FinalBlitPass);
                    if (usePipelineAnalyze)
                    {
                        PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.PostProcessPass_FinalBlit);
                    }
                }

#if ENABLE_VR && ENABLE_XR_MODULE
                bool depthTargetResolved =
                    // active depth is depth target, we don't need a blit pass to resolve
                    m_ActiveCameraDepthAttachment == RenderTargetHandle.GetCameraTarget(cameraData.xr);

                if (!depthTargetResolved && cameraData.xr.copyDepth)
                {
                    m_XRCopyDepthPass.Setup(m_ActiveCameraDepthAttachment, RenderTargetHandle.GetCameraTarget(cameraData.xr));
                    EnqueuePass(m_XRCopyDepthPass);
                }
#endif
            }

            // stay in RT so we resume rendering on stack after post-processing
            else if (applyPostProcessing)
            {
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
                if (renderingData.maximumEyeBufferRenderingEnabled && cameraData.cameraIndexInStack == cameraData.lastCameraIndexWhoNeedOffScreen)
                {
                    m_PostProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment,
                        RenderTargetHandle.CameraTarget, m_ActiveCameraDepthAttachment, m_ColorGradingLut,
                        applyFinalPostProcessing, true, hasPassesAfterPostProcessing,
                        m_ColorGradingLutPass.GetInternalLutTexture(ref renderingData));
                    EnqueuePass(m_PostProcessPass);
                    if (usePipelineAnalyze)
                    {
                        PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.PostProcessPass_Post);
                    }
                }
                else
                {
                    //PicoVideo;Basic;YangFan;Begin
                    m_PostProcessPass.Setup(cameraTargetDescriptor, m_ActiveCameraColorAttachment, m_AfterPostProcessColor, m_ActiveCameraDepthAttachment, m_ColorGradingLut, false, false, true, m_ColorGradingLutPass.GetInternalLutTexture(ref renderingData));		//PicoVideo;FSR;ZhengLingFeng
                    //PicoVideo;Basic;YangFan;End
                    EnqueuePass(m_PostProcessPass);    
                    if (usePipelineAnalyze)
                    {
                        PipelineAnalyze.AddRenderPass(PipelineState.RenderPassType.PostProcessPass_Post);
                    }
                }
                //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
            }
            
            if(overlayPassNeedStore)
            {
                m_RenderOverlayOpaqueForwardPass.ConfigureColorStoreAction(RenderBufferStoreAction.Store);
                m_RenderOverlayOpaqueForwardPass.ConfigureDepthStoreAction(RenderBufferStoreAction.Store);
                
                m_RenderOverlayTransparentForwardPass.ConfigureColorStoreAction(RenderBufferStoreAction.Store);
                m_RenderOverlayTransparentForwardPass.ConfigureDepthStoreAction(RenderBufferStoreAction.Store);
            }
            
            if (cameraData.renderType == CameraRenderType.Base)
            {
                if (additionalCameraData != null)
                {
                    if (additionalCameraData.overlayLayerMask != 0)
                    {
                        m_RenderOverlayOpaqueForwardPass.ClearDepth(true);
                        m_RenderOverlayOpaqueForwardPass.ResetLayerMask(additionalCameraData.overlayLayerMask);
                        m_RenderOverlayTransparentForwardPass.ResetLayerMask(additionalCameraData.overlayLayerMask);
                        EnqueuePass(m_RenderOverlayOpaqueForwardPass);
                        EnqueuePass(m_RenderOverlayTransparentForwardPass);
                    }
                }
            }

#if UNITY_EDITOR
            if (isSceneViewCamera)
            {
                // Scene view camera should always resolve target (not stacked)
                Assertions.Assert.IsTrue(lastCameraInTheStack, "Editor camera must resolve target upon finish rendering.");
                m_SceneViewDepthCopyPass.Setup(m_DepthTexture);
                EnqueuePass(m_SceneViewDepthCopyPass);
            }
#endif
            //PicoVideo;FoveatedFeature;YangFan;Begin
            // 1. 存在后期且后期后还有pass
            // 2. 存在后期且后期后还有overlay相机（开启了ORP MaximumEyeBufferRendering特性后，改为判断有后期的相机是否是最后一个离屏相机） 
            // 3. 存在不透明贴图拷贝
            if (cameraData.renderType == CameraRenderType.Base)
            {
                UniversalRenderPipeline.cameraColorTextureAsRenderTargetAfterSampling = false;
            }
            UniversalRenderPipeline.cameraColorTextureAsRenderTargetAfterSampling |= applyPostProcessing && hasPassesAfterPostProcessing;
            if (renderingData.maximumEyeBufferRenderingEnabled)
            {
                UniversalRenderPipeline.cameraColorTextureAsRenderTargetAfterSampling |= applyPostProcessing && (renderingData.cameraData.cameraIndexInStack < renderingData.cameraData.lastCameraIndexWhoNeedOffScreen);    
            }
            else
            {
                UniversalRenderPipeline.cameraColorTextureAsRenderTargetAfterSampling |= applyPostProcessing && !lastCameraInTheStack;    
            }
            UniversalRenderPipeline.cameraColorTextureAsRenderTargetAfterSampling |= copyColorPass;
            UniversalRenderPipeline.cameraColorTextureAsRenderTargetAfterSampling |= copyTransparentColorPass;
            //PicoVideo;FoveatedFeature;YangFan;End
            
            if (usePipelineAnalyze)
            {
                PipelineAnalyze.SetEyeBufferSize(UniversalRenderPipeline.asset.renderScale);
                PipelineAnalyze.SetMSAALevel(UniversalRenderPipeline.asset.msaaSampleCount);
                PipelineAnalyze.SetFFRLevel(UniversalRenderPipeline.asset.enableTextureFoveatedFeature ? UniversalRenderPipeline.asset.currentTextureFoveatedFeatureQuality : TextureFoveatedFeatureQuality.Close);
                if (UniversalRenderPipeline.asset.enableSubsampledLayout)
                {
                    PipelineAnalyze.EnableSubsampledLayout();
                }
                if (UniversalRenderPipeline.asset.useSRPBatcher)
                {
                    PipelineAnalyze.EnableSRPBatch();
                }
                if (UniversalRenderPipeline.asset.supportsHDR)
                {
                    PipelineAnalyze.EnableHDR();
                }
                if (renderFeatureEnabled)
                {
                    PipelineAnalyze.EnableRenderFeature();
                }
                if (renderingData.cameraData.includeOverlay)
                {
                    PipelineAnalyze.EnableOverlayCamera();
                }

                bool analyzeSubPassIsOpen = SubPassManager.needSubPass && SubPassManager.supported;
                bool analyzeSubPassIsUseless = !SubPassManager.needSubPass;
                bool analyzeSubPassIsIsAssetClose = !UniversalRenderPipeline.asset.GetOptimizedRenderPipelineFeatureActivated(OptimizedRenderPipelineFeature.SubPass);
                bool analyzeSubPassIsCustomPassUnSupport = !ForwardRendererExtend.GetFlag(extendSupportFlag, ForwardRendererExtend.SUPPORT_SUBPASS);
                PipelineAnalyze.SetSubPassState(analyzeSubPassIsOpen, analyzeSubPassIsUseless, analyzeSubPassIsIsAssetClose, analyzeSubPassIsCustomPassUnSupport);
                PipelineAnalyze.FinishPipelineState();
            }
        }

        /// <inheritdoc />
        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.Setup(context, ref renderingData);

            // Perform per-tile light culling on CPU
            if (this.actualRenderingMode == RenderingMode.Deferred)
                m_DeferredLights.SetupLights(context, ref renderingData);
        }

        /// <inheritdoc />
        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters,
            ref CameraData cameraData)
        {
            // TODO: PerObjectCulling also affect reflection probes. Enabling it for now.
            // if (asset.additionalLightsRenderingMode == LightRenderingMode.Disabled ||
            //     asset.maxAdditionalLightsCount == 0)
            // {
            //     cullingParameters.cullingOptions |= CullingOptions.DisablePerObjectCulling;
            // }

            // We disable shadow casters if both shadow casting modes are turned off
            // or the shadow distance has been turned down to zero
            bool isShadowCastingDisabled = !UniversalRenderPipeline.asset.supportsMainLightShadows && !UniversalRenderPipeline.asset.supportsAdditionalLightShadows;
            bool isShadowDistanceZero = Mathf.Approximately(cameraData.maxShadowDistance, 0.0f);
            if (isShadowCastingDisabled || isShadowDistanceZero)
            {
                cullingParameters.cullingOptions &= ~CullingOptions.ShadowCasters;
            }

            if (this.actualRenderingMode == RenderingMode.Deferred)
                cullingParameters.maximumVisibleLights = 0xFFFF;
            else
            {
                // We set the number of maximum visible lights allowed and we add one for the mainlight...
                cullingParameters.maximumVisibleLights = UniversalRenderPipeline.maxVisibleAdditionalLights + 1;
            }
            cullingParameters.shadowDistance = cameraData.maxShadowDistance;
        }

        /// <inheritdoc />
        public override void FinishRendering(CommandBuffer cmd)
        {
            if (m_ActiveCameraColorAttachment != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(m_ActiveCameraColorAttachment.id);
                m_ActiveCameraColorAttachment = RenderTargetHandle.CameraTarget;
            }

            if (m_ActiveCameraDepthAttachment != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(m_ActiveCameraDepthAttachment.id);
                m_ActiveCameraDepthAttachment = RenderTargetHandle.CameraTarget;
            }
        }

        void EnqueueDeferred(ref RenderingData renderingData, bool hasDepthPrepass, bool applyMainShadow, bool applyAdditionalShadow)
        {
            // the last slice is the lighting buffer created in DeferredRenderer.cs
            m_GBufferHandles[(int)DeferredLights.GBufferHandles.Lighting] = m_ActiveCameraColorAttachment;

            m_DeferredLights.Setup(
                ref renderingData,
                applyAdditionalShadow ? m_AdditionalLightsShadowCasterPass : null,
                hasDepthPrepass,
                renderingData.cameraData.renderType == CameraRenderType.Overlay,
                m_DepthTexture,
                m_DepthInfoTexture,
                m_TileDepthInfoTexture,
                m_ActiveCameraDepthAttachment, m_GBufferHandles
            );

            EnqueuePass(m_GBufferPass);

            EnqueuePass(m_RenderOpaqueForwardOnlyPass);

            //Must copy depth for deferred shading: TODO wait for API fix to bind depth texture as read-only resource.
            if (!hasDepthPrepass)
            {
                m_GBufferCopyDepthPass.Setup(m_CameraDepthAttachment, m_DepthTexture);
                EnqueuePass(m_GBufferCopyDepthPass);
            }

            // Note: DeferredRender.Setup is called by UniversalRenderPipeline.RenderSingleCamera (overrides ScriptableRenderer.Setup).
            // At this point, we do not know if m_DeferredLights.m_Tilers[x].m_Tiles actually contain any indices of lights intersecting tiles (If there are no lights intersecting tiles, we could skip several following passes) : this information is computed in DeferredRender.SetupLights, which is called later by UniversalRenderPipeline.RenderSingleCamera (via ScriptableRenderer.Execute).
            // However HasTileLights uses m_HasTileVisLights which is calculated by CheckHasTileLights from all visibleLights. visibleLights is the list of lights that have passed camera culling, so we know they are in front of the camera. So we can assume m_DeferredLights.m_Tilers[x].m_Tiles will not be empty in that case.
            // m_DeferredLights.m_Tilers[x].m_Tiles could be empty if we implemented an algorithm accessing scene depth information on the CPU side, but this (access depth from CPU) will probably not happen.
            if (m_DeferredLights.HasTileLights())
            {
                // Compute for each tile a 32bits bitmask in which a raised bit means "this 1/32th depth slice contains geometry that could intersect with lights".
                // Per-tile bitmasks are obtained by merging together the per-pixel bitmasks computed for each individual pixel of the tile.
                EnqueuePass(m_TileDepthRangePass);

                // On some platform, splitting the bitmasks computation into two passes:
                //   1/ Compute bitmasks for individual or small blocks of pixels
                //   2/ merge those individual bitmasks into per-tile bitmasks
                // provides better performance that doing it in a single above pass.
                if (m_DeferredLights.HasTileDepthRangeExtraPass())
                    EnqueuePass(m_TileDepthRangeExtraPass);
            }

            EnqueuePass(m_DeferredPass);
        }

        private struct RenderPassInputSummary
        {
            internal bool requiresDepthTexture;
            internal bool requiresDepthPrepass;
            internal bool requiresNormalsTexture;
            internal bool requiresColorTexture;
            //PicoVideo;Basic;Ernst;Begin
            internal bool requresCreateColorTexture;
            internal bool requresStencilBuffer;
            internal bool requireDepthStencilInPost;
            internal bool supportSubPass;
            //PicoVideo;Basic;Ernst;End
        }

        private RenderPassInputSummary GetRenderPassInputs(ref RenderingData renderingData)
        {
            RenderPassInputSummary inputSummary = new RenderPassInputSummary();
            //PicoVideo;SubPass;Ernst;Begin
            inputSummary.supportSubPass = true;
            //PicoVideo;SubPass;Ernst;End
            for (int i = 0; i < activeRenderPassQueue.Count; ++i)
            {
                ScriptableRenderPass pass = activeRenderPassQueue[i];
                bool needsDepth   = (pass.input & ScriptableRenderPassInput.Depth) != ScriptableRenderPassInput.None;
                bool needsNormals = (pass.input & ScriptableRenderPassInput.Normal) != ScriptableRenderPassInput.None;
                bool needsColor   = (pass.input & ScriptableRenderPassInput.Color) != ScriptableRenderPassInput.None;
                bool eventBeforeOpaque = pass.renderPassEvent <= RenderPassEvent.BeforeRenderingOpaques;

                inputSummary.requiresDepthTexture   |= needsDepth;
                inputSummary.requiresDepthPrepass   |= needsNormals || needsDepth && eventBeforeOpaque;
                inputSummary.requiresNormalsTexture |= needsNormals;
                inputSummary.requiresColorTexture   |= needsColor;
                //PicoVideo;Basic;Ernst;Begin
                inputSummary.requresCreateColorTexture |= (pass.input & ScriptableRenderPassInput.CreateColorTexture) != ScriptableRenderPassInput.None;
                inputSummary.requresStencilBuffer |= (pass.input & ScriptableRenderPassInput.UseStencilBuffer) != ScriptableRenderPassInput.None;
                inputSummary.requireDepthStencilInPost |= (pass.input & ScriptableRenderPassInput.UseStencilInPost) != ScriptableRenderPassInput.None;
                inputSummary.supportSubPass &= (pass.input & ScriptableRenderPassInput.SupportSubPass) != ScriptableRenderPassInput.None;
                //PicoVideo;Basic;Ernst;End
            }

            return inputSummary;
        }

        void CreateCameraRenderTarget(ScriptableRenderContext context, ref RenderingData renderingData, ref RenderTextureDescriptor descriptor, bool createColor, bool createDepth)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, Profiling.createCameraRenderTarget))
            {
                if (createColor)
                {
                    bool useDepthRenderBuffer = m_ActiveCameraDepthAttachment == RenderTargetHandle.CameraTarget;
                    var colorDescriptor = descriptor;
                    colorDescriptor.useMipMap = false;
                    colorDescriptor.autoGenerateMips = false;
                    colorDescriptor.depthBufferBits = (useDepthRenderBuffer) ? k_DepthStencilBufferBits : 0;
                    //PicoVideo;OptimizedRenderPipeline;YangFan;Begin
                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan && colorDescriptor.msaaSamples > 1 && 
                        UniversalRenderPipeline.asset.GetOptimizedRenderPipelineFeatureActivated(OptimizedRenderPipelineFeature.MemorylessMSAA) && 
                        !UniversalRenderPipeline.cameraColorTextureAsRenderTargetAfterSampling && !renderingData.cameraData.includeOverlay)
                    {
                        colorDescriptor.memoryless = RenderTextureMemoryless.MSAA;
                    }
                    //PicoVideo;OptimizedRenderPipeline;YangFan;End
                    //PicoVideo;FoveatedFeature;YangFan;Begin
#if PICO_VIDEO_VRS_EXTEND3_SUPPORTED
                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan && s_OnFocus && renderingData.isMainCamera && colorDescriptor.width == renderingData.cameraData.xr.renderTargetDesc.width && colorDescriptor.height == renderingData.cameraData.xr.renderTargetDesc.height)
                    {
                        colorDescriptor.useFoveatedImage = UniversalRenderPipeline.fixedFoveatedRenderingEnabled || UniversalRenderPipeline.eyeTrackingFoveatedRenderingEnabled;
                    }
#endif
                    //PicoVideo;FoveatedFeature;YangFan;End
                    cmd.GetTemporaryRT(m_ActiveCameraColorAttachment.id, colorDescriptor, FilterMode.Bilinear);
                    //PicoVideo;FoveatedFeature;YangFan;Begin
#if PICO_VIDEO_VRS_EXTEND3_SUPPORTED
                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan && s_FoveationImageIsDirty && renderingData.isMainCamera && colorDescriptor.width == renderingData.cameraData.xr.renderTargetDesc.width && colorDescriptor.height == renderingData.cameraData.xr.renderTargetDesc.height)
                    {
                        cmd.ReleaseTemporaryRT(m_ActiveCameraColorAttachment.id, true);
                        cmd.GetTemporaryRT(m_ActiveCameraColorAttachment.id, colorDescriptor, FilterMode.Bilinear);
                        s_FoveationImageIsDirty = false;
                    }
#endif
                    if (renderingData.isMainCamera && colorDescriptor.width == renderingData.cameraData.xr.renderTargetDesc.width && colorDescriptor.height == renderingData.cameraData.xr.renderTargetDesc.height)
                    {
                        UniversalRenderPipeline.SetTextureFoveatedRendering(cmd, m_ActiveCameraColorAttachment.id, ref colorDescriptor, true);
                    }
                    //PicoVideo;FoveatedFeature;YangFan;End
                }

                if (createDepth)
                {
                    var depthDescriptor = descriptor;
                    depthDescriptor.useMipMap = false;
                    depthDescriptor.autoGenerateMips = false;
#if ENABLE_VR && ENABLE_XR_MODULE
                    // XRTODO: Enabled this line for non-XR pass? URP copy depth pass is already capable of handling MSAA.
                    depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
#endif
                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Vulkan && 
                        UniversalRenderPipeline.asset.GetOptimizedRenderPipelineFeatureActivated(OptimizedRenderPipelineFeature.MemorylessMSAA) && 
                        !UniversalRenderPipeline.cameraColorTextureAsRenderTargetAfterSampling && !renderingData.cameraData.includeOverlay)
                    {
                        depthDescriptor.memoryless = RenderTextureMemoryless.Depth;
                    }
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthBufferBits = k_DepthStencilBufferBits;
                    cmd.GetTemporaryRT(m_ActiveCameraDepthAttachment.id, depthDescriptor, FilterMode.Point);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public static bool PlatformRequiresExplicitMsaaResolve()//PicoVideo;SubPass;Ernst
        {
            // On Metal/iOS the MSAA resolve is done implicitly as part of the renderpass, so we do not need an extra intermediate pass for the explicit autoresolve.
            // TODO: should also be valid on Metal MacOS/Editor, but currently not working as expected. Remove the "mobile only" requirement once trunk has a fix.
            //PicoVideo;SubPass;Ernst;Begin
            //编辑器模式和非手机平台下不支持开启MSAA后直接渲染到BackBuffer(需要修改较多的引擎源码)
            if (!Application.isMobilePlatform || Application.isEditor)
            {
                return true;
            }
            //PicoVideo;SubPass;Ernst;End
            return !SystemInfo.supportsMultisampleAutoResolve &&
                   !(SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal && Application.isMobilePlatform);
        }

        /// <summary>
        /// Checks if the pipeline needs to create a intermediate render texture.
        /// </summary>
        /// <param name="cameraData">CameraData contains all relevant render target information for the camera.</param>
        /// <param name="renderingData"></param>
        /// <seealso cref="CameraData"/>
        /// <returns>Return true if pipeline needs to render to a intermediate render texture.</returns>
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
        bool RequiresIntermediateColorTexture(ref CameraData cameraData, ref RenderingData renderingData)
        //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
        {
            // When rendering a camera stack we always create an intermediate render texture to composite camera results.
            // We create it upon rendering the Base camera.
            
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;Begin
            if (renderingData.maximumEyeBufferRenderingEnabled && renderingData.isMainCamera)
            {
                if (cameraData.cameraIndexInStack <= cameraData.lastCameraIndexWhoNeedOffScreen)
                    return true;    
            }
            else
            {
                if (cameraData.renderType == CameraRenderType.Base && !cameraData.resolveFinalTarget)
                    return true;
            }
            //PicoVideo;OptimizedRenderPipeline;BaoQinShun;End
            

            // Always force rendering into intermediate color texture if deferred rendering mode is selected.
            // Reason: without intermediate color texture, the target camera texture is y-flipped.
            // However, the target camera texture is bound during gbuffer pass and deferred pass.
            // Gbuffer pass will not be y-flipped because it is MRT (see ScriptableRenderContext implementation),
            // while deferred pass will be y-flipped, which breaks rendering.
            // This incurs an extra blit into at the end of rendering.
            if (this.actualRenderingMode == RenderingMode.Deferred)
                return true;

            bool isSceneViewCamera = cameraData.isSceneViewCamera;
            var cameraTargetDescriptor = cameraData.cameraTargetDescriptor;
            int msaaSamples = cameraTargetDescriptor.msaaSamples;
            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isCompatibleBackbufferTextureDimension = cameraTargetDescriptor.dimension == TextureDimension.Tex2D;
            bool requiresExplicitMsaaResolve = msaaSamples > 1 && PlatformRequiresExplicitMsaaResolve();
            bool isOffscreenRender = cameraData.targetTexture != null && !isSceneViewCamera;
            bool isCapturing = cameraData.captureActions != null;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
            {
                isScaledRender = false;
                isCompatibleBackbufferTextureDimension = cameraData.xr.renderTargetDesc.dimension == cameraTargetDescriptor.dimension;
            }
#endif

            bool requiresBlitForOffscreenCamera = cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || requiresExplicitMsaaResolve || !cameraData.isDefaultViewport;
            //PicoVideo;SubPass;Ernst;Begin
            if (cameraData.renderType == CameraRenderType.Base)
            {
                requiresBlitForOffscreenCamera |= UniversalRenderPipeline.asset.supportsCameraTransparentTexture;
            }
            //PicoVideo;SubPass;Ernst;End
            if (isOffscreenRender)
                return requiresBlitForOffscreenCamera;

            return requiresBlitForOffscreenCamera || isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                   !isCompatibleBackbufferTextureDimension || isCapturing || cameraData.requireSrgbConversion;
        }

        bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = cameraData.cameraTargetDescriptor.msaaSamples > 1;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool supportsDepthTarget = RenderingUtils.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

            // TODO:  We don't have support to highp Texture2DMS currently and this breaks depth precision.
            // currently disabling it until shader changes kick in.
            //bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
            bool msaaDepthResolve = false;
            return supportsDepthCopy || msaaDepthResolve;
        }
    }
}
