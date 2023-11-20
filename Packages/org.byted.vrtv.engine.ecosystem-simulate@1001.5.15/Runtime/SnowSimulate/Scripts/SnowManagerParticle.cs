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
using UnityEngine.ParticleSystemJobs;

namespace Matrix.EcosystemSimulate
{
    public partial class SnowManager
    {
        [Header("雪粒子参数")]
        [SerializeField] private bool m_EnableSnowParticle = true;
        [Range(0, 1)]
        [SerializeField] private float m_SnowStrength = 0.5f;
        [SerializeField] private int m_SnowMaxCount = 4096;
        [SerializeField] private Vector2 m_SnowStartSize = new Vector2(0.05f, 0.2f);
        [SerializeField] private float m_SnowMaxLifeTime = 6;
        [SerializeField] private Vector3 m_SnowVelocityScale = Vector3.one;
        [SerializeField] private Vector3 m_ParticleBoundsSize = new Vector3(32, 32, 32);
        [SerializeField] private Texture m_SnowParticleTexture = null;
        [SerializeField] private Color m_SnowParticleColor = Color.white;
        [Space]
        [SerializeField] private bool m_EnableFadingSnowParticle = false;
        [SerializeField] private float m_FadingSnowLifeTime = 2;
        [SerializeField] private Color m_FadingSnowParticleColor = Color.white;
        
        private ParticleSystem m_SnowParticle = null;
        private Material m_SnowParticleMat = null;
        private ParticleSystem m_FadingSnowParticle = null;
        private Material m_FadingSnowParticleMat = null;
        private float m_ParticleMovementDistance = 2;
        private float m_SnowAmountDelay = 0;
        private float m_SnowAmountRate = 1f;
        
        //预设速度偏移
        private static readonly float s_SnowParticleSystemLowYSpeedMin = -8f;
        private static readonly float s_SnowParticleSystemLowYSpeedMax = -6f;
        private static readonly float s_RainParticleVolOffset = 5.0f;   //VOL : Velocity Over LifeTime
        private UpdateParticlesHeightMapJob m_ParticleJob = new UpdateParticlesHeightMapJob();
        private static readonly int s_MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int s_TintColor = Shader.PropertyToID("_TintColor");
        private static readonly int s_CurrentHeight = Shader.PropertyToID("_CurrentHeight");

        struct UpdateParticlesHeightMapJob : IJobParticleSystem
        {
            public void Execute(ParticleSystemJobData particles)
            {
                var positionsX = particles.positions.x;
                var positionsY = particles.positions.y;
                var positionsZ = particles.positions.z;
                var aliveTimePercent = particles.aliveTimePercent;
                var colors = particles.startColors;
                for (int i = 0; i < particles.count; i++)
                {
                    float height = HeightMapManager.GetHeightInCPUStatic(new Vector3(positionsX[i], 0, positionsZ[i]));
                    if (positionsY[i] < height + 0.5f)
                    {
                        positionsY[i] = height + 0.2f;
                        aliveTimePercent[i] = 100;
                    }
                }
            }
        }

        private void InitSnowGameObject()
        {
            if (snowGameObject == null)
            {
                return;
            }

            ParticleSystem[] particleSystems =
                snowGameObject.GetComponentsInChildren<ParticleSystem>();
            m_SnowParticle = particleSystems[0];
            m_SnowParticleMat = m_SnowParticle.GetComponent<Renderer>().sharedMaterial;
            if (particleSystems.Length > 1)
            {
                m_FadingSnowParticle = particleSystems[1];
                m_FadingSnowParticleMat = m_FadingSnowParticle.GetComponent<Renderer>().sharedMaterial;
            }

            if (m_Enable)
            {
                PlaySnowEffect();
            }
            else
            {
                StopSnowEffect();
            }
            
            snowGameObject.SetActive(m_Enable && m_EnableSnowParticle);
        }
        
        private void SetupSnowParticleSystem()
        {
            if (m_SnowParticle == null || !m_EnableSnowParticle || !enable)
            {
                return;
            }

            CheckWindChanged();

            ParticleSystem.MainModule main = m_SnowParticle.main;
            main.startLifetime = m_SnowMaxLifeTime;
            main.cullingMode = ParticleSystemCullingMode.Automatic;
            main.maxParticles = Mathf.FloorToInt(m_SnowMaxCount * m_SnowStrength);
            main.prewarm = true;
            main.startSize = new ParticleSystem.MinMaxCurve(m_SnowStartSize.x, m_SnowStartSize.y);

            ParticleSystem.EmissionModule emission = m_SnowParticle.emission;
            emission.rateOverTime = m_SnowMaxCount * m_SnowStrength / m_SnowMaxLifeTime;

            SetSnowParticleWindParams();

            if (m_FadingSnowParticle != null)
            {
                var fadingSnowMain = m_FadingSnowParticle.main;
                fadingSnowMain.startLifetime = m_FadingSnowLifeTime;
            }

            if (m_SnowParticleTexture != null)
            {
                m_SnowParticleMat.SetTexture(s_MainTex, m_SnowParticleTexture);
                if (m_FadingSnowParticleMat != null)
                {
                    m_FadingSnowParticleMat.SetTexture(s_MainTex, m_SnowParticleTexture);
                }
            }

            m_SnowParticleMat.SetColor(s_TintColor, m_SnowParticleColor);
            if (m_FadingSnowParticleMat != null)
            {
                m_FadingSnowParticleMat.SetColor(s_TintColor, m_FadingSnowParticleColor);
                m_FadingSnowParticleMat.SetFloat(s_CurrentHeight, m_FadingSnowParticle.transform.position.y);
            }
        }

        private void UpdateSnowParticleHeight()
        {
            if (m_HeightMapManager == null)
            {
                m_HeightMapManager = EcosystemManager.instance.Get<HeightMapManager>(EcosystemType.HeightMapManager);
            }
            if (m_HeightMapManager != null && Application.isPlaying && m_SnowParticle != null)
            {
                m_ParticleJob.Schedule(m_SnowParticle);
            }
        }

        private void UpdateSnowEffect()
        {
            if (!m_EnableSnowParticle || !enable)
            {
                return;
            }

            if (m_SnowParticle == null)
            {
                return;
            }

            //根据静态风来改变雪粒子移动速度和方向
            m_SnowParticle.transform.position = m_TargetPosition;

            //根据强度修改粒子数量
            if (Mathf.Abs(m_TargetStrength - m_SnowStrength) > Mathf.Abs(m_TransitionRate))
            {
                ParticleSystem.MainModule snowPariticleMain = m_SnowParticle.main;
                snowPariticleMain.maxParticles = Mathf.FloorToInt(m_SnowMaxCount * m_SnowStrength);

                ParticleSystem.EmissionModule emission = m_SnowParticle.emission;
                emission.rateOverTime = m_SnowMaxCount * m_SnowStrength / m_SnowMaxLifeTime;
            }
            
            if ((m_OldGlobalWind - m_GlobalWind).magnitude > 0.01f)
            {
                SetSnowParticleWindParams();
            }
            
            if (m_FadingSnowParticleMat != null)
            {
                m_FadingSnowParticleMat.SetFloat(s_CurrentHeight, m_FadingSnowParticle.transform.position.y);
            }

            m_OldGlobalWind = m_GlobalWind;

            var subEmitters = m_SnowParticle.subEmitters;
            subEmitters.enabled = m_EnableFadingSnowParticle;
        }

        private void SetSnowParticleWindParams()
        {
            //设置粒子速度
            ParticleSystem.VelocityOverLifetimeModule vof = m_SnowParticle.velocityOverLifetime;
            vof.enabled = true;
            float VOLX = m_WindIntensity * m_GlobalWind.x;
            float VOLZ = m_WindIntensity * m_GlobalWind.z;
            vof.x = new ParticleSystem.MinMaxCurve((VOLX - s_RainParticleVolOffset) * m_SnowVelocityScale.x,
                (VOLX + s_RainParticleVolOffset) * m_SnowVelocityScale.x);
            vof.y = new ParticleSystem.MinMaxCurve(s_SnowParticleSystemLowYSpeedMin * m_SnowVelocityScale.y,
                s_SnowParticleSystemLowYSpeedMax * m_SnowVelocityScale.y);
            vof.z = new ParticleSystem.MinMaxCurve((VOLZ - s_RainParticleVolOffset) * m_SnowVelocityScale.z,
                (VOLZ + s_RainParticleVolOffset) * m_SnowVelocityScale.z);
                
            //设置粒子起始位置偏移
            ParticleSystem.ShapeModule shape = m_SnowParticle.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = m_BindWindSimulate ? new Vector3(-m_GlobalWind.x * m_ParticleBoundsSize.x * 0.1f * m_SnowVelocityScale.x * m_WindIntensity,
                -m_GlobalWind.z * m_ParticleBoundsSize.y * 0.1f * m_SnowVelocityScale.z * m_WindIntensity,
                -m_ParticleBoundsSize.z / 2) : new Vector3(0, 0, -m_ParticleBoundsSize.z / 2);

            shape.scale = new Vector3(m_ParticleBoundsSize.x, m_ParticleBoundsSize.y, m_ParticleBoundsSize.z);
        }

        private void PlaySnowEffect()
        {
            if (m_SnowGameObject != null)
            {
                m_SnowGameObject.SetActive(m_EnableSnowParticle);
                m_SnowGameObject.transform.position = m_TargetPosition;
            }

            m_CurrentSnowAmountDelay = m_SnowAmountDelay;

            if (m_SnowParticle != null)
            {
                SetupSnowParticleSystem();
                m_SnowParticle.Play();
                if (m_FadingSnowParticle != null)
                {
                    m_FadingSnowParticle.Play();
                }
            }

            SetGlobalSnowSurfaceShader();
        }

        private void StopSnowEffect()
        {
            Shader.DisableKeyword(s_SnowSurfaceKeyWord);

            if (m_SnowGameObject != null)
            {
                m_SnowGameObject.SetActive(false);
            }

            m_CurrentSnowAmountDelay = m_SnowAmountDelay;
            
            if (m_SnowParticle != null)
            {
                m_SnowParticle.Stop();
                if (m_FadingSnowParticle != null)
                {
                    m_FadingSnowParticle.Stop();
                }
            }
        }
                
        public bool enableSnowParticle
        {
            get { return m_EnableSnowParticle; }
            set
            {
                m_EnableSnowParticle = value;

                if (m_EnableSnowParticle)
                {
                    PlaySnowEffect();
                }
                else
                {
                    StopSnowEffect();
                }
            }
        }

        public Vector3 particleBoundsSize
        {
            get { return m_ParticleBoundsSize; }
            set
            {
                m_ParticleBoundsSize = value;
            }
        }

        public float particleMovementDistance
        {
            get { return m_ParticleMovementDistance; }
            set { m_ParticleMovementDistance = value; }
        }

        public int snowMaxCount
        {
            get { return m_SnowMaxCount; }
            set
            {
                m_SnowMaxCount = value;
                SetupSnowParticleSystem();
            }
        }
        
        public float snowMaxLifeTime
        {
            get { return m_SnowMaxLifeTime; }
            set
            {
                m_SnowMaxLifeTime = value;
            }
        }

        public float snowAmountDelay
        {
            get { return m_SnowAmountDelay; }
            set { m_SnowAmountDelay = value; }
        }

        public float snowAmountRate
        {
            get { return m_SnowAmountRate; }
            set { m_SnowAmountRate = value; }
        }
    }
}
