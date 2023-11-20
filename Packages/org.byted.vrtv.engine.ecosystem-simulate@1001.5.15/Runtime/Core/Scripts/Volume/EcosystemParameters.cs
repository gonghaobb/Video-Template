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
    public abstract class EcosystemParameters : ScriptableObject
    {
        public enum SwitchType
        {
            Default = 0,
            Late = 2
        }
        
        protected float m_Priority = 0;

        public virtual float priority
        {
            get { return m_Priority; }
            set { m_Priority = value; }
        }
        
        public abstract EcosystemType GetEcosystemType();

        public virtual SwitchType GetStartType()
        {
            return SwitchType.Default;
        }
        
        public virtual SwitchType GetEndType()
        {
            return SwitchType.Default;
        }
        
        public virtual bool IsSupportTransition()
        {
            return false;
        }

        public virtual bool IsSupportDefaultParameters()
        {
            return true;
        }
        
        public virtual void Clear()
        {
            Scale(0);
            m_Priority = 0;
        }

        public virtual void Scale(float scale)
        {
            
        }

        public abstract void Blend(EcosystemParameters other);

        public virtual void Override(EcosystemParameters other)
        {
            m_Priority = other.priority;
            Blend(other);
        }

        public virtual void Mixed(EcosystemParameters l, EcosystemParameters r, float lScale , float rScale , float totalLerp)
        {
            Clear();
            l.Scale(1 - totalLerp);
            Blend(l);
            r.Scale(totalLerp);
            Blend(r);
        }
    }
}

