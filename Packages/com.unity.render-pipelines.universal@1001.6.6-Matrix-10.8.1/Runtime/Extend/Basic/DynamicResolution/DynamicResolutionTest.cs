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
    public class DynamicResolutionTest : MonoBehaviour
    {
        private float m_ScreenScale = 1;

        private uint m_FrameCount = 0;
        private const uint kNumFrameTimings = 2;
        private double m_GPUFrameTime;
        private double m_CPUFrameTime;
        private FrameTiming[] m_FrameTimings = new FrameTiming[3];

        private bool m_UseAutomatic = true;

        private UniversalRenderPipelineAsset asset
        {
            get => GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        }
        private DynamicResolutionController.DynamicResolutionType m_DynamicResolutionType = DynamicResolutionController.DynamicResolutionType.Disable;

        private float PerformDynamicResolution()
        {
            return m_ScreenScale;
        }

        private void Update()
        {
            ++m_FrameCount;
            if (m_FrameCount <= kNumFrameTimings)
            {
                return;
            }
            FrameTimingManager.CaptureFrameTimings();
            FrameTimingManager.GetLatestTimings(kNumFrameTimings, m_FrameTimings);
            m_GPUFrameTime = (double)m_FrameTimings[0].gpuFrameTime;
            m_CPUFrameTime = (double)m_FrameTimings[0].cpuFrameTime;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            asset.dynamicResolutionType = m_DynamicResolutionType;
            asset.automaticScaleMin = m_ScreenScale;
            if (GUI.Button(new Rect(0, 0, 100, 100), "(-)" + m_ScreenScale.ToString()))
            {
                m_ScreenScale -= 0.1f;
            }
            else if (GUI.Button(new Rect(100, 0, 100, 100), "(+)" + m_ScreenScale.ToString()))
            {
                m_ScreenScale += 0.1f;
            }
            if (GUI.Button(new Rect(200, 0, 100, 100), "(-)" + asset.automaticFPSTarget.ToString()))
            {
                asset.automaticFPSTarget -= 1f;
            }
            else if (GUI.Button(new Rect(300, 0, 100, 100), "(+)" + asset.automaticFPSTarget.ToString()))
            {
                asset.automaticFPSTarget += 1f;
            }
            else if (GUI.Button(new Rect(0, 100, 200, 50), m_DynamicResolutionType.ToString()))
            {
                int index = (int)m_DynamicResolutionType;
                index++;
                if (index > 3)
                {
                    index = 0;
                }
                m_DynamicResolutionType = (DynamicResolutionController.DynamicResolutionType)index;
            }
            else if (GUI.Button(new Rect(0, 150, 200, 50), "Excute : " + DynamicResolutionController.excutedType.ToString()))
            {
                
            }
            else if (GUI.Button(new Rect(0, 200, 250, 50), DynamicResolutionController.curFPS.ToString("#0.00") + "," + DynamicResolutionController.curTimeMS.ToString("#0.00")))
            {

            }
            else if (GUI.Button(new Rect(0, 250, 250, 50), DynamicResolutionController.curAverageFPS.ToString("#0.00") + "," + DynamicResolutionController.curAverageTimeMS.ToString("#0.00")))
            {
                DynamicResolutionController.ResetFPS();
            }
            else if (GUI.Button(new Rect(0, 300, 250, 50), "CurScale : " + DynamicResolutionController.curScale.ToString()))
            {

            }
            else if (GUI.Button(new Rect(0, 350, 250, 50), "Automatic : " + m_UseAutomatic.ToString()))
            {
                m_UseAutomatic = !m_UseAutomatic;
                if (!m_UseAutomatic)
                {
                    DynamicResolutionController.SetPerformDynamicResolution(PerformDynamicResolution);
                }
                else
                {
                    DynamicResolutionController.ClearPerformDynamicResolution();
                }
            }

            int rezWidth = (int)Mathf.Ceil(ScalableBufferManager.widthScaleFactor * Screen.currentResolution.width);
            int rezHeight = (int)Mathf.Ceil(ScalableBufferManager.heightScaleFactor * Screen.currentResolution.height);
            string res = string.Format("CPU:{0:F3}xGPU:{1:F3}\nResolution: {2}x{3}\n",
                m_CPUFrameTime,
                m_GPUFrameTime,
                rezWidth,
                rezHeight);
            if (GUI.Button(new Rect(0, Screen.height - 100, 300, 100), res))
            {

            }
        }
#endif
    }
}
