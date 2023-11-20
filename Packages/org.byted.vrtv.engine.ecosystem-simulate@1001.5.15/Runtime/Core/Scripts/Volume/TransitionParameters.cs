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

namespace Matrix.EcosystemSimulate
{
    [Serializable]
    public class TransitionParameters
    {
        [SerializeField] [Min(0.001f)]
        private float m_StartingTime = 10;
        [SerializeField] 
        private float m_StartingDelayTime = 2;
        [SerializeField] [Min(0.001f)]
        private float m_EndingTime = 10;
        [SerializeField] 
        private float m_EndingDelayTime = 2;
        
        public float startingTime
        {
            get { return m_StartingTime; }
            set { m_StartingTime = value; }
        }

        public float startingDelayTime
        {
            get { return m_StartingDelayTime; }
            set { m_StartingDelayTime = value; }
        }

        public float endingTime
        {
            get { return m_EndingTime; }
            set { m_EndingTime = value; }
        }

        public float endingDelayTime
        {
            get { return m_EndingDelayTime; }
            set { m_EndingDelayTime = value; }
        }
    }
}
