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

namespace Matrix.EcosystemSimulate
{
    [CreateAssetMenu(fileName = "HeightMapSimulate", menuName = "EcosystemSimulate/HeightMapSimulate/HeightMapManager")]
    public partial class HeightMapManager : SubEcosystem
    {
        public override void Enable()
        {
            DynamicHeightMapEnable();
        }

        public override void Disable()
        {
            DynamicHeightMapDisable();
        }

        public override void Update()
        {
            DynamicHeightMapUpdate();
        }

        public override void OnGUI()
        {
            DynamicHeightMapOnGUI();
        }

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }
    }
}