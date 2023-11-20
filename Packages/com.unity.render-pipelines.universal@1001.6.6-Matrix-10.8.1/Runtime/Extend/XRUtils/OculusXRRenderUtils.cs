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

#if UNITY_ANDROID && !UNITY_EDITOR && PLATFORM_OCULUS_SUPPORTED
#define PLATFORM_OCULUS
using Unity.XR.Oculus;
#endif

namespace UnityEngine.Rendering
{
    public class OculusXRRenderUtils : XRRenderUtils
    {
#if PLATFORM_OCULUS
        public override void SetSystemFoveatedFeature(int level)
        {
            int oculusFoveatedLevel = level + 1;
            if (oculusFoveatedLevel != Utils.GetFoveationLevel())
            {
                Utils.SetFoveationLevel(oculusFoveatedLevel);
            }
        }
#endif
    }
}
