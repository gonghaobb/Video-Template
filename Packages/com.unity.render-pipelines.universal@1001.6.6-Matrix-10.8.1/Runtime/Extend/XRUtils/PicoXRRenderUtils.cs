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

#if UNITY_ANDROID && !UNITY_EDITOR && PLATFORM_PICO_SUPPORTED
#define PLATFORM_PICO
using Unity.XR.PXR;
#endif

using System;
using UnityEngine.XR;
using System.Collections.Generic;

namespace UnityEngine.Rendering
{
    public class PicoXRRenderUtils : XRRenderUtils
    {
#if PLATFORM_PICO
        // 这里重新搞了个PXRManager的Instance，因为SDK自带的如果没获取到会报一个log，URP不太好判断他的awake时机
        // TODO: 已报给SDK，SDK后面会修复这部分，暂时这么处理
        private static PXR_Manager s_PXRManagerInstance = null;
        private static PXR_Manager PXRManagerInstance
        {
            get
            {
                if (s_PXRManagerInstance == null)
                {
                    s_PXRManagerInstance = Object.FindObjectOfType<PXR_Manager>();
                }
                return s_PXRManagerInstance;
            }
        }
        
        private static List<InputDevice> s_InputDeviceList = new List<InputDevice>();
        private static bool s_GetEyeTrackingSupportedFinished = false;
        private static bool s_EyeTrackingSupported = false;
        private static bool s_GetIsXRDeviceFinished = false;
        private static bool s_IsXRDevice = false;

        public override bool IsXRDevice()
        {
            if (!s_GetIsXRDeviceFinished)
            {
                s_InputDeviceList.Clear();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, s_InputDeviceList);
                s_IsXRDevice = s_InputDeviceList.Count != 0;
                s_GetIsXRDeviceFinished = true;
            }
            return s_IsXRDevice;
        }
        
        public override bool IsSupportedEyeTracking()
        {
            if (PXRManagerInstance == null)
            {
                return false;
            }
            // YangFan:特定情况下有性能问题，所以只判断一次，把结果Cache下来
            if (!s_GetEyeTrackingSupportedFinished)
            {
                s_InputDeviceList.Clear();
                InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking | InputDeviceCharacteristics.HeadMounted, s_InputDeviceList);
                s_EyeTrackingSupported = s_InputDeviceList.Count != 0;
                s_GetEyeTrackingSupportedFinished = true;
            }
            
            return s_EyeTrackingSupported;
        }
        
        public override void RegisterFocusLostCallback(Action action)
        {
            PXR_Plugin.System.FocusStateLost += action;
        }
        
        public override void UnregisterFocusLostCallback(Action action)
        {
            PXR_Plugin.System.FocusStateLost -= action;
        }

        public override void RegisterFocusAcquiredCallback(Action action)
        {
            PXR_Plugin.System.FocusStateAcquired += action;
        }
        
        public override void UnregisterFocusAcquiredCallback(Action action)
        {
            PXR_Plugin.System.FocusStateAcquired -= action;
        }

        public override Vector2 GetFoveatedFocalPoint(FrustumPlanes frustumPlanes)
        {
            if (PXRManagerInstance == null || !PXR_EyeTracking.GetFoveatedGazeDirection(out Vector3 eyeTrackingDirection) || eyeTrackingDirection.Equals(Vector3.zero))
            {
                return Vector2.zero; 
            }
            
            Vector2 focalPoint = Vector2.zero;
            focalPoint.x = -(2 * frustumPlanes.zNear * eyeTrackingDirection[0] / eyeTrackingDirection[2]) /
                           (frustumPlanes.right - frustumPlanes.left);
            focalPoint.y = -(2 * frustumPlanes.zNear * eyeTrackingDirection[1] / eyeTrackingDirection[2]) /
                           (frustumPlanes.top - frustumPlanes.bottom);
            return focalPoint;
        }

       public override void SetEyeTrackingEnabled(bool eyeTrackingEnabled)
        {
            if (PXRManagerInstance != null && eyeTrackingEnabled != PXRManagerInstance.eyeTracking)
            {
                PXRManagerInstance.eyeTracking = eyeTrackingEnabled;
                PXR_Plugin.System.UPxr_EnableEyeTracking(eyeTrackingEnabled);
                Debug.Log($"URPPxrUtils : Set EyeTrackingEnabled {eyeTrackingEnabled}");
            }
        }

        public override void SetSystemFoveatedFeature(int level)
        {
            if (PXRManagerInstance != null && level != (int) PXRManagerInstance.foveationLevel)
            {
                PXRManagerInstance.foveationLevel = (FoveationLevel) level;
                PXR_Plugin.Render.UPxr_SetFoveationLevel((FoveationLevel) level);
                Debug.Log($"URPPxrUtils : Set SystemFoveatedFeature {(FoveationLevel) level}");
            }
        }

        private static bool s_GetSystemSupportedSubsampledLayoutFinished = false; 
        private static bool s_GetSystemSupportedSubsampledLayoutResult = false;

        public override bool IsSupportedSubsampledLayout()
        {
            if (s_GetSystemSupportedSubsampledLayoutFinished)
            {
                return s_GetSystemSupportedSubsampledLayoutResult;
            }

            try
            {
                // Android OS 10 / API-29 (5.5.0/smartcm.1675799175)
                string operatingSystem =
                    SystemInfo.operatingSystem.Substring(SystemInfo.operatingSystem.IndexOf('(') + 1);
                int idx = operatingSystem.IndexOf('/');
                string substring = operatingSystem.Substring(0, idx > 0 ? idx : 0);
                if (!string.IsNullOrWhiteSpace(substring))
                {
                    string[] versionNumber = substring.Split('.');
                    int versionCount = 0;
                    int power = 1;
                    for (int i = versionNumber.Length - 1; i >= 0; i--)
                    {
                        if (int.TryParse(versionNumber[i], out int v))
                        {
                            versionCount += v * power;
                            power *= 10;
                        }
                    }

                    Debug.Log($"OS Version : {versionCount}");
                    if (versionNumber.Length == 3)
                    {
                        s_GetSystemSupportedSubsampledLayoutResult = versionCount >= 550;
                    }
                    else if(versionNumber.Length == 4)
                    {
                        s_GetSystemSupportedSubsampledLayoutResult = versionCount >= 5500;
                    }
                    else
                    {
                        s_GetSystemSupportedSubsampledLayoutResult = false;
                    }
                }
            }
            catch (Exception e)
            {
                s_GetSystemSupportedSubsampledLayoutResult = false;
            }
            
            s_GetSystemSupportedSubsampledLayoutFinished = true;
            return s_GetSystemSupportedSubsampledLayoutResult;
        }
        
#if SUBSAMPLED_LAYOUT_SUPPORTED
        private static bool s_LastSubsampledLayoutEnabled = false;
        private static bool s_InitSubsampledLayoutEnableFinished = false;
#endif
        
        public override void SetSystemSubsampledLayout(bool enabled)
        {
#if SUBSAMPLED_LAYOUT_SUPPORTED
            // 有变更或者第一次调用的时候执行
            if (enabled != s_LastSubsampledLayoutEnabled || !s_InitSubsampledLayoutEnableFinished)
            {
                // PXR_Plugin.Render.UPxr_SetFFRSubsampled(enabled);
                s_LastSubsampledLayoutEnabled = enabled;
                s_InitSubsampledLayoutEnableFinished = true;
                Debug.Log($"URPPxrUtils : Set SubsampledLayoutEnabled {enabled}");
            }
#endif
        }
#endif
    }
}

