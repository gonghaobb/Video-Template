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
    [CreateAssetMenu(fileName = "SnowSimulate", menuName = "EcosystemSimulate/SnowSimulate")]
    public partial class SnowManager : SubEcosystem
    {
        #region SerializeField 属性
        [Header("雪粒子Prefab")]
        [Reload("Runtime/SnowSimulate/Resource/Prefabs/SnowParticleSystem.prefab")]
        [SerializeField] private GameObject m_SnowPrefab = null;
        [Header("是否绑定风模拟组件")]
        [SerializeField] private bool m_BindWindSimulate = false;
        [SerializeField] private Vector3 m_GlobalWind = new Vector3(1, 0, 1);
        [Header("绑定目标")]
        [SerializeField] private bool m_EnableBindTarget = true;
        [SerializeField] private bool m_EnableBindMainCamera = false;
        [SerializeField] private Vector3 m_TargetPosition = Vector3.zero;
        [SerializeField] private float m_WindIntensity = 2f;

        [Header("高度图参数")]
        [SerializeField] private bool m_EnableParticleHeightMap = false;
        [SerializeField] private bool m_EnableSurfaceHeightMap = false;
        #endregion
        
        #region Private 属性

        private GameObject m_SnowGameObject = null;

        // Height Map
        private bool m_EnableHeightMap = false;
        private bool m_OldEnableHeightMap = false;
        private HeightMapManager m_HeightMapManager = null;
        
        private float m_SnowAmount = 0;
        private float m_CurrentSnowAmountDelay = 0;

        private Action m_SetSnowStrengthFinishCallback = null;
        private float m_TargetStrength = 0;
        private float m_TransitionRate = 0;
        private bool m_InSetSnowStrengthProcess = false;
        private bool m_IsWaitSnowAmountProcess = false;


        private Vector3 m_OldGlobalWind = Vector3.zero;
        public const string s_SnowSurfaceKeyWord = "_GLOBAL_SNOW_SURFACE";
        private static readonly int s_GlobalSceneSnowAmountShaderIndex = Shader.PropertyToID("_GlobalSnowAmount");
        private static readonly int s_GlobalSceneSnowStrengthShaderIndex = Shader.PropertyToID("_GlobalSnowStrength");
        private static readonly int s_EnableSnowParticleHeightMapShaderID = Shader.PropertyToID("_EnableSnowParticleHeightMap");
        private static readonly int s_EnableSnowSurfaceHeightMapShaderID = Shader.PropertyToID("_EnableSnowSurfaceHeightMap");

        private WindManager m_WindManager = null;
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
            InitSnowPrefab();
            
            InitParams();
            
            CheckIfBindCamera();
            m_TargetPosition = m_EnableBindTarget ? m_TargetTransform.position : m_TargetPosition;
            
            if (m_SnowGameObject != null)
            {
                m_SnowGameObject.SetActive(true);
                InitSnowGameObject();
            }
        }

        public override void Disable()
        {
            m_SnowAmount = 0;
            Shader.SetGlobalFloat(s_GlobalSceneSnowAmountShaderIndex, m_SnowAmount);
            Shader.DisableKeyword(s_SnowSurfaceKeyWord);
            
            if (m_SnowGameObject != null)
            {
                SafeDestroy(m_SnowGameObject);
                m_SnowGameObject = null;
            }
        }

        public override void Update()
        {
            if (m_EnableSnowSurface)
            {
                Shader.EnableKeyword(s_SnowSurfaceKeyWord);
            }
            else
            {
                Shader.DisableKeyword(s_SnowSurfaceKeyWord);
            }
            CheckWindChanged();
            CheckIfBindCamera();
            CheckHeightMap();
            CheckCombineTex();
 
            if (m_EnableBindTarget && Vector3.Distance(m_TargetTransform.position, m_TargetPosition) > particleMovementDistance)
            {
                m_TargetPosition = m_TargetTransform.position;
            }

            UpdateSnowEffect();
            UpdateSnowParticleHeight();
        }

        public override void FixedUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            if (m_InSetSnowStrengthProcess)
            {
                if (Mathf.Abs(m_TargetStrength - m_SnowStrength) < Mathf.Abs(m_TransitionRate))
                {
                    m_InSetSnowStrengthProcess = false;
                    if (!m_IsWaitSnowAmountProcess)
                    {
                        m_SetSnowStrengthFinishCallback?.Invoke();
                        m_SetSnowStrengthFinishCallback = null;
                    }
                }
                else
                {
                    m_SnowStrength += m_TransitionRate;
                    m_SnowStrength = Mathf.Clamp01(m_SnowStrength);
                }
            }

            m_CurrentSnowAmountDelay -= Time.fixedDeltaTime;
            if (m_CurrentSnowAmountDelay > 0)
            {
                return;
            }

            m_SnowAmount += m_SnowAmountRate * Time.fixedDeltaTime * (m_Enable && m_SnowStrength > 0.05f ? 1 : -1);
            m_CurrentSnowAmountDelay = 0;
            m_SnowAmount = Mathf.Clamp(m_SnowAmount, 0, 1);
            Shader.SetGlobalFloat(s_GlobalSceneSnowAmountShaderIndex, m_SnowAmount);
            Shader.SetGlobalFloat(s_GlobalSceneSnowStrengthShaderIndex, m_EnableSnowSurface ? m_SnowSurfaceStrength : 0);

            if (m_SnowAmount > 0.001f)
            {
                if (m_IsWaitSnowAmountProcess && m_TransitionRate > 0)
                {
                    m_SetSnowStrengthFinishCallback?.Invoke();
                    m_SetSnowStrengthFinishCallback = null;
                    m_IsWaitSnowAmountProcess = false;
                }
            }

            if (m_SnowAmount <= 0.001f)
            {
                if (m_IsWaitSnowAmountProcess && m_TransitionRate < 0)
                {
                    m_SetSnowStrengthFinishCallback?.Invoke();
                    m_SetSnowStrengthFinishCallback = null;
                    m_IsWaitSnowAmountProcess = false;
                }
            }
        }

        public override bool SupportRunInEditor()
        {
            return true;
        }

#if UNITY_EDITOR
        public override void OnDrawGizmos()
        {
            if (enable)
            {
                DrawSnowEmitterShape();
            }
        }
#endif
        #endregion

        #region 私有方法
        private void InitParams()
        {
            m_SnowAmount = 0;
            m_CurrentSnowAmountDelay = 0;
            m_SnowParticle = null;
            m_SnowAmountDelay = 0;
            m_SnowAmountRate = 1;
            m_OldEnableHeightMap = false;
            m_EnableHeightMap = false;
            m_HeightMapManager = null;
            m_SetSnowStrengthFinishCallback = null;
            m_TargetStrength = 0;
            m_InSetSnowStrengthProcess = false;
            m_TransitionRate = 0;
            m_CombineTexFinishedFlag = false;
            m_FadingSnowParticleMat = null;
            InitCombineMat();
            Shader.SetGlobalFloat(s_GlobalSceneSnowAmountShaderIndex, m_SnowAmount);
        }
        
        private void InitSnowPrefab()
        {
            if (m_SnowPrefab == null || !Application.isPlaying)
            {
                return;
            }

            if (m_SnowGameObject == null)
            {
                m_SnowGameObject = EcosystemTagFinder.Find("SnowSimulate");
                if (m_SnowGameObject == null)
                {
                    m_SnowGameObject = Instantiate(m_SnowPrefab, EcosystemManager.instance.gameObject.transform);
                    m_SnowGameObject.name = "SnowSimulate";
                    var tag = m_SnowGameObject.AddComponent<EcosystemTagFinder>();
                    tag.Init("SnowSimulate");
                    DontDestroyOnLoad(m_SnowGameObject);
                }
            }

            if (m_SnowGameObject != null)
            {
                m_SnowGameObject.SetActive(true);
                InitSnowGameObject();
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
                if (m_SnowParticleMat && m_FadingSnowParticleMat)
                {
                    m_SnowParticleMat.SetFloat(s_EnableSnowParticleHeightMapShaderID, 0);
                    m_FadingSnowParticleMat.SetFloat(s_EnableSnowParticleHeightMapShaderID, 0);
                }

                Shader.SetGlobalFloat(s_EnableSnowSurfaceHeightMapShaderID, 0);
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

            if (m_SnowParticleMat && m_FadingSnowParticleMat)
            { 
                m_SnowParticleMat.SetFloat(s_EnableSnowParticleHeightMapShaderID, m_EnableParticleHeightMap ? 1 : 0);
                m_FadingSnowParticleMat.SetFloat(s_EnableSnowParticleHeightMapShaderID, m_EnableParticleHeightMap ? 1 : 0);
            }
            Shader.SetGlobalFloat(s_EnableSnowSurfaceHeightMapShaderID, m_EnableSurfaceHeightMap ? 1 : 0);
        }
        
        private void CheckWindChanged()
        {
            if (m_BindWindSimulate && windManager != null)
            {
                m_GlobalWind = windManager.GetGlobalWind();
            }
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
        private void DrawSnowEmitterShape()
        {
            if (m_SnowParticle != null)
            {
                var shape = m_SnowParticle.shape;
                Vector3 size = shape.scale;
                Vector3[] points = new Vector3[8];
                float x1 = -size.x / 2;
                float x2 = +size.x / 2;
                float y1 = -size.y / 2;
                float y2 = +size.y / 2;
                float z1 = -size.z / 2;
                float z2 = +size.z / 2;
                points[0] = new Vector3(x1, y1, z1);
                points[1] = new Vector3(x1, y1, z2);
                points[2] = new Vector3(x1, y2, z1);
                points[3] = new Vector3(x1, y2, z2);
                points[4] = new Vector3(x2, y1, z1);
                points[5] = new Vector3(x2, y1, z2);
                points[6] = new Vector3(x2, y2, z1);
                points[7] = new Vector3(x2, y2, z2);
                Vector3 forward = Vector3.forward;
                Vector3 position = shape.position;
                Matrix4x4 gameObjectLocalToWorld = m_SnowParticle.transform.localToWorldMatrix;
                Matrix4x4 localToWorld = Matrix4x4.TRS(position, Quaternion.LookRotation(forward), Vector3.one);
                for (int i = 0; i < 8; ++i)
                {
                    points[i] = gameObjectLocalToWorld.MultiplyPoint3x4(localToWorld.MultiplyPoint3x4(points[i]));
                }
                WindUtil.DrawBox(points, Color.white);
            }
            else
            {
                Vector3 position = m_BindWindSimulate ? new Vector3(-m_GlobalWind.x * m_ParticleBoundsSize.x * 0.1f * m_SnowVelocityScale.x * m_WindIntensity,
                    -m_GlobalWind.z * m_ParticleBoundsSize.y * 0.1f * m_SnowVelocityScale.z * m_WindIntensity,
                    -m_ParticleBoundsSize.z / 2) : new Vector3(0, 0, -m_ParticleBoundsSize.z / 2);
                Vector3 size = new Vector3(m_ParticleBoundsSize.x, m_ParticleBoundsSize.y, m_ParticleBoundsSize.z);
                Vector3[] points = new Vector3[8];
                float x1 = -size.x / 2;
                float x2 = +size.x / 2;
                float y1 = -size.y / 2;
                float y2 = +size.y / 2;
                float z1 = -size.z / 2;
                float z2 = +size.z / 2;
                points[0] = new Vector3(x1, y1, z1);
                points[1] = new Vector3(x1, y1, z2);
                points[2] = new Vector3(x1, y2, z1);
                points[3] = new Vector3(x1, y2, z2);
                points[4] = new Vector3(x2, y1, z1);
                points[5] = new Vector3(x2, y1, z2);
                points[6] = new Vector3(x2, y2, z1);
                points[7] = new Vector3(x2, y2, z2);
                Vector3 forward = Vector3.forward;
                Matrix4x4 gameObjectLocalToWorld = Matrix4x4.TRS(m_TargetPosition, Quaternion.Euler(90,0,0), Vector3.one);
                Matrix4x4 localToWorld = Matrix4x4.TRS(position, Quaternion.LookRotation(forward), Vector3.one);
                for (int i = 0; i < 8; ++i)
                {
                    points[i] = gameObjectLocalToWorld.MultiplyPoint3x4(localToWorld.MultiplyPoint3x4(points[i]));
                }
                WindUtil.DrawBox(points, Color.white);
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
        
        public bool bindWindSimulate
        {
            get { return m_BindWindSimulate; }
            set
            {
                m_BindWindSimulate = value;
                SetupSnowParticleSystem();
            }
        }
        
        public Vector3 globalWind
        {
            get { return m_GlobalWind; }
            set { m_GlobalWind = value; }
        }

        public GameObject snowPrefab
        {
            get { return m_SnowPrefab; }
            set
            {
                m_SnowPrefab = value;

                if (Application.isPlaying)
                {
                    InitSnowPrefab();
                }
            }
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
                    PlaySnowEffect();
                }
                else
                {
                    StopSnowEffect();
                }
            }
        }

        public float snowStrength
        {
            get { return m_SnowStrength; }
            set
            {
                m_SnowStrength = value;
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

        public GameObject snowGameObject
        {
            get { return m_SnowGameObject; }
            set
            {
                m_SnowGameObject = value;
                InitSnowGameObject();
            }
        }

        public Vector2 snowStartSize
        {
            get { return m_SnowStartSize; }
            set { m_SnowStartSize = value; }
        }

        public Vector3 velocityScale
        {
            get { return m_SnowVelocityScale; }
            set { m_SnowVelocityScale = value; }
        }
        public bool enableSurfaceHeightMap
        {
            get { return m_EnableSurfaceHeightMap; }
            set
            {
                if (value != m_EnableSurfaceHeightMap)
                {
                    m_CombineTexFinishedFlag = false;
                }
                m_EnableSurfaceHeightMap = value;
            }
        }

        #endregion
        
        #region 公开接口

        public void SetSnowStrengthWithTime(float targetSnowStrength, float transitionTime, Action callback = null, bool waitSnowAmountProcess = true)
        {
            m_InSetSnowStrengthProcess = true;
            m_TargetStrength = targetSnowStrength;
            m_TransitionRate = (m_TargetStrength - m_SnowStrength) / (transitionTime + 0.01f) * Time.fixedDeltaTime;
            m_SetSnowStrengthFinishCallback = callback;
            m_IsWaitSnowAmountProcess = waitSnowAmountProcess;
        }

        #endregion
        
        #region Editor 

#if UNITY_EDITOR
        public override void OnValidate()
        {
            if (!Application.isPlaying )
            {
                return;
            }

            enable = enable;
            enableSnowParticle = enableSnowParticle;
            particleBoundsSize = particleBoundsSize;
            snowMaxCount = snowMaxCount;
            snowMaxLifeTime = snowMaxLifeTime;
            SnowSurfaceGlobalOnValidate();
        }
#endif

        #endregion
    }
}
