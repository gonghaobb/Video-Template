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
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Matrix.EcosystemSimulate
{
    [Serializable]
    public abstract class EcosystemCoverMapRenderer
    {
        protected struct CoverMapVolumeBuffer
        {
            public Vector4 volumeParameters;
            public Vector4 volumeFadeParameters;
            public Vector4 ecosystemParameters0;
            public Vector4 ecosystemParameters1;
        }

        [SerializeField] 
        private bool m_IsDebugCoverMap = false;
        [SerializeField]
        private ComputeShader m_CoverMapComputeShader = null;
        [SerializeField] [Min(128)]
        private int m_CoverMapTextureSize = 256;
        [SerializeField] [Min(4)]
        private int m_CoverMapMetersPrePixel = 8;
        [SerializeField] [Min(1)] 
        private int m_CoverMapUpdateDistance = 32;

        private CoverMapVolumeBuffer m_DefaultCoverMapVolumeBuffer = new CoverMapVolumeBuffer();
        private readonly Dictionary<EcosystemVolume, CoverMapVolumeBuffer> m_CoverMapVolumeBufferCacheDict =
            new Dictionary<EcosystemVolume, CoverMapVolumeBuffer>();
        private readonly List<CoverMapVolumeBuffer> m_BufferList = new List<CoverMapVolumeBuffer>();
        
        private int m_CoverMapKernelIndex = -1;
        private bool m_CoverMapRenderFinish = true;
        private Vector3 m_LastCoverMapCenterPosition = Vector3.zero;
        private RenderTexture m_CoverMap = null;
        private ComputeBuffer m_CoverMapComputeBuffer = null;
        private ProfilingSampler m_ProfilingSampler;
        private string m_Name = string.Empty;
        
        public void Init(string name)
        {
            if (coverMapComputeShader == null)
            {
                return;
            }

            m_Name = name;
            
            UnInit();
            m_CoverMap = new RenderTexture(coverMapTextureSize, coverMapTextureSize, 0, 
                GraphicsFormat.R8G8B8A8_UNorm)
            {
                enableRandomWrite = true
            };
            Shader.SetGlobalTexture($"_{m_Name}CoverMap" , m_CoverMap);
            Shader.SetGlobalInt($"_{m_Name}CoverMapSize" , coverMapTextureSize);
            Shader.SetGlobalInt($"_{m_Name}CoverMapMeterPrePixel" , coverMapMetersPrePixel);
            
            m_CoverMapKernelIndex = coverMapComputeShader.FindKernel("FillCoverMapKernel");
            coverMapComputeShader.SetTexture(m_CoverMapKernelIndex , "_CoverMap" , m_CoverMap);
            
            RenderPipelineManager.beginCameraRendering += OnRenderPipelineManagerOnBeginCameraRendering;
            m_ProfilingSampler = new ProfilingSampler(m_Name);
        }

        private void OnRenderPipelineManagerOnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (!Application.isPlaying || EcosystemManager.instance == null || camera != EcosystemManager.instance.mainCamera || 
                EcosystemManager.instance.GetTarget() == null || coverMapComputeShader == null )
            {
                return;   
            }

            Vector3 targetPosition = EcosystemManager.instance.GetTarget().position;
            if (Vector3.Distance(m_LastCoverMapCenterPosition, targetPosition) > coverMapUpdateDistance)
            {
                m_CoverMapRenderFinish = false;
            }

            if (m_CoverMapRenderFinish || m_BufferList.Count == 0)
            {
                return;
            }
            
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                if (m_CoverMapComputeBuffer != null && m_CoverMapComputeBuffer.count != m_BufferList.Count)
                {
                    m_CoverMapComputeBuffer.Release();
                    m_CoverMapComputeBuffer = null;
                }
                m_CoverMapComputeBuffer ??= new ComputeBuffer(m_BufferList.Count, sizeof(float) * 16);
                
#if UNITY_2021_3_OR_NEWER
                cmd.SetBufferData(m_CoverMapComputeBuffer, m_BufferList);
#else
                cmd.SetComputeBufferData(m_CoverMapComputeBuffer, m_BufferList);
#endif
                cmd.SetComputeBufferParam(coverMapComputeShader, m_CoverMapKernelIndex , "_CoverMapComputeBuffer" , m_CoverMapComputeBuffer);
                cmd.SetComputeIntParam(coverMapComputeShader, "_CoverMapComputeBufferLength" , m_BufferList.Count);

                Vector3 floorPosition = targetPosition;
                floorPosition.x = Mathf.FloorToInt(floorPosition.x / coverMapMetersPrePixel) * coverMapMetersPrePixel;
                floorPosition.y = 0;
                floorPosition.z = Mathf.FloorToInt(floorPosition.z / coverMapMetersPrePixel) * coverMapMetersPrePixel;
                cmd.SetComputeVectorParam(coverMapComputeShader , $"_{m_Name}CoverMapCenterPosition" , floorPosition);
                cmd.SetGlobalVector($"_{m_Name}CoverMapCenterPosition" , floorPosition);
                cmd.DispatchCompute(coverMapComputeShader , m_CoverMapKernelIndex , m_CoverMap.width / 32 , m_CoverMap.height / 32 , 1);
            }
            
            context.ExecuteCommandBuffer(cmd);
            context.Submit();
            
            CommandBufferPool.Release(cmd);
            m_CoverMapRenderFinish = true;
            m_LastCoverMapCenterPosition = targetPosition;
        }
        
        public void UnInit()
        {
            RenderPipelineManager.beginCameraRendering -= OnRenderPipelineManagerOnBeginCameraRendering;

            if (m_CoverMapComputeBuffer != null)
            {
                m_CoverMapComputeBuffer.Release();
                m_CoverMapComputeBuffer = null;
            }
            
            if (m_CoverMap != null)
            {
                m_CoverMap.Release();
                m_CoverMap = null;
            }

            m_CoverMapRenderFinish = false;
            m_LastCoverMapCenterPosition = Vector3.zero;
        }

        protected abstract void UpdateCoverMapEcosystemParameters(EcosystemVolume volume,
            ref CoverMapVolumeBuffer buffer);

        public void UpdateCoverMap(List<EcosystemVolume> volumeList)
        {
            m_BufferList.Clear();
            m_BufferList.Add(m_DefaultCoverMapVolumeBuffer);
            foreach (EcosystemVolume volume in volumeList)
            {
                if (volume.isGlobal)    // CoverMap 不需要处理 Global Volume
                {
                    continue;
                }
                
                if (!m_CoverMapVolumeBufferCacheDict.TryGetValue(volume , out CoverMapVolumeBuffer buffer))
                {
                    buffer = new CoverMapVolumeBuffer();
                    m_CoverMapVolumeBufferCacheDict.Add(volume , buffer);
                }

                buffer.volumeParameters.x = volume.transform.position.x;
                buffer.volumeParameters.y = volume.transform.position.z;
                if (volume.colliderType == EcosystemVolume.ColliderType.Sphere)
                {
                    buffer.volumeParameters.z = volume.sphereCollider.radius;
                    buffer.volumeParameters.w = 0;
                    buffer.volumeFadeParameters.x = volume.sphereInvFadeRadius;
                    buffer.volumeFadeParameters.z = volume.weight;
                }

                if (volume.colliderType == EcosystemVolume.ColliderType.Box)
                {
                    Vector3 size = volume.boxCollider.size;
                    buffer.volumeParameters.z = size.x;
                    buffer.volumeParameters.w = size.z;
                    buffer.volumeFadeParameters.x = volume.boxInvFadeSize.x;
                    buffer.volumeFadeParameters.y = volume.boxInvFadeSize.z;
                    buffer.volumeFadeParameters.z = volume.weight;
                }
                UpdateCoverMapEcosystemParameters(volume , ref buffer);
                
                m_BufferList.Add(buffer);
            }

            if (m_BufferList.Count > 0)
            {
                m_CoverMapRenderFinish = false;
            }
        }
        
        public RenderTexture GetCoverMap()
        {
            return m_CoverMap;
        }

        public void OnGUI()
        {
            if (m_IsDebugCoverMap)
            {
                if (m_CoverMap != null)
                {
                    Rect rect = new Rect(Screen.width - 512, 0, 512, 512);
                    GUI.DrawTexture(rect, m_CoverMap);
                }
            }
        }
        
        public ComputeShader coverMapComputeShader
        {
            get { return m_CoverMapComputeShader; }
            set { m_CoverMapComputeShader = value; }
        }

        public int coverMapTextureSize
        {
            get { return m_CoverMapTextureSize; }
            set { m_CoverMapTextureSize = value; }
        }

        public int coverMapMetersPrePixel
        {
            get { return m_CoverMapMetersPrePixel; }
            set { m_CoverMapMetersPrePixel = value; }
        }

        public int coverMapUpdateDistance
        {
            get { return m_CoverMapUpdateDistance; }
            set { m_CoverMapUpdateDistance = value; }
        }
    }
}


