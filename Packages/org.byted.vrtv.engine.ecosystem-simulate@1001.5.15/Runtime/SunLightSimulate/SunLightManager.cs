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
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

namespace Matrix.EcosystemSimulate
{
    /// <summary>
    /// 体积光测试参考：
    /// 测试场景为没烘焙的实时光场景
    /// 黑鲨3，降采样=6，模糊=1.4，步进次数=30，射线距离=30，未开启体积光时40-60帧，开启体积光时降帧比较明显，降帧数在4-10帧之间
    /// 小米10，降采样=2，模糊=1.4，步进次数=30，射线距离=30，未开启体积光时60帧，开启体积光时继续保持60帧
    /// 本身渐进式实时体积光具有一定的性能消耗，项目需看情况酌情使用，如顶配手机才支持启用。
    /// </summary>
    [CreateAssetMenu(fileName = "SunLightSimulate", menuName = "EcosystemSimulate/SunLightSimulate")]
    public partial class SunLightManager : SubEcosystem
    {
        private UserDefinedPass m_Pass;
        private bool m_EnableSunLight;

        [HideInInspector]
        public Camera m_MainCamera;

        #region 太阳散射
        public enum ShaftsScreenBlendMode
        {
            Screen = 0,
            Add = 1,
        }
        [Header("指定shader（不指定时为默认shader）")]
        public Shader m_SunShaftsShader;

        [SerializeField]
        [Header("当前场景太阳光名称（注意保持唯一，不设置时，默认为RenderSettings.sun）")]
        public string m_SunLightName;
        [SerializeField]
        [Header("指定体积光方向，场景中不额外增加光源")]
        public Vector3 m_SunLightForward;
        [SerializeField]
        [Header("太阳光晕方向")]
        public Vector3 m_LensFlareForward;
        [HideInInspector]
        public Light m_MainLight;

        [Header("散射融合模式")]
        public ShaftsScreenBlendMode m_ScreenBlendMode = ShaftsScreenBlendMode.Screen;
        [Header("降采样（移动平台建议>=2）"), Range(1, 10), Tooltip("移动平台建议>=2，当降低分辨率时，效率更高，但会出现明显抖动，不适合于树林")]
        public int m_DownSample = 2;
        [Header("太阳光颜色阈值")]
        public Color m_SunThreshold = new Color(0.87f, 0.44f, 0.65f);
        [Header("散射颜色")]
        public Color m_ShaftsColor = Color.white;
        [Header("太阳光半径"), Range(0, 1)]
        public float m_SunRadius = 1f;
        [Header("扩散迭代次数（移动平台建议<=2）"), Range(1, 4)]
        public int m_BlurIterations = 2;
        [Header("扩散递进半径"), Range(0, 4)]
        public float m_BlurRadius = 3f;
        [Header("散射强度"), Range(0, 10)]
        [Tooltip("最终强度为 (太阳光源强度 * 散射强度)")]
        public float m_SunShaftIntensity = 1.5f;
        #endregion

        #region 体积光
        [Header("指定shader（不指定时为默认shader）")]
        public Shader m_VolumetricShader;
        [Header("降采样"), Range(1, 10), Tooltip("移动平台建议>=2")]
        public int m_VolumetricDownSample = 2;
        [Header("高斯模糊"), Range(0, 4)]
        public float m_SamplerScale = 1.0f;
        [Header("步进次数"), Range(0, 256), Tooltip("移动平台建议<=30")]
        public int m_StepNum = 30;
        [Header("射线距离"), Range(0, 100), Tooltip("移动平台建议<=30")]
        public float m_MaxRayLength = 30f;

        [Header("体积光颜色")]
        public Color m_VolumetricColor = Color.white;
        [Header("主光源范围"), Range(0f, 0.9f)]
        public float m_LightRange = 0.0f;
        [Header("主光源强度"), Range(0f, 5f)]
        public float m_LightIntensity = 2.5f;
        [Header("主光源散射因子"), Range(0f, 10f)]
        public float m_LightScatteringFactor = 2f;
        [Header("固定角度才出现体积光")]
        public bool m_EnableViewDirOptimize = true;
        #endregion

        #region 太阳光晕
        [Header("启用太阳光晕")]
        [Space(20)]
        public bool m_EnableLensFlare = true;
        [Header("太阳自身半径，远近距离（建议使用默认参数)")]
        public float m_OcclusionRadius = 1.0f;
        public float m_NearFadeStartDistance = 1.0f;
        public float m_NearFadeEndDistance = 3.0f;
        public float m_FarFadeStartDistance = 10.0f;
        public float m_FarFadeEndDistance = 100.0f;

        private const string LENS_FLARE_NAME = "LensFlare";
        private Transform m_LensFlareTransform;
        private MeshRenderer m_MeshRenderer;
        private MeshFilter m_MeshFilter;

        [System.Serializable]
        public class FlareSettings
        {
            public string Name;
            public float RayPosition;
            public Material Material;
            [ColorUsage(true, true)]
            public Color Color;
            public bool MultiplyByLightColor;
            public Vector2 Size;
            public float Rotation;
            public bool AutoRotate;

            public FlareSettings()
            {
                RayPosition = 0.0f;
                Color = Color.white;
                MultiplyByLightColor = true;
                Size = new Vector2(0.3f, 0.3f);
                Rotation = 0.0f;
                AutoRotate = false;
            }
        }

        [Header("光斑元素设置")]
        [SerializeField]
        public List<FlareSettings> m_FlareList;
        #endregion

        public override void Enable()
        {
            OnInit();
            CreateSunLight();
            CreateLensFlare();
        }

        public override void Disable()
        {
            DestroySunLight();
            DestroyLensFlare();
            m_MainLight = null;
        }

        private void OnInit()
        {
            m_MainLight = null;
            if(!string.IsNullOrEmpty(m_SunLightName))
            {
                GameObject lightObject = GameObject.Find(m_SunLightName);
                if(lightObject != null)
                {
                    m_MainLight = lightObject.GetComponent<Light>();
                }
            }
            if(m_MainLight == null && RenderSettings.sun != null)
            {
                m_MainLight = RenderSettings.sun;
            }
            m_MainCamera = Camera.main;
        }

        public void CreateSunLight()
        {
            if (m_MainCamera == null || m_MainLight == null)
            {
                return;
            }
            
            DestroySunLight();
            m_Pass = new SunShaftsLightPass(this, m_MainCamera);
            m_EnableSunLight = true;
        }

        private void DestroySunLight()
        {
            if (m_Pass != null && m_Pass.renderCamera != null)
            {
                m_Pass.Release();
                m_Pass = null;
            }
        }

        public override void Update()
        {
            base.Update();
            UpdateLensFlarePos();
        }

        public override bool SupportRunInEditor()
        {
            return true;
        }

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }

        public override void OnValidate()
        {
            if (Application.isPlaying)
            {
                OnValidateLensFlare();
            }
        }

        #region 外部接口

        public bool enableSunLight
        {
            get { return m_EnableSunLight; }
            set
            {
                m_EnableSunLight = value;
                if (m_Pass != null)
                {
                    m_Pass.enable = m_EnableSunLight;
                }
            }
        }

        #endregion
    }
}