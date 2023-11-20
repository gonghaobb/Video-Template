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
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Matrix.EcosystemSimulate
{
    [CreateAssetMenu(fileName = "RainSimulate", menuName = "EcosystemSimulate/RainSimulate")]
    public partial class RainManager : SubEcosystem
    {
        #region SerializeField 属性
        [Header("雨粒子Prefab")]
        [Reload("Runtime/RainSimulate/Resource/Prefabs/RainParticleSystem.prefab")]
        [SerializeField] private GameObject m_RainPrefab = null;
        [Header("是否绑定风模拟组件")]
        [SerializeField] private bool m_BindWindSimulate = true;
        [SerializeField] private Vector3 m_GlobalWind = new Vector3(2, 0, 2);
        [SerializeField] private float m_WindIntensity = 2;
        [Header("绑定目标")]
        [SerializeField] private bool m_EnableBindMainCamera = false;
        [SerializeField] private bool m_EnableBindTarget = false;
        [SerializeField] private Vector3 m_TargetPosition = Vector3.zero;
        [Header("天气效果全局控制")]
        [Range(0, 1)]
        [SerializeField] private float m_RainStrength = 0.8f;
        [Range(0, 1)]
        [SerializeField] private float m_WeatherBrightness = 0.7f;
        [Header("高度图参数")]
        [SerializeField] private bool m_EnableParticleHeightMap = false;
        [SerializeField] private bool m_EnableSurfaceHeightMap = false;
        #endregion
        
        #region Private 属性
        private GameObject m_RainGameObject = null;
        private float m_RainHumidnessDelay = 0.0f;
        private float m_RainHumidnessRate = 1f;
        private float m_RainHumidness = 0;
        private float m_CurrentRainHumidnessDelay = 0;

        private Action m_SetRainStrengthFinishCallback = null;
        private float m_TargetStrength = 0;
        private float m_TransitionRate = 0;
        private bool m_InSetRainStrengthProcess = false;
        private bool m_IsWaitRainHumidnessProcess = false;

        private static readonly int s_GlobalSceneHumidnessShaderIndex = Shader.PropertyToID("_GlobalRainSurface");
        private static readonly int s_GlobalSceneWeatherBrightnessShaderIndex = Shader.PropertyToID("_EcosystemSimulateWeatherBrightness");
        private static readonly int s_EnableRainParticleHeightMapShaderID = Shader.PropertyToID("_EnableRainParticleHeightMap");
        private static readonly int s_EnableRainSurfaceHeightMapShaderID = Shader.PropertyToID("_EnableRainSurfaceHeightMap");

        private static readonly float s_RainParticleSystemLowYSpeed = -80f;
        private static readonly float s_RainParticleVolOffset = 10.0f;
        
        private Vector3 m_OldGlobalWind = Vector3.zero;

        private WindManager m_WindManager = null;
        private bool m_EnableHeightMap = false;
        private bool m_OldEnableHeightMap = false;
        private HeightMapManager m_HeightMapManager = null;
        
        private Transform m_TargetTransform = null;
        private Camera m_MainCamera = null;
        #endregion
        
        #region 默认事件

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }
        
        public override void Enable()
        {
            InitRainPrefab();
            
            InitParams();
            
            CheckIfBindCamera();
            m_TargetPosition = m_EnableBindTarget ? m_TargetTransform.position : m_TargetPosition;
            
            if (m_RainGameObject != null)
            {
                m_RainGameObject.SetActive(true);
                InitRainGameObject();
            }
        }

        public override void Disable()
        {
            m_RainHumidness = 0;
            Shader.DisableKeyword(s_RainSurfaceKeyword);
            StopRainSurface();
            Shader.SetGlobalFloat(s_GlobalSceneHumidnessShaderIndex, m_RainHumidness);

            if (m_HeightMapManager != null)
            {
                if (m_EnableHeightMap)
                {
                    m_HeightMapManager.StopHeightMap();
                }
            }
            
            if (m_RainGameObject != null)
            {
                SafeDestroy(m_RainGameObject);
                m_RainGameObject = null;
            }
        }

        public override void Update()
        {
            CheckHeightMap();
            CheckWindChanged();
            CheckIfBindCamera();
            CheckCombineTex();

            if (m_EnableBindTarget && Vector3.Distance(m_TargetTransform.position, m_TargetPosition) > particleMovementDistance)
            {
                m_TargetPosition = m_TargetTransform.position;
            }
            
            if (m_EnableRainHumidness)
            {
                Shader.EnableKeyword(s_RainSurfaceKeyword);
            }
            else
            {
                Shader.DisableKeyword(s_RainSurfaceKeyword);
            }

            UpdateRainEffect();
            UpdateRainRippleAnimation();
        }
        
        public override void FixedUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (m_InSetRainStrengthProcess)
            {
                if (Mathf.Abs(m_TargetStrength - m_RainStrength) < Mathf.Abs(m_TransitionRate))
                {
                    m_InSetRainStrengthProcess = false;

                    if (!m_IsWaitRainHumidnessProcess)
                    {
                        m_SetRainStrengthFinishCallback?.Invoke();
                        m_SetRainStrengthFinishCallback = null;
                    }
                }
                else
                {
                    m_RainStrength += m_TransitionRate;
                    m_RainStrength = Mathf.Clamp01(m_RainStrength);
                }
            }

            m_CurrentRainHumidnessDelay -= Time.fixedDeltaTime;
            if (m_CurrentRainHumidnessDelay > 0)
            {
                return;
            }
            m_RainHumidness += m_RainHumidnessRate * Time.fixedDeltaTime * (m_Enable && m_RainStrength > 0.05f ? 1 : -1);
            m_CurrentRainHumidnessDelay = 0;
            m_RainHumidness = Mathf.Clamp(m_RainHumidness, 0, 1);
            Shader.SetGlobalFloat(s_GlobalSceneHumidnessShaderIndex, m_EnableRainHumidness ? m_RainHumidness * m_RainSurfaceStrength * m_WeatherBrightness : 0);

            if (m_RainHumidness > 0.05f)
            {
                if (m_IsWaitRainHumidnessProcess && m_TransitionRate > 0)
                {
                    m_SetRainStrengthFinishCallback?.Invoke();
                    m_SetRainStrengthFinishCallback = null;
                    m_IsWaitRainHumidnessProcess = false;
                }

                StartRainSurface();
            }

            if (m_RainHumidness <= 0.05f || !m_EnableRainHumidness)
            {
                if (m_IsWaitRainHumidnessProcess && m_TransitionRate < 0)
                {
                    m_SetRainStrengthFinishCallback?.Invoke();
                    m_SetRainStrengthFinishCallback = null;
                    m_IsWaitRainHumidnessProcess = false;
                }

                StopRainSurface();
            }
        }

        //用于绘制Editor下Gizmos
        public override bool SupportRunInEditor()
        {
            return true;
        }
        
#if UNITY_EDITOR
        public override void OnDrawGizmos()
        {
            if (enable)
            {
                DrawRainEmitterShape();
            }
        }
#endif
        #endregion


        #region 私有方法
        private void InitRainPrefab()
        {
            if (m_RainPrefab == null || !Application.isPlaying)
            {
                return;
            }

            if (m_RainGameObject == null)
            {
                m_RainGameObject = EcosystemTagFinder.Find("RainSimulate");
                if (m_RainGameObject == null)
                {
                    if(m_RainPrefab == null)
                    {
                        Debugger.LogError("RainSimulate : Rain Prefab 丢失，请检查！");
                    }
                    m_RainGameObject = Instantiate(m_RainPrefab, EcosystemManager.instance.gameObject.transform);
                    m_RainGameObject.name = "RainSimulate";
                    var tag = m_RainGameObject.AddComponent<EcosystemTagFinder>();
                    tag.Init("RainSimulate");
                    DontDestroyOnLoad(m_RainGameObject);
                }
            }

            if (m_RainGameObject != null)
            {
                m_RainGameObject.SetActive(true);
                InitRainGameObject();
            }
        }
        private void CheckHeightMap()
        {
            if (m_HeightMapManager == null)
            {
                m_HeightMapManager = EcosystemManager.instance.Get<HeightMapManager>(EcosystemType.HeightMapManager);
            }

            if (m_HeightMapManager == null)
            {
                if (m_RainParticleMat && m_RainRippleParticleMat)
                {
                    m_RainParticleMat.SetFloat(s_EnableRainParticleHeightMapShaderID, 0);
                    m_RainRippleParticleMat.SetFloat(s_EnableRainParticleHeightMapShaderID, 0);
                }

                Shader.SetGlobalFloat(s_EnableRainSurfaceHeightMapShaderID, 0);
                return;
            }
            
            m_EnableHeightMap = m_EnableParticleHeightMap || m_EnableSurfaceHeightMap;
            if (m_OldEnableHeightMap != m_EnableHeightMap)
            {
                if (m_EnableHeightMap)
                {
                    m_HeightMapManager.StartHeightMap();
                }
                else
                {
                    m_HeightMapManager.StopHeightMap();
                }

                m_OldEnableHeightMap = m_EnableHeightMap;
            }

            if (m_RainParticleMat && m_RainRippleParticleMat)
            {
                m_RainParticleMat.SetFloat(s_EnableRainParticleHeightMapShaderID, m_EnableParticleHeightMap ? 1 : 0);
                m_RainRippleParticleMat.SetFloat(s_EnableRainParticleHeightMapShaderID, m_EnableParticleHeightMap ? 1 : 0);
            }
            Shader.SetGlobalFloat(s_EnableRainSurfaceHeightMapShaderID, m_EnableSurfaceHeightMap ? 1 : 0);
        }

        private void CheckWindChanged()
        {
            if (m_BindWindSimulate && windManager != null)
            {
                m_GlobalWind = windManager.GetGlobalWind();
            }
        }
        
        private void InitParams()
        {
            m_TimeSinceSetup = Time.realtimeSinceStartup;
            m_SetRainStrengthFinishCallback = null;
            m_TargetStrength = 0;
            m_InSetRainStrengthProcess = false;
            m_TransitionRate = 0;
            m_EnableHeightMap = false;
            m_OldEnableHeightMap = false;
            m_HeightMapManager = null;
            m_WindManager = null;
            InitCombineMat();
            Shader.SetGlobalFloat(s_GlobalSceneWeatherBrightnessShaderIndex, m_WeatherBrightness);
        }

        private void CheckIfBindCamera()
        {
            if (!m_EnableBindMainCamera && EcosystemManager.instance.GetTarget())
            {
                m_TargetTransform = EcosystemManager.instance.GetTarget();
            }
            else
            {
                if (mainCamera != null)
                {
                    m_TargetTransform = mainCamera.transform;
                }
            }
        }

#if UNITY_EDITOR
        private void DrawRainEmitterShape()
        {
            if (m_RainParticle != null)
            {
                var shape = m_RainParticle.shape;
                float radius = shape.radius;
                Vector3 position = shape.position;
                var localToWorldMatrix = m_RainParticle.transform.localToWorldMatrix;
                position = localToWorldMatrix.MultiplyPoint3x4(position);
                WindUtil.DrawCurve(position, Vector3.forward, radius, 360, Vector3.up);
            }
            else
            {
                float radius = m_ParticleBoundsRadius / 2f;
                Vector3 position = m_BindWindSimulate ? new Vector3(-m_GlobalWind.x * m_ParticleBoundsRadius * 0.03f * m_RainVelocityScale.x * m_WindIntensity,
                    -m_GlobalWind.z * m_ParticleBoundsRadius * 0.03f * m_RainVelocityScale.x * m_WindIntensity, -m_ParticleBoundsHeight) : new Vector3(0, 0, -m_ParticleBoundsHeight);
                var localToWorldMatrix = Matrix4x4.TRS(m_TargetPosition, Quaternion.Euler(90, 0, 0), Vector3.one);
                position = localToWorldMatrix.MultiplyPoint3x4(position);
                WindUtil.DrawCurve(position, Vector3.forward, radius, 360, Vector3.up);
            }
        }
#endif

        private void SafeDestroy(Object obj)
        {
            if (obj != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
#else
                Destroy(obj);
#endif
            }
        }
        #endregion


        #region Get 和 Set 方法
        
        public Vector3 globalWind
        {
            get { return m_GlobalWind; }
            set { m_GlobalWind = value; }
        }

        public bool bindWindSimulate
        {
            get { return m_BindWindSimulate; }
            set
            {
                m_BindWindSimulate = value;
                SetupRainParticleSystem();
            }
        }

        public float weatherBrightness
        {
            get { return m_WeatherBrightness; }
            set
            {
                m_WeatherBrightness = value;
                Shader.SetGlobalFloat(s_GlobalSceneWeatherBrightnessShaderIndex, m_WeatherBrightness);
            }
        }

        public GameObject rainPrefab
        {
            get { return m_RainPrefab; }
            set
            {
                m_RainPrefab = value;

                if (Application.isPlaying)
                {
                    InitRainPrefab();
                }
            }
        }

        public float rainHumidnessDelay
        {
            get { return m_RainHumidnessDelay; }
            set { m_RainHumidnessDelay = value; }
        }

        public float rainHumidnessRate
        {
            get { return m_RainHumidnessRate; }
            set { m_RainHumidnessRate = value; }
        }

        public override bool enable
        {
            get { return m_Enable; }
            set
            {
                if (m_Enable == value)
                {
                    return;
                }
                m_Enable = value;

                if (m_Enable)
                {
                    PlayRainEffect();
                    StartRainSurface();
                }
                else
                {
                    StopRainEffect();
                    StopRainSurface();
                }
            }
        }

        public float rainStrength
        {
            get { return m_RainStrength; }
            set
            {
                m_RainStrength = value;
            }
        }
        
        private WindManager windManager
        {
            get
            {
                if (m_WindManager == null)
                {
                    if (EcosystemManager.instance != null)
                    {
                        m_WindManager = EcosystemManager.instance.Get<WindManager>(EcosystemType.WindManager);
                    }
                }
                return m_WindManager;
            }
        }
        
        public string particleCount
        {
            get
            {
                return m_RainParticle.particleCount.ToString() + " " + m_RainRippleParticle.particleCount.ToString();
            }
        }
        
        public Camera mainCamera
        {
            get
            {
                if (m_MainCamera == null)
                {
                    m_MainCamera = Camera.main;
                }
        
                return m_MainCamera;
            }
        }
        #endregion


        #region 公开接口

        public void SetRainStrengthWithTime(float targetRainStrength, float transitionTime, Action callback = null, bool waitRainHumidnessProcess = true)
        {
            m_InSetRainStrengthProcess = true;
            m_TargetStrength = targetRainStrength;
            m_TransitionRate = (m_TargetStrength - m_RainStrength) / (transitionTime + 0.01f) * Time.fixedDeltaTime;
            m_SetRainStrengthFinishCallback = callback;
            m_IsWaitRainHumidnessProcess = waitRainHumidnessProcess;
        }
        #endregion


        #region Editor 

#if UNITY_EDITOR
        public override void OnValidate()
        {
            enable = enable;
            enableRainParticle = enableRainParticle;
            enableRainRippleParticle = enableRainRippleParticle;
            ParticleBoundsRadius = ParticleBoundsRadius;
            weatherBrightness = weatherBrightness;
            rainMaxCount = rainMaxCount;
            rainMaxLifeTime = rainMaxLifeTime;
            RainSurfaceOnValidate();
        }
#endif
        #endregion
    }
    
}

