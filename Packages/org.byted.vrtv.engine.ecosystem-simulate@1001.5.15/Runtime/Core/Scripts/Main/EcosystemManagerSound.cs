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
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Rendering;

namespace Matrix.EcosystemSimulate
{
    [Serializable]
    public enum EcosystemSoundType
    {
        None,
        StrongRain,
        MiddleRain,
        LightRain,
        StrongSnow,
        MiddleSnow,
        LightSnow,
        StrongWind,
        MiddleWind,
        LightWind,
        Thunder
    }
    
    public partial class EcosystemManager : MonoBehaviour
    {
        private float m_SoundTransitionTime = 2f;
        public float soundTransitionTime
        {
            set => m_SoundTransitionTime = value;
        }

        public enum TransitionStage
        {
            Stop,
            Countdown,
            FadeIn,
            Normal,
            FadeOut
        }
        
        public class SoundInfo
        {
            public EcosystemSoundType sountType;
            public TransitionStage stage;
            public bool isLoop;
            public float time;
            public Vector3 position;
            public int soundId;

            public void Reset()
            {
                stage = TransitionStage.Countdown;
                isLoop = false;
                time = 0;
                position = Vector3.zero;
                soundId = -1;
            }
        }

        private bool m_EnableSound = true;
        public bool enableSound
        {
            set
            {
                if (m_EnableSound != value)
                {
                    m_EnableSound = value;
                    m_SoundSwitchAction?.Invoke(value);
                    if (m_EnableSound)
                    {
                        for (int i = 0; i < m_SoundInfoList.Count; ++i)
                        {
                            var soundInfo = m_SoundInfoList[i];
                            if (m_PlaySoundAction != null)
                            {
                                soundInfo.soundId = PlaySoundImmediately(soundInfo.sountType, soundInfo.isLoop,
                                    soundInfo.position, 0f);
                            }
                            soundInfo.stage = TransitionStage.FadeIn;
                            soundInfo.time = m_SoundTransitionTime;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < m_SoundInfoList.Count; ++i)
                        {
                            if (!m_SoundInfoList[i].isLoop)
                            {
                                m_SoundInfoPool.Release(m_SoundInfoList[i]);
                                m_SoundInfoList.RemoveAt(i);
                                i--;
                            }
                            else
                            {
                                if (m_SoundInfoList[i].stage != TransitionStage.Countdown)
                                {
                                    m_StopSoundAction?.Invoke(m_SoundInfoList[i].soundId);
                                }

                                if (m_SoundInfoList[i].stage != TransitionStage.FadeOut)
                                {
                                    m_SoundInfoList[i].stage = TransitionStage.Stop;
                                }
                            }
                        }
                    }
                }
            }
            get => m_EnableSound;
        }
        
        private ObjectPool<SoundInfo> m_SoundInfoPool = null;
        private readonly List<SoundInfo> m_SoundInfoList = new List<SoundInfo>();
        private readonly Dictionary<EcosystemSoundType, SoundInfo> m_CurEcosystemSoundDict = new Dictionary<EcosystemSoundType, SoundInfo>();
        
        private Action<bool> m_SoundSwitchAction = null;
        private Func<EcosystemSoundType, bool, Vector3, float, int> m_PlaySoundAction = null;
        private Action<int> m_StopSoundAction = null;
        private Action<int, float> m_SetSoundVolume = null;

        private void SoundInit()
        {
            if (m_SoundInfoPool == null)
            {
                m_SoundInfoPool = new ObjectPool<SoundInfo>(null, PoolRelease);
            }
        }

        private void SoundUpdate()
        {
            for (int i = 0; i < m_SoundInfoList.Count; ++i)
            {
                var soundInfo = m_SoundInfoList[i];
                if (soundInfo.time >= 0f)
                {
                    soundInfo.time -= Time.deltaTime;
                    if (soundInfo.stage == TransitionStage.FadeIn)
                    {
                        SetSoundVolume(soundInfo.soundId, 1 - soundInfo.time / m_SoundTransitionTime);
                    }
                    else if (soundInfo.stage == TransitionStage.FadeOut)
                    {
                        SetSoundVolume(soundInfo.soundId, soundInfo.time / m_SoundTransitionTime);
                    }

                    if (soundInfo.time <= 0)
                    {
                        switch (soundInfo.stage)
                        {
                            case TransitionStage.Countdown:
                                if (soundInfo.isLoop)
                                {
                                    soundInfo.soundId = PlaySoundImmediately(soundInfo.sountType, soundInfo.isLoop, soundInfo.position, 0f);
                                    soundInfo.time = m_SoundTransitionTime;
                                    soundInfo.stage = TransitionStage.FadeIn;
                                }
                                else
                                {
                                    PlaySoundImmediately(soundInfo.sountType, soundInfo.isLoop, soundInfo.position, 1f);
                                    m_SoundInfoPool.Release(soundInfo);
                                    m_SoundInfoList.RemoveAt(i);
                                    i--;
                                }
                                break;
                            case TransitionStage.FadeIn:
                                soundInfo.stage = TransitionStage.Normal;
                                break;
                            case TransitionStage.FadeOut:
                                m_StopSoundAction?.Invoke(soundInfo.soundId);
                                m_SoundInfoPool.Release(soundInfo);
                                m_SoundInfoList.RemoveAt(i);
                                m_CurEcosystemSoundDict.Remove(soundInfo.sountType);
                                break;
                        }
                    }
                }
            }
        }

        private void PoolRelease(SoundInfo soundInfo)
        {
            soundInfo.Reset();
        }

        public void SetSoundDelegate(Action<bool> soundSwitchAction,
                                     Func<EcosystemSoundType, bool, Vector3, float, int> playSoundAction,
                                     Action<int> stopSoundAction,
                                     Action<int, float> setSoundVolume)
        {
            m_SoundSwitchAction = soundSwitchAction;
            m_PlaySoundAction = playSoundAction;
            m_StopSoundAction = stopSoundAction;
            m_SetSoundVolume = setSoundVolume;
        }

        public void ClearSoundDelegate()
        {
            m_SoundSwitchAction = null;
            m_PlaySoundAction = null;
            m_StopSoundAction = null;
            m_SetSoundVolume = null;
        }

        public void PlaySound(EcosystemSoundType soundType, Vector3 position, bool isLoop = false, float delay = 0)
        {
            if (isLoop)
            {
                if (!m_CurEcosystemSoundDict.TryGetValue(soundType, out var curSoundInfo))
                {
                    m_SoundInfoPool.Get(out var soundInfo);
                    soundInfo.sountType = soundType;
                    soundInfo.isLoop = true;
                    soundInfo.time = m_EnableSound ? delay : 0;
                    soundInfo.position = position;
                    soundInfo.stage = m_EnableSound ? TransitionStage.Countdown : TransitionStage.Stop;
                    m_SoundInfoList.Add(soundInfo);
                    m_CurEcosystemSoundDict.Add(soundType, soundInfo);
                }
                else
                {
                    curSoundInfo.position = position;
                    if (m_EnableSound)
                    {
                        if (curSoundInfo.stage == TransitionStage.FadeOut)
                        {
                            curSoundInfo.stage = TransitionStage.FadeIn;
                        }
                    }
                    else
                    {
                        curSoundInfo.stage = TransitionStage.Stop;
                    }
                }
            }
            else
            {
                if (m_EnableSound)
                {
                    if (delay == 0)
                    {
                        PlaySoundImmediately(soundType, false, position, 1f);
                    }
                    else
                    {
                        m_SoundInfoPool.Get(out var soundInfo);
                        soundInfo.sountType = soundType;
                        soundInfo.isLoop = false;
                        soundInfo.time = delay;
                        soundInfo.position = position;
                        soundInfo.stage = TransitionStage.Countdown;
                        m_SoundInfoList.Add(soundInfo);
                    }
                }
            }
        }

        public void StopSound(EcosystemSoundType soundType)
        {
            if (m_CurEcosystemSoundDict.TryGetValue(soundType, out var soundInfo))
            {
                if (m_EnableSound)
                {
                    soundInfo.stage = TransitionStage.FadeOut;
                    soundInfo.time = m_SoundTransitionTime;
                }
                else
                {
                    m_SoundInfoList.Remove(soundInfo);
                    m_SoundInfoPool.Release(soundInfo);
                    m_CurEcosystemSoundDict.Remove(soundType);
                }
            }
        }

        private int PlaySoundImmediately(EcosystemSoundType sound, bool isLoop, Vector3 position, float volume)
        {
            if (m_PlaySoundAction != null)
            {
                return m_PlaySoundAction.Invoke(sound, isLoop, position, volume);
            }
            return -1;
        }

        private void SetSoundVolume(int id, float volume)
        {
            volume = Mathf.Max(0, volume);
            volume = Mathf.Min(1, volume);
            m_SetSoundVolume?.Invoke(id, volume);
        }
    }
}

