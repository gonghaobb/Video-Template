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
    [CreateAssetMenu(fileName = "MistVFXSimulate", menuName = "EcosystemSimulate/MistVFXSimulate")]
    public partial class MistVFXManager : SubEcosystem
    {
        #region SerializeField 属性
        [Header("雾粒子Prefab")]
        [Reload("Runtime/MistVFXSimulate/Resource/Prefabs/MistVFXParticle.prefab")]
        [SerializeField] private GameObject m_MistPrefab = null;
        [Header("是否绑定风模拟组件")]
        [SerializeField] private bool m_BindWindSimulate = true;
        [SerializeField] private Vector3 m_GlobalWind = new Vector3(1, 0, 1);
        [SerializeField] private float m_WindIntensity = 1;
        [Header("绑定目标")]
        [SerializeField] private bool m_EnableBindMainCamera = false;
        [SerializeField] private bool m_EnableBindTarget = false;
        [SerializeField] private Vector3 m_TargetPosition = Vector3.zero;
        #endregion
        
        #region Private 属性

        private GameObject m_MistGameObject = null;
        private float m_MistAmount = 0;
        private float m_CurrentMistAmountDelay = 0;

        private Action m_SetMistStrengthFinishCallback = null;
        private float m_TargetStrength = 0;
        private float m_TransitionRate = 0;
        private bool m_InSetMistStrengthProcess = false;
        private bool m_IsWaitMistAmountProcess = false;

        private Transform m_TargetTransform = null;
        private Camera m_MainCamera = null;
        private Vector3 m_OldGlobalWind = Vector3.zero;
        private WindManager m_WindManager = null;
        #endregion
        
        #region 默认事件
        public override bool SupportInCurrentPlatform()
        {
            return true;
        }

        public override void Enable()
        {
            InitMistPrefab();
            
            InitParams();
            CheckIfBindCamera();

            m_TargetPosition = m_EnableBindTarget ? m_TargetTransform.position : m_TargetPosition;

            if (m_MistGameObject != null)
            {
                m_MistGameObject.SetActive(true);
                InitMistGameObject();
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
                m_TargetTransform = mainCamera.transform;
            }
        }

        public override void Disable()
        {
            m_MistAmount = 0;
            
            if (m_MistGameObject != null)
            {
                SafeDestroy(m_MistGameObject);
                m_MistGameObject = null;
            }
        }

        public override void Update()
        {
            CheckWindChanged();
            CheckIfBindCamera();
            if (m_EnableBindTarget && Vector3.Distance(m_TargetTransform.position, m_TargetPosition) > particleMovementDistance)
            {
                m_TargetPosition = m_TargetTransform.position;
            }
        
            UpdateMistEffect();
        }
        
        public override void FixedUpdate()
        {
            if (m_InSetMistStrengthProcess)
            {
                if (Mathf.Abs(m_TargetStrength - m_MistStrength) < Mathf.Abs(m_TransitionRate))
                {
                    m_InSetMistStrengthProcess = false;
                    if (!m_IsWaitMistAmountProcess)
                    {
                        m_SetMistStrengthFinishCallback?.Invoke();
                        m_SetMistStrengthFinishCallback = null;
                    }
                }
                else
                {
                    m_MistStrength += m_TransitionRate;
                    m_MistStrength = Mathf.Clamp01(m_MistStrength);
                }
            }
        
            m_CurrentMistAmountDelay -= Time.fixedDeltaTime;
            if (m_CurrentMistAmountDelay > 0)
            {
                return;
            }
        
            m_MistAmount += m_MistAmountRate * Time.fixedDeltaTime * (m_Enable && m_MistStrength > 0.05f ? 1 : -1);
            m_CurrentMistAmountDelay = 0;
            m_MistAmount = Mathf.Clamp(m_MistAmount, 0, 1);
        
            if (m_MistAmount > 0.001f)
            {
                if (m_IsWaitMistAmountProcess && m_TransitionRate > 0)
                {
                    m_SetMistStrengthFinishCallback?.Invoke();
                    m_SetMistStrengthFinishCallback = null;
                    m_IsWaitMistAmountProcess = false;
                }
            }
        
            if (m_MistAmount <= 0.001f)
            {
                if (m_IsWaitMistAmountProcess && m_TransitionRate < 0)
                {
                    m_SetMistStrengthFinishCallback?.Invoke();
                    m_SetMistStrengthFinishCallback = null;
                    m_IsWaitMistAmountProcess = false;
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
                DrawMistEmitterShape();
            }
        }
#endif
        #endregion

        #region 私有方法
        private void InitParams()
        {
            m_MistAmount = 0;
            m_CurrentMistAmountDelay = 0;
            m_MistParticle = null;
            m_MistAmountDelay = 0;
            m_MistAmountRate = 1;
            m_SetMistStrengthFinishCallback = null;
            m_TargetStrength = 0;
            m_InSetMistStrengthProcess = false;
            m_TransitionRate = 0;
        }

        private void CheckWindChanged()
        {
            if (m_BindWindSimulate && windManager != null)
            {
                m_GlobalWind = windManager.GetGlobalWind();
            }
        }
        
        private void InitMistPrefab()
        {
            if (m_MistPrefab == null || !Application.isPlaying)
            {
                return;
            }

            if (m_MistGameObject == null)
            {
                m_MistGameObject = EcosystemTagFinder.Find("MistVFXSimulate");
                if (m_MistGameObject == null)
                {
                    m_MistGameObject = Instantiate(m_MistPrefab, EcosystemManager.instance.gameObject.transform);
                    m_MistGameObject.name = "MistVFXSimulate";
                    var tag = m_MistGameObject.AddComponent<EcosystemTagFinder>();
                    tag.Init("MistVFXSimulate");
                    DontDestroyOnLoad(m_MistGameObject);
                }
            }
        }

#if UNITY_EDITOR
        private void DrawMistEmitterShape()
        {
            if (m_MistParticle != null)
            {
                var shape = m_MistParticle.shape;
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
                Matrix4x4 gameObjectLocalToWorld = m_MistParticle.transform.localToWorldMatrix;
                Matrix4x4 localToWorld = Matrix4x4.TRS(position, Quaternion.LookRotation(forward), Vector3.one);
                for (int i = 0; i < 8; ++i)
                {
                    points[i] = gameObjectLocalToWorld.MultiplyPoint3x4(localToWorld.MultiplyPoint3x4(points[i]));
                }

                WindUtil.DrawBox(points, Color.white);
            }
            else
            {
                Vector3 position = m_BindWindSimulate
                    ? new Vector3(
                        -m_GlobalWind.x * m_ParticleBoundsSize.x * 0.2f * m_MistVelocityScale.x * m_WindIntensity,
                        m_ParticleBoundsSize.y * 2,
                        -m_GlobalWind.z * m_ParticleBoundsSize.z * 0.2f * m_MistVelocityScale.z * m_WindIntensity)
                    : new Vector3(0, m_ParticleBoundsSize.y * 2, 0);

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
                Matrix4x4 gameObjectLocalToWorld = Matrix4x4.TRS(m_TargetPosition, Quaternion.Euler(0,0,0), Vector3.one);
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
        public Vector3 globalWind
        {
            get { return m_GlobalWind; }
            set { m_GlobalWind = value; }
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
        
        public bool bindWindSimulate
        {
            get { return m_BindWindSimulate; }
            set
            {
                m_BindWindSimulate = value;
                SetupMistParticleSystem();
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
                    PlayMistEffect();
                }
                else
                {
                    StopMistEffect();
                }
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

        public GameObject mistGameObject
        {
            get { return m_MistGameObject; }
            set
            {
                m_MistGameObject = value;
                InitMistGameObject();
            }
        }

        public Vector2 mistStartSize
        {
            get { return m_MistStartSize; }
            set { m_MistStartSize = value; }
        }

        public Vector3 velocityScale
        {
            get { return m_MistVelocityScale; }
            set { m_MistVelocityScale = value; }
        }

        #endregion
        
        #region 公开接口

        public void SetMistStrengthWithTime(float targetMistStrength, float transitionTime, Action callback = null, bool waitMistAmountProcess = true)
        {
            m_InSetMistStrengthProcess = true;
            m_TargetStrength = targetMistStrength;
            m_TransitionRate = (m_TargetStrength - m_MistStrength) / (transitionTime + 0.01f) * Time.fixedDeltaTime;
            m_SetMistStrengthFinishCallback = callback;
            m_IsWaitMistAmountProcess = waitMistAmountProcess;
        }

        #endregion
        
        #region Editor
#if UNITY_EDITOR
        public override void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            enable = enable;
            enableMistParticle = enableMistParticle;
            particleBoundsSize = particleBoundsSize;
            mistMaxCount = mistMaxCount;
            mistMaxLifeTime = mistMaxLifeTime;
        }
#endif
        #endregion
    }
}
