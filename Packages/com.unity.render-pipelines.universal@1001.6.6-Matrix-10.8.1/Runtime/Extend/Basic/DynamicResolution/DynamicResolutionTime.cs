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

namespace UnityEngine.Rendering.Universal
{
    public static partial class DynamicResolutionController
    {
        private static float m_AutomaticCheckTime = 0;
        private static float m_AutomaticCheckCircle = 1.0f;

        public static float UPDATE_INTERVAL = 0.2f;
        public static float AVERAGE_CHECK_CIRCLE = 2f;

        private static double m_LastInterval = 0;
        private static int m_FrameCount = 0;
        private static int m_Frames = 0;
        private static float m_FramesDelay = 0;
        private static float m_CurFPS = 0;
        private static float m_CurTimeMS = 0;
        private static float m_CurAverageFPS = float.MaxValue;
        private static float m_CurAverageTimeMS = float.MaxValue;
        private static float m_AverageFPS = 0;
        private static float m_AverageTimeMS = 0;
        private static int m_AverageTimes = 0;
        private static float m_AverageCircle = 0;

        private static bool m_DynamicFrameSupported = true;
        private static FrameTiming[] m_FrameTimings = new FrameTiming[3];

        public static float curFPS
        {
            get
            {
                return m_CurFPS;
            }
        }

        public static float curAverageFPS
        {
            get
            {
                return m_CurAverageFPS;
            }
        }

        public static float curTimeMS
        {
            get
            {
                return m_CurTimeMS;
            }
        }

        public static float curAverageTimeMS
        {
            get
            {
                return m_CurAverageTimeMS;
            }
        }

        public static void ResetFPS()
        {
            m_CurAverageFPS = float.MaxValue;
            m_CurAverageTimeMS = float.MaxValue;
            m_AverageFPS = 0;
            m_AverageTimes = 0;
            m_AverageTimeMS = 0;
            m_AverageCircle = 0;
        }

        
        private static void AutomaticScale()
        {
            m_AutomaticCheckTime += Time.deltaTime;
            if (m_AutomaticCheckTime >= m_AutomaticCheckCircle)
            {
                m_AutomaticCheckTime = 0;

                //https://software.intel.com/content/www/us/en/develop/articles/dynamic-resolution-rendering-article.html
                const float K = 1f;
                float S = curScale;
                float T = 1000f / asset.automaticFPSTarget;
                float t = curAverageTimeMS;
                float sS = K * S * ((T - t) / T);
                float nS = S + sS;
                nS = Mathf.Max(asset.automaticScaleMin, nS);
                nS = Mathf.Min(asset.automaticScaleMax, nS);
                SetScale(nS);
            }
        }

        internal static void CalculateFPS()
        {
            ++m_FrameCount;
            if (m_FrameCount < 10)
            {
                return;
            }

            if (m_DynamicFrameSupported)
            {
                FrameTimingManager.CaptureFrameTimings();
                FrameTimingManager.GetLatestTimings(2, m_FrameTimings);
                double gpuFrameTime = m_FrameTimings[0].gpuFrameTime;
                if (gpuFrameTime.Equals(0f))
                {
                    m_DynamicFrameSupported = false;

                    m_FramesDelay = Time.realtimeSinceStartup;
                    ManualComputeFPS();
                }
                else
                {
                    m_CurTimeMS = (float)gpuFrameTime;
                    m_CurFPS = (float)(1000f / m_CurTimeMS);

                    m_AverageFPS += m_CurFPS;
                    m_AverageTimeMS += m_CurTimeMS;
                    m_AverageTimes++;
                }
            }
            else
            {
                ManualComputeFPS();
            }

            m_AverageCircle += Time.deltaTime;
            if (m_AverageCircle >= AVERAGE_CHECK_CIRCLE)
            {
                m_AverageCircle = 0;
                if (m_CurAverageFPS.Equals(float.MaxValue))
                {
                    m_CurAverageFPS = m_AverageFPS / m_AverageTimes;
                    m_CurAverageTimeMS = m_AverageTimeMS / m_AverageTimes;
                }
                else
                {
                    m_CurAverageFPS = (m_AverageFPS / m_AverageTimes + m_CurAverageFPS) / 2;
                    m_CurAverageTimeMS = (m_AverageTimeMS / m_AverageTimes + m_CurAverageTimeMS) / 2;
                }

                m_AverageTimeMS = 0;
                m_AverageFPS = 0;
                m_AverageTimes = 0;
            }
        }

        private static void ManualComputeFPS()
        {
            ++m_Frames;
            float timeNow = Time.realtimeSinceStartup - m_FramesDelay;
            if (timeNow > m_LastInterval + UPDATE_INTERVAL)
            {
                m_CurTimeMS = (float)((timeNow - m_LastInterval) * 1000 / m_Frames);
                m_CurFPS = (float)(m_Frames / (timeNow - m_LastInterval));
                m_Frames = 0;
                m_LastInterval = timeNow;

                m_AverageFPS += m_CurFPS;
                m_AverageTimeMS += m_CurTimeMS;
                m_AverageTimes++;
            }
        }
    }
}
