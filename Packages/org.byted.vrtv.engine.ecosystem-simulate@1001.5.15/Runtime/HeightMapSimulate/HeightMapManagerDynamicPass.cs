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

using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using Object = UnityEngine.Object;

namespace Matrix.EcosystemSimulate
{
    public class HeightMapManagerDynamicPass :  UserDefinedPass
    {
        private const string s_ProfilerTag = "DynamicHeightMap";
        private Vector3 m_LastTargetPosition;
        private Camera m_HeightMapCamera;
        private RenderTexture m_HeightMapTexture;
        private RenderTexture m_HeightMapTexturePoint;
        private readonly HeightMapManager m_HeightMapManager;
        private readonly ShaderTagId m_HeightMapShaderTagId;
        private FilteringSettings m_FilteringSettings;
        private static readonly int s_DynamicHeightMapTextureShaderID = Shader.PropertyToID("_ESSDynamicHeightMapTexture");
        private static readonly int s_DynamicHeightMapTexturePointShaderID = Shader.PropertyToID("_ESSDynamicHeightMapTexturePoint");
        private static readonly int s_DynamicHeightMapTextureSizeShaderID = Shader.PropertyToID("_ESSDynamicHeightMapTextureSize");
        private static readonly int s_EssDynamicHeightMapHaxHeight = Shader.PropertyToID("_ESSDynamicHeightMapHaxHeight");
        private static readonly int s_EssDynamicHeightMapHeightRange = Shader.PropertyToID("_ESSDynamicHeightMapHeightRange");
        private float m_PrewarmTime;
        private bool m_NeedPrewarm;
        private bool m_ForceUpdateHeightmapOnce = false;
        public HeightMapManagerDynamicPass(HeightMapManager heightMapManager, Camera camera ,bool enablePass = true, float prewarmTime = 1f) 
            : base(RenderPassEvent.BeforeRendering, camera, enablePass)
        {
            m_LastTargetPosition = -1000 * Vector3.one;
            m_HeightMapManager = heightMapManager;
            m_HeightMapShaderTagId = new ShaderTagId("HeightMap");
            m_NeedPrewarm = true;
            m_PrewarmTime = prewarmTime;
            m_ForceUpdateHeightmapOnce = false;
        }

        public override void Configure(CustomPass pass, CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
        }

        public override void Execute(CustomPass pass, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!Application.isPlaying || EcosystemManager.instance == null || EcosystemManager.instance.GetTarget() == null)
            {
                return;
            }

            if (m_HeightMapCamera == null)
            {
                InitHeightMapCamera();
            }
            
            m_NeedPrewarm = m_PrewarmTime > 0;
            m_PrewarmTime = m_PrewarmTime > 0 ? m_PrewarmTime - Time.deltaTime : 0;
            
            Vector3 targetPos = EcosystemManager.instance.GetTarget().position;
            if (
#if UNITY_EDITOR
                !m_HeightMapManager.forceRefreshHeightMap &&
#endif
                !m_ForceUpdateHeightmapOnce &&
                (m_NeedPrewarm || new Vector2(targetPos.x - m_LastTargetPosition.x, targetPos.z - m_LastTargetPosition.z).magnitude < m_HeightMapManager.dynamicHeightMapUpdateDistance))
            {
                return;
            }

            m_ForceUpdateHeightmapOnce = false;
            m_LastTargetPosition = targetPos;
            
            CommandBuffer cmd = CommandBufferPool.Get("HeightMap");

            int heightMapTextureSize = m_HeightMapManager.heightMapMeterSize *
                                       m_HeightMapManager.heightMapPixelPreMeter;
            if (m_HeightMapTexture != null && m_HeightMapTexture.width != heightMapTextureSize)
            {
                m_HeightMapTexture.Release();
                m_HeightMapTexture = null;
            }
            if (m_HeightMapTexturePoint != null && m_HeightMapTexturePoint.width != heightMapTextureSize)
            {
                m_HeightMapTexturePoint.Release();
                m_HeightMapTexturePoint = null;
            }

            if (m_HeightMapTexturePoint == null)
            {
                m_HeightMapCamera.orthographicSize = m_HeightMapManager.heightMapMeterSize / 2.0f;

                m_HeightMapTexturePoint = new RenderTexture(heightMapTextureSize, heightMapTextureSize,
                    8, RenderTextureFormat.R16);
                m_HeightMapTexturePoint.filterMode = FilterMode.Point;
                m_HeightMapTexturePoint.Create();
                cmd.SetGlobalTexture(s_DynamicHeightMapTexturePointShaderID, m_HeightMapTexturePoint);
            }
            
            if (m_HeightMapTexture == null)
            {
                m_HeightMapCamera.orthographicSize = m_HeightMapManager.heightMapMeterSize / 2.0f;

                m_HeightMapTexture = new RenderTexture(heightMapTextureSize, heightMapTextureSize,
                    8, RenderTextureFormat.R16);
                m_HeightMapTexture.filterMode = FilterMode.Bilinear;
                m_HeightMapTexture.Create();
                cmd.SetGlobalTexture(s_DynamicHeightMapTextureShaderID, m_HeightMapTexture);
                cmd.SetGlobalInt(s_DynamicHeightMapTextureSizeShaderID, m_HeightMapManager.heightMapMeterSize);
                cmd.SetGlobalInt("_ESSDynamicHeightMapPixelPreMeter", m_HeightMapManager.heightMapPixelPreMeter);
            }

            targetPos.y = m_HeightMapManager.dynamicHeightMapMaxHeight;
            Shader.SetGlobalFloat(s_EssDynamicHeightMapHaxHeight, m_HeightMapManager.dynamicHeightMapMaxHeight);
            Shader.SetGlobalFloat(s_EssDynamicHeightMapHeightRange, m_HeightMapManager.dynamicHeightMapHeightRange);
            m_HeightMapCamera.transform.position = targetPos;

            CoreUtils.SetRenderTarget(cmd, m_HeightMapTexture);
            CoreUtils.ClearRenderTarget(cmd, ClearFlag.All, Color.clear);

            m_HeightMapCamera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
            CullingResults result = context.Cull(ref cullingParameters);

            RendererListDesc desc = new RendererListDesc(m_HeightMapShaderTagId, result, m_HeightMapCamera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.BackToFront,
                excludeObjectMotionVectors = false,
                layerMask = m_HeightMapManager.heightMapRenderLayerMask,
            };
            RendererList rendererList = RendererList.Create(desc);
            
            Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(m_HeightMapCamera.projectionMatrix, true);
            Matrix4x4 gpuView = m_HeightMapCamera.worldToCameraMatrix;

            cmd.SetGlobalMatrix("_ESSDynamicHeightMapCameraVPMatrix", gpuProj * gpuView);
            cmd.SetGlobalVector("_ESSDynamicHeightMapCameraWorldPos", new Vector2(targetPos.x, targetPos.z));

            CoreUtils.DrawRendererList(context, cmd, rendererList);
            cmd.Blit(m_HeightMapTexture,m_HeightMapTexturePoint);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            m_HeightMapManager.currentHeightMapCenter = new Vector2(targetPos.x, targetPos.z);
            m_HeightMapManager.currentHeightMapData = new Vector4(m_HeightMapManager.heightMapMeterSize,
                m_HeightMapManager.heightMapPixelPreMeter
                , m_HeightMapManager.dynamicHeightMapMaxHeight, 
                m_HeightMapManager.dynamicHeightMapHeightRange);
            m_HeightMapManager.heightMapInitialized = true;
        }
        
        private void InitHeightMapCamera()
        {
            GameObject go = new GameObject("Ecosystem-HeightMapSimulate");
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(new Vector3(90, 0, 0)));
            Object.DontDestroyOnLoad(go);

            m_HeightMapCamera = go.AddComponent<Camera>();
            m_HeightMapCamera.backgroundColor = Color.clear;
            m_HeightMapCamera.enabled = false;
            m_HeightMapCamera.aspect = 1;
            m_HeightMapCamera.nearClipPlane = -1024;
            m_HeightMapCamera.farClipPlane = 1024;
            m_HeightMapCamera.orthographic = true;
        }

        public override void Release()
        {
            if (m_HeightMapTexture != null)
            {
                m_HeightMapTexture.Release();
                m_HeightMapTexture = null;
            }
            
            if (m_HeightMapCamera != null)
            {
                Object.Destroy(m_HeightMapCamera.gameObject);
            }

            m_HeightMapCamera = null;
        }

        public RenderTexture GetDynamicHeightMapTexture()
        {
            return m_HeightMapTexture;
        }
    }
}