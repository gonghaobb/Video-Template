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
using UnityEngine;
using UnityEngine.Serialization;

namespace Matrix.EcosystemSimulate
{
    [Serializable]
    [CreateAssetMenu(fileName = "RainScreenEffectParameters" , 
        menuName = "EcosystemSimulate/ScreenEffectSimulate/RainScreenEffectParametersProfile")]
    public class RainScreenEffectParameters : EcosystemParameters
    {
        [Range(0 , 1)]
        [SerializeField]
        private float m_ScreenEffectStrength = 1f;

        [SerializeField]
        private RainScreenEffectManager.DynamicParam m_RainDropParam = new RainScreenEffectManager.DynamicParam();
        
        [SerializeField]
        private RainScreenEffectManager.StaticParam m_StaticRainDropParam = new RainScreenEffectManager.StaticParam();

        public float screenEffectStrength
        {
            get => m_ScreenEffectStrength;
            set => m_ScreenEffectStrength = value;
        }

        public RainScreenEffectManager.DynamicParam rainDropParam
        {
            get => m_RainDropParam;
            set => m_RainDropParam = value;
        }

        public RainScreenEffectManager.StaticParam staticRainDropParam
        {
            get => m_StaticRainDropParam;
            set => m_StaticRainDropParam = value;
        }

        public override bool IsSupportTransition()
        {
            return true;
        }

        public override EcosystemType GetEcosystemType()
        {
            return EcosystemType.RainScreenEffectManager;
        }

        public override void Scale(float scale)
        {
            m_ScreenEffectStrength *= scale;
        }
        
        public override void Blend(EcosystemParameters other)
        {
            RainScreenEffectParameters otherParameters = (RainScreenEffectParameters) other;
            if (otherParameters != null)
            {
                if (priority < otherParameters.priority || (priority == otherParameters.priority && m_ScreenEffectStrength < otherParameters.screenEffectStrength))
                {
                    m_ScreenEffectStrength = otherParameters.screenEffectStrength;
                    m_RainDropParam.CopyFrom(otherParameters.rainDropParam, m_ScreenEffectStrength);
                    m_StaticRainDropParam.CopyFrom(otherParameters.staticRainDropParam, m_ScreenEffectStrength);
                    
                    priority = otherParameters.priority;
                }
            }
        }

        private void OnValidate()
        {
            if (m_RainDropParam == null)
                return;

            m_RainDropParam.OnValidate();
        }
    }
}

