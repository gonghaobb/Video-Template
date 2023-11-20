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

namespace Matrix.EcosystemSimulate
{
    public class Debug {}

    public static class Debugger
    {
        private static readonly bool IS_PRINTF = UnityEngine.Debug.isDebugBuild;

        private static Action<UnityEngine.LogType, string> m_LogAction = null;

        public static Action<UnityEngine.LogType, string> logAction
        {
            set { m_LogAction = value; }
        }

        public static void Assert(bool condition)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.Assert(condition);
            }
        }
        
        public static void AssertFormat(bool condition, string format, params object[] args)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.AssertFormat(condition, format, args);
            }
        }

        public static void Log(string message)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.Log(message);
            }
            else
            {
                if (m_LogAction != null)
                {
                    m_LogAction(UnityEngine.LogType.Log, message);
                }
            }
        }
        
        public static void Log(object obj)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.Log(obj.ToString());
            }
            else
            {
                if (m_LogAction != null)
                {
                    m_LogAction(UnityEngine.LogType.Log, obj.ToString());
                }
            }
        }

        public static void LogNoTrack(string message)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.Log(message);
            }
        }

        public static void Log(string str, object arg0)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogFormat(str, arg0);
            }
        }

        public static void Log(string str, object arg0, object arg1)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogFormat(str, arg0, arg1);
            }
        }

        public static void Log(string str, object arg0, object arg1, object arg2)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogFormat(str, arg0, arg1, arg2);
            }
        }

        public static void Log(string str, object arg0, object arg1, object arg2, object arg3)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogFormat(str, arg0, arg1, arg2, arg3);
            }
        }

        public static void Log(string str, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogFormat(str, arg0, arg1, arg2, arg3, arg4);
            }
        }

        public static void LogWarning(string message)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogWarning(message);
            }
            else
            {
                if (m_LogAction != null)
                {
                    m_LogAction(UnityEngine.LogType.Warning, message);
                }
            }
        }

        public static void LogWarningNoTrack(string message)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogWarning(message);
            }
        }

        public static void LogWarning(string str, object arg0)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogWarningFormat(str, arg0);
            }
        }

        public static void LogWarning(string str, object arg0, object arg1)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogWarningFormat(str, arg0, arg1);
            }
        }

        public static void LogWarning(string str, object arg0, object arg1, object arg2)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogWarningFormat(str, arg0, arg1, arg2);
            }
        }

        public static void LogWarning(string str, object arg0, object arg1, object arg2, object arg3)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogWarningFormat(str, arg0, arg1, arg2, arg3);
            }
        }

        public static void LogWarning(string str, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogWarningFormat(str, arg0, arg1, arg2, arg3, arg4);
            }
        }

        public static void LogError(string message)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogError(message);
            }
            else
            {
                if (m_LogAction != null)
                {
                    m_LogAction(UnityEngine.LogType.Error, message);
                }
            }
        }

        public static void LogErrorNoTrack(string message)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogError(message);
            }
        }

        public static void LogError(string str, object arg0)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogErrorFormat(str, arg0);
            }
        }

        public static void LogError(string str, object arg0, object arg1)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogErrorFormat(str, arg0, arg1);
            }
        }

        public static void LogError(string str, object arg0, object arg1, object arg2)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogErrorFormat(str, arg0, arg1, arg2);
            }
        }

        public static void LogError(string str, object arg0, object arg1, object arg2, object arg3)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogErrorFormat(str, arg0, arg1, arg2, arg3);
            }
        }

        public static void LogError(string str, object arg0, object arg1, object arg2, object arg3, object arg4)
        {
            if (IS_PRINTF)
            {
                UnityEngine.Debug.LogErrorFormat(str, arg0, arg1, arg2, arg3, arg4);
            }
        }
    }
}