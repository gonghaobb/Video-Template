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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Matrix.EcosystemSimulate
{
    [CreateAssetMenu(fileName = "CloudShadow", menuName = "EcosystemSimulate/CloudShadow")]
    public class CloudShadowManager : SubEcosystem
    {
        [Header("云投影贴图(R:第一层强度,G:第一层噪声,B:第二层强度,A:第二层噪声)")]
        [SerializeField]
        [Reload("Runtime/CloudShadowSimulate/Resource/Textures/CloudShadowMap.tga")]
        private Texture2D m_CloudShadowMap;

        [Header("云投影颜色")]
        [SerializeField]
        private Color m_CloudShadowColor = Color.black;

        [Serializable]
        [ReloadGroup]
        public class CloudShadowParam
        {
            [Header("云投影大小")]
            public Vector2 cloudShadowMapSize = new Vector2(100, 100);

            [Header("云投影强度")]
            [Range(0f, 1f)]
            public float cloudShadowIntensity = 1f;

            [Header("云投影方向")]
            [Range(0f, 360f)]
            public float cloudShadowDirection = 180f;

            public bool cloudShadowUseWindDirection = false;

            [Header("云投影速度")]
            public Vector2 cloudShadowSpeed = new Vector2(1, 1);

            [Header("云投影噪声强度")]
            [Range(0f, 1f)]
            public float cloudShadowNoiseIntensity = 1f;

            [Header("云投影噪声速度")]
            public Vector2 cloudShadowNoiseSpeed = new Vector2(1, 1);

            [Header("云投影噪声Tiling")]
            public Vector2 cloudShadowNoiseTiling = new Vector2(1, 1);
        }

        [Header("第一层云阴影")]
        [SerializeField]
        public CloudShadowParam m_firstCloudShadowParam;

        [Header("第二层云阴影")]
        [SerializeField]
        public CloudShadowParam m_secondCloudShadowParam;

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }

        public override bool SupportRunInEditor()
        {
            return true;
        }
        

        public override void Enable()
        {
            UpdateCloudShadow();
        }

        public override void Disable()
        {
            Shader.SetGlobalFloat("_FirstCloudShadowIntensity", 0f);
            Shader.SetGlobalFloat("_SecondCloudShadowIntensity", 0f);
        }
        
        public override void Update()
        {
            base.Update();

#if UNITY_EDITOR
            UpdateCloudShadow();
#endif
        }

        private void UpdateCloudShadow()
        {
            Shader.SetGlobalTexture("_CloudShadowMap", m_CloudShadowMap);
            Shader.SetGlobalColor("_CloudShadowColor", m_CloudShadowColor);

            //first cloud shadow
            Shader.SetGlobalVector("_FirstCloudShadowMapTiling", new Vector4(1.0f / m_firstCloudShadowParam.cloudShadowMapSize.x, 1.0f / m_firstCloudShadowParam.cloudShadowMapSize.y,
                m_firstCloudShadowParam.cloudShadowNoiseTiling.x, m_firstCloudShadowParam.cloudShadowNoiseTiling.y));
            Shader.SetGlobalFloat("_FirstCloudShadowIntensity", m_firstCloudShadowParam.cloudShadowIntensity);
            Shader.SetGlobalVector("_FirstCloudShadowDirection", new Vector4(Mathf.Sin(Mathf.Deg2Rad * m_firstCloudShadowParam.cloudShadowDirection), Mathf.Cos(Mathf.Deg2Rad * m_firstCloudShadowParam.cloudShadowDirection)));
            Shader.SetGlobalFloat("_FirstCloudShadowUseWindDirection", m_firstCloudShadowParam.cloudShadowUseWindDirection ? 1 : 0);
            Shader.SetGlobalVector("_FirstCloudShadowSpeed", new Vector4(m_firstCloudShadowParam.cloudShadowSpeed.x, m_firstCloudShadowParam.cloudShadowSpeed.y,
                m_firstCloudShadowParam.cloudShadowNoiseSpeed.x, m_firstCloudShadowParam.cloudShadowNoiseSpeed.y));
            Shader.SetGlobalFloat("_FirstCloudShadowNoiseIntensity", m_firstCloudShadowParam.cloudShadowNoiseIntensity);

            //second cloud shadow
            Shader.SetGlobalVector("_SecondCloudShadowMapTiling", new Vector4(1.0f / m_secondCloudShadowParam.cloudShadowMapSize.x, 1.0f / m_secondCloudShadowParam.cloudShadowMapSize.y,
                m_secondCloudShadowParam.cloudShadowNoiseTiling.x, m_secondCloudShadowParam.cloudShadowNoiseTiling.y));
            Shader.SetGlobalFloat("_SecondCloudShadowIntensity", m_secondCloudShadowParam.cloudShadowIntensity);
            Shader.SetGlobalVector("_SecondCloudShadowDirection", new Vector4(Mathf.Sin(Mathf.Deg2Rad * m_secondCloudShadowParam.cloudShadowDirection), Mathf.Cos(Mathf.Deg2Rad * m_secondCloudShadowParam.cloudShadowDirection)));
            Shader.SetGlobalFloat("_SecondCloudShadowUseWindDirection", m_secondCloudShadowParam.cloudShadowUseWindDirection ? 1 : 0);
            Shader.SetGlobalVector("_SecondCloudShadowSpeed", new Vector4(m_secondCloudShadowParam.cloudShadowSpeed.x, m_secondCloudShadowParam.cloudShadowSpeed.y,
                m_secondCloudShadowParam.cloudShadowNoiseSpeed.x, m_secondCloudShadowParam.cloudShadowNoiseSpeed.y));
            Shader.SetGlobalFloat("_SecondCloudShadowNoiseIntensity", m_secondCloudShadowParam.cloudShadowNoiseIntensity);
        }
    }
}