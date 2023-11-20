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
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Scripting;
[assembly: AlwaysLinkAssembly]
[assembly: Preserve]
namespace Matrix.EcosystemSimulate
{
    public enum EcosystemType
    {
        //底层模块
        HeightMapManager = 0,
        WindManager = 1,

        //视觉模块
        SnowManager = 4,
        SunLightManager = 7,
        FogManager = 8,
        RainManager = 10,
        PostEffectManager = 36,
        MistVFXManager = 11,
        CloudShadowManager = 12,
        RainScreenEffectManager = 9,
        VegetationManager = 13,
        WaterManager = 14,
        SceneLightManager = 15,
    }
    
    public enum WeatherEffectLevel
    {
        HIGH,
        MEDIUM,
        LOW,
    }
    
    [Serializable]
    public class SubEcosystemDict : SerializableDictionary<EcosystemType, SubEcosystem> { }

    [ExecuteAlways]
    public partial class EcosystemManager : MonoBehaviour
    {
        private const string PACKAGE_ROOT = "Packages/org.byted.vrtv.engine.ecosystem-simulate";
        
        public enum QualityLevel
        {
            Ultra = 3,
            High = 2, 
            Medium = 1,
            Low = 0
        }
        [SerializeField]
        private Transform m_Target = null;

        [SerializeField] 
        private QualityLevel m_QualityLevel = QualityLevel.High;

        [SerializeField] 
        private Camera m_Camera = null;

        public Camera mainCamera
        {
            get
            {
                if (m_Camera == null)
                {
                    m_Camera = Camera.main;
                }

                return m_Camera;
            }
        }

        [SerializeField]
        private SubEcosystemDict m_SubEcosystemDict = new SubEcosystemDict();

        public SubEcosystemDict subEcosystemDict
        {
            get { return m_SubEcosystemDict; }
        }

        private bool m_IsQuitting = false;
        //保存当前帧的Enable列表，避免重复Enable和Disable
        private static List<EcosystemManager> s_EnableList = new List<EcosystemManager>();
        
        private static List<EcosystemManager> s_InstanceList = new List<EcosystemManager>();
        
        public static EcosystemManager instance
        {
            get
            {
                if (s_InstanceList.Count > 0)
                {
                    return s_InstanceList.Last();
                }

                return null;
            }
        }
        
        private readonly List<SubEcosystem> m_UpdateList = new List<SubEcosystem>();
        private Vector3 m_Offset = Vector3.zero;

        public Vector3 offset
        {
            get => m_Offset;
            set => m_Offset = value;
        }

        public QualityLevel qualityLevel
        {
            get { return m_QualityLevel; }
            set
            {
                if (m_QualityLevel != value)
                {
                    OnChangedQuality(m_QualityLevel , value);
                    m_QualityLevel = value;
                }
            }
        }

        private void OnChangedQuality(QualityLevel current , QualityLevel next)
        {
            // Shader.DisableKeyword("ESS_QUALITY_ULTRA");
            // Shader.DisableKeyword("ESS_QUALITY_HIGH");
            // Shader.DisableKeyword("ESS_QUALITY_MEDIUM");
            // Shader.DisableKeyword("ESS_QUALITY_LOW");
            // switch (next)
            // {
            //     case QualityLevel.Ultra:
            //         Shader.EnableKeyword("ESS_QUALITY_ULTRA");
            //         break;
            //     case QualityLevel.High:
            //         Shader.EnableKeyword("ESS_QUALITY_HIGH");
            //         break;
            //     case QualityLevel.Medium:
            //         Shader.EnableKeyword("ESS_QUALITY_MEDIUM");
            //         break;
            //     case QualityLevel.Low:
            //         Shader.EnableKeyword("ESS_QUALITY_LOW");
            //         break;
            //     default:
            //         throw new ArgumentOutOfRangeException();
            // }
            
            foreach (SubEcosystem subEcosystem in m_UpdateList)
            {
                subEcosystem.OnChangedQuality(current , next);
            }
        }

        public void RefreshUpdateList()
        {
            m_UpdateList.Clear();
            using IEnumerator<SubEcosystem> map = m_SubEcosystemDict.Values.GetEnumerator();
            while (map.MoveNext())
            {
                if (map.Current != null)
                {
                    m_UpdateList.Add(map.Current);
                }
            }
        }
        
        public void SetTarget(Transform target)
        {
            if (target != null)
            {
                using IEnumerator<SubEcosystem> map = m_SubEcosystemDict.Values.GetEnumerator();
                while (map.MoveNext())
                {
                    if (map.Current != null)
                    {
                        map.Current.SetTarget(target);
                    }
                }
                m_Target = target;
                var camera = target.GetComponent<Camera>();
                if (camera != null)
                {
                    m_Camera = camera;
                }
            }
        }

        public void SetCamera(Camera camera)
        {
            m_Camera = camera;
        }

        public void OnEnable()
        {
            s_EnableList.Add(this);
        }

        private void OnEnableInternal()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, PACKAGE_ROOT);
#endif
            
            if (m_Target == null)
            {
                m_Target = transform;
            }
            else
            {
                SetTarget(m_Target);
            }

            m_UpdateList.Clear();
            SoundInit();
            using IEnumerator<SubEcosystem> map = m_SubEcosystemDict.Values.GetEnumerator();
            while (map.MoveNext())
            {
                if (map.Current != null)
                {
                    if (map.Current.SupportInCurrentPlatform())
                    {
#if UNITY_EDITOR
                        if (Application.isPlaying ||
                            !Application.isPlaying && map.Current.SupportRunInEditor())
                        {
                            map.Current.Enable();
                            m_UpdateList.Add(map.Current);
                        }
#else
                        map.Current.Enable();
                        m_UpdateList.Add(map.Current);
#endif
                    }
                    else
                    {
                        Debugger.LogError($"{map.Current.name} does not support in current platform.");
                    }
                }
            }
            
            OnChangedQuality(qualityLevel , qualityLevel);
        }
        
        public void OnDisable()
        {
            s_EnableList.Remove(this);
            
            if (instance == this)//理论上只有栈顶的元素处于Enable状态
            {
                OnDisableInternal();
                s_InstanceList.Remove(this);//移除当前实例
                
                //激活新实例
                if (instance != null && m_IsQuitting == false)
                {
                    instance.OnEnableInternal();
                }
            }
            else
            {
                s_InstanceList.Remove(this);//移除当前实例
            }
            
            //移除空引用或者已经disable的实例
            s_InstanceList.RemoveAll((manager) => { return manager == null || manager.enabled == false; });
        }

        private void OnDisableInternal()
        {
            using IEnumerator<SubEcosystem> map = m_SubEcosystemDict.Values.GetEnumerator();
            while (map.MoveNext())
            {
                if (map.Current != null)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying ||
                        !Application.isPlaying && map.Current.SupportRunInEditor())
                    {
                        map.Current.Disable();
                    }
#else
                    map.Current.Disable();
#endif
                }
            }
        }

        private void OnApplicationQuit()
        {
            m_IsQuitting = true;
        }
        
        //保留销毁接口，只用作清除集合
        public void OnDestroy()
        {
            m_UpdateList.Clear();
            m_SubEcosystemDict.Clear();
        }
        
        protected void Update()
        {
            //添加当前帧激活的实例列表
            if (s_EnableList.Count > 0)
            {
                //移除空引用或者已经disable的实例
                s_EnableList.RemoveAll((manager) => { return manager == null || manager.enabled == false; });
                if (s_EnableList.Count > 0)
                {
                    instance?.OnDisableInternal();
                    s_InstanceList.AddRange(s_EnableList);
                    s_EnableList.Clear();
                    instance.OnEnableInternal(); //激活栈顶实例,下面重复添加的实例没必要激活
                }
            }
            
            if (instance != this)
            {
                return;
            }
            
            if (m_Target != null)
            {
                SoundUpdate();
                EcosystemVolumeManager.Update();
                foreach (SubEcosystem t in m_UpdateList)
                {
#if UNITY_EDITOR
                    if (t != null)
                    {
                        t.CheckEnabled();
                    }
#endif
                    if (t != null && t.enable)
                    {
#if UNITY_EDITOR
                        if (Application.isPlaying ||
                            !Application.isPlaying && t.SupportRunInEditor())
                        {
                            t.Update();
                        }
#else
                        if (mainCamera != null)
                        {
                            t.Update();
                        }
#endif
                    }
                }
            }
        }

        protected void LateUpdate()
        {
            if (instance != this)
            {
                return;
            }
            
            if (m_Target != null)
            {
                foreach (SubEcosystem t in m_UpdateList)
                {
                    if (t != null && t.enable)
                    {
#if UNITY_EDITOR
                        if (Application.isPlaying ||
                            !Application.isPlaying && t.SupportRunInEditor())
                        {
                            t.LateUpdate();
                        }
#else
                        if (mainCamera != null)
                        {
                            t.LateUpdate();
                        }
#endif
                    }
                }
            }
        }

        protected void FixedUpdate()
        {
            if (instance != this)
            {
                return;
            }
            
            if (m_Target != null)
            {
                foreach (SubEcosystem t in m_UpdateList)
                {
                    if (t != null && t.enable)
                    {
#if UNITY_EDITOR
                        if (Application.isPlaying ||
                            !Application.isPlaying && t.SupportRunInEditor())
                        {
                            t.FixedUpdate();
                        }
#else
                        if (mainCamera != null)
                        {
                            t.FixedUpdate();
                        }
#endif
                    }
                }
            }
        }
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        protected void OnGUI()
        {
            foreach (SubEcosystem t in m_UpdateList)
            {
                if (t != null && t.enable)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying ||
                        !Application.isPlaying && t.SupportRunInEditor())
                    {
                        t.OnGUI();
                    }
#else
                    t.OnGUI();
#endif
                }
            }
        }
#endif
        
#if UNITY_EDITOR 
        protected void OnDrawGizmos()
        {
            if (instance != this)
            {
                return;
            }

            using IEnumerator<SubEcosystem> map = m_SubEcosystemDict.Values.GetEnumerator();
            while (map.MoveNext())
            {
                if (map.Current != null)
                {
                    if (Application.isPlaying ||
                        !Application.isPlaying && map.Current.SupportRunInEditor())
                    {
                        if (map.Current.enableGizmos)
                        {
                            map.Current.OnDrawGizmos();
                        }
                    }
                }
            }
        }
#endif
        
        public Transform GetTarget()
        {
            return m_Target;
        }

        public SubEcosystem Get(EcosystemType type)
        {
            if (m_SubEcosystemDict.TryGetValue(type, out SubEcosystem sub))
            {
                return sub; 
            }
            return null;
        }

        public T Get<T>(EcosystemType type) where T : SubEcosystem
        {
            if (m_SubEcosystemDict.TryGetValue(type, out SubEcosystem sub))
            {
                return (T)sub;
            }
            return null;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public void Set(EcosystemType type, SubEcosystem subEco)
        {
            if (m_SubEcosystemDict.TryGetValue(type, out SubEcosystem preSub))
            {
                if (preSub != null)
                {
                    if (Application.isPlaying ||
                        !Application.isPlaying && preSub.SupportRunInEditor())
                    {
                        preSub.Disable();
                    }
                    
                    m_UpdateList.Remove(preSub);//从UpdateList移除原有的sub
                }
            }
            m_SubEcosystemDict[type] = subEco;
            if (subEco != null)
            {
                if (Application.isPlaying ||
                    !Application.isPlaying && subEco.SupportRunInEditor())
                {
                    subEco.SetTarget(m_Target);
                    //添加到当前实例才马上激活
                    if (instance == this)
                    {
                        subEco.Enable();
                    }
                }

                //UpdateList添加新sub
                if (subEco.SupportInCurrentPlatform())
                {
#if UNITY_EDITOR
                    if (Application.isPlaying ||
                        !Application.isPlaying && subEco.SupportRunInEditor())
                    {
                        m_UpdateList.Add(subEco);
                    }
#else
                    m_UpdateList.Add(subEco);
#endif
                }
            }
        }

        public void Remove(EcosystemType type, bool bRemoveItem = true)
        {
            if (m_SubEcosystemDict.TryGetValue(type, out SubEcosystem preSub))
            {
                if (preSub != null)
                {
                    if (Application.isPlaying ||
                        !Application.isPlaying && preSub.SupportRunInEditor())
                    {
                        preSub.Disable();
                    }

                    m_UpdateList.Remove(preSub);//从UpdateList移除原有的sub
                }
                if (bRemoveItem)
                {
                    m_SubEcosystemDict.Remove(type);
                }
                else
                {
                    m_SubEcosystemDict[type] = null;
                }
            }
        }

        public bool Has(Type type)
        {
            foreach (KeyValuePair<EcosystemType, SubEcosystem> component in m_SubEcosystemDict)
            {
                if (GetType(component.Key.ToString()).Equals(GetType(type.ToString())))
                {
                    return true;
                }
            }

            return false;
        }

        public void CreateSubManager(Type type)
        {
            string strAll = type.ToString();
            string strType = GetType(strAll);
            EcosystemType eType = (EcosystemType)Enum.Parse(typeof(EcosystemType), strType, true);
            Set(eType, null);
        }

        private string GetType(string strAll)
        {
            int lastPos = strAll.LastIndexOf('.');
            return strAll.Substring(lastPos + 1, strAll.Length - lastPos - 1);
        }
        
                
        //外部接口，用于手动初始化
        public void Init()
        {
            OnEnable();
        }
        
        //外部接口，用于手动反初始化
        public void UnInit()
        {
            OnDisable();
            OnDestroy();
        }
    }
}
