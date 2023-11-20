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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Matrix.EcosystemSimulate
{
    public static class EcosystemVolumeManager
    {
        private static EcosystemVolume s_Current = null;
        public static EcosystemVolume current
        {
            get
            {
                return s_Current;
            }
            set
            {
                s_Current = value;
            }
        }
        
        private static EcosystemVolume s_Next = null;
        public static EcosystemVolume next
        {
            get { return s_Next; }
        }
        
        private static Action s_UpdateCallback = null;
        private static Action s_LateUpdateCallback = null;
        private static float s_TransitionTime = 0;
        private static float s_TransitionFade = 0;
        private static float s_LateTransitionTime = 0;
        private static float s_LateTransitionFade = 0;

        public static float GetSystemVolumeTransitionFade(bool isCurrent)
        {
            float result = 0;
            float currentTime = s_TransitionFade * s_TransitionTime;
            if (isCurrent && current != null)
            {
                currentTime *= current.switchSpeedMultiplier;
                TransitionParameters t = current.transitionParameters;
                result = 1 - Mathf.Clamp01((currentTime - t.endingDelayTime) / t.endingTime);
            }

            if (!isCurrent && next != null)
            {
                currentTime *= next.switchSpeedMultiplier;
                TransitionParameters t = next.transitionParameters;
                result = Mathf.Clamp01((currentTime - t.startingDelayTime) / t.startingTime);
            }

            return result;
        }

        public static float GetEcosystemVolumeTransitionFade(EcosystemType type, bool isCurrent)
        {
            float result = 0;
            float currentTime = s_TransitionFade * s_TransitionTime;
            if (isCurrent && current != null)
            {
                currentTime *= current.switchSpeedMultiplier;
                current.TryGetTransitionParameters(type , out TransitionParameters t);
                result = 1 - Mathf.Clamp01((currentTime - t.endingDelayTime) / t.endingTime);
            }

            if (!isCurrent && next != null)
            {
                currentTime *= next.switchSpeedMultiplier;
                next.TryGetTransitionParameters(type , out TransitionParameters t);
                result = Mathf.Clamp01((currentTime - t.startingDelayTime) / t.startingTime);
            }

            return result;
        }
        
        public static float GetEcosystemVolumeLateTransitionFade(EcosystemType type, bool isCurrent)
        {
            float result = 0;
            float currentTime = s_LateTransitionFade * s_LateTransitionTime;
            if (isCurrent && current != null)
            {
                currentTime *= current.switchSpeedMultiplier;
                current.TryGetTransitionParameters(type , out TransitionParameters t);
                result = 1 - Mathf.Clamp01((currentTime - t.endingDelayTime) / t.endingTime);
            }

            if (!isCurrent && next != null)
            {
                currentTime *= next.switchSpeedMultiplier;
                next.TryGetTransitionParameters(type , out TransitionParameters t);
                result = Mathf.Clamp01((currentTime - t.startingDelayTime) / t.startingTime);
            }

            return result;
        }

        private static Dictionary<EcosystemType, EcosystemParameters> s_ActivatedEcosystemParametersDict =
            new Dictionary<EcosystemType, EcosystemParameters>();

        public static List<Action<EcosystemVolume , float>> s_SwitchVolumeCallbackList =
            new List<Action<EcosystemVolume , float>>();
        public static List<Action<EcosystemVolume>> s_ResetVolumeCallbackList =
            new List<Action<EcosystemVolume>>();

        private static EcosystemParameters GetOrCreateParameters(EcosystemType type)
        {
            if (!s_ActivatedEcosystemParametersDict.TryGetValue(type , out EcosystemParameters parameters))
            {
                parameters = ScriptableObject.CreateInstance(EcosystemVolume.s_ParametersConfigDict[type]) as EcosystemParameters;
                s_ActivatedEcosystemParametersDict.Add(type , parameters);
            }

            return parameters;
        }

        public static void SwitchVolume(EcosystemVolume dst , float time , float lateTime , 
            Action updateCallback = null , Action lateUpdateCallback = null)
        {
            s_Next = dst;
            s_TransitionFade = 0;
            s_TransitionTime = Mathf.Max(1, time);            
            s_LateTransitionFade = 0;
            s_LateTransitionTime = Mathf.Max(1, lateTime);
            s_Current.ResetState(EcosystemVolume.VolumeState.Running);
            s_UpdateCallback = updateCallback;
            s_LateUpdateCallback = lateUpdateCallback;

            foreach (Action<EcosystemVolume , float> action in s_SwitchVolumeCallbackList)
            {
                action?.Invoke(s_Next , time);
            }
        }
        
        public static void ResetVolume(EcosystemVolume dst)
        {
            s_Current?.ResetState(EcosystemVolume.VolumeState.Pending);
            s_Next = null;
            s_Current = dst;
            s_TransitionFade = 0;
            s_LateTransitionFade = 0;
            s_Current.ResetState(EcosystemVolume.VolumeState.Running);
            
            foreach (Action<EcosystemVolume> action in s_ResetVolumeCallbackList)
            {
                action?.Invoke(s_Current);
            }
        }

        internal static void Update()
        {
            if (s_Next == null)
            {
                return;
            }

            if (s_TransitionFade < 1.0f)
            {
                s_TransitionFade += Time.deltaTime / s_TransitionTime;
            }
            
            if (s_TransitionFade >= 1.0f)
            {
                s_UpdateCallback?.Invoke();
                s_UpdateCallback = null;
                s_LateTransitionFade += Time.deltaTime / s_LateTransitionTime;
            }

            if (s_LateTransitionFade >= 1.0f)
            {
                s_TransitionFade = 0;
                s_LateTransitionFade = 0;
                s_Current.ResetState(EcosystemVolume.VolumeState.Pending);
                s_Next.ResetState(EcosystemVolume.VolumeState.Running);
                s_Current = s_Next;
                s_Next = null;
                s_LateUpdateCallback?.Invoke();
            }
        }

        private static List<EcosystemVolume> s_EcosystemVolumeList = new List<EcosystemVolume>();
        private static Dictionary<EcosystemType, EcosystemParameters> s_DefaultEcosystemParametersDict =
            new Dictionary<EcosystemType, EcosystemParameters>();

        public static EcosystemParameters GetDefaultParameters(EcosystemType type)
        {
            if (s_DefaultEcosystemParametersDict.TryGetValue(type , out EcosystemParameters defaultParameters))
            {
                return defaultParameters;
            }

            EcosystemParameters param = 
                ScriptableObject.CreateInstance(EcosystemVolume.s_ParametersConfigDict[type]) as EcosystemParameters;
            param?.Clear();
            s_DefaultEcosystemParametersDict.Add(type , param);
            return param;
        }

        public static void RegisterVolume(EcosystemVolume ecosystemVolume)
        {
            s_EcosystemVolumeList.Add(ecosystemVolume);
            ReSortVolumeListByPriority();
        }
        
        public static void UnRegisterVolume(EcosystemVolume ecosystemVolume)
        {
            s_EcosystemVolumeList.Remove(ecosystemVolume);
        }

        public static void ReSortVolumeListByPriority()
        {
            s_EcosystemVolumeList.Sort(
                (EcosystemVolume a , EcosystemVolume b) => 
                    Math.Abs(a.priority - b.priority) < 0.001f ? 0 : a.priority > b.priority ? 1 : -1);
        }
        
        public static void GetVolumeList(EcosystemType type, ref List<EcosystemVolume> volumeList)
        {
            foreach (EcosystemVolume t in s_EcosystemVolumeList)
            {
                if (!t.isGlobal && t.ContainsParameters(type))
                {
                    volumeList.Add(t);
                }
            }
        }

        public static float MaxDistanceFade(float priority)
        {
            float localFade = 0;
            foreach (EcosystemVolume t in s_EcosystemVolumeList)
            {
                if (!t.isGlobal && t.priority > priority)
                {
                    localFade = Mathf.Max(localFade, t.distanceFade);
                }
            }

            return localFade;
        }

        public static int TryGetParameters(EcosystemType type, EcosystemParameters parameters , 
            bool includeGlobal = true, bool includeLocal = true)
        {
            parameters.Clear();
            int parametersCount = 0;
            float maxLocalFade = 0;
            bool needDefaultParameters = true;
            if (includeLocal)
            {
                foreach (EcosystemVolume t in s_EcosystemVolumeList)
                {
                    if (!t.isGlobal && t.TryGetParametersWithDistanceFade(type, 
                        out EcosystemParameters tempParameters , needDefaultParameters))
                    {
                        parameters.Blend(tempParameters);
                        maxLocalFade = Mathf.Max(maxLocalFade, t.distanceFade);
                        parametersCount++;
                        needDefaultParameters = false;
                    }
                }
            }

            if (includeGlobal && TryGetParametersFromGlobalVolume(type , out EcosystemParameters globalParameters))
            {
                parametersCount++;
                globalParameters.Scale(1 - maxLocalFade);
                parameters.Blend(globalParameters);
            }
            
            return parametersCount;
        }

        public static bool TryGetParametersFromGlobalVolume(EcosystemType type, out EcosystemParameters outParameters)
        {
            outParameters = GetOrCreateParameters(type);
            if (s_Current == null)
            {
                return false;
            }

            outParameters.Clear();
            bool hasCurrent = s_Current.TryGetParametersWithoutTransitionFade(type, out EcosystemParameters currentParameters);
            if (s_Next != null)
            {
                bool hasNext = s_Next.TryGetParametersWithoutTransitionFade(type, out EcosystemParameters nextParameters);
                
                float currentScale = currentParameters.GetEndType() == EcosystemParameters.SwitchType.Default ? 
                    GetEcosystemVolumeTransitionFade(type, true) : 
                    GetEcosystemVolumeLateTransitionFade(type, true);
                float nextScale = nextParameters.GetStartType() == EcosystemParameters.SwitchType.Default ? 
                    GetEcosystemVolumeTransitionFade(type, false) : 
                    GetEcosystemVolumeLateTransitionFade(type, false);
                if (hasCurrent && hasNext)
                {
                    outParameters.Mixed(currentParameters , nextParameters , currentScale , nextScale , s_TransitionFade);
                    return true;
                }
                
                if (hasNext)
                {
                    nextParameters.Scale(nextScale);
                    outParameters.Blend(nextParameters);
                    return true;
                }
            }
            
            if (hasCurrent)
            {
                currentParameters.Scale(currentParameters.GetEndType() == EcosystemParameters.SwitchType.Default ? 
                    GetEcosystemVolumeTransitionFade(type, true) : 
                    GetEcosystemVolumeLateTransitionFade(type, true));
                outParameters.Blend(currentParameters);
                return true;
            }

            return false;
        }

        public static void GetGlobalVolumeList(ref List<EcosystemVolume> volumeList)
        {
            volumeList.Clear();
            foreach (EcosystemVolume volume in s_EcosystemVolumeList)
            {
                if (volume.isGlobal)
                {
                    volumeList.Add(volume);
                }
            }
        }
        
        private static Dictionary<EcosystemType, bool> s_EcosystemLocalVolumeParametersHasChangedDict =
            new Dictionary<EcosystemType, bool>();

        public static void SetLocalVolumeParametersChanged(EcosystemType type)
        {
            s_EcosystemLocalVolumeParametersHasChangedDict[type] = true;
        }
        
        public static void ClearLocalVolumeParametersChanged(EcosystemType type)
        {
            s_EcosystemLocalVolumeParametersHasChangedDict[type] = false;
        }
        
        public static bool HasLocalVolumeParametersChanged(EcosystemType type)
        {
            if (s_EcosystemLocalVolumeParametersHasChangedDict.TryGetValue(type , out bool changed))
            {
                return changed;
            }
            
            return true;
        }

        private static bool s_EcosystemGlobalVolumeListHasChanged = true;

        public static void SetGlobalVolumeListChanged()
        {
            s_EcosystemGlobalVolumeListHasChanged = true;
        }

        public static void ClearGlobalVolumeListChanged()
        {
            s_EcosystemGlobalVolumeListHasChanged = false;
        }

        public static bool HasGlobalVolumeListChanged()
        {
            return s_EcosystemGlobalVolumeListHasChanged;
        }

        public static bool CanSwitchGlobalVolume()
        {
            return s_Next == null;
        }
    }
}