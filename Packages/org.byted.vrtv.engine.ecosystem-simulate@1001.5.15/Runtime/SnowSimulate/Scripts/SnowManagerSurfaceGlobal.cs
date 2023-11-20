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
//      Procedural LOGO:https://www.shadertoy.com/view/WdyfDm 
//                  @us:https://kdocs.cn/l/sawqlPuqKX7f
//
//      The team was set up on September 4, 2019.

using UnityEngine;
using UnityEngine.Rendering;

namespace Matrix.EcosystemSimulate
{
    public partial class SnowManager : SubEcosystem
    {
        [Header("雪表面参数")]
        [SerializeField] private bool m_EnableSnowSurface = true;
        [Range(0,1f)]
        [SerializeField] private float m_SnowSurfaceStrength = 1f;
        [SerializeField] private Vector4 m_SnowTexPosSize = new Vector4(0, 0, 64, 64);
        [Header("雪表面遮罩参数")]
        [SerializeField] private bool m_EnableSnowSurfaceMask = false;
        [Reload("Runtime/RainSimulate/Resource/Textures/TestMask.png")]
        [SerializeField] private Texture2D m_SnowMaskTex;
        [SerializeField] private Color m_SnowMaskColor = new Color(0.5f, 0.5f, 0.5f, 1);
        
        [Header("雪表面贴图参数")]
        [SerializeField] private bool m_EnableSnowDetailTex = false;
        [Reload("Runtime/SnowSimulate/Resource/Textures/snow_albedo.jpg")]
        [SerializeField] private Texture2D m_SnowTex;
        [Reload("Runtime/SnowSimulate/Resource/Textures/snow_normal.jpg")]
        [SerializeField] private Texture2D m_SnowNormalTex;
        [SerializeField] private Vector2 m_SnowTexScale  = new Vector2(100, 100);
        [Range(0,1)]
        [SerializeField] private float m_SnowNormalRatio = 0.5f;
        [Range(0,1)]
        [SerializeField] private float m_SnowAORatio = 0.9f;

        private bool m_EnableTextureBind = true;
        [SerializeField] private int m_CombineTexSize = 64;
        [Reload("Runtime/Core/Shaders/CombineTexShader.shader")]
        [SerializeField] private Shader m_CombineShader;
        private RenderTexture m_CombineTex;
        private Material m_CombineMat;
        private bool m_CombineTexFinishedFlag = false;
        private Vector2 m_OldCenterPos = Vector2.zero;
        private Vector4 m_SnowNoiseTexScaleOffset = new Vector4(1f, 1f, 0f, 0f);
        private static readonly int s_EnableSnowDetailTex = Shader.PropertyToID("_EnableSnowDetailTex");
        private static readonly int s_SnowSurfaceMaskTex = Shader.PropertyToID("_SnowSurfaceMaskTex");
        private static readonly int s_SnowMaskColor = Shader.PropertyToID("_SnowMaskColor");
        private static readonly int s_SnowNoiseTexScaleOffset = Shader.PropertyToID("_SnowNoiseTex_ScaleOffset");
        private static readonly int s_SnowSurfaceTex = Shader.PropertyToID("_SnowSurfaceTex");
        private static readonly int s_SnowSurfaceNormal = Shader.PropertyToID("_SnowSurfaceNormal");
        private static readonly int s_SnowSurfaceTexScale = Shader.PropertyToID("_SnowSurfaceTex_Scale");
        private static readonly int s_SnowNormalRatio = Shader.PropertyToID("_SnowNormalRatio");
        private static readonly int s_SnowAORatio = Shader.PropertyToID("_SnowAORatio");
        private static readonly int s_SnowSurfaceTexPosSize = Shader.PropertyToID("_SnowSurfaceTex_Pos_Size");
        private static readonly int s_SnowSurfaceCombineTex = Shader.PropertyToID("_SnowSurfaceCombineTex");
        private static readonly int s_CombineSrcTexR = Shader.PropertyToID("_CombineSrcTexR");
        private static readonly int s_CombineSrcTexG = Shader.PropertyToID("_CombineSrcTexG");
        private static readonly int s_OutputTexSize = Shader.PropertyToID("_OutputTexSize");
        private static readonly int s_TexRParams = Shader.PropertyToID("_TexRParams");
        private static readonly int s_TexGParams = Shader.PropertyToID("_TexGParams");

        private void InitCombineMat()
        {
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
            if ((m_OldCenterPos - new Vector2(m_SnowTexPosSize.x, m_SnowTexPosSize.y)).magnitude > 0.01f)
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
                Shader.SetGlobalTexture(s_SnowSurfaceCombineTex, m_EnableSnowSurface && m_EnableSnowSurfaceMask && m_SnowMaskTex ? m_SnowMaskTex : Texture2D.whiteTexture);
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

                if (m_CombineTex.width != m_CombineTexSize || !m_EnableSnowSurface)
                {
                    m_CombineTex.Release();
                    InitCombineTex();
                }

                CommandBuffer cmd = CommandBufferPool.Get("Combine Texture");

                m_CombineMat.SetTexture(s_CombineSrcTexR,
                    m_EnableSnowSurface && m_EnableSnowSurfaceMask && m_SnowMaskTex != null
                        ? m_SnowMaskTex
                        : Texture2D.whiteTexture);
                m_CombineMat.SetTexture(s_CombineSrcTexG,
                    m_EnableSnowSurface && m_EnableSurfaceHeightMap && m_HeightMapManager.heightMapTexture2D != null
                        ? m_HeightMapManager.heightMapTexture2D
                        : Texture2D.blackTexture);

                m_CombineMat.SetFloat(s_OutputTexSize, m_CombineTexSize);
                Vector4 texRParams = new Vector4(m_SnowMaskTex.width, (float)m_SnowTexPosSize.z / m_SnowMaskTex.width,
                    m_SnowTexPosSize.x, m_SnowTexPosSize.y);
                m_CombineMat.SetVector(s_TexRParams, texRParams);
                if (m_HeightMapManager.heightMapTexture2D != null)
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
                Shader.SetGlobalTexture(s_SnowSurfaceCombineTex, m_CombineTex);

                m_CombineTexFinishedFlag = true;
            }
        }

        private void InitCombineTex()
        {
            float texSize = m_EnableSnowSurface ? m_CombineTexSize : 1;
            m_CombineTex = new RenderTexture(m_CombineTexSize, m_CombineTexSize, 0, RenderTextureFormat.ARGB32, 0);
            m_CombineTex.filterMode = FilterMode.Bilinear;
            m_CombineTex.enableRandomWrite = true;
            m_CombineTex.Create();
            Shader.SetGlobalTexture(s_SnowSurfaceCombineTex, m_CombineTex);
        }

        private void SetGlobalSnowSurfaceShader()
        {
            Shader.SetGlobalTexture(s_SnowSurfaceMaskTex, m_EnableSnowSurfaceMask && m_SnowMaskTex != null ? m_SnowMaskTex : Texture2D.whiteTexture);
            Shader.SetGlobalColor(s_SnowMaskColor, m_SnowMaskColor);

            Shader.SetGlobalVector(s_SnowNoiseTexScaleOffset, m_SnowNoiseTexScaleOffset);

            Shader.SetGlobalTexture(s_SnowSurfaceTex, m_EnableSnowDetailTex && m_SnowTex? m_SnowTex : Texture2D.whiteTexture);
            Shader.SetGlobalTexture(s_SnowSurfaceNormal, m_EnableSnowDetailTex && m_SnowNormalTex ? m_SnowNormalTex : Texture2D.whiteTexture);
            Shader.SetGlobalVector(s_SnowSurfaceTexScale, m_SnowTexScale);
            Shader.SetGlobalFloat(s_EnableSnowDetailTex, m_EnableSnowDetailTex? 1 : 0);
            Shader.SetGlobalFloat(s_SnowNormalRatio, m_SnowNormalRatio);
            Shader.SetGlobalFloat(s_SnowAORatio, m_SnowAORatio);

            Shader.SetGlobalVector(s_SnowSurfaceTexPosSize, m_SnowTexPosSize);

            Shader.SetGlobalFloat(s_GlobalSceneSnowStrengthShaderIndex, m_EnableSnowSurface ? m_SnowSurfaceStrength : 0);
        }

        public void SetSnowSurfaceMaskParams()
        {
            Shader.SetGlobalVector(s_SnowSurfaceTexPosSize, m_SnowTexPosSize);
            Shader.SetGlobalTexture(s_SnowSurfaceMaskTex, m_EnableSnowSurfaceMask && m_SnowMaskTex != null ? m_SnowMaskTex : Texture2D.whiteTexture);
            m_CombineTexFinishedFlag = false;
        }
#if UNITY_EDITOR
        private void SnowSurfaceGlobalOnValidate()
        {
            SetGlobalSnowSurfaceShader();
            m_CombineTexFinishedFlag = false;
        }
#endif
        
        public Texture2D snowTex
        {
            get { return m_SnowTex; }
            set { m_SnowTex = value; }
        }

        public Texture2D snowNormalTex
        {
            get { return m_SnowNormalTex; }
            set { m_SnowNormalTex = value; }
        }

        public Vector4 snowTexPosSize
        {
            get { return m_SnowTexPosSize; }
            set { m_SnowTexPosSize = value; }
        }

        public Vector2 snowTexScale
        {
            get { return m_SnowTexScale; }
            set { m_SnowTexScale = value; }
        }
        
        public Texture2D snowSurfaceMaskTex
        {
            get { return m_SnowMaskTex; }
            set { m_SnowMaskTex = value; }
        }
        
        public bool enableSnowSurfaceMask
        {
            get { return m_EnableSnowSurfaceMask; }
            set
            {
                if (value != m_EnableSnowSurfaceMask)
                {
                    m_CombineTexFinishedFlag = false;
                }
                m_EnableSnowSurfaceMask = value;
            }
        }
        
        public bool enableSnowSurface
        {
            get { return m_EnableSnowSurface; }
            set
            {
                if (value != m_EnableSnowSurface)
                {
                    m_CombineTexFinishedFlag = false;
                }
                m_EnableSnowSurface = value; 
                
            }
        }
    }
}