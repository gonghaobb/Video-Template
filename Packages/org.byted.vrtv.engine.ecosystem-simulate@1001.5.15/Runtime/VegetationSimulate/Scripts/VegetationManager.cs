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
using System;
using UnityEngine.Serialization;

namespace Matrix.EcosystemSimulate
{
    [CreateAssetMenu(fileName = "VegetationSimulate", menuName = "EcosystemSimulate/VegetationSimulate")]
    public class VegetationManager : SubEcosystem
    {
        [Serializable]
        public class VegetationWind
        {
            [Header("基础风强度")]
            [Range(0f, 5f)]
            public float baseWindPower = 3f;
            public float baseWindSpeed = 1f;
            [Header("扰动风强度")]
            [Range(0f, 10f)]
            public float burstsPower = 0.5f;
            public float burstsSpeed = 5f;
            public float burstsScale = 10f;
            [Header("细节风强度(草仅受此参数影响)")]
            [Range(0f, 1f)]
            public float microPower = 0.1f;
            public float microSpeed = 1f;
            public float microFrequency = 3f;
        }

        [Header("植被风参数")]
        [SerializeField] private VegetationWind m_VegetationWind;
        
        [FormerlySerializedAs("m_CaptureSettings")]
        [Space]
        [Header("植被吸色参数")]
        [SerializeField] private SceneCaptureSettings m_SceneCaptureSettings;

        [SerializeField] [Range(0, 1)] private float m_GrassHeightBlendPower;
        private static readonly int s_VegetationWindPower = Shader.PropertyToID("_VegetationWindPower");
        private static readonly int s_VegetationWindSpeed = Shader.PropertyToID("_VegetationWindSpeed");
        private static readonly int s_VegetationWindBurstsPower = Shader.PropertyToID("_VegetationWindBurstsPower");
        private static readonly int s_VegetationWindBurstsSpeed = Shader.PropertyToID("_VegetationWindBurstsSpeed");
        private static readonly int s_VegetationWindBurstsScale = Shader.PropertyToID("_VegetationWindBurstsScale");
        private static readonly int s_VegetationWindMicroPower = Shader.PropertyToID("_VegetationWindMicroPower");
        private static readonly int s_VegetationWindMicroSpeed = Shader.PropertyToID("_VegetationWindMicroSpeed");
        private static readonly int s_VegetationWindMicroFrequency = Shader.PropertyToID("_VegetationWindMicroFrequency");
        private static readonly int s_VegetationWindDirection = Shader.PropertyToID("_VegetationWindDirection");
        private static readonly int s_VegetationSceneCaptureGroundColor = Shader.PropertyToID("_VegetationSceneCaptureGroundColor");
        private static readonly int s_VegetationSceneCaptureParams = Shader.PropertyToID("_VegetationSceneCaptureParams");

        private WindManager m_WindManager;

        public override void Enable()
        {
            RefreshVegetationParams();
            windManager = null;
            if (windManager != null)
            {
                windManager.onGlobalWindParamsChanged += RefreshVegetationParams;
            }
        }

        public override void Disable()
        {
            if (windManager != null)
            {
                windManager.onGlobalWindParamsChanged -= RefreshVegetationParams;
            }
        }

        public void RefreshVegetationParams()
        {
            SetGroundColorParams();
            SetVegetationWindParams();
        }

        private void SetGroundColorParams()
        {
            if (sceneCaptureSettings != null)
            {
                Shader.SetGlobalTexture(s_VegetationSceneCaptureGroundColor,
                    sceneCaptureSettings.sceneColorTexture ? sceneCaptureSettings.sceneColorTexture : Texture2D.blackTexture);
                Shader.SetGlobalVector(s_VegetationSceneCaptureParams,
                    sceneCaptureSettings.sceneColorTexture
                        ? new Vector4(sceneCaptureSettings.centerPos.x, sceneCaptureSettings.centerPos.z, sceneCaptureSettings.orthographicSize * 2,
                            m_GrassHeightBlendPower)
                        : new Vector4(0, 0, 1, 0));
            }
            else
            {
                Shader.SetGlobalTexture(s_VegetationSceneCaptureGroundColor, Texture2D.blackTexture);
                Shader.SetGlobalVector(s_VegetationSceneCaptureParams, new Vector4(0, 0, 1, 0));
            }
        }
        
        private void SetVegetationWindParams()
        {
            float globalWindMultiplier = windManager != null ? windManager.singleWindGlobalStrength : 1;
            Shader.SetGlobalFloat(s_VegetationWindPower, m_VegetationWind.baseWindPower * globalWindMultiplier);
            Shader.SetGlobalFloat(s_VegetationWindSpeed, m_VegetationWind.baseWindSpeed);
            Shader.SetGlobalFloat(s_VegetationWindBurstsPower, m_VegetationWind.burstsPower * globalWindMultiplier);
            Shader.SetGlobalFloat(s_VegetationWindBurstsSpeed, m_VegetationWind.burstsSpeed);
            Shader.SetGlobalFloat(s_VegetationWindBurstsScale, m_VegetationWind.burstsScale);
            Shader.SetGlobalFloat(s_VegetationWindMicroPower, m_VegetationWind.microPower * globalWindMultiplier);
            Shader.SetGlobalFloat(s_VegetationWindMicroSpeed, m_VegetationWind.microSpeed);
            Shader.SetGlobalFloat(s_VegetationWindMicroFrequency, m_VegetationWind.microFrequency);
            if (windManager != null)
            {
                Shader.SetGlobalVector(s_VegetationWindDirection, new Vector4(Mathf.Sin(Mathf.Deg2Rad * windManager.singleWindGlobalDirection), Mathf.Cos(Mathf.Deg2Rad * windManager.singleWindGlobalDirection)));
            }
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            RefreshVegetationParams();
        }
#endif

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }

        public override bool SupportRunInEditor()
        {
            return true;
        }

        public SceneCaptureSettings sceneCaptureSettings
        {
            get => m_SceneCaptureSettings;
            set => m_SceneCaptureSettings = value;
        }
        
        public WindManager windManager
        {
            set
            {
                m_WindManager = value;
            }
            get
            {
                if (m_WindManager == null && EcosystemManager.instance != null)
                {
                    m_WindManager = EcosystemManager.instance.Get<WindManager>(EcosystemType.WindManager);
                }

                return m_WindManager;
            }
        }
    }
}
