using System;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a shadow map for the main Light.
    /// </summary>
    public class MainLightShadowCasterPass : ScriptableRenderPass
    {
        private static class MainLightShadowConstantBuffer
        {
            public static int _WorldToShadow;
            public static int _ShadowParams;
            public static int _CascadeShadowSplitSpheres0;
            public static int _CascadeShadowSplitSpheres1;
            public static int _CascadeShadowSplitSpheres2;
            public static int _CascadeShadowSplitSpheres3;
            public static int _CascadeShadowSplitSphereRadii;
            public static int _ShadowOffset0;
            public static int _ShadowOffset1;
            public static int _ShadowOffset2;
            public static int _ShadowOffset3;
            public static int _ShadowmapSize;
        }

        const int k_MaxCascades = 4;
        const int k_ShadowmapBufferBits = 16;
        float m_MaxShadowDistance;
        int m_ShadowmapWidth;
        int m_ShadowmapHeight;
        int m_ShadowCasterCascadesCount;
        bool m_SupportsBoxFilterForShadows;

        RenderTargetHandle m_MainLightShadowmap;
        RenderTexture m_MainLightShadowmapTexture;

        Matrix4x4[] m_MainLightShadowMatrices;
        ShadowSliceData[] m_CascadeSlices;
        Vector4[] m_CascadeSplitDistances;

        ProfilingSampler m_ProfilingSetupSampler = new ProfilingSampler("Setup Main Shadowmap");

        //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;Begin
        Vector3[] m_CustomShadowRangePoints = new Vector3[8];
        static bool s_FirstInitCustomShadowRange = true;
        //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;End

        public MainLightShadowCasterPass(RenderPassEvent evt)
        {
            base.profilingSampler = new ProfilingSampler(nameof(MainLightShadowCasterPass));
            renderPassEvent = evt;

            m_MainLightShadowMatrices = new Matrix4x4[k_MaxCascades + 1];
            m_CascadeSlices = new ShadowSliceData[k_MaxCascades];
            m_CascadeSplitDistances = new Vector4[k_MaxCascades];

            MainLightShadowConstantBuffer._WorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");
            MainLightShadowConstantBuffer._ShadowParams = Shader.PropertyToID("_MainLightShadowParams");
            MainLightShadowConstantBuffer._CascadeShadowSplitSpheres0 = Shader.PropertyToID("_CascadeShadowSplitSpheres0");
            MainLightShadowConstantBuffer._CascadeShadowSplitSpheres1 = Shader.PropertyToID("_CascadeShadowSplitSpheres1");
            MainLightShadowConstantBuffer._CascadeShadowSplitSpheres2 = Shader.PropertyToID("_CascadeShadowSplitSpheres2");
            MainLightShadowConstantBuffer._CascadeShadowSplitSpheres3 = Shader.PropertyToID("_CascadeShadowSplitSpheres3");
            MainLightShadowConstantBuffer._CascadeShadowSplitSphereRadii = Shader.PropertyToID("_CascadeShadowSplitSphereRadii");
            MainLightShadowConstantBuffer._ShadowOffset0 = Shader.PropertyToID("_MainLightShadowOffset0");
            MainLightShadowConstantBuffer._ShadowOffset1 = Shader.PropertyToID("_MainLightShadowOffset1");
            MainLightShadowConstantBuffer._ShadowOffset2 = Shader.PropertyToID("_MainLightShadowOffset2");
            MainLightShadowConstantBuffer._ShadowOffset3 = Shader.PropertyToID("_MainLightShadowOffset3");
            MainLightShadowConstantBuffer._ShadowmapSize = Shader.PropertyToID("_MainLightShadowmapSize");

            m_MainLightShadowmap.Init("_MainLightShadowmapTexture");
            m_SupportsBoxFilterForShadows = Application.isMobilePlatform || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch;
        }

        public bool Setup(ref RenderingData renderingData)
        {
            using var profScope = new ProfilingScope(null, m_ProfilingSetupSampler);

            if (!renderingData.shadowData.supportsMainLightShadows)
                return false;

            Clear();
            int shadowLightIndex = renderingData.lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return false;

            VisibleLight shadowLight = renderingData.lightData.visibleLights[shadowLightIndex];
            Light light = shadowLight.light;
            if (light.shadows == LightShadows.None)
                return false;

            if (shadowLight.lightType != LightType.Directional)
            {
                Debug.LogWarning("Only directional lights are supported as main light.");
            }

            Bounds bounds;
            if (!renderingData.cullResults.GetShadowCasterBounds(shadowLightIndex, out bounds))
                return false;

            //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;Begin
#if UNITY_EDITOR
            InitCustomMainLightShadowRange(bounds);
#endif
            bool enableCustomMainLightShadowRange = UniversalRenderPipeline.asset.enableCustomMainLightShadowRange;
            CustomMainLightShadowRange shadowRange = UniversalRenderPipeline.asset.customMainLightShadowRange;
            if (enableCustomMainLightShadowRange)
            {
                bool isValidShadowRange = shadowRange.maxX > shadowRange.minX;
                if (shadowRange.maxX <= shadowRange.minX)
                {
                    Debug.LogError("CustomMainLightShadowRange is InValid, MaxX Must Great Than MinX.");
                }
                
                if (shadowRange.maxY <= shadowRange.minY)
                {
                    Debug.LogError("CustomMainLightShadowRange is InValid, MaxY Must Great Than MinY.");
                }
                
                if (shadowRange.maxZ <= shadowRange.minZ)
                {
                    Debug.LogError("CustomMainLightShadowRange is InValid, MaxZ Must Great Than MinZ.");
                }

                isValidShadowRange &= shadowRange.maxY > shadowRange.minY;
                isValidShadowRange &= shadowRange.maxZ > shadowRange.minZ;
                enableCustomMainLightShadowRange &= isValidShadowRange;
            }

            if (enableCustomMainLightShadowRange)
            {
                m_ShadowCasterCascadesCount = 1;
                int shadowResolution = renderingData.shadowData.mainLightShadowmapWidth;
                m_ShadowmapWidth = renderingData.shadowData.mainLightShadowmapWidth;
                m_ShadowmapHeight = renderingData.shadowData.mainLightShadowmapHeight;

                m_CustomShadowRangePoints[0] = new Vector3(shadowRange.minX, shadowRange.maxY, shadowRange.minZ);
                m_CustomShadowRangePoints[1] = new Vector3(shadowRange.maxX, shadowRange.maxY, shadowRange.minZ);
                m_CustomShadowRangePoints[2] = new Vector3(shadowRange.maxX, shadowRange.minY, shadowRange.minZ);
                m_CustomShadowRangePoints[3] = new Vector3(shadowRange.minX, shadowRange.minY, shadowRange.minZ);
                m_CustomShadowRangePoints[4] = new Vector3(shadowRange.minX, shadowRange.maxY, shadowRange.maxZ);
                m_CustomShadowRangePoints[5] = new Vector3(shadowRange.maxX, shadowRange.maxY, shadowRange.maxZ);
                m_CustomShadowRangePoints[6] = new Vector3(shadowRange.maxX, shadowRange.minY, shadowRange.maxZ);
                m_CustomShadowRangePoints[7] = new Vector3(shadowRange.minX, shadowRange.minY, shadowRange.maxZ);

#if UNITY_EDITOR
                if (UniversalRenderPipeline.asset.debugCustomMainLightShadowRange)
                {
                    // draw shadow camera visible box in world space
                    Debug.DrawLine(m_CustomShadowRangePoints[0], m_CustomShadowRangePoints[1], Color.red);
                    Debug.DrawLine(m_CustomShadowRangePoints[1], m_CustomShadowRangePoints[2], Color.red);
                    Debug.DrawLine(m_CustomShadowRangePoints[2], m_CustomShadowRangePoints[3], Color.red);
                    Debug.DrawLine(m_CustomShadowRangePoints[3], m_CustomShadowRangePoints[0], Color.red);

                    Debug.DrawLine(m_CustomShadowRangePoints[4], m_CustomShadowRangePoints[5], Color.red);
                    Debug.DrawLine(m_CustomShadowRangePoints[5], m_CustomShadowRangePoints[6], Color.red);
                    Debug.DrawLine(m_CustomShadowRangePoints[6], m_CustomShadowRangePoints[7], Color.red);
                    Debug.DrawLine(m_CustomShadowRangePoints[7], m_CustomShadowRangePoints[4], Color.red);

                    Debug.DrawLine(m_CustomShadowRangePoints[0], m_CustomShadowRangePoints[4], Color.red);
                    Debug.DrawLine(m_CustomShadowRangePoints[1], m_CustomShadowRangePoints[5], Color.red);
                    Debug.DrawLine(m_CustomShadowRangePoints[2], m_CustomShadowRangePoints[6], Color.red);
                    Debug.DrawLine(m_CustomShadowRangePoints[3], m_CustomShadowRangePoints[7], Color.red);
                }
#endif
                if (!SystemInfo.usesReversedZBuffer)
                {
                    m_CascadeSlices[0].viewMatrix = Matrix4x4.Scale(new Vector3(1, 1, -1)) * light.transform.worldToLocalMatrix;
                }
                else
                {
                    m_CascadeSlices[0].viewMatrix = light.transform.worldToLocalMatrix;
                }

                Matrix4x4 transViewMatrix = Matrix4x4.Scale(new Vector3(1, 1, -1)) * m_CascadeSlices[0].viewMatrix;
                //变换8个顶点到light空间
                for (int i = 0; i < m_CustomShadowRangePoints.Length; i++)
                {
                    m_CustomShadowRangePoints[i] = transViewMatrix.MultiplyPoint(m_CustomShadowRangePoints[i]);
                }

                float minX = Mathf.Infinity;
                float maxX = Mathf.NegativeInfinity;
                float minY = Mathf.Infinity;
                float maxY = Mathf.NegativeInfinity;
                float minZ = Mathf.Infinity;
                float maxZ = Mathf.NegativeInfinity;

                for (int i = 0; i < m_CustomShadowRangePoints.Length; i++)
                {
                    minX = Mathf.Min(minX, m_CustomShadowRangePoints[i].x);
                    maxX = Mathf.Max(maxX, m_CustomShadowRangePoints[i].x);
                    minY = Mathf.Min(minY, m_CustomShadowRangePoints[i].y);
                    maxY = Mathf.Max(maxY, m_CustomShadowRangePoints[i].y);
                    minZ = Mathf.Min(minZ, m_CustomShadowRangePoints[i].z);
                    maxZ = Mathf.Max(maxZ, m_CustomShadowRangePoints[i].z);
                }

#if UNITY_EDITOR
                if (UniversalRenderPipeline.asset.debugCustomMainLightShadowRange)
                {
                    m_CustomShadowRangePoints[0] = new Vector3(minX, maxY, minZ);
                    m_CustomShadowRangePoints[1] = new Vector3(maxX, maxY, minZ);
                    m_CustomShadowRangePoints[2] = new Vector3(maxX, minY, minZ);
                    m_CustomShadowRangePoints[3] = new Vector3(minX, minY, minZ);
                    m_CustomShadowRangePoints[4] = new Vector3(minX, maxY, maxZ);
                    m_CustomShadowRangePoints[5] = new Vector3(maxX, maxY, maxZ);
                    m_CustomShadowRangePoints[6] = new Vector3(maxX, minY, maxZ);
                    m_CustomShadowRangePoints[7] = new Vector3(minX, minY, maxZ);

                    for (int i = 0; i < m_CustomShadowRangePoints.Length; i++)
                    {
                        m_CustomShadowRangePoints[i] = transViewMatrix.inverse.MultiplyPoint(m_CustomShadowRangePoints[i]);
                    }

                    // draw shadow camera visible box in light space
                    Debug.DrawLine(m_CustomShadowRangePoints[0], m_CustomShadowRangePoints[1], Color.green);
                    Debug.DrawLine(m_CustomShadowRangePoints[1], m_CustomShadowRangePoints[2], Color.green);
                    Debug.DrawLine(m_CustomShadowRangePoints[2], m_CustomShadowRangePoints[3], Color.green);
                    Debug.DrawLine(m_CustomShadowRangePoints[3], m_CustomShadowRangePoints[0], Color.green);

                    Debug.DrawLine(m_CustomShadowRangePoints[4], m_CustomShadowRangePoints[5], Color.green);
                    Debug.DrawLine(m_CustomShadowRangePoints[5], m_CustomShadowRangePoints[6], Color.green);
                    Debug.DrawLine(m_CustomShadowRangePoints[6], m_CustomShadowRangePoints[7], Color.green);
                    Debug.DrawLine(m_CustomShadowRangePoints[7], m_CustomShadowRangePoints[4], Color.green);

                    Debug.DrawLine(m_CustomShadowRangePoints[0], m_CustomShadowRangePoints[4], Color.green);
                    Debug.DrawLine(m_CustomShadowRangePoints[1], m_CustomShadowRangePoints[5], Color.green);
                    Debug.DrawLine(m_CustomShadowRangePoints[2], m_CustomShadowRangePoints[6], Color.green);
                    Debug.DrawLine(m_CustomShadowRangePoints[3], m_CustomShadowRangePoints[7], Color.green);
                }
#endif

                //前后左右上下再扩大1,减少采样到边缘的问题
                Matrix4x4 projectionMatrix = Matrix4x4.Ortho(minX - 1, maxX + 1, minY - 1, maxY + 1, minZ - 1, maxZ + 1);
                Matrix4x4 gpuProjectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);
                m_CascadeSlices[0].projectionMatrix = gpuProjectionMatrix;
                m_CascadeSlices[0].shadowTransform = ShadowUtils.GetShadowTransform(gpuProjectionMatrix, m_CascadeSlices[0].viewMatrix);
                m_CascadeSlices[0].offsetX = m_CascadeSlices[0].offsetY = 0;
                m_CascadeSlices[0].resolution = shadowResolution;
            }
            else//PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;End
            {
                m_ShadowCasterCascadesCount = renderingData.shadowData.mainLightShadowCascadesCount;

                int shadowResolution = ShadowUtils.GetMaxTileResolutionInAtlas(renderingData.shadowData.mainLightShadowmapWidth,
                    renderingData.shadowData.mainLightShadowmapHeight, m_ShadowCasterCascadesCount);
                m_ShadowmapWidth = renderingData.shadowData.mainLightShadowmapWidth;
                m_ShadowmapHeight = (m_ShadowCasterCascadesCount == 2) ?
                    renderingData.shadowData.mainLightShadowmapHeight >> 1 :
                    renderingData.shadowData.mainLightShadowmapHeight;

                for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
                {
                    bool success = ShadowUtils.ExtractDirectionalLightMatrix(ref renderingData.cullResults, ref renderingData.shadowData,
                        shadowLightIndex, cascadeIndex, m_ShadowmapWidth, m_ShadowmapHeight, shadowResolution, light.shadowNearPlane,
                        out m_CascadeSplitDistances[cascadeIndex], out m_CascadeSlices[cascadeIndex], out m_CascadeSlices[cascadeIndex].viewMatrix, out m_CascadeSlices[cascadeIndex].projectionMatrix);

                    if (!success)
                        return false;
                }
            }

            m_MaxShadowDistance = renderingData.cameraData.maxShadowDistance * renderingData.cameraData.maxShadowDistance;

            return true;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_MainLightShadowmapTexture = ShadowUtils.GetTemporaryShadowTexture(m_ShadowmapWidth,
                    m_ShadowmapHeight, k_ShadowmapBufferBits);
            ConfigureTarget(new RenderTargetIdentifier(m_MainLightShadowmapTexture));
            ConfigureClear(ClearFlag.All, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RenderMainLightCascadeShadowmap(ref context, ref renderingData.cullResults, ref renderingData.lightData, ref renderingData.shadowData);
        }

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            if (cmd == null)
                throw new ArgumentNullException("cmd");

            if (m_MainLightShadowmapTexture)
            {
                RenderTexture.ReleaseTemporary(m_MainLightShadowmapTexture);
                m_MainLightShadowmapTexture = null;
            }
        }

        void Clear()
        {
            m_MainLightShadowmapTexture = null;

            for (int i = 0; i < m_MainLightShadowMatrices.Length; ++i)
                m_MainLightShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
                m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            for (int i = 0; i < m_CascadeSlices.Length; ++i)
                m_CascadeSlices[i].Clear();
        }

        void RenderMainLightCascadeShadowmap(ref ScriptableRenderContext context, ref CullingResults cullResults, ref LightData lightData, ref ShadowData shadowData)
        {
            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return;

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.MainLightShadow)))
            {
                var settings = new ShadowDrawingSettings(cullResults, shadowLightIndex);

                for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
                {
                    var splitData = settings.splitData;
                    splitData.cullingSphere = m_CascadeSplitDistances[cascadeIndex];
                    settings.splitData = splitData;
                    Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex, ref shadowData, m_CascadeSlices[cascadeIndex].projectionMatrix, m_CascadeSlices[cascadeIndex].resolution);
                    ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                    ShadowUtils.RenderShadowSlice(cmd, ref context, ref m_CascadeSlices[cascadeIndex],
                        ref settings, m_CascadeSlices[cascadeIndex].projectionMatrix, m_CascadeSlices[cascadeIndex].viewMatrix);
                }

                bool softShadows = shadowLight.light.shadows == LightShadows.Soft && shadowData.supportsSoftShadows;
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadows, true);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.MainLightShadowCascades, m_ShadowCasterCascadesCount > 1);//PicoVideo;CustomMainLightShadowRange;XiaoPengCheng
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.SoftShadows, softShadows);

                SetupMainLightShadowReceiverConstants(cmd, shadowLight, shadowData.supportsSoftShadows);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupMainLightShadowReceiverConstants(CommandBuffer cmd, VisibleLight shadowLight, bool supportsSoftShadows)
        {
            Light light = shadowLight.light;
            bool softShadows = shadowLight.light.shadows == LightShadows.Soft && supportsSoftShadows;

            int cascadeCount = m_ShadowCasterCascadesCount;
            for (int i = 0; i < cascadeCount; ++i)
                m_MainLightShadowMatrices[i] = m_CascadeSlices[i].shadowTransform;

            // We setup and additional a no-op WorldToShadow matrix in the last index
            // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
            // out of bounds. (position not inside any cascade) and we want to avoid branching
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m22 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
            for (int i = cascadeCount; i <= k_MaxCascades; ++i)
                m_MainLightShadowMatrices[i] = noOpShadowMatrix;

            float invShadowAtlasWidth = 1.0f / m_ShadowmapWidth;
            float invShadowAtlasHeight = 1.0f / m_ShadowmapHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
            float softShadowsProp = softShadows ? 1.0f : 0.0f;

            //To make the shadow fading fit into a single MAD instruction:
            //distanceCamToPixel2 * oneOverFadeDist + minusStartFade (single MAD)
            float startFade = m_MaxShadowDistance * 0.9f;
            float oneOverFadeDist = 1/(m_MaxShadowDistance - startFade);
            float minusStartFade = -startFade * oneOverFadeDist;


            cmd.SetGlobalTexture(m_MainLightShadowmap.id, m_MainLightShadowmapTexture);
            cmd.SetGlobalMatrixArray(MainLightShadowConstantBuffer._WorldToShadow, m_MainLightShadowMatrices);
            cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowParams, new Vector4(light.shadowStrength, softShadowsProp, oneOverFadeDist, minusStartFade));

            if (m_ShadowCasterCascadesCount > 1)
            {
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres0,
                    m_CascadeSplitDistances[0]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres1,
                    m_CascadeSplitDistances[1]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres2,
                    m_CascadeSplitDistances[2]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres3,
                    m_CascadeSplitDistances[3]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSphereRadii, new Vector4(
                    m_CascadeSplitDistances[0].w * m_CascadeSplitDistances[0].w,
                    m_CascadeSplitDistances[1].w * m_CascadeSplitDistances[1].w,
                    m_CascadeSplitDistances[2].w * m_CascadeSplitDistances[2].w,
                    m_CascadeSplitDistances[3].w * m_CascadeSplitDistances[3].w));
            }

            // Inside shader soft shadows are controlled through global keyword.
            // If any additional light has soft shadows it will force soft shadows on main light too.
            // As it is not trivial finding out which additional light has soft shadows, we will pass main light properties if soft shadows are supported.
            // This workaround will be removed once we will support soft shadows per light.
            if (supportsSoftShadows)
            {
                //PicoVideo;FixSoftShadowsInEditor;XiaoPengCheng;Begin
                //if (m_SupportsBoxFilterForShadows)
                //PicoVideo;FixSoftShadowsInEditor;XiaoPengCheng;End
                {
                    cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowOffset0,
                        new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowOffset1,
                        new Vector4(invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowOffset2,
                        new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
                    cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowOffset3,
                        new Vector4(invHalfShadowAtlasWidth, invHalfShadowAtlasHeight, 0.0f, 0.0f));
                }

                // Currently only used when !SHADER_API_MOBILE but risky to not set them as it's generic
                // enough so custom shaders might use it.
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowmapSize, new Vector4(invShadowAtlasWidth,
                    invShadowAtlasHeight,
                    m_ShadowmapWidth, m_ShadowmapHeight));
            }
        }

        //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;Begin
#if UNITY_EDITOR
        void InitCustomMainLightShadowRange(Bounds bounds)
        {
            if (UniversalRenderPipeline.asset.enableCustomMainLightShadowRange)
            {
                bool resetRange = s_FirstInitCustomShadowRange || UniversalRenderPipeline.asset.resetCustomMainLightShadowRange;

                if (s_FirstInitCustomShadowRange)
                {
                    s_FirstInitCustomShadowRange = false;
                }

                if (UniversalRenderPipeline.asset.resetCustomMainLightShadowRange)
                {
                    UniversalRenderPipeline.asset.resetCustomMainLightShadowRange = false;
                }

                if (resetRange)
                {
                    CustomMainLightShadowRange shadowRange = UniversalRenderPipeline.asset.customMainLightShadowRange;
                    shadowRange.minX = bounds.min.x;
                    shadowRange.maxX = bounds.max.x;
                    shadowRange.minY = bounds.min.y;
                    shadowRange.maxY = bounds.max.y;
                    shadowRange.minZ = bounds.min.z;
                    shadowRange.maxZ = bounds.max.z;
                }
            }
        }
#endif
        //PicoVideo;CustomMainLightShadowRange;XiaoPengCheng;End
    };
}
