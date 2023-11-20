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
using UnityEngine;
using UnityEngine.Rendering;

namespace Matrix.EcosystemSimulate
{
    [RequireComponent(typeof(Volume))]
    public class EcosystemVolumeBinder : MonoBehaviour
    {
        [SerializeField] 
        private EcosystemVolume m_EcosystemVolume;

        private float m_LastWeight = -1;
        private float m_LastPriority = -1;
        private Volume m_SystemVolume = null;

        private static float s_MaxLocalDistanceFade = 0;
        private static int s_UpdateFrameCount = -1;
        
        private void Start()
        {
            m_SystemVolume = GetComponent<Volume>();
            if (m_EcosystemVolume == null)
            {
                m_EcosystemVolume = GetComponentInParent<EcosystemVolume>();
            }
        }

        private void Update()
        {
            if (m_EcosystemVolume == null)
            {
                return;
            }

            if (s_UpdateFrameCount != Time.frameCount)  // 会存在多个 volume 的 update ，所以每帧只计算一次
            {
                s_MaxLocalDistanceFade = EcosystemVolumeManager.MaxDistanceFade(m_EcosystemVolume.priority);
                s_UpdateFrameCount = Time.frameCount;
            }

            float weight = m_EcosystemVolume.weight;
            if (m_EcosystemVolume.isGlobal)
            {
                if (m_EcosystemVolume == EcosystemVolumeManager.current)
                {
                    weight *= EcosystemVolumeManager.GetSystemVolumeTransitionFade(true) * (1 - s_MaxLocalDistanceFade);
                } 
                else if (m_EcosystemVolume == EcosystemVolumeManager.next)
                {
                    weight *= EcosystemVolumeManager.GetSystemVolumeTransitionFade(false) * (1 - s_MaxLocalDistanceFade);
                }
                else
                {
                    weight = 0;
                }
            }


            if (Math.Abs(m_LastWeight - weight) > 0.001f)
            {
                m_SystemVolume.weight = weight;
                m_LastWeight = weight;
            }

            float priority = m_EcosystemVolume.priority;
            if (Math.Abs(m_LastPriority - priority) > 0.001f)
            {
                m_SystemVolume.priority = priority;
                m_LastPriority = priority;
            }

            m_SystemVolume.isGlobal = m_EcosystemVolume.isGlobal;
        }
    }
}