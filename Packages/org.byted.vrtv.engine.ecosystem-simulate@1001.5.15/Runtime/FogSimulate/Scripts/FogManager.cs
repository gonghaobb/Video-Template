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
using UnityEngine.Rendering;

namespace Matrix.EcosystemSimulate
{
    [CreateAssetMenu(fileName = "FogSimulate", menuName = "EcosystemSimulate/FogSimulate")]
    public class FogManager : SubEcosystem
    {
        public enum CloudMotionMode : int
        {
            Rotation = 0,
            Direction,
        }
        
        [Serializable]
        public class CommonParam
        {
            [Header("绑定主光源")]
            public bool bindMainLight = false;
            [Header("自定义主光源方向")]
            public Vector3 sunRotation = new Vector3(45, -45, 0);
        }
        
        [Serializable]
        [ReloadGroup]
        public class FogNoiseParam
        {
            public bool enableFogNoise = false;
            [Range(0, 1f)]
            public float fogNoiseIntensity = 1;
            public Vector3 fogNoiseSpeed = new Vector3(0.1f, 0, 0);
            [Min(0.01f)]
            public float fogNoiseDistance = 200;
            [Min(0.01f)]
            public float fogNoiseScale = 100;
            [Reload("Runtime/FogSimulate/Resource/Textures/3DNoise.tga")]
            public Texture3D noise3D;
        }

        [Serializable]
        public class FogDirectionParam
        {
            [ColorUsageAttribute(false, true)] 
            public Color fogDirectionalColor = Color.white;
            [Range(0, 1f)] 
            public float directionalIntensity = 1;
            [Min(1)] 
            public int directionalFalloff = 1;
        }

        [Serializable]
        [ReloadGroup]
        public class FogParam
        {
            [Header("雾效基础参数")]
            public bool fogEnabled = true;
            [Range(0, 1f)]
            public float fogIntensity = 1f;
            [ColorUsageAttribute(true, true)]
            public Color fogColorStart = Color.gray;
            [ColorUsageAttribute(true, true)]
            public Color fogColorEnd;
            [Header("距离雾")]
            public float fogDistanceStart = 5f;
            public float fogDistanceEnd = 70f;
            [Header("高度雾效参数独立")]
            public bool fogHeightFactorIndependent = false;
            [Header("高度雾效参数修复(默认关闭以兼容旧场景)")]
            public bool fogHeightFactorFixed = false;
            // 高度控制，用的区间是0-1
            [Header("高度雾"),Range(0, 1)]
            public float fogHeightFactor = 0.5f;
            public float fogHeightStart = 0f;
            public float fogHeightEnd = 20.0f;
            [Header("雾噪声参数")]
            public FogNoiseParam fogNoiseParam = new FogNoiseParam();
            [Header("雾方向颜色参数")]
            public FogDirectionParam fogDirectionParam = new FogDirectionParam();
            [Header("强制刷新雾效(注意会有额外消耗,仅调试使用)")]
            public bool forceUpdate = false;
        }

        [Serializable]
        [ReloadGroup]
        public class SkyParam
        {
            [Header("开启自定义天空盒")] 
            public bool skyboxEnable = true;
            [Header("天空盒基础参数"), Range(0, 2)]
            public float exposure = 1;
            [Reload("Runtime/FogSimulate/Resource/Materials/SkyCubeMapMat.mat")]
            public Material skybox = null;
            [Header("天空盒背景")][Reload("Runtime/FogSimulate/Resource/Textures/Galaxy.jpg")]
            public Cubemap skyCube = null;
            [ColorUsageAttribute(false, true)]
            public Color skyColor= Color.white;
            [Range(0, 360)]
            public float rotateOffset = 0;
            [Header("云天空盒")][Reload("Runtime/FogSimulate/Resource/Textures/Cloud.png")]
            public Cubemap cloudCube = null;
            [ColorUsageAttribute(false, true)]
            public Color cloudColor = Color.white;           
            [Range(0, 360)]
            public float cloudRotateOffset = 0;
            [Header("云层移动参数")] 
            public CloudMotionMode cloudMotionMode = CloudMotionMode.Rotation;
            [Range(0,360)]
            public float cloudOrientation = 0f;
            public float cloudDirectionSpeed = 1f;
            public float cloudRotateSpeed = 1f;
            [NonSerialized]
            public Texture2D fogControl = null;
            [Header("天空盒受雾效影响梯度")]
            public Gradient fogGrad = null;
            [Range(0, 0.5f)]
            public float fogGradHeight = 0.5f;
            [Header("太阳参数")] 
            public bool enableSun = false;
            public Color sunColor = Color.white;
            public float sunDistance = 130;
            [Min(0)]
            public float sunPower = 2;

            [Header("原始天空盒材质(用于还原)")] 
            public Material originSkyboxMat = null;
            
            public const int h = 2;
            public const int w = 256;

            public SkyParam()
            {
                GradientColorKey[] gck;
                GradientAlphaKey[] gak;
                fogGrad = new Gradient();
                gck = new GradientColorKey[2];
                gck[0].color = Color.gray;
                gck[0].time = 0.0F;
                gck[1].color = Color.white;
                gck[1].time = 1.0F;
                gak = new GradientAlphaKey[2];
                gak[0].alpha = 1.0F;
                gak[0].time = 1;
                gak[1].alpha = 1;
                gak[1].time = 1.0F;
                fogGrad.SetKeys(gck, gak);
            }

            public void RefreshGradient()
            {
                if (fogControl == null)
                {
                    if (fogControl != null)
                        UnityEngine.Object.DestroyImmediate(fogControl, true);

                    fogControl = new Texture2D(w, SkyParam.h, TextureFormat.RGB24, false, QualitySettings.activeColorSpace == ColorSpace.Linear);
                    fogControl.wrapMode = TextureWrapMode.Clamp;
                    fogControl.filterMode = FilterMode.Bilinear;
                }

                float k = 1f / w;
                for (int i = 0; i < w; i++)
                {
                    for (int j = 0; j < SkyParam.h; j++)
                    {
                        fogControl.SetPixel(i, j, fogGrad.Evaluate(i * k));
                    }
                }
                fogControl.Apply();
            }

            public void ValidateGradient()
            {
                RefreshGradient();
            }

            public void ClearGradient()
            {
                if (fogControl != null)
                    UnityEngine.Object.DestroyImmediate(fogControl, true);

                fogControl = null;
            }
        }

        [Header("通用参数")]
        public CommonParam rawCommon = new CommonParam();

        [Header("高度雾参数")]
        public FogParam rawFog = new FogParam();

        [Header("天空参数")]
        public SkyParam rawSky = new SkyParam();

        private float m_ScrollFactor = 0;
        private float m_LastTime = 0;
        private bool m_OldSKyBoxEnable = false;
        private static readonly int s_SkyBoxCloudMotionParameters = Shader.PropertyToID("_SkyBoxCloudMotionParameters");
        private static readonly int s_Tex = Shader.PropertyToID("_Tex");
        private static readonly int s_Rotation = Shader.PropertyToID("_Rotation");
        private static readonly int s_CloudRotation = Shader.PropertyToID("_CloudRotation");
        private static readonly int s_SkyColor = Shader.PropertyToID("_SkyColor");
        private static readonly int s_CloudColor = Shader.PropertyToID("_CloudColor");
        private static readonly int s_CloudSampler = Shader.PropertyToID("_CloudSampler");
        private static readonly int s_RotationCloudSpeed = Shader.PropertyToID("_RotationCloudSpeed");
        private static readonly int s_FogHeight = Shader.PropertyToID("_FogHeight");
        private static readonly int s_FogWaveFrequency = Shader.PropertyToID("_FogWaveFrequency");
        private static readonly int s_FogGradHeight = Shader.PropertyToID("_FogGradHeight");
        private static readonly int s_FogOffsetHeight = Shader.PropertyToID("_FogOffsetHeight");
        private static readonly int s_FogGrad = Shader.PropertyToID("_FogGrad");
        private static readonly int s_Exposure = Shader.PropertyToID("_CustomExposure");

        public FogParam fog
        {
            get
            {
                return rawFog;
            }
        }
        public SkyParam sky
        {
            get
            {
                return rawSky;
            }
        }

        public CommonParam common
        {
            get
            {
                return rawCommon;
            }
        }

        public bool fogEnable
        {
            get => rawFog.fogEnabled;
            set
            {
                if (rawFog.fogEnabled != value)
                {
                    rawFog.fogEnabled = value;
                    Refresh();
                }
            }
        }
        
        public bool skyBoxEnable
        {
            get => rawSky.skyboxEnable;
            set
            {
                if (rawSky.skyboxEnable != value)
                {
                    rawSky.skyboxEnable = value;
                    Refresh();
                }
            }
        }

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }
        
        public override void Update()
        {
            if(Application.isEditor || fog.forceUpdate)
            {
                Refresh();
            }

            if (rawSky.skyboxEnable && rawSky.cloudMotionMode == CloudMotionMode.Direction)
            {
                UpdateScrollFactor();
            }

            m_OldSKyBoxEnable = rawSky.skyboxEnable;
        }

        private void UpdateScrollFactor()
        {
            m_ScrollFactor += sky.cloudMotionMode == CloudMotionMode.Direction
            ? sky.cloudDirectionSpeed * (Time.realtimeSinceStartup - m_LastTime) : 0.0f;
            float rot = Mathf.Deg2Rad * (sky.cloudOrientation + sky.cloudRotateOffset);
            if (RenderSettings.skybox != null)
            {
                RenderSettings.skybox.SetVector(s_SkyBoxCloudMotionParameters, new Vector4(-Mathf.Cos(rot), -Mathf.Sin(rot), m_ScrollFactor / 200.0f, 1));
            }
            m_LastTime = Time.realtimeSinceStartup;
        }

        public override bool SupportRunInEditor()
        {
            return true;
        }

        public override void OnValidate() 
        {
            if (EcosystemManager.instance != null && EcosystemManager.instance.Get<FogManager>(EcosystemType.FogManager) != null)
            { 
                Refresh();
            }
        }

        public void Refresh()
        {
            RefreshFog();
            
            RefreshSkyBox();
        }

        private void RefreshFog()
        {
            RenderSettings.fog = false;

            if (fog.fogEnabled)
            {
                Shader.EnableKeyword("CUSTOM_FOG");
            }
            else
            {
                Shader.DisableKeyword("CUSTOM_FOG");
            }
            
            if (fog.fogEnabled)
            {
                fog.fogDistanceEnd = Mathf.Max(fog.fogDistanceEnd, fog.fogDistanceStart + 0.01f);
                fog.fogHeightEnd = Mathf.Max(fog.fogHeightEnd, fog.fogHeightStart + 0.01f);

                float x = fog.fogHeightEnd;
                float y = 1.0f / (fog.fogHeightEnd - fog.fogHeightStart);
                float z = fog.fogHeightFactor;
                float w = fog.fogIntensity;
                Shader.SetGlobalVector(s_FogHeight, new Vector4(x, y, z, w));
                Vector3 fogDirectionalParam;
                if (common.bindMainLight && RenderSettings.sun != null)
                {
                    fogDirectionalParam = RenderSettings.sun.transform.rotation * Vector3.back;
                }
                else
                {
                    fogDirectionalParam = Quaternion.Euler(common.sunRotation) * Vector3.back;
                }
                float offset = fog.fogDistanceEnd - fog.fogDistanceStart;
                Shader.SetGlobalVector("_FogLinearParam",new Vector4(-1 / offset, fog.fogDistanceEnd / offset, 0, 0));
                
                Shader.SetGlobalColor("_FogDirectionalColor",
                    new Vector4(fog.fogDirectionParam.fogDirectionalColor.r,
                        fog.fogDirectionParam.fogDirectionalColor.g, fog.fogDirectionParam.fogDirectionalColor.b,
                        (fog.fogHeightFactorIndependent ? 0.5f : 0) + (fog.fogHeightFactorFixed ? 0.25f : 0)));
                Shader.SetGlobalVector("_FogDirectionalParam",
                    new Vector4(fogDirectionalParam.x, fogDirectionalParam.y, fogDirectionalParam.z,
                        fog.fogDirectionParam.directionalIntensity));
                Shader.SetGlobalVector("_FogNoiseParam0",
                    new Vector4(fog.fogNoiseParam.fogNoiseSpeed.x, fog.fogNoiseParam.fogNoiseSpeed.y,
                        fog.fogNoiseParam.fogNoiseSpeed.z, fog.fogNoiseParam.fogNoiseIntensity));
                Shader.SetGlobalVector("_FogNoiseParam1",
                    new Vector4(fog.fogNoiseParam.fogNoiseDistance, 1 / fog.fogNoiseParam.fogNoiseScale,
                        fog.fogNoiseParam.enableFogNoise ? 1 : 0, fog.fogDirectionParam.directionalFalloff));
                if (fog.fogNoiseParam.enableFogNoise)
                {
                    Shader.SetGlobalTexture("_FogNoise3D", fog.fogNoiseParam.noise3D);
                }
                else
                {
                    Shader.SetGlobalTexture("_FogNoise3D", Texture2D.blackTexture);
                }
                Shader.SetGlobalColor("_FogColorStart", fog.fogColorStart);
                Shader.SetGlobalColor("_FogColorEnd", fog.fogColorEnd);
            }
        }

        private void RefreshSkyBox()
        {
            if (!sky.skyboxEnable)
            {
                if (sky.skyboxEnable != m_OldSKyBoxEnable)
                {
                    RenderSettings.skybox = sky.originSkyboxMat;
                }

                return;
            }

            if (sky.originSkyboxMat == null && RenderSettings.skybox != sky.skybox)
            {
                sky.originSkyboxMat = RenderSettings.skybox;
            }
            RenderSettings.skybox = sky.skybox;
            if (RenderSettings.skybox != null)
            {
                RenderSettings.skybox.SetTexture(s_Tex, sky.skyCube);
                RenderSettings.skybox.SetFloat(s_Rotation, sky.rotateOffset);
                RenderSettings.skybox.SetFloat(s_CloudRotation, sky.cloudRotateOffset);
                RenderSettings.skybox.SetColor(s_SkyColor, sky.skyColor * (common.bindMainLight && RenderSettings.sun != null ? RenderSettings.sun.intensity : 1));
                if (sky.cloudCube != null)
                {
                    RenderSettings.skybox.EnableKeyword("_CLOUD_MAP");
                    RenderSettings.skybox.SetColor(s_CloudColor, sky.cloudColor * (common.bindMainLight && RenderSettings.sun != null ? RenderSettings.sun.intensity : 1));
                    if (RenderSettings.skybox.HasProperty(s_CloudSampler))
                    {
                        RenderSettings.skybox.SetTexture(s_CloudSampler, sky.cloudCube);

                        RenderSettings.skybox.SetFloat(s_RotationCloudSpeed, sky.cloudRotateSpeed);

                        if (sky.cloudMotionMode == CloudMotionMode.Direction)
                        {
                            RenderSettings.skybox.EnableKeyword("SKY_CLOUD_DIRECTIONAL_MOTION");
                        }
                        else
                        {
                            RenderSettings.skybox.DisableKeyword("SKY_CLOUD_DIRECTIONAL_MOTION");
                        }
                    }
                }
                else
                {
                    RenderSettings.skybox.DisableKeyword("_CLOUD_MAP");
                }
                
                if (fog.fogEnabled)
                {
                    sky.ValidateGradient();

                    RenderSettings.skybox.SetTexture(s_FogGradHeight, sky.fogControl);
                    RenderSettings.skybox.SetFloat(s_FogOffsetHeight, 0.5f - sky.fogGradHeight);
                }
                else
                {
                    sky.ClearGradient();
                    RenderSettings.skybox.SetTexture(s_FogGradHeight, Texture2D.whiteTexture);
                }

                Color sunColor;
                Vector3 fogDirectionalParam = Vector3.zero;
                if (common.bindMainLight && RenderSettings.sun != null)
                {
                    sunColor = RenderSettings.sun.color;
                    fogDirectionalParam = RenderSettings.sun.transform.rotation * Vector3.back;
                }
                else
                {
                    sunColor = sky.sunColor;
                    fogDirectionalParam = Quaternion.Euler(common.sunRotation) * Vector3.back;
                } 

                RenderSettings.skybox.SetColor("_SunColor", sunColor);
                RenderSettings.skybox.SetFloat("_SunDistance", sky.sunDistance);
                RenderSettings.skybox.SetFloat("_SunPower", sky.sunPower);
                RenderSettings.skybox.SetVector("_SunDirection", fogDirectionalParam);
                if (sky.enableSun)
                {
                    RenderSettings.skybox.EnableKeyword("_SUN_ENABLE");
                }
                else
                {
                    RenderSettings.skybox.DisableKeyword("_SUN_ENABLE");
                }
                
                SetExposureInstensity();
            }
        }

        public void SetExposureInstensity()
        {
            RenderSettings.skybox.SetFloat(s_Exposure, sky.exposure);
        }

        public override void Disable()
        {
            Shader.SetGlobalVector(s_FogHeight, new Vector4(0, 0, 0, 0));
            Shader.DisableKeyword("CUSTOM_FOG");
#if UNITY_EDITOR
            if (rawSky.skyboxEnable)
            {
                RenderSettings.skybox = sky.originSkyboxMat;
            }
#endif
        }

        public override void Enable()
        {
            RenderSettings.fog = false;

            Refresh();
        }
    }
}
