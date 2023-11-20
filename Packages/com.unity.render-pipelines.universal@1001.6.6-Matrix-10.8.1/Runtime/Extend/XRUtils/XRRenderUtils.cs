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

namespace UnityEngine.Rendering
{
    public abstract class XRRenderUtils
    {
        private static XRRenderUtils m_Instance;
        
        public static XRRenderUtils instance
        {
            get
            {
                if (m_Instance == null)
                {
#if IXR_OCULUS
                    m_Instance = new OculusXRRenderUtils();
#else
                    m_Instance = new PicoXRRenderUtils();
#endif
                }

                return m_Instance;
            }
        }

        public virtual bool IsXRDevice()
        {
            return false;
        }
        
        public virtual void RegisterFocusLostCallback(Action action)
        {
        }
        
        public virtual void UnregisterFocusLostCallback(Action action)
        {
        }

        public virtual void RegisterFocusAcquiredCallback(Action action)
        {
        }
        
        public virtual void UnregisterFocusAcquiredCallback(Action action)
        {
        }
        
        public virtual bool IsSupportedEyeTracking()
        {
            return false;
        }

        public virtual bool IsSupportedSubsampledLayout()
        {
            return false;
        }

        public virtual Vector2 GetFoveatedFocalPoint(FrustumPlanes frustumPlanes)
        {
            return Vector2.zero;
        }

        public virtual void SetEyeTrackingEnabled(bool eyeTrackingEnabled)
        {
        }

        public virtual void SetSystemFoveatedFeature(int level)
        {
        }
        
        public virtual void SetSystemSubsampledLayout(bool enabled)
        {
        }
    }
}