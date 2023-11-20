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
//      Procedural LOGO:https://www.shadertoy.com/view/WdyfDm 
//                  @us:https://kdocs.cn/l/sawqlPuqKX7f
//
//      The team was set up on September 4, 2019.
//

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Matrix.EcosystemSimulate
{
    public partial class EcosystemVolume : MonoBehaviour
    {
        public static ReadOnlyDictionary<EcosystemType, Type> s_ParametersConfigDict =
            new ReadOnlyDictionary<EcosystemType, Type>(new Dictionary<EcosystemType, Type>
            {
                //{EcosystemType.WindManager, typeof(WindParameters)},
                //{EcosystemType.SandManager, typeof(SandParameters)},
                //{EcosystemType.TrailManager, typeof(TrailParameters)},
                //{EcosystemType.ThermalDistortManager, typeof(ThermalDistortParameters)},
                //{EcosystemType.RainVFXManager, typeof(RainVFXParameters)},
                //{EcosystemType.SnowVFXManager, typeof(SnowVFXParameters)},
                //{EcosystemType.EnvironmentalLightManager, typeof(EnvironmentalLightParameters)},
                //{EcosystemType.MistVFXManager, typeof(MistVFXParameters)},
                //{EcosystemType.EnvironmentInteractionManager, typeof(EnvironmentInteractionParameters)},
                //{EcosystemType.WetSurfaceManager, typeof(WetSurfaceParameters)},
                //{EcosystemType.SnowSurfaceManager, typeof(SnowSurfaceParameters)},
				//{EcosystemType.LightningManager, typeof(LightningParameters)},
     			//{EcosystemType.RainScreenEffectManager, typeof(RainScreenEffectParameters)},
       			//{EcosystemType.WaterManager, typeof(WaterParameters)},
       			//{EcosystemType.BurnManager, typeof(BurnParameters)},
       			//{EcosystemType.AtmosphereSkyManager, typeof(AtmosphereSkyParameters)},
                //{EcosystemType.SnowScreenEffectManager, typeof(SnowScreenEffectParameters)},
                //{EcosystemType.EnvironmentVFXManager, typeof(EnvironmentVFXParameters)},
            });
        
        public enum VolumeType
        {
            Local ,
            Global
        }

        [SerializeField] private VolumeType m_VolumeType = VolumeType.Local;
        [SerializeField] private float m_Weight = 1;
        [SerializeField] private float m_Priority = 0;
        [SerializeField] private List<EcosystemParameters> m_EcosystemParametersValueList = new List<EcosystemParameters>();
        
        private readonly Dictionary<EcosystemType, EcosystemParameters> m_TempEcosystemParametersDict =
            new Dictionary<EcosystemType, EcosystemParameters>();

        private VolumeType m_LastVolumeType = VolumeType.Global;

        private void OnEnable()
        {
            m_LastVolumeType = m_VolumeType;
            
            if (m_VolumeType == VolumeType.Local)
            {
                EcosystemVolumeLocalOnEnable();
            }
            else
            {
                EcosystemVolumeGlobalOnEnable();
            }
            
            EcosystemVolumeManager.RegisterVolume(this);
        }

        private void OnDisable()
        {
            if (m_VolumeType == VolumeType.Local)
            {
                EcosystemVolumeLocalOnDisable();
            }
            else
            {
                EcosystemVolumeGlobalOnDisable();
            }
            
            EcosystemVolumeManager.UnRegisterVolume(this);
        }

        private void Update()
        {
            if (m_VolumeType == VolumeType.Local)
            {
                EcosystemVolumeLocalUpdate();
            }
        }

        public bool TryGetParameters<T>(EcosystemType type , out T parameters) where T : EcosystemParameters
        {
            int idx = IndexOfParameters(type);
            if (idx >= 0)
            {
                parameters = m_EcosystemParametersValueList[idx] as T;
                if (parameters == null)
                {
                    return false;
                }
                parameters.priority = m_Priority;
                return true;
            }
            
            parameters = null;
            return false;
        }

        public int IndexOfParameters(EcosystemType type)
        {
            for (int i = 0; i < ecosystemParametersValueList.Count; i++)
            {
                if (ecosystemParametersValueList[i] != null && 
                    ecosystemParametersValueList[i].GetEcosystemType() == type)
                {
                    return i;
                }
            }

            return -1;
        }
        
        public bool ContainsParameters(EcosystemType type)
        {
            foreach (EcosystemParameters t in ecosystemParametersValueList)
            {
                if (t != null && t.GetEcosystemType() == type)
                {
                    return true;
                }
            }

            return false;
        }

        public bool AddParametersSettings(EcosystemParameters parameters = null)
        {
            if (parameters == null || ContainsParameters(parameters.GetEcosystemType()))
            {
                return false;
            }
            
            m_EcosystemParametersValueList.Add(parameters);
            return true;
        }
        
        private void ChangeVolumeType(VolumeType from , VolumeType to)
        {
            if (from == to || from != m_VolumeType || to == m_VolumeType)
            {
                return;
            }

            if (from == VolumeType.Local)
            {
                EcosystemVolumeLocalOnDisable();
            }
            else
            {
                EcosystemVolumeGlobalOnDisable();
            }

            if (to == VolumeType.Local)
            {
                EcosystemVolumeLocalOnEnable();
            }
            else
            {
                EcosystemVolumeGlobalOnEnable();
            }
        }
        
        private EcosystemParameters GetOrCreateParameters(EcosystemType type)
        {
            if (!m_TempEcosystemParametersDict.TryGetValue(type, out EcosystemParameters parameters))
            {
                parameters = ScriptableObject.CreateInstance(s_ParametersConfigDict[type]) as EcosystemParameters;
                m_TempEcosystemParametersDict[type] = parameters;
            }

            return parameters;
        }
        
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            if (m_LastVolumeType != m_VolumeType)
            {
                ChangeVolumeType(m_LastVolumeType , m_VolumeType);
                m_LastVolumeType = m_VolumeType;
            }
        }
        
        public float weight
        {
            get { return m_Weight; }
            set { m_Weight = value; }
        }

        public List<EcosystemParameters> ecosystemParametersValueList
        {
            get { return m_EcosystemParametersValueList; }
        }
        
        public VolumeType volumeType
        {
            get { return m_VolumeType; }
            set
            {
                ChangeVolumeType(m_VolumeType , value);
                m_VolumeType = value;
            }
        }
        
        public float priority
        {
            get { return m_Priority; }
            set
            {
                m_Priority = value;
                EcosystemVolumeManager.ReSortVolumeListByPriority();
            }
        }

        public bool isGlobal
        {
            get
            {
                return m_VolumeType == VolumeType.Global;
            }
        }
    }
}
