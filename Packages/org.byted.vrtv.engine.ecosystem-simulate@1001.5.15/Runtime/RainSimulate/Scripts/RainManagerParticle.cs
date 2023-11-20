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
        [Header("雨粒子参数")]
        [SerializeField] private bool m_EnableRainParticle = true;
        [SerializeField] private int m_RainMaxCount = 2048;
        [SerializeField] private Vector2 m_RainStartSize = new Vector2(0.01f, 0.01f);
        [SerializeField] private float m_RainMaxLifeTime = 1;
        [SerializeField] private Vector3 m_RainVelocityScale = Vector3.one;
        [SerializeField] private float m_ParticleBoundsRadius = 32;
        [SerializeField] private float m_ParticleBoundsHeight = 20;
        [Reload("Runtime/RainSimulate/Resource/Textures/RainDrop.png")]
        [SerializeField] private Texture m_RainParticleTexture = null;
        [SerializeField] private Color m_RainParticleColor = Color.white;
        
        [Header("雨滴粒子参数")]
        [SerializeField] private bool m_EnableRainRippleParticle = false;
        [SerializeField] private int m_RainRippleCount = 1024;
        [SerializeField] private Vector2 m_RippleStartSize = new Vector2(0.8f, 1.2f);
        [Reload("Runtime/RainSimulate/Resource/Textures/RainRipple.png")]
        [SerializeField] private Texture m_RainRippleTexture = null;
        [SerializeField] private Vector2 m_RainRippleTextureAnimationTiles = new Vector2(4f, 8f);
        [SerializeField] private Color m_RainRippleColor = Color.white;
        [SerializeField] private Vector2 m_RainRippleLifeTime = new Vector2(0.5f, 0.7f);
        
        private float m_ParticleMovementDistance = 2;
        private ParticleSystem m_RainParticle = null;
        private Material m_RainParticleMat = null;
        private ParticleSystem m_RainRippleParticle = null;
        private Material m_RainRippleParticleMat = null;
        private static readonly int s_CurrentHeight = Shader.PropertyToID("_CurrentHeight");
        private static readonly int s_MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int s_TintColor = Shader.PropertyToID("_TintColor");

        private void InitRainGameObject()
        {
            if (rainGameObject == null)
            {
                return;
            }

            ParticleSystem[] particleSystems =
                rainGameObject.GetComponentsInChildren<ParticleSystem>();
            m_RainParticle = particleSystems[0];
            m_RainParticleMat = m_RainParticle.GetComponent<Renderer>().sharedMaterial;
            if (particleSystems.Length > 1)
            {
                m_RainRippleParticle = particleSystems[1];
                m_RainRippleParticleMat = m_RainRippleParticle.GetComponent<Renderer>().sharedMaterial;
                m_RainRippleParticle.Stop();
            }

            if (m_RainParticle != null)
            {
                m_RainParticle.gameObject.SetActive(true);
            }

            if (m_Enable)
            {
                PlayRainEffect();
            }
            else
            {
                StopRainEffect();
            }

            rainGameObject.SetActive(m_Enable && m_EnableRainParticle);
        }
        
        private void SetupRainParticleSystem()
        {
            if (m_RainParticle == null || !m_EnableRainParticle || !enable)
            {
                return;
            }

            CheckWindChanged();

            var main = m_RainParticle.main;
            main.startLifetime = m_RainMaxLifeTime;
            main.cullingMode = ParticleSystemCullingMode.Automatic;
            main.maxParticles = Mathf.FloorToInt(m_RainMaxCount * m_RainStrength);
            main.prewarm = true;
            main.startSize = new ParticleSystem.MinMaxCurve(m_RainStartSize.x, m_RainStartSize.y);
            
            var emission = m_RainParticle.emission;
            emission.rateOverTime = m_RainMaxCount * m_RainStrength / m_RainMaxLifeTime;

            var shape = m_RainParticle.shape;
            shape.radius = m_ParticleBoundsRadius / 2f;
            float windPositionScale = m_ParticleBoundsRadius * 0.03f * m_RainVelocityScale.x * m_WindIntensity;
            shape.position = m_BindWindSimulate ? new Vector3(
                -m_GlobalWind.x * windPositionScale,
                -m_GlobalWind.z * windPositionScale, 
                -m_ParticleBoundsHeight) 
                : new Vector3(0, 0, -m_ParticleBoundsHeight);
            
            ParticleSystem.VelocityOverLifetimeModule vof = m_RainParticle.velocityOverLifetime;
            vof.enabled = true;

            float tempVOLX = m_WindIntensity * m_GlobalWind.x * m_RainVelocityScale.x;
            float tempVOLY = s_RainParticleSystemLowYSpeed * m_RainVelocityScale.y;
            float tempVOLZ = m_WindIntensity * m_GlobalWind.z * m_RainVelocityScale.z;
            float tempVOLOffsetX = s_RainParticleVolOffset * m_RainVelocityScale.x;
            float tempVOLOffsetY = s_RainParticleVolOffset * m_RainVelocityScale.y;
            float tempVOLOffsetZ = s_RainParticleVolOffset * m_RainVelocityScale.z;
            vof.x = new ParticleSystem.MinMaxCurve(tempVOLX - tempVOLOffsetX, tempVOLX + tempVOLOffsetX);
            vof.y = new ParticleSystem.MinMaxCurve(tempVOLY - tempVOLOffsetY, tempVOLY + tempVOLOffsetY);
            vof.z = new ParticleSystem.MinMaxCurve(tempVOLZ - tempVOLOffsetZ, tempVOLZ + tempVOLOffsetZ);
            
            if (m_RainParticleMat != null)
            {
                if (m_RainParticleTexture != null)
                {
                    m_RainParticleMat.SetTexture(s_MainTex, m_RainParticleTexture);
                }

                m_RainParticleMat.SetColor(s_TintColor, m_RainParticleColor * m_WeatherBrightness);
            }

            if (m_RainRippleParticle != null && m_RainRippleParticleMat != null)
            {
                var rippleMain = m_RainRippleParticle.main;
                rippleMain.maxParticles = Mathf.FloorToInt(m_RainRippleCount * m_RainStrength);
                rippleMain.cullingMode = ParticleSystemCullingMode.Automatic;
                rippleMain.startSize = new ParticleSystem.MinMaxCurve(m_RippleStartSize.x, m_RippleStartSize.y);
                rippleMain.startLifetime =
                    new ParticleSystem.MinMaxCurve(m_RainRippleLifeTime.x, m_RainRippleLifeTime.y);

                var rippleEmission = m_RainRippleParticle.emission;
                rippleEmission.rateOverTime = m_RainRippleCount * m_RainStrength /
                    (m_RainRippleLifeTime.x + m_RainRippleLifeTime.y) * 2;
                m_RainRippleParticleMat.SetFloat(s_CurrentHeight, m_RainRippleParticle.transform.position.y);
                var textureSheetAnimation = m_RainRippleParticle.textureSheetAnimation;
                textureSheetAnimation.numTilesX = (int)m_RainRippleTextureAnimationTiles.x;
                textureSheetAnimation.numTilesY = (int)m_RainRippleTextureAnimationTiles.y;

                if (m_RainRippleTexture != null)
                {
                    m_RainRippleParticleMat.SetTexture(s_MainTex, m_RainRippleTexture);
                }

                m_RainRippleParticleMat.SetColor(s_TintColor, m_RainRippleColor * m_WeatherBrightness);
            }
        }

        private void UpdateRainEffect()
        {
            if (!m_EnableRainParticle || !enable || m_RainParticle == null)
            {
                return;
            }

            if (!m_EnableRainRippleParticle)
            {
                m_RainRippleParticle.Stop();
            }
            
            m_RainParticle.transform.position = m_TargetPosition;

            if (Mathf.Abs(m_TargetStrength - m_RainStrength) > Mathf.Abs(m_TransitionRate))
            {
                ParticleSystem.MainModule rainPariticleMain = m_RainParticle.main;
                rainPariticleMain.maxParticles = Mathf.FloorToInt(m_RainMaxCount * m_RainStrength);

                ParticleSystem.EmissionModule emission = m_RainParticle.emission;
                emission.rateOverTime = m_RainMaxCount * m_RainStrength / m_RainMaxLifeTime / 2;
            }

            if ((m_OldGlobalWind - m_GlobalWind).magnitude > 0.01f)
            {
                ParticleSystem.VelocityOverLifetimeModule vof = m_RainParticle.velocityOverLifetime;
                vof.enabled = true;

                float tempVOLX = m_WindIntensity * m_GlobalWind.x * m_RainVelocityScale.x;
                float tempVOLZ = m_WindIntensity * m_GlobalWind.z * m_RainVelocityScale.z;
                float tempVOLOffsetX = s_RainParticleVolOffset * m_RainVelocityScale.x;
                float tempVOLOffsetZ = s_RainParticleVolOffset * m_RainVelocityScale.z;
                vof.x = new ParticleSystem.MinMaxCurve(tempVOLX - tempVOLOffsetX, tempVOLX + tempVOLOffsetX);
                vof.z = new ParticleSystem.MinMaxCurve(tempVOLZ - tempVOLOffsetZ, tempVOLZ + tempVOLOffsetZ);
                
                var shape = m_RainParticle.shape;
                shape.radius = m_ParticleBoundsRadius / 2f;
                float windPositionScale = m_ParticleBoundsRadius * 0.03f * m_RainVelocityScale.x * m_WindIntensity;
                shape.position = m_BindWindSimulate
                    ? new Vector3(
                        -m_GlobalWind.x * windPositionScale,
                        -m_GlobalWind.z * windPositionScale,
                        -m_ParticleBoundsHeight) 
                    : new Vector3(0, 0, -m_ParticleBoundsHeight);
            }

            if (m_RainRippleParticleMat != null)
            {
                m_RainRippleParticleMat.SetFloat(s_CurrentHeight, m_RainRippleParticle.transform.position.y);
            }

            m_OldGlobalWind = m_GlobalWind;
        }
        
        private void PlayRainEffect()
        {
            if (m_RainGameObject != null)
            {
                m_RainGameObject.SetActive(m_EnableRainParticle);
                m_RainGameObject.transform.position = m_TargetPosition;
            }

            m_CurrentRainHumidnessDelay = m_RainHumidnessDelay;

            if (m_RainParticle != null)
            {
                SetupRainParticleSystem();
                m_RainParticle.Play();
            }

            if (m_RainRippleParticle != null && m_EnableRainRippleParticle)
            {
                m_RainRippleParticle.Play();
            }
        }

        private void StopRainEffect()
        {
            Shader.DisableKeyword(s_RainSurfaceKeyword);

            if (m_RainGameObject != null)
            {
                m_RainGameObject.SetActive(false);
            }

            m_CurrentRainHumidnessDelay = m_RainHumidnessDelay;

            if (m_RainParticle != null)
            {
                // LoadParticleData();
                m_RainParticle.Stop();
                m_RainRippleParticle.Stop();
            }
        }
        
        public bool enableRainParticle
        {
            get { return m_EnableRainParticle; }
            set
            {
                m_EnableRainParticle = value;

                if (rainGameObject != null)
                {
                    rainGameObject.SetActive(m_Enable && m_EnableRainParticle);
                    if (m_EnableRainParticle)
                    {
                        SetupRainParticleSystem();
                        m_RainParticle.Play();
                    }
                }
            }
        }
        public bool enableRainRippleParticle
        {
            get { return m_EnableRainRippleParticle; }
            set
            {
                m_EnableRainRippleParticle = value;

                if (m_RainRippleParticle != null)
                {
                    if (m_EnableRainRippleParticle)
                    {
                        m_RainRippleParticle.Play();
                    }
                    else
                    {
                        m_RainRippleParticle.Stop();
                    }
                }
            }
        }
        
        public float ParticleBoundsRadius
        {
            get { return m_ParticleBoundsRadius; }
            set
            {
                m_ParticleBoundsRadius = value;
            }
        }

        public float particleMovementDistance
        {
            get { return m_ParticleMovementDistance; }
            set { m_ParticleMovementDistance = value; }
        }
        
        public int rainMaxCount
        {
            get { return m_RainMaxCount; }
            set
            {
                m_RainMaxCount = value;
                SetupRainParticleSystem();
            }
        }

        public float rainMaxLifeTime
        {
            get { return m_RainMaxLifeTime; }
            set
            {
                m_RainMaxLifeTime = value;
            }
        }
        
        public GameObject rainGameObject
        {
            get { return m_RainGameObject; }
            set
            {
                m_RainGameObject = value;

                InitRainGameObject();
            }
        }

        public Vector3 velocityScale
        {
            get { return m_RainVelocityScale; }
            set { m_RainVelocityScale = value; }
        }

        public Vector2 rippleStartSize
        {
            get { return m_RippleStartSize; }
            set
            {
                m_RippleStartSize = value;
                SetupRainParticleSystem();
            }
        }

        public int rainRippleCount
        {
            get { return m_RainRippleCount; }
            set
            {
                m_RainRippleCount = value;
                SetupRainParticleSystem();
            }
        }
    }
}
