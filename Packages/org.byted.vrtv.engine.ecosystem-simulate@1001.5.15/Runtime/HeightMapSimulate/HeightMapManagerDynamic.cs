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

using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Matrix.EcosystemSimulate
{
    public partial class HeightMapManager
    {
        [Header("动态高度图绘制区域大小")] [Min(64)] 
        [SerializeField] private int m_HeightMapMeterSize = 64;
        [Header("动态高度图绘制贴图精度")] [Min(1)] 
        [SerializeField] private int m_HeightMapPixelPreMeter = 2;
        [Header("动态高度图刷新距离")] [Min(1)]
        [SerializeField] private int m_DynamicHeightMapUpdateDistance = 16;
        [Header("动态高度图绘制高度范围")]
        [SerializeField] private int m_DynamicHeightMapMaxHeight = 100;
        [SerializeField] private int m_DynamicHeightMapHeightRange = 100;
        [Header("动态高度图绘制 Layer")] 
        [SerializeField] private LayerMask m_HeightMapRenderLayerMask = -1;
        [SerializeField] private float m_HeightMapRenderDelayTime = 1;
        [SerializeField] private ComputeShader m_HeightMapReadBackComputeShader = null;
        [Header("是否开启Debug输出动态高度图(编辑器下)")]
        [SerializeField] private bool m_ForceRefreshHeightMap = false;
        private static Vector2 m_CurrentHeightMapCenter = Vector2.zero;
        private static Vector4 m_CurrentHeightMapData = Vector4.zero;
        private int m_HeightMapReferenceCount = 0;
        private HeightMapManagerDynamicPass m_HeightMapDynamicCustomPass;
        private bool m_HeightMapInitialized = false;
        private Texture2D m_HeightMapTexture2D = null;
        private static NativeArray<float> m_HeightNativeArray = new NativeArray<float>();
        private int m_HeightReadBackKernel = 0;
        private int m_DispacthNum = 0;
        private ComputeBuffer m_HeightMapReadBackComputeBuffer = null;
        public void StartHeightMap()
        {
            if (m_HeightMapDynamicCustomPass != null)
            {
                m_HeightMapReferenceCount++;
                m_HeightMapDynamicCustomPass.enable = m_HeightMapReferenceCount > 0;
            }
        }
        
        public void StopHeightMap()
        {
            if (m_HeightMapDynamicCustomPass != null)
            {
                m_HeightMapReferenceCount--;
                m_HeightMapReferenceCount = Mathf.Max(m_HeightMapReferenceCount, 0);
                if (Camera.main != null)
                {
                    m_HeightMapDynamicCustomPass.enable = m_HeightMapReferenceCount > 0;
                }
            }
        }

        public void DynamicHeightMapEnable()
        {
            m_HeightMapReferenceCount = 0;
            if (m_HeightMapDynamicCustomPass == null)
            {
                m_HeightMapDynamicCustomPass = new HeightMapManagerDynamicPass(this, Camera.main,false, m_HeightMapRenderDelayTime);
            }
        }

        private void InitHeightMapReadBackCS()
        {
            if (m_HeightMapReadBackComputeShader == null)
            {
                return;
            }
            m_HeightReadBackKernel = m_HeightMapReadBackComputeShader.FindKernel("HeightMapReadBack");
        }

        //目前opengl不支持异步回读，unity2021.2以后才能用
        public void GetHeightReadBack()
        {
            CommandBuffer cmd = CommandBufferPool.Get("HeightMap ReadBack");
            if (m_HeightNativeArray.IsCreated)
            {
                m_HeightNativeArray.Dispose();
            }
            int heightMapSize = Mathf.FloorToInt(m_CurrentHeightMapData.x * m_CurrentHeightMapData.y);
            m_HeightNativeArray = new NativeArray<float>(heightMapSize * heightMapSize, Allocator.Persistent);
            if (m_HeightMapReadBackComputeBuffer == null || !m_HeightMapReadBackComputeBuffer.IsValid())
            {
                m_HeightMapReadBackComputeBuffer = new ComputeBuffer(heightMapSize * heightMapSize, 4);
            }
            m_DispacthNum = heightMapSize / 16;
            cmd.SetComputeBufferParam(m_HeightMapReadBackComputeShader, m_HeightReadBackKernel, "_HeightReadBack", m_HeightMapReadBackComputeBuffer);
            cmd.SetComputeFloatParam(m_HeightMapReadBackComputeShader,"_HeightMapSize", heightMapSize);
            cmd.SetComputeTextureParam(m_HeightMapReadBackComputeShader, m_HeightReadBackKernel,
                "_DynamicHeightMap", GetDynamicHeightMap());
            cmd.DispatchCompute(m_HeightMapReadBackComputeShader, m_HeightReadBackKernel, m_DispacthNum,
                m_DispacthNum, 1);
            cmd.RequestAsyncReadback(m_HeightMapReadBackComputeBuffer, request =>
            {
                NativeArray<float> result = request.GetData<float>();
                NativeArray<float>.Copy(result, m_HeightNativeArray);
                result.Dispose();
            });
            Graphics.ExecuteCommandBuffer(cmd);
        }

        public void DynamicHeightMapDisable()
        {
            m_HeightMapReferenceCount = 0;
            if (m_HeightMapDynamicCustomPass != null)
            {
                if (Camera.main != null)
                {
                    m_HeightMapDynamicCustomPass.enable = false;
                }
                m_HeightMapDynamicCustomPass.Release();
                m_HeightMapDynamicCustomPass = null;
            }
            if (m_HeightNativeArray.IsCreated)
            {
                m_HeightNativeArray.Dispose();
            }
        }

        public void DynamicHeightMapUpdate()
        {
            if (m_HeightMapInitialized)
            {
                GetTexture2D();
                // GetHeightReadBack();
                m_HeightMapInitialized = false;
            }
        }

        private Texture2D ToTexture2D(RenderTexture rTex)
        {
            Texture2D tex = new Texture2D(rTex.width, rTex.height, TextureFormat.R16, false);
            // ReadPixels looks at the active RenderTexture.
            RenderTexture.active = rTex;
            tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
            tex.Apply();
            return tex;
        }

#if UNITY_EDITOR
        [SerializeField]
        private bool m_HeightMapTextureDebug = false;
#endif
        
        public void DynamicHeightMapOnGUI()
        {
#if UNITY_EDITOR
            RenderTexture tex = m_HeightMapDynamicCustomPass.GetDynamicHeightMapTexture();
            if (m_HeightMapTextureDebug && tex != null)
            {
                GUI.DrawTexture(new Rect(Screen.width - 512, 0, 512, 512), tex);
            }         
#endif
        }

        public static float GetHeightInCPUStatic(Vector3 position)
        {
            if (m_HeightNativeArray.Length == 0)
            {
                return - 100;
            }

            int heightmapSize = Mathf.FloorToInt(m_CurrentHeightMapData.x * m_CurrentHeightMapData.y);
            int uvx = Mathf.FloorToInt((position.x - m_CurrentHeightMapCenter.x) * m_CurrentHeightMapData.y) + heightmapSize / 2;
            int uvy = Mathf.FloorToInt((position.z - m_CurrentHeightMapCenter.y) * m_CurrentHeightMapData.y) + heightmapSize / 2;
            if (uvx < 0 || uvx >= heightmapSize || uvy < 0 || uvy >= heightmapSize)
            {
                return -100;
            }

            float heightColor = 1 - m_HeightNativeArray[uvx + heightmapSize * uvy];
            float height = m_CurrentHeightMapData.z - heightColor * m_CurrentHeightMapData.w;
            return height;
        }
        
        public float GetHeightInCPU(Vector3 position)
        {
            if (m_HeightNativeArray.Length == 0)
            {
                return - 100;
            }

            int heightmapSize = Mathf.FloorToInt(m_CurrentHeightMapData.x * m_CurrentHeightMapData.y);
            int uvx = Mathf.FloorToInt((position.x - m_CurrentHeightMapCenter.x) * m_CurrentHeightMapData.y) + heightmapSize / 2;
            int uvy = Mathf.FloorToInt((position.z - m_CurrentHeightMapCenter.y) * m_CurrentHeightMapData.y) + heightmapSize / 2;
            if (uvx < 0 || uvx >= heightmapSize || uvy < 0 || uvy >= heightmapSize)
            {
                return -100;
            }

            float heightColor = 1 - m_HeightMapTexture2D.GetPixel(uvx, uvy).r;
            float height = m_CurrentHeightMapData.z - heightColor * m_CurrentHeightMapData.w;
            return height;
        }

        public void GetTexture2D()
        {
            if (GetDynamicHeightMap() == null)
            {
                return;
            }
            m_HeightMapTexture2D = ToTexture2D(GetDynamicHeightMap());
            if (m_HeightNativeArray.IsCreated)
            {
                m_HeightNativeArray.Dispose();
            }
            int heightMapSize = Mathf.FloorToInt(m_CurrentHeightMapData.x * m_CurrentHeightMapData.y);
            m_HeightNativeArray = new NativeArray<float>(heightMapSize * heightMapSize, Allocator.Persistent);
            for(int i = 0; i < heightMapSize; i++)
            {
                for (int j = 0; j < heightMapSize; j++)
                {
                    m_HeightNativeArray[i + heightMapSize * j] = m_HeightMapTexture2D.GetPixel(i, j).r;
                }
            }
        }

        //外部接口,用于手动调用刷新高度图绘制
        public void RefreshHeightTexture2D()
        {
            m_HeightMapInitialized = true;
        }

        public RenderTexture GetDynamicHeightMap()
        {
            return m_HeightMapDynamicCustomPass.GetDynamicHeightMapTexture();
        }

        public int heightMapMeterSize
        {
            get { return m_HeightMapMeterSize; }
            set { m_HeightMapMeterSize = value; }
        }

        public int heightMapPixelPreMeter
        {
            get { return m_HeightMapPixelPreMeter; }
            set { m_HeightMapPixelPreMeter = value; }
        }

        public LayerMask heightMapRenderLayerMask
        {
            get { return m_HeightMapRenderLayerMask; }
            set { m_HeightMapRenderLayerMask = value; }
        }

        public int dynamicHeightMapUpdateDistance
        {
            get { return m_DynamicHeightMapUpdateDistance; }
            set { m_DynamicHeightMapUpdateDistance = value; }
        }
        
        public int dynamicHeightMapMaxHeight
        {
            get { return m_DynamicHeightMapMaxHeight; }
            set { m_DynamicHeightMapMaxHeight = value; }
        }
        
        public int dynamicHeightMapHeightRange
        {
            get { return m_DynamicHeightMapHeightRange; }
            set { m_DynamicHeightMapHeightRange = value; }
        }
        public Vector2 currentHeightMapCenter
        {
            get { return m_CurrentHeightMapCenter; }
            set { m_CurrentHeightMapCenter = value; }
        }
        public Vector4 currentHeightMapData
        {
            get { return m_CurrentHeightMapData; }
            set { m_CurrentHeightMapData = value; }
        }
        public bool heightMapInitialized
        {
            get { return m_HeightMapInitialized; }
            set { m_HeightMapInitialized = value; }
        }
        public bool forceRefreshHeightMap
        {
            get { return m_ForceRefreshHeightMap; }
            set { m_ForceRefreshHeightMap = value; }
        }
        public Texture2D heightMapTexture2D
        {
            get { return m_HeightMapTexture2D; }
            set { m_HeightMapTexture2D = value; }
        }
    }
}