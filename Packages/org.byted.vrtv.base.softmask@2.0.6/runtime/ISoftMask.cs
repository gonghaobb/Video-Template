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

namespace UnityEngine.UI
{
    public interface ISoftMask
    {
        bool isAlive { get; }
        bool isMaskingEnabled { get; }
        // May return null.
        Material GetReplacement(Material original);
        void ReleaseReplacement(Material replacement);
        void ApplyParameters(Material renderMaterial);
        void UpdateTransformChildren(Transform transform);
    }
}
