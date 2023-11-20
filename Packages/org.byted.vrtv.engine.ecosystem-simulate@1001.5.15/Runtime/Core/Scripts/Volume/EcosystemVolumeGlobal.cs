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
using UnityEngine;

namespace Matrix.EcosystemSimulate
{
    public partial class EcosystemVolume
    {
        [Serializable]
        public enum VolumeState 
        {
            Pending ,
            Running
        }

        [SerializeField]
        private VolumeState m_VolumeState = VolumeState.Running;
        [SerializeField] [Min(0.2f)] 
        private float m_SwitchSpeedMultiplier = 1; 

        [SerializeField] private TransitionParameters m_TransitionParameters;

        [SerializeField]
        private SerializableDictionary<EcosystemType, TransitionParameters> m_TransitionParametersDict =
            new SerializableDictionary<EcosystemType, TransitionParameters>();

        private Action m_Callback;

#if UNITY_EDITOR
        public static float s_TransitionTime = 0;
        public static float s_TransitionStartTime = 0;
#endif

        public void TryGetTransitionParameters(EcosystemType type, out TransitionParameters parameters)
        {
            if (!m_TransitionParametersDict.TryGetValue(type , out parameters))
            {
                parameters = new TransitionParameters();
                m_TransitionParametersDict.Add(type , parameters);
            }
        }

        public Vector2 MaxTransitionTimeToNextState()
        {
            float transitionTime = 0;
            float lateTransitionTime = 0;
            foreach (EcosystemParameters param in m_EcosystemParametersValueList)
            {
                if (param == null || !param.IsSupportTransition())
                {
                    continue;
                }
                
                TryGetTransitionParameters(param.GetEcosystemType() , out TransitionParameters tempTransitionParameters);
                
                if (m_VolumeState == VolumeState.Pending)
                {
                    if (param.GetStartType() == EcosystemParameters.SwitchType.Default)
                    {
                        transitionTime = Mathf.Max(transitionTime,
                            tempTransitionParameters.startingTime + tempTransitionParameters.startingDelayTime);
                    }
                    
                    if (param.GetStartType() == EcosystemParameters.SwitchType.Late)
                    {
                        lateTransitionTime = Mathf.Max(lateTransitionTime,
                            tempTransitionParameters.startingTime + tempTransitionParameters.startingDelayTime);
                    }
                }
                else
                {
                    if (param.GetEndType() == EcosystemParameters.SwitchType.Default)
                    {
                        transitionTime = Mathf.Max(transitionTime,
                            tempTransitionParameters.endingTime + tempTransitionParameters.endingDelayTime);
                    }
                    
                    if (param.GetEndType() == EcosystemParameters.SwitchType.Late)
                    {
                        lateTransitionTime = Mathf.Max(lateTransitionTime,
                            tempTransitionParameters.endingTime + tempTransitionParameters.endingDelayTime);
                    }
                }
            }

            transitionTime = Mathf.Max(transitionTime,
                m_VolumeState == VolumeState.Pending
                    ? m_TransitionParameters.startingTime + m_TransitionParameters.startingDelayTime
                    : m_TransitionParameters.endingTime + m_TransitionParameters.endingDelayTime);

            return new Vector2(transitionTime , lateTransitionTime) / m_SwitchSpeedMultiplier;
        }

        public void ResetState(VolumeState state)
        {
            if (!isGlobal)
            {
                return;
            }
            
            m_VolumeState = state;
        }

        private void EcosystemVolumeGlobalOnEnable()
        {
            ResetState(m_VolumeState);
            EcosystemVolumeManager.SetGlobalVolumeListChanged();
        }

        private void EcosystemVolumeGlobalOnDisable()
        {
            ResetState(m_VolumeState);
            EcosystemVolumeManager.SetGlobalVolumeListChanged();
        }

        private VolumeState NextState(VolumeState state)
        {
            return 1 - state;
        }
        
        public bool TryGetParametersWithoutTransitionFade(EcosystemType type, out EcosystemParameters parameters)
        {
            if (!isGlobal)
            {
                parameters = null;
                return false;
            }

            parameters = GetOrCreateParameters(type);

            int idx = IndexOfParameters(type);
            if (idx >= 0)
            {
                parameters.Clear();
                parameters.Override(m_EcosystemParametersValueList[idx]);
                parameters.Scale(weight);
                parameters.priority = m_Priority;
                return true;
            }
            
            return false;
        }

        public VolumeState volumeState
        {
            get { return m_VolumeState; }
            set
            {
                m_VolumeState = value;
                ResetState(m_VolumeState);
            }
        }

        public TransitionParameters transitionParameters
        {
            get { return m_TransitionParameters; }
            set { m_TransitionParameters = value; }
        }

        public float switchSpeedMultiplier
        {
            get { return m_SwitchSpeedMultiplier; }
            set { m_SwitchSpeedMultiplier = value; }
        }
    }
}
