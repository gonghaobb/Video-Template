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
using UnityEngine.Rendering;

namespace Matrix.EcosystemSimulate
{
    public partial class RainManager 
    {
        [Header("雨表面全局参数")]
        [SerializeField] private bool m_EnableRainHumidness = true;
        [Range(0, 1f)] 
        [SerializeField] private float m_RainSurfaceStrength = 1f;
        [Range(0, 1)]
        [SerializeField] private float m_RainSurfaceColorDump = 0.7f;
        [Range(0, 1)]
        [SerializeField] private float m_RainSurfaceMetallicDump = 0f;
        [SerializeField] private Vector4 m_RainSurfaceTexWorldPosSize = new Vector4(0,0, 64, 64);
        
        [Header("雨表面遮罩参数")] 
        [SerializeField] private bool m_EnableRainSurfaceMask = false;
        [Reload("Runtime/RainSimulate/Resource/Textures/TestMask2.png")]
        [SerializeField] private Texture2D m_RainSurfaceMaskTex;
        [Range(0, 1)] 
        [SerializeField] private float m_RainSurfaceNonMaskStrength = 0.1f;

        [Header("雨表面扰动参数")]
        [Range(0, 1)] 
        [SerializeField] private float m_RainHumidnessStrength = 1;
        [Reload("Runtime/RainSimulate/Resource/Textures/jiangnanRainNormalTest.png")]
        [SerializeField] private Texture2D m_RainSurfaceWaveTex;
        [SerializeField] private Vector2 m_RainSurfaceWaveTexSacle = new Vector2(50, 50);
        [SerializeField] private Vector2 m_RainSurfaceWaveSlice = new Vector2(8, 8);
        [SerializeField] private float m_RainSurfaceAnimationRate = 30;
        [Reload("Runtime/RainSimulate/Resource/Textures/haunqiuTestCubeMap.exr")]
        [SerializeField] private Cubemap m_RainSurfaceReflectionMap = null;
        [Range(0, 1)]
        [SerializeField] private float m_RainSurfaceGlobalReflectionStrength = 1;
        [SerializeField] private int m_RainSurfaceFresnelPow = 2;
        [Space]
        [SerializeField] private int m_BindTexSize = 64;
        [Reload("Runtime/Core/Shaders/CombineTexShader.shader")]
        [SerializeField] private Shader m_CombineShader;
        private RenderTexture m_CombineTex;
        private bool m_EnableTextureBind = true;
        private Material m_CombineMat;
        private bool m_CombineTexFinishedFlag = false;
        private Vector2 m_OldCenterPos = Vector2.zero;
        private bool m_IsEnableRainSurface = false;
        private Vector2 m_RainSurfaceCurrentIndex = new Vector2(0, 0);
        private float m_TimeSinceSetup = 0;
        private const string s_RainSurfaceKeyword = "_GLOBAL_RAIN_SURFACE";
        private static readonly int s_RainHumidnessStrength = Shader.PropertyToID("_RainHumidnessStrength");
        private static readonly int s_RainSurfaceWaterTex = Shader.PropertyToID("_RainSurfaceWaterMaskTex");
        private static readonly int s_RainSurfaceWaterTexPosSize = Shader.PropertyToID("_RainSurfaceWaterMaskTex_Pos_Size");
        private static readonly int s_RainSurfaceColorDump = Shader.PropertyToID("_RainSurfaceColorDump");
        private static readonly int s_RainSurfaceMetallicDump = Shader.PropertyToID("_RainSurfaceMetallicDump");
        private static readonly int s_RainSurfaceWaveTex = Shader.PropertyToID("_RainSurfaceWaveTex");
        private static readonly int s_RainSurfaceWaveTexScale = Shader.PropertyToID("_RainSurfaceWaveTex_Scale");
        private static readonly int s_RainSurfaceUVSlice = Shader.PropertyToID("_RainSurfaceUVSlice");
        private static readonly int s_RainSurfaceReflectionMap = Shader.PropertyToID("_RainSurfaceReflectionMap");
        private static readonly int s_RainSurfaceNonMaskStrength = Shader.PropertyToID("_RainSurfaceNonMaskStrength");
        private static readonly int s_RainSurfaceGlobalReflectionStrength = Shader.PropertyToID("_RainSurfaceGlobalReflectionStrength");
        private static readonly int s_RainSurfaceFresnelPow = Shader.PropertyToID("_RainSurfaceFresnelPow");
        private static readonly int s_RainSurfaceCombineTex = Shader.PropertyToID("_RainSurfaceCombineTex");
        private static readonly int s_CombineSrcTexR = Shader.PropertyToID("_CombineSrcTexR");
        private static readonly int s_CombineSrcTexG = Shader.PropertyToID("_CombineSrcTexG");
        private static readonly int s_OutputTexSize = Shader.PropertyToID("_OutputTexSize");
        private static readonly int s_TexRParams = Shader.PropertyToID("_TexRParams");
        private static readonly int s_TexGParams = Shader.PropertyToID("_TexGParams");

        private void InitCombineMat()
        {
            m_CombineTexFinishedFlag = false;
            if (m_CombineShader == null)
            {
                return;
            }

            m_EnableTextureBind = true;
            m_CombineMat = new Material(m_CombineShader);
        }
        
        private void CheckCombineTex()
        {
#if UNITY_EDITOR
            if ((m_OldCenterPos - new Vector2(m_RainSurfaceTexWorldPosSize.x, m_RainSurfaceTexWorldPosSize.y)).magnitude > 0.01f)
            {
                m_CombineTexFinishedFlag = false;
            }
#endif
            
            if (m_HeightMapManager)
            {
                CombineTexture();
            }
            else
            {
                Shader.SetGlobalTexture(s_RainSurfaceCombineTex, m_EnableRainHumidness && m_EnableRainSurfaceMask &&  m_RainSurfaceMaskTex ? m_RainSurfaceMaskTex : Texture2D.whiteTexture);
            }
        }
        
        private void CombineTexture()
        {
            if (!m_CombineTexFinishedFlag)
            {
                if (m_CombineTex == null)
                {
                    InitCombineTex();
                }

                if (m_CombineTex.width != m_BindTexSize)
                {
                    m_CombineTex.Release();
                    InitCombineTex();
                }

                CommandBuffer cmd = CommandBufferPool.Get("Combine Texture");

                m_CombineMat.SetTexture(s_CombineSrcTexR,
                    m_EnableRainHumidness && m_EnableRainSurfaceMask && m_RainSurfaceMaskTex
                        ? m_RainSurfaceMaskTex
                        : Texture2D.whiteTexture);
                m_CombineMat.SetTexture(s_CombineSrcTexG,
                    m_EnableRainHumidness && m_EnableSurfaceHeightMap && m_HeightMapManager.heightMapTexture2D
                        ? m_HeightMapManager.heightMapTexture2D
                        : Texture2D.blackTexture);
                m_CombineMat.SetFloat(s_OutputTexSize, m_BindTexSize);
                Vector4 texRParams = new Vector4(m_RainSurfaceMaskTex.width,
                    (float)m_RainSurfaceTexWorldPosSize.z / m_RainSurfaceMaskTex.width,
                    m_RainSurfaceTexWorldPosSize.x, m_RainSurfaceTexWorldPosSize.y);
                m_CombineMat.SetVector(s_TexRParams, texRParams);
                if (m_HeightMapManager.heightMapTexture2D)
                {
                    m_CombineMat.SetVector(s_TexGParams,
                        new Vector4(m_HeightMapManager.heightMapTexture2D.width,
                            (float)1 / m_HeightMapManager.heightMapPixelPreMeter,
                            m_HeightMapManager.currentHeightMapCenter.x, m_HeightMapManager.currentHeightMapCenter.y));
                }
                else
                {
                    m_CombineMat.SetVector(s_TexGParams, texRParams);
                }

                cmd.SetRenderTarget(m_CombineTex);
                cmd.Blit(null, m_CombineTex, m_CombineMat);

                Graphics.ExecuteCommandBuffer(cmd);
                Shader.SetGlobalTexture(s_RainSurfaceCombineTex, m_CombineTex);

                m_CombineTexFinishedFlag = true;
            }
        }
        
        private void InitCombineTex()
        {
            m_CombineTex = new RenderTexture(m_BindTexSize, m_BindTexSize, 0, RenderTextureFormat.ARGB32, 0);
            m_CombineTex.filterMode = FilterMode.Bilinear;
            m_CombineTex.enableRandomWrite = true;
            m_CombineTex.Create();
            Shader.SetGlobalTexture(s_RainSurfaceCombineTex, m_CombineTex);
        }
        
        private void StartRainSurface()
        {
            if (!m_IsEnableRainSurface)
            {
                EnableRainSurface(true);
                m_IsEnableRainSurface = true;

                SetGlobalRainSurfaceShader();
            }
        }

        private void StopRainSurface()
        {
            if (m_IsEnableRainSurface)
            {
                EnableRainSurface(false);
                m_IsEnableRainSurface = false;
                DisableRainSurface();
            }
        }

        public void EnableRainSurface(bool bEnable)
        {
            if (bEnable)
            {
                Shader.EnableKeyword(s_RainSurfaceKeyword);
            }
            else
            {
                Shader.DisableKeyword(s_RainSurfaceKeyword);
            }
        }
        
        private void UpdateRainRippleAnimation()
        {
            float currentTime = Time.realtimeSinceStartup;
            currentTime %= 3600.0f;
            if (currentTime - m_TimeSinceSetup > 2f / m_RainSurfaceAnimationRate)
            {
                    float x = m_RainSurfaceCurrentIndex.x;
                    float y = m_RainSurfaceCurrentIndex.y;
                    x++;
                    if (x >= m_RainSurfaceWaveSlice.x)
                    {
                        y ++;
                        y %= m_RainSurfaceWaveSlice.y;
                    }

                    x %= m_RainSurfaceWaveSlice.x;
                    m_RainSurfaceCurrentIndex.x = x;
                    m_RainSurfaceCurrentIndex.y = y;
                
                Shader.SetGlobalVector(s_RainSurfaceUVSlice, new Vector4(m_RainSurfaceCurrentIndex.x, 7 - m_RainSurfaceCurrentIndex.y, 1 / m_RainSurfaceWaveSlice.x, 1 / m_RainSurfaceWaveSlice.y));
                m_TimeSinceSetup = currentTime;
            }
        }

        public bool IsEnableRainSurfaceSplash()
        {
            return m_RainHumidness > 0.1f;
        }

        private void SetGlobalRainSurfaceShader()
        {
            SetRainSurfaceWaterTex();
            SetRainSurfaceWaterTexPosSize();
            SetRainSurfaceWaveTex();
            SetRainSurfaceWaveTexScale();
            
            Shader.SetGlobalFloat(s_RainSurfaceColorDump, m_RainSurfaceColorDump);
            Shader.SetGlobalFloat(s_RainSurfaceMetallicDump, m_RainSurfaceMetallicDump);
            Shader.SetGlobalFloat(s_RainHumidnessStrength, m_EnableRainHumidness ? m_RainHumidnessStrength : 0);
            Shader.SetGlobalFloat(s_RainSurfaceNonMaskStrength, m_RainSurfaceNonMaskStrength);
            Shader.SetGlobalFloat(s_RainSurfaceGlobalReflectionStrength, m_RainSurfaceGlobalReflectionStrength);
            Shader.SetGlobalFloat(s_RainSurfaceFresnelPow, m_RainSurfaceFresnelPow);
            Shader.SetGlobalTexture(s_RainSurfaceReflectionMap, m_RainSurfaceReflectionMap);
        }

        public void DisableRainSurface()
        {
            Shader.SetGlobalFloat(s_RainHumidnessStrength, 0);
            Shader.SetGlobalTexture(s_RainSurfaceReflectionMap, Texture2D.blackTexture);
            Shader.SetGlobalTexture(s_RainSurfaceWaterTex, Texture2D.whiteTexture);
            Shader.SetGlobalTexture(s_RainSurfaceWaveTex, Texture2D.whiteTexture);
        }

        public void SetRainSurfaceMaskParams()
        {
            Shader.SetGlobalTexture(s_RainSurfaceWaterTex, m_EnableRainSurfaceMask && m_RainSurfaceMaskTex != null ? m_RainSurfaceMaskTex : Texture2D.whiteTexture);
            Shader.SetGlobalVector(s_RainSurfaceWaterTexPosSize, rainSurfaceTexWorldPosSize);
            m_CombineTexFinishedFlag = false;
        }
        
        public void SetRainSurfaceWaterTex()
        {
            Shader.SetGlobalTexture(s_RainSurfaceWaterTex, m_EnableRainSurfaceMask && m_RainSurfaceMaskTex != null ? m_RainSurfaceMaskTex : Texture2D.whiteTexture);
            m_CombineTexFinishedFlag = false;
        }

        public void SetRainSurfaceWaterTexPosSize()
        {
            Shader.SetGlobalVector(s_RainSurfaceWaterTexPosSize, rainSurfaceTexWorldPosSize);
        }

        public void SetRainSurfaceWaveTex()
        {
            Shader.SetGlobalTexture(s_RainSurfaceWaveTex, m_RainSurfaceWaveTex);
        }

        public void SetRainSurfaceWaveTexScale()
        {
            Shader.SetGlobalVector(s_RainSurfaceWaveTexScale, m_RainSurfaceWaveTexSacle);
        }

#if UNITY_EDITOR
        private void RainSurfaceOnValidate()
        {
            SetGlobalRainSurfaceShader();
            m_CombineTexFinishedFlag = false;
        }
#endif
        
        public Texture2D rainSurfaceMaskTex
        {
            get { return m_RainSurfaceMaskTex; }
            set { m_RainSurfaceMaskTex = value; }
        }

        public Vector4 rainSurfaceTexWorldPosSize
        {
            get { return m_RainSurfaceTexWorldPosSize; }
            set { m_RainSurfaceTexWorldPosSize = value; }
        }

        public Texture2D rainSurfaceWaveTex
        {
            get { return m_RainSurfaceWaveTex; }
            set { m_RainSurfaceWaveTex = value; }
        }

        public Vector2 rainSurfaceWaveTexScale
        {
            get { return m_RainSurfaceWaveTexSacle; }
            set { m_RainSurfaceWaveTexSacle = value; }
        }
        public bool enableRainHumidness
        {
            get { return m_EnableRainHumidness; }
            set
            {
                if (value != m_EnableRainHumidness)
                {
                    m_CombineTexFinishedFlag = false;
                }
                m_EnableRainHumidness = value;
            }
        }
        public bool enableRainSurfaceMask
        {
            get { return m_EnableRainSurfaceMask; }
            set
            {
                if (value != m_EnableRainSurfaceMask)
                {
                    m_CombineTexFinishedFlag = false;
                }
                m_EnableRainSurfaceMask = value;
            }
        }
    }
}