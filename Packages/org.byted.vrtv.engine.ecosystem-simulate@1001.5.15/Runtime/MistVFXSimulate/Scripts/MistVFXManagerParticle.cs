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
    public partial class MistVFXManager
    {
        [Header("雾粒子参数")]
        [SerializeField] private bool m_EnableMistParticle = true;
        [SerializeField] [Range(0, 1)]private float m_MistStrength = 0.5f;
        [SerializeField] [Min(0)]private int m_MistMaxCount = 5;
        [SerializeField] private Vector2 m_MistStartSize = new Vector2(15f, 20f);
        [SerializeField] private float m_MistMaxLifeTime = 3;
        [SerializeField] private Vector3 m_MistVelocityScale = Vector3.one;
        [SerializeField] private Vector3 m_ParticleBoundsSize = new Vector3(8,8,8);
        [Reload("Runtime/MistVFXSimulate/Resource/Textures/MistVFXTexture1.png")]
        [SerializeField] private Texture m_MistParticleTexture = null;
        [SerializeField] private Vector2 m_MistParticleTextureSheetTiles = new Vector2(5, 5);
        [SerializeField] private Color m_MistParticleColor = Color.white * 0.5f;
        [SerializeField] private bool m_EnableMistSoftParticle = false;
        
        //预设速度偏移
        private static readonly float s_MistParticleSystemLowYSpeedMin = -10f;
        private static readonly float s_MistParticleSystemLowYSpeedMax = -7f;
        private static readonly float s_RainParticleVolOffset = 1.0f; 
        private float m_ParticleMovementDistance = 2;
        private float m_MistAmountDelay = 0;
        private float m_MistAmountRate = 1f;
        private ParticleSystem m_MistParticle = null;
        private Material m_MistParticleMat = null;
        private static readonly int s_MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int s_TintColor = Shader.PropertyToID("_TintColor");

        private void InitMistGameObject()
        {
            if (mistGameObject == null)
            {
                return;
            }

            ParticleSystem[] particleSystems =
                mistGameObject.GetComponentsInChildren<ParticleSystem>();
            m_MistParticle = particleSystems[0];
            m_MistParticleMat = m_MistParticle.GetComponent<Renderer>().sharedMaterial;

            if (m_Enable)
            {
                PlayMistEffect();
            }
            else
            {
                StopMistEffect();
            }
            
            mistGameObject.SetActive(m_Enable && m_EnableMistParticle);
        }
        
        private void SetupMistParticleSystem()
        {
            if (m_MistParticle == null || !m_EnableMistParticle || !enable)
            {
                return;
            }

            CheckWindChanged();

            ParticleSystem.MainModule main = m_MistParticle.main;
            main.startLifetime = m_MistMaxLifeTime;
            main.cullingMode = ParticleSystemCullingMode.Automatic;
            main.maxParticles = Mathf.FloorToInt(m_MistMaxCount * m_MistStrength);
            main.prewarm = true;
            main.startSize = new ParticleSystem.MinMaxCurve(m_MistStartSize.x, m_MistStartSize.y);

            ParticleSystem.EmissionModule emission = m_MistParticle.emission;
            emission.rateOverTime = m_MistMaxCount * m_MistStrength / m_MistMaxLifeTime;

            var sheet = m_MistParticle.textureSheetAnimation;
            sheet.numTilesX = (int)m_MistParticleTextureSheetTiles.x;
            sheet.numTilesY = (int)m_MistParticleTextureSheetTiles.y;
            
            SetMistParticleWindParams();
            if (m_MistParticleTexture != null)
            {
                m_MistParticleMat.SetTexture(s_MainTex, m_MistParticleTexture);
            }
            m_MistParticleMat.SetColor(s_TintColor, m_MistParticleColor);
            if (m_EnableMistSoftParticle)
            {
                m_MistParticleMat.EnableKeyword("SOFTPARTICLES_ON");
            }
            else
            {
                m_MistParticleMat.DisableKeyword("SOFTPARTICLES_ON");
            }
        }

        private void UpdateMistEffect()
        {
            if (!m_EnableMistParticle || !enable)
            {
                return;
            }

            if (m_MistParticle == null)
            {
                return;
            }

            //根据静态风来改变雪粒子移动速度和方向
            m_MistParticle.transform.position = m_TargetPosition;

            //根据强度修改粒子数量
            if (Mathf.Abs(m_TargetStrength - m_MistStrength) > Mathf.Abs(m_TransitionRate))
            {
                ParticleSystem.MainModule MistPariticleMain = m_MistParticle.main;
                MistPariticleMain.maxParticles = Mathf.FloorToInt(m_MistMaxCount * m_MistStrength);

                ParticleSystem.EmissionModule emission = m_MistParticle.emission;
                emission.rateOverTime = m_MistMaxCount * m_MistStrength / m_MistMaxLifeTime;
            }
            
            if ((m_OldGlobalWind - m_GlobalWind).magnitude > 0.01f)
            {
                SetMistParticleWindParams();
            }

            m_OldGlobalWind = m_GlobalWind;
        }

        private void SetMistParticleWindParams()
        {
            //设置粒子速度
            ParticleSystem.VelocityOverLifetimeModule vof = m_MistParticle.velocityOverLifetime;
            vof.enabled = true;
            float VOLX = m_WindIntensity * m_GlobalWind.x;
            float VOLZ = m_WindIntensity * m_GlobalWind.z;
            float VOLY = m_WindIntensity * m_GlobalWind.y;
            vof.x = new ParticleSystem.MinMaxCurve((VOLX - s_RainParticleVolOffset) * m_MistVelocityScale.x,
                (VOLX + s_RainParticleVolOffset) * m_MistVelocityScale.x);
            vof.y = new ParticleSystem.MinMaxCurve((s_MistParticleSystemLowYSpeedMin + VOLY) * m_MistVelocityScale.y,
            (s_MistParticleSystemLowYSpeedMax + VOLY) * m_MistVelocityScale.y);
            vof.z = new ParticleSystem.MinMaxCurve((VOLZ - s_RainParticleVolOffset) * m_MistVelocityScale.z,
                (VOLZ + s_RainParticleVolOffset) * m_MistVelocityScale.z);
                
            //设置粒子起始位置偏移
            ParticleSystem.ShapeModule shape = m_MistParticle.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = m_BindWindSimulate
                ? new Vector3(-m_GlobalWind.x * m_ParticleBoundsSize.x * 0.2f * m_MistVelocityScale.x * m_WindIntensity,
                    m_ParticleBoundsSize.y * 2,
                    -m_GlobalWind.z * m_ParticleBoundsSize.z * 0.2f * m_MistVelocityScale.z * m_WindIntensity)
                : new Vector3(0, m_ParticleBoundsSize.y * 2, 0);

            shape.scale = new Vector3(m_ParticleBoundsSize.x, m_ParticleBoundsSize.y, m_ParticleBoundsSize.z);
        }

        private void PlayMistEffect()
        {
            if (m_MistGameObject != null)
            {
                m_MistGameObject.SetActive(m_EnableMistParticle);
                m_MistGameObject.transform.position = m_TargetPosition;
            }

            m_CurrentMistAmountDelay = m_MistAmountDelay;

            if (m_MistParticle != null)
            {
                SetupMistParticleSystem();
                m_MistParticle.Play();
            }
        }

        private void StopMistEffect()
        {
            if (m_MistGameObject != null)
            {
                m_MistGameObject.SetActive(false);
            }

            m_CurrentMistAmountDelay = m_MistAmountDelay;
            
            if (m_MistParticle != null)
            {
                m_MistParticle.Stop();
            }
        }
        
        public bool enableMistParticle
        {
            get { return m_EnableMistParticle; }
            set
            {
                m_EnableMistParticle = value;

                if (mistGameObject != null)
                {
                    mistGameObject.SetActive(m_EnableMistParticle);
                }

                if (m_EnableMistParticle)
                {
                    SetupMistParticleSystem();
                    m_MistParticle.Play();
                }
            }
        }

        public GameObject mistPrefab
        {
            get { return m_MistPrefab; }
            set
            {
                m_MistPrefab = value;

                if (Application.isPlaying)
                {
                    InitMistPrefab();
                }
            }
        }
        
        public float particleMovementDistance
        {
            get { return m_ParticleMovementDistance; }
            set { m_ParticleMovementDistance = value; }
        }
        
        public int mistMaxCount
        {
            get { return m_MistMaxCount; }
            set
            {
                m_MistMaxCount = value;
                SetupMistParticleSystem();
            }
        }

        public float mistMaxLifeTime
        {
            get { return m_MistMaxLifeTime; }
            set
            {
                m_MistMaxLifeTime = value;
            }
        }

        public float mistAmountDelay
        {
            get { return m_MistAmountDelay; }
            set { m_MistAmountDelay = value; }
        }

        public float mistAmountRate
        {
            get { return m_MistAmountRate; }
            set { m_MistAmountRate = value; }
        }
        
        public Vector3 particleBoundsSize
        {
            get { return m_ParticleBoundsSize; }
            set
            {
                m_ParticleBoundsSize = value;
            }
        }
        
        public float mistStrength
        {
            get { return m_MistStrength; }
            set
            {
                m_MistStrength = value;
            }
        }
    }
}
