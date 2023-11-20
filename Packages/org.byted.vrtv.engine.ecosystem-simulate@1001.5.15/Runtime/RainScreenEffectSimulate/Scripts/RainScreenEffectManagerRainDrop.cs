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
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace Matrix.EcosystemSimulate
{
    public partial class RainScreenEffectManager
    {
        [Serializable]
        public class DynamicParam
        {
            [Header("开启雨滴")] 
            public bool enableDynamicRainDrop = false;
            [Header("清晰区域取反（默认中间区域清晰）")]
            public bool isInvert = false;
            [Header("清晰区域")]
            [Range(0.0f, 1.0f)]
            public float unaffectedRange = 0.5f;
            [Header("延时")]
            public float delay = 0f;
            [Header("最大数量")]
            public int maxRainSpawnCount = 30;
            [Header("最短持续时间")]
            [Range(0f, 10.0f)]
            public float lifetimeMin = 0.6f;
            [Header("最长持续时间")]
            [Range(0f, 10.0f)]
            public float lifetimeMax = 1.4f;
            [Header("雨滴下滑距离（占屏幕高度比例）")]
            [Range(0.0f, 1f)]
            public float posYOffset = 0.1f;
            [Header("雨滴下滑距离曲线")]
            // [HideInInspector]
            public AnimationCurve posYOverLifetime;
            [Header("每秒最多雨滴数量")]
            [Range(0, 50f)]
            public int emissionRateMax = 5;
            [Header("每秒最少雨滴数量")]
            [Range(0, 50f)]
            public int emissionRateMin = 2;
            [Header("Scale-X 缩放最小值")]
            [Range(0.0f, 20f)]
            //[HideInInspector]
            public float sizeMinX = 0.75f;
            [Header("Scale-X 缩放最大值")]
            [Range(0.0f, 20f)]
            //[HideInInspector]
            public float sizeMaxX = 0.75f;
            [Header("Scale-Y 缩放最小值")]
            [Range(0.0f, 20f)]
            //[HideInInspector]
            public float sizeMinY = 0.75f;
            [Header("Scale-Y 缩放最大值")]
            [Range(0.0f, 20f)]
            //[HideInInspector]
            public float sizeMaxY = 0.75f;
            [Header("Scale 缩放曲线")]
            [HideInInspector]
            public AnimationCurve sizeOverLifetime;
            [Header("折射系数")]
            [HideInInspector]
            [Range(0.0f, 200.0f)]
            public float distortionValue;
            [Header("折射系数曲线")]
            [HideInInspector]
            public AnimationCurve distortionOverLifetime;
            [Header("模糊度")] 
            [HideInInspector]
            public float blurValue;
            [Header("模糊度曲线")] 
            [HideInInspector]
            public AnimationCurve blurOverLifetime;
            [Header("拖尾时间")] 
            [Range(0.5f, 10.0f)]
            public float trailLifeTime = 1;

            private Action m_OnValidate;
            
            public void RegistOnValidate(Action callback)
            {
                m_OnValidate = callback;
            }

            public void CopyFrom(DynamicParam rainDropParam, float ratio)
            {
                enableDynamicRainDrop = rainDropParam.enableDynamicRainDrop;
                isInvert = rainDropParam.isInvert;
                unaffectedRange = rainDropParam.unaffectedRange;
                delay = rainDropParam.delay;
                maxRainSpawnCount = (int)(rainDropParam.maxRainSpawnCount * ratio);
                lifetimeMin = rainDropParam.lifetimeMin * ratio;
                lifetimeMax = rainDropParam.lifetimeMax * ratio;
                posYOffset = rainDropParam.posYOffset * ratio;
                posYOverLifetime = rainDropParam.posYOverLifetime;
                emissionRateMax = Mathf.Max(1, (int)(rainDropParam.emissionRateMax * ratio));
                emissionRateMin = Mathf.Max(1, (int)(rainDropParam.emissionRateMin * ratio));
                sizeMinX = rainDropParam.sizeMinX * ratio;
                sizeMaxX = rainDropParam.sizeMaxX * ratio;
                sizeMinY = rainDropParam.sizeMinY * ratio;
                sizeMaxY = rainDropParam.sizeMaxY * ratio;
                sizeOverLifetime = rainDropParam.sizeOverLifetime;
                distortionValue = rainDropParam.distortionValue * ratio;
                distortionOverLifetime = rainDropParam.distortionOverLifetime;
                blurValue = rainDropParam.blurValue * ratio;
                blurOverLifetime = rainDropParam.blurOverLifetime;
                trailLifeTime = rainDropParam.trailLifeTime * ratio;
            }
            
            public void OnValidate()
            {
                if (maxRainSpawnCount < 0)
                {
                    maxRainSpawnCount = 0;
                }
                if (lifetimeMin > lifetimeMax)
                {
                    swap(ref lifetimeMin, ref lifetimeMax);
                }
                if (emissionRateMin > emissionRateMax)
                {
                    swap(ref emissionRateMin, ref emissionRateMax);
                }
                if (sizeMinX > sizeMaxX)
                {
                    swap(ref sizeMinX, ref sizeMaxX);
                }
                if (sizeMinY > sizeMaxY)
                {
                    swap(ref sizeMinY, ref sizeMaxY);
                }
                
                m_OnValidate?.Invoke();
            }
            
            private void swap<T>(ref T a, ref T b)
            {
                T tmp = a;
                a = b;
                b = tmp;
            }
        }
        
        [Serializable]
        public class StaticParam
        {
            [Header("开启静态雨滴")] 
            [ReadOnly, HideInInspector]
            public bool enableStaticRainDrop = true;
            [Header("折射系数")]
            [Range(0.0f, 200.0f)]
            public float distortionValue;
            [Header("模糊度")] 
            public float blurValue;

            private Action m_OnValidate;
            
            public void RegistOnValidate(Action callback)
            {
                m_OnValidate = callback;
            }

            public void CopyFrom(StaticParam staticRainDropParam, float ratio)
            {
                enableStaticRainDrop = staticRainDropParam.enableStaticRainDrop;
                distortionValue = staticRainDropParam.distortionValue * ratio;
                blurValue = staticRainDropParam.blurValue * ratio;
            }

            public void OnValidate()
            {
                m_OnValidate?.Invoke();
            }
        }

        [Serializable][ReloadGroup]
        public class CommonParam
        {
            [Header("降采样（移动平台建议>=2）"), Range(1, 10)]
            public int downSample = 2;
            [Header("雨滴网格")]
            public Mesh rainDropMesh = null;
            [Header("动态雨滴混合颜色")] 
            public Color overlayColor = new Color(0.73f, 0.73f, 0.73f, 0.45f);
            [Reload("Runtime/RainScreenEffectSimulate/Resource/Materials/RainScreenEffect.mat")]
            [Header("雨滴mat")]
            public Material rainDropMat = null;
            [Header("动态雨滴Relief图")]
            public Texture rainDropReliefMap;
            [Header("动态雨滴法线贴图")]
            public Texture rainDropNormalMap;
            [Header("静态雨滴混合颜色")] 
            public Color staticOverlayColor = new Color(0.7f, 0.86f, 1f, 0.25f);
            [Header("静态雨滴Relief图")]
            public Texture staticRainDropReliefMap;
            [Header("静态雨滴法线贴图")]
            public Texture staticRainDropNormalMap;
        }
        
        [Serializable][ReloadGroup]
        public class RainScreenEffectSettings
        {
            [Header("开启屏幕雨滴")] public bool enableRainScreenEffect = true;
            [Header("动态雨滴参数")] public DynamicParam dynamicParam = new DynamicParam();
            [Header("静态雨滴参数")] public StaticParam staticParam = new StaticParam();
            [Header("雨滴配置参数")] public CommonParam commonParam = new CommonParam();
        }
        
        [Header("屏幕雨滴参数")] [SerializeField] 
        private RainScreenEffectSettings m_Settings = new RainScreenEffectSettings();

        [Range(0 , 1)]
        [SerializeField]
        private float m_EffectStrength = 1;
        
        //Rain Drop Pass
        private RainScreenEffectPass m_ScreenEffectPass;

        public RainScreenEffectSettings settings => m_Settings;

        public float effectStrength
        {
            get => m_EffectStrength;
            set => m_EffectStrength = value;
        }

        private void RainDropInit()
        {
            if (m_ScreenEffectPass == null)
            {
                m_ScreenEffectPass = new RainScreenEffectPass(Camera.main, this);
            }
        }

        private void RainDropEnable()
        {
            if (m_ScreenEffectPass != null)
            {
                m_ScreenEffectPass.SetParam(m_Settings);
            }
        }

        private void RainDropUpdate()
        {
            if (m_ScreenEffectPass != null)
            {
                m_ScreenEffectPass.enablePass = enable && m_EffectStrength > 0 &&
                                                (m_Settings.dynamicParam.enableDynamicRainDrop ||
                                                 m_Settings.staticParam.enableStaticRainDrop);
            }
        }

        private void RainDropDisable()
        {
            if (m_ScreenEffectPass != null)
            {
                m_ScreenEffectPass.enablePass = false;
            }
            
            if (m_ScreenEffectPass != null && m_ScreenEffectPass.renderCamera != null)
            {
                m_ScreenEffectPass.Release();
            }
            
            m_ScreenEffectPass = null;
        }

        private void RainDropSetActive(bool isActive)
        {
            m_Settings.enableRainScreenEffect = isActive;
            RainDropOnValidate();
        }

        private void RainDropOnGUI()
        {
            if (m_ScreenEffectPass != null)
            {
                m_ScreenEffectPass.OnGUI();
            }
        }

        private void RainDropOnValidate()
        {
            m_Settings.dynamicParam.OnValidate();
            m_Settings.staticParam.OnValidate();
            if (m_ScreenEffectPass != null && m_ScreenEffectPass.enablePass)
            {
                m_ScreenEffectPass.isActive = m_Settings.enableRainScreenEffect;
                m_ScreenEffectPass.SetParam(m_Settings);
            }
        }
    }
}
