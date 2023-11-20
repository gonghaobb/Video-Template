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
    [CreateAssetMenu(fileName = "RainScreenEffectManager", menuName = "EcosystemSimulate/RainScreenEffectSimulate")]
    public partial class RainScreenEffectManager : SubEcosystem
    {
        private Transform m_Target;
        
        public override void SetTarget(Transform target)
        {
            m_Target = target;
        }
        
        public override bool SupportInCurrentPlatform()
        {
            return true;
        }
        
        public override void Enable()
        {
            if (!Application.isPlaying)
            {
                return;
            }
            
            RainDropInit();
            RainDropEnable();
        }

        public override void Disable()
        {
            RainDropDisable();
        }

        public override void Update()
        {
            if (ReferenceEquals(EcosystemManager.instance, null) || ReferenceEquals(EcosystemManager.instance.GetTarget(), null))
            {
                return;
            }
            
            RainDropUpdate();
        }

        public void SetActive(bool isActive)
        {
            RainDropSetActive(isActive);
        }
        
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public override void OnGUI()
        {
            base.OnGUI();
            RainDropOnGUI();
        }

        public override void OnValidate()
        {
            RainDropOnValidate();
        }
#endif
    }
}