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
    [Serializable, ReloadGroup]
    public abstract class SubEcosystem : ScriptableObject
    {
        [SerializeField]
        [Header("是否启用")]
        protected bool m_Enable = true;
        
        private const string PACKAGE_ROOT = "Packages/org.byted.vrtv.engine.ecosystem-simulate";
        private bool m_IsOpen = false;

        [SerializeField][HideInInspector]
        protected bool m_EnableGizmos = false;
        public bool enableGizmos
        {
            get => m_EnableGizmos;
            set => m_EnableGizmos = value;
        }

        public void Create()
        {
            Reset();
        }

        public abstract bool SupportInCurrentPlatform();

        public virtual bool SupportRunInEditor()
        {
            return false;
        }
        
        public virtual void Enable()
        {
        
        }

        public virtual void Reset()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, PACKAGE_ROOT);
#endif
        }
        public virtual void Disable() { }
        public virtual void Update() { }
        public virtual void LateUpdate() { }
        public virtual void FixedUpdate() { }
        public virtual void OnDrawGizmos(){ }
        public virtual void SetTarget(Transform target) { }
        public virtual void OnGUI(){}
        
        public virtual void OnValidate()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, PACKAGE_ROOT);
#endif
        }

        public static GameObject FindObjectByName(string name)
        {
            Transform[] trans = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (Transform t in trans)
            {
                if (t.hideFlags == HideFlags.None && t.name == name)
                {
                    return t.gameObject;
                }
            }
            return null;
        }
        
        public virtual void OnChangedQuality(EcosystemManager.QualityLevel current , EcosystemManager.QualityLevel next)
        {
            
        }

#if UNITY_EDITOR
        private bool m_LastEnable = true;
        public void CheckEnabled()
        {
            if (m_LastEnable != m_Enable)
            {
                if (Application.isPlaying ||
                    !Application.isPlaying && SupportRunInEditor())
                {
                    if (m_Enable)
                    {
                        Enable();
                    }
                    else
                    {
                        Disable();
                    }
                }
                m_LastEnable = m_Enable;
            }
        }
#endif
        
        public bool isOpen
        {
            set
            {
                m_IsOpen = value;
            }
            get
            {
                return m_IsOpen; 
            }
        }

        public virtual bool enable 
        {
            get => m_Enable;
            set
            {
                if (m_Enable != value)
                {
                    m_Enable = value;
#if UNITY_EDITOR
                    if (Application.isPlaying ||
                        !Application.isPlaying && SupportRunInEditor())
                    {
#endif
                        if (m_Enable)
                        {
                            Enable();
                        }
                        else
                        {
                            Disable();
                        }
#if UNITY_EDITOR
                    }
#endif
                }
            }
        }
    }
}

/*
    生态系统模块开发注意事项

1、通过EcosystemManager获取mainCamera，不需要自己在代码中调用Camera.main，如果mainCamera为空，则不会执行生命周期函数，大多数情况下无需手动判断为空
2、对平台有限制的所有模块必须重载SupportInCurrentPlatform以做平台检测，检测失败则不会执行生命周期函数，各个模块无需在内部判断支持CS等，如果内部有判断则需要提供不支持的情况下的解决方案
public override bool SupportInCurrentPlatform()
{
    return SystemInfo.supportsTessellationShaders && SystemInfo.supportsComputeShaders;
}
3、对于OnGUI/OnDrawGizmos的重载方法不用自己加宏进行限定执行，管理器会统一处理
4、自己模块内部OnDrawGizmos必须要判定自己模块的enableGizmos才能执行绘制
5、所有模块都应当实现自己的m_Enable，循环会自动判断，调用enable属性进行控制，会自动调用OnEnable和OnDisable
6、子模块可以重写SupportRunInEditor以标识该模块是否支持编辑器模式下运行
*/