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

#if !PICO_UGUI
using System;
using UnityEngine.EventSystems;
namespace UnityEngine.UI
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MaskableGraphic))]
    public class SoftMaskMaterialEmptyModifier : UIBehaviour, IMaterialModifier
    {
        public virtual Material GetModifiedMaterial(Material baseMaterial)
        {
            return baseMaterial;
        }
    }
}
#endif
