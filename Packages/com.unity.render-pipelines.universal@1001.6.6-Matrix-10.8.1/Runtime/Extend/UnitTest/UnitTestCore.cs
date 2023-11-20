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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    public static class UnitTestCore
    {
        public class UnitTestException : Exception
        {
            public UnitTestException()
            {
            }

            public UnitTestException(string message) : base(message)
            {
            }

            public UnitTestException(string message, Exception inner) : base(message, inner)
            {
            }
        }
        
        public static void Failed(object message)
        {
            Debug.LogException(new UnitTestException($"Matrix Unit Test : {message}"));
        }
    }
}
