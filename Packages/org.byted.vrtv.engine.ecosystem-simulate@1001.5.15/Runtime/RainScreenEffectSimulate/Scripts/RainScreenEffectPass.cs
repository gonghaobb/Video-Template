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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using Random = UnityEngine.Random;

namespace Matrix.EcosystemSimulate
{
    public class RainScreenEffectPass : UserDefinedPass
    {
        private const int RAIN_DROP_MATRIX_SIZE = 50;
        private const string PROFILER_TAG = "RainScreenEffectPass";
        private const float CAMERA_SIZE = 5;
        private const float INTENSITY_TRANSITION_TIME = 3;

        private RainScreenEffectManager.DynamicParam m_DynamicParam = new RainScreenEffectManager.DynamicParam();
        private RainScreenEffectManager.StaticParam m_StaticParam = new RainScreenEffectManager.StaticParam();
        private RainScreenEffectManager.CommonParam m_CommonParam;
        private RainScreenEffectManager m_Manager;

        private readonly int m_DownSample = 2;
        private Material m_Material = null;
        private Material m_StaticMaterial = null;
        private Mesh m_Mesh = null;

        private int m_ScreenWidth = int.MaxValue;
        private int m_ScreenHeight = int.MaxValue;
        private RenderTargetHandle m_SourceTempRT;
        // private RenderTargetHandle m_TempRT;
        private RenderTexture m_TrailMap1;
        private RenderTexture m_TrailMap2;
        private RenderTexture m_CurTrailMap;
        private Matrix4x4 m_ViewMatrix;
        private Matrix4x4 m_ProjectionMatrix;
        private Matrix4x4 m_WorldToScreenMatrix;
        private Matrix4x4 m_ScreenToWorldMatrix;

        private MaterialPropertyBlock m_MaterialPropertyBlock;
        private Matrix4x4[] m_RainDropMatrixs = null;
        private List<RainScreenEffectController> m_ControllerList = null;
        private float m_Interval = 0;
        private float m_IntervalTimer = 0;

        private List<float> m_DistortionList = null;

        private List<float> m_BlurList = null;
        
        private List<float> m_StaticDistortionList = null;
        private List<float> m_StaticBlurList = null;
        private Matrix4x4[] m_StaticRainMatrixs = null;

        private bool m_IsInDoor = true;
        private float m_CurIntensity = 0;

        private bool m_EnablePass = false;
        private float m_OldRainStrength = 0;
        
        private static readonly int s_RainScreenEffectNormalMap = Shader.PropertyToID("_RainScreenEffectNormalMap");
        private static readonly int s_RainScreenEffectReliefMap = Shader.PropertyToID("_RainScreenEffectReliefMap");
        private static readonly int s_RainScreenEffectOverlayColor = Shader.PropertyToID("_RainScreenEffectOverlayColor");
        private static readonly int s_RainScreenEffectDistortions = Shader.PropertyToID("_RainScreenEffectDistortions");
        private static readonly int s_RainScreenEffectBlurs = Shader.PropertyToID("_RainScreenEffectBlurs");
        private static readonly int s_RainScreenEffectAlphas = Shader.PropertyToID("_RainScreenEffectAlphas");
        private static readonly int s_RainScreenEffectCameraColorTexture = Shader.PropertyToID("_RainScreenEffectCameraColorTexture");
        private static readonly int s_RainScreenEffectTrailLifeTime = Shader.PropertyToID("_RainScreenEffectTrailLifeTime");
        private static readonly int s_RainScreenEffectTrailMap = Shader.PropertyToID("_RainScreenEffectTrailMap");
        private static readonly int s_RainScreenEffectDistortion = Shader.PropertyToID("_RainScreenEffectDistortion");
        private static readonly int s_RainScreenEffectBlur = Shader.PropertyToID("_RainScreenEffectBlur");
        private static readonly int s_RainScreenEffectStrength = Shader.PropertyToID("_RainScreenEffectStrength");

        public bool enablePass
        {
            get => m_EnablePass;
            set
            {
                if (m_EnablePass != value)
                {
                    m_EnablePass = value;
                    if (m_EnablePass)
                    {
                        m_TrailMap1 = RenderTexture.GetTemporary(new RenderTextureDescriptor(Screen.width / m_DownSample, Screen.height / m_DownSample,
                            RenderTextureFormat.Default, 0));
            
                        m_TrailMap2 = RenderTexture.GetTemporary(new RenderTextureDescriptor(Screen.width / m_DownSample, Screen.height / m_DownSample,
                            RenderTextureFormat.Default, 0));

                        Graphics.SetRenderTarget(m_TrailMap1);
                        GL.Clear(true, true, Color.clear);
                        
                        Graphics.SetRenderTarget(m_TrailMap2);
                        GL.Clear(true, true, Color.clear);
                        
                        FetchTrailTexture();
                    }
                    else
                    {
                        m_ControllerList.Clear();
                        m_DistortionList.Clear();
                        m_BlurList.Clear();
                        RenderTexture.ReleaseTemporary(m_TrailMap1);
                        RenderTexture.ReleaseTemporary(m_TrailMap2);
                    }
                }
                m_EnablePass = value;
            }
        }

        public bool isActive
        {
            get
            {
                return m_EnablePass && (m_CurIntensity > 0 || !m_IsInDoor);
            }
            set
            {
                m_IsInDoor = !value;
            }
        }

        public RainScreenEffectPass(Camera camera, RainScreenEffectManager manager, bool enablePass = true)
        : base(RenderPassEvent.BeforeRenderingPostProcessing, camera, enablePass)
        {
            renderCamera = camera;
            m_Manager = manager;
            m_CommonParam = m_Manager.settings.commonParam;
            if (m_CommonParam.rainDropMat == null)
            {
                Debugger.LogError("RainScreenEffectSimulate : 屏幕雨滴 mat 丢失，请检查！");
                return;
            }

            if (m_CommonParam.rainDropMesh == null)
            {
                Debugger.LogError("RainScreenEffectSimulate : 屏幕雨滴 Mesh 丢失，请检查！");
                return;
            }

            m_DownSample = m_CommonParam.downSample;
            m_Material = new Material(m_CommonParam.rainDropMat);
            m_Material.SetTexture(s_RainScreenEffectNormalMap, m_CommonParam.rainDropNormalMap);
            m_Material.SetTexture(s_RainScreenEffectReliefMap, m_CommonParam.rainDropReliefMap);
            m_Material.SetColor(s_RainScreenEffectOverlayColor, m_CommonParam.overlayColor);
            m_Material.enableInstancing = true;
            
            m_StaticMaterial = new Material(m_CommonParam.rainDropMat);
            m_StaticMaterial.SetTexture(s_RainScreenEffectNormalMap, m_CommonParam.staticRainDropNormalMap);
            m_StaticMaterial.SetTexture(s_RainScreenEffectReliefMap, m_CommonParam.staticRainDropReliefMap);
            m_StaticMaterial.SetColor(s_RainScreenEffectOverlayColor, m_CommonParam.staticOverlayColor);
            m_StaticMaterial.enableInstancing = true;

            m_Mesh = m_CommonParam.rainDropMesh;

            m_RainDropMatrixs = new Matrix4x4[RAIN_DROP_MATRIX_SIZE];
            m_ControllerList = new List<RainScreenEffectController>(RAIN_DROP_MATRIX_SIZE);
            m_DistortionList = new List<float>(RAIN_DROP_MATRIX_SIZE);
            m_BlurList = new List<float>(RAIN_DROP_MATRIX_SIZE);
            m_MaterialPropertyBlock = new MaterialPropertyBlock();
            
            m_StaticRainMatrixs = new Matrix4x4[1];
            m_StaticDistortionList = new List<float>(1){0};
            m_StaticBlurList = new List<float>(1){0};
            
            // m_TempRT.Init("_TempRT");
        }

        public void SetParam(RainScreenEffectManager.RainScreenEffectSettings screenEffectParam)
        {
            isActive = screenEffectParam.enableRainScreenEffect;
            m_DynamicParam.CopyFrom(screenEffectParam.dynamicParam, 1);
            m_DynamicParam.RegistOnValidate(ResetParam);
            m_StaticParam.CopyFrom(screenEffectParam.staticParam, 1);
            m_StaticParam.RegistOnValidate(ResetStaticParam);
            m_CommonParam = screenEffectParam.commonParam;
        }

        public void ResetParam()
        {
            if (m_DynamicParam.maxRainSpawnCount > RAIN_DROP_MATRIX_SIZE)
            {
                m_DynamicParam.maxRainSpawnCount = RAIN_DROP_MATRIX_SIZE;
            }
            if (m_RainDropMatrixs.Length != m_DynamicParam.maxRainSpawnCount)
            {
                m_RainDropMatrixs = new Matrix4x4[m_DynamicParam.maxRainSpawnCount];
                if (m_ControllerList.Count > m_DynamicParam.maxRainSpawnCount)
                {

                    m_ControllerList.RemoveRange(0, m_ControllerList.Count - m_DynamicParam.maxRainSpawnCount);
                    m_DistortionList.RemoveRange(0, m_DistortionList.Count - m_DynamicParam.maxRainSpawnCount);
                    m_BlurList.RemoveRange(0, m_BlurList.Count - m_DynamicParam.maxRainSpawnCount);
                }
                m_ControllerList.Capacity = m_DynamicParam.maxRainSpawnCount;
                m_DistortionList.Capacity = m_DynamicParam.maxRainSpawnCount;
                m_BlurList.Capacity = m_DynamicParam.maxRainSpawnCount;
            }
        }

        public void ResetStaticParam()
        {
        }

        public void OnGUI()
        {
            if (!m_EnablePass || !Application.isPlaying)
            {
                return;
            }
        }
        
        public override int RequireFlag()
        {
            if (renderCamera != null && renderCamera.cullingMask == 0 || !isActive)
            {
                return 0;
            }

            return ForwardRendererExtend.REQUIRE_TRANSPARENT_TEXTURE | ForwardRendererExtend.REQUIRE_CREATE_COLOR_TEXTURE;
        }
        
        public override void Configure(CustomPass pass, CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // RenderTextureDescriptor opaqueDesc = cameraTextureDescriptor;
            // cmd.GetTemporaryRT(m_SourceTempRT.id, opaqueDesc, FilterMode.Bilinear);
            // // if (!(ForwardRendererExtend.ExcuteFrameBufferFetch()))
            // {
            //     m_SourceTempRT.id = ForwardRendererExtend.GetFullScreenTempRT(cmd);
            // }

            // opaqueDesc.colorFormat = RenderTextureFormat.BGRA32;
            // opaqueDesc.width = Screen.width / m_DownSample;
            // opaqueDesc.height = Screen.height / m_DownSample;
            // opaqueDesc.depthBufferBits = 0;   // opengl必须设置,要不然读取不到值
            
            // cmd.GetTemporaryRT(m_TempRT.id, opaqueDesc);
        }

        public override void Execute(CustomPass pass, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!isActive || !Application.isPlaying)
            {
                return;
            }

            UpdateParams();

            if (m_EnablePass)
            {
                UpdateRainDrop();
            }
            m_StaticMaterial.SetColor(s_RainScreenEffectOverlayColor, m_CommonParam.staticOverlayColor);
            m_Material.SetColor(s_RainScreenEffectOverlayColor, m_CommonParam.overlayColor);
            
            CommandBuffer cmd = CommandBufferPool.Get(PROFILER_TAG);
            m_SourceTempRT = ForwardRendererExtend.GetActiveColorRenderTargetHandle();
            
            cmd.SetViewProjectionMatrices(m_ViewMatrix, m_ProjectionMatrix);
            
            // CoreUtils.SetRenderTarget(cmd, m_TempRT.id);
            // cmd.DrawProcedural(Matrix4x4.identity, m_Material, 4, MeshTopology.Quads, 4, 1);
            
            if (m_DynamicParam.enableDynamicRainDrop && m_ControllerList.Count > 0 && !m_IsInDoor && m_CurIntensity > 0)
            {
                m_DistortionList.Clear();
                m_BlurList.Clear();
                int count = 0;
                for (; count < m_ControllerList.Count && count < m_DynamicParam.maxRainSpawnCount; ++count)
                {
                    Vector3 pos = m_ScreenToWorldMatrix.MultiplyPoint3x4(m_ControllerList[count].currentScreenPos);
                    pos.z = 1;
                    m_RainDropMatrixs[count] = Matrix4x4.TRS(pos, Quaternion.Euler(m_ControllerList[count].currentRotation), m_ControllerList[count].currentScale);
            
                    m_DistortionList.Add(m_ControllerList[count].currentDistortion * m_CurIntensity);
                    m_BlurList.Add(m_ControllerList[count].currentBlur * m_CurIntensity);
                }
            
                m_MaterialPropertyBlock.Clear();

                if (m_DistortionList.Count > 0)
                {
                    m_MaterialPropertyBlock.SetFloatArray(s_RainScreenEffectDistortions, m_DistortionList);
                    m_MaterialPropertyBlock.SetFloatArray(s_RainScreenEffectBlurs, m_BlurList);
                }
                cmd.SetGlobalFloat(s_RainScreenEffectStrength, m_OldRainStrength);
                // cmd.SetGlobalTexture(s_RainScreenEffectCameraColorTexture, m_TempRT.id);
                
                //弃用代码段落
                // CoreUtils.SetRenderTarget(cmd, m_SourceTempRT.id);
                // cmd.DrawMeshInstanced(m_Mesh, 0, m_Material, 0, m_RainDropMatrixs, count, m_MaterialPropertyBlock);

                ForwardRendererExtend.SetSinglePassActive(renderingData.cameraData, cmd, false);
                
                CoreUtils.SetRenderTarget(cmd, m_CurTrailMap);
                cmd.DrawMeshInstanced(m_Mesh, 0, m_Material, 2, m_RainDropMatrixs, count);
            
                ForwardRendererExtend.SetSinglePassActive(renderingData.cameraData, cmd, true);
            }
            
            {
                cmd.SetGlobalFloat(s_RainScreenEffectTrailLifeTime, m_DynamicParam.trailLifeTime);
                cmd.SetGlobalTexture(s_RainScreenEffectTrailMap, m_CurTrailMap);
                
                FetchTrailTexture();

                CoreUtils.SetRenderTarget(cmd, m_CurTrailMap);
                cmd.DrawProcedural(Matrix4x4.identity, m_Material, 3, MeshTopology.Quads, 4, 1);
            }

            if(m_StaticParam.enableStaticRainDrop && m_CurIntensity > 0)
            {
                m_StaticDistortionList[0] = m_StaticParam.distortionValue * m_CurIntensity;
                m_StaticBlurList[0] = m_StaticParam.blurValue * m_CurIntensity;
                
                cmd.SetGlobalFloat(s_RainScreenEffectDistortion, m_StaticDistortionList[0]);
                cmd.SetGlobalFloat(s_RainScreenEffectBlur, m_StaticBlurList[0]);
                cmd.SetGlobalFloat(s_RainScreenEffectStrength, m_OldRainStrength);
                cmd.SetGlobalTexture(s_RainScreenEffectTrailMap, m_CurTrailMap);
                // cmd.SetGlobalTexture(s_RainScreenEffectCameraColorTexture, m_TempRT.id);

                CoreUtils.SetRenderTarget(cmd, m_SourceTempRT.id);
                cmd.DrawProcedural(Matrix4x4.identity, m_StaticMaterial, 1, MeshTopology.Quads, 4, 1);
            }
                
            cmd.SetViewProjectionMatrices(renderCamera.worldToCameraMatrix, renderCamera.projectionMatrix);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void FetchTrailTexture()
        {
            m_CurTrailMap = m_CurTrailMap == m_TrailMap1 ? m_TrailMap2 : m_TrailMap1;
        }

        public override void FrameCleanup(CustomPass pass, CommandBuffer cmd)
        {
            base.FrameCleanup(pass, cmd);
            // cmd.ReleaseTemporaryRT(m_TempRT.id);
        }

        public override void Release()
        {
            base.Release();
            if (m_Material != null)
            {
                m_Material = null;
            }
            if (m_StaticMaterial != null)
            {
                m_StaticMaterial = null;
            }
        }
        
        public Matrix4x4 CreateOrthoMatrix(float size)
        {
            return CreateOrthoMatrix(size, 0.3f, 1000f);
        }

        public Matrix4x4 CreateOrthoMatrix(float size, float nearClip, float farClip)
        {
            float aspect = Screen.width * 1.0f / Screen.height;
            float h = size;
            float w = size * aspect;
            return Matrix4x4.Ortho(-w, w, -h, h, nearClip, farClip);
        }

        private void UpdateParams()
        {
            if (!m_ScreenWidth.Equals(Screen.width) || !m_ScreenHeight.Equals(Screen.height))
            {
                enablePass = false;
                enablePass = true;
                m_ScreenWidth = Screen.width;
                m_ScreenHeight = Screen.height;
                m_ViewMatrix = Matrix4x4.identity;
                m_ViewMatrix.m22 = -1;
                m_ProjectionMatrix = CreateOrthoMatrix(CAMERA_SIZE);
                // m_ProjectionMatrix = Matrix4x4.identity;
                m_WorldToScreenMatrix = m_ViewMatrix * m_ProjectionMatrix;
                m_ScreenToWorldMatrix = m_WorldToScreenMatrix.inverse;
                
                float aspect = Screen.width * 1.0f / Screen.height;
                m_StaticRainMatrixs[0] = Matrix4x4.TRS(Vector3.forward, Quaternion.Euler(Vector3.zero), new Vector3(CAMERA_SIZE * aspect * 2, CAMERA_SIZE * 2, 1));
            }
            
            if (Mathf.Abs(m_OldRainStrength - m_Manager.effectStrength) > 0.01f)
            {
                m_OldRainStrength = m_Manager.effectStrength;
                m_DynamicParam.CopyFrom(m_Manager.settings.dynamicParam, m_Manager.effectStrength);
                m_StaticParam.CopyFrom(m_Manager.settings.staticParam, m_Manager.effectStrength);
            }

            m_CurIntensity = Mathf.Clamp(m_CurIntensity + (m_IsInDoor ? -1 : 1) * Time.deltaTime / INTENSITY_TRANSITION_TIME, 0f, 1f);
        }

        private void MaskAction(bool b)
        {
            m_IsInDoor = b;
        }

        private void UpdateRainDrop()
        {
            if (m_ControllerList.Count < m_DynamicParam.maxRainSpawnCount)
            {
                if (m_Interval <= 0)
                {
                    m_Interval = 1f / Random.Range(m_DynamicParam.emissionRateMin, m_DynamicParam.emissionRateMax);
                }
                m_IntervalTimer += Time.deltaTime;
                if (m_IntervalTimer >= m_Interval)
                {
                    int num = (int)Mathf.Min((m_IntervalTimer / m_Interval), m_DynamicParam.maxRainSpawnCount - m_ControllerList.Count);
                    for (int i = 0; i < num; i++)
                    {
                        m_ControllerList.Add(new RainScreenEffectController(m_DynamicParam));
                    }
                    m_IntervalTimer = 0;
                    m_Interval = 0;
                }
            }

            for (int i = 0; i < m_ControllerList.Count; ++i)
            {
                m_ControllerList[i].Update();
            }
        }
    }
}
