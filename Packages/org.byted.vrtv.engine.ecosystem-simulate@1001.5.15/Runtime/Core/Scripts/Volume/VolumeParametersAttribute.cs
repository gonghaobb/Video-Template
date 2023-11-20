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


using UnityEngine;

namespace Matrix.EcosystemSimulate
{
    public class VolumeParametersAttribute : PropertyAttribute
    {
        public enum DisplayType
        {
            All = 0 ,
            Local = 1 , 
            Global = 2
        }

        private DisplayType m_DisplayType;

        public DisplayType displayType
        {
            get { return m_DisplayType; }
            set { m_DisplayType = value; }
        }
    }
}


