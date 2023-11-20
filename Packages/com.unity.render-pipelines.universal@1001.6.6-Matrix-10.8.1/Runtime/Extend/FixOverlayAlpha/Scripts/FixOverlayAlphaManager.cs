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

namespace UnityEngine.Rendering.Universal
{
    public class FixOverlayAlphaManager
    {
        private static FixOverlayAlphaManager s_Instance = null;

        public static FixOverlayAlphaManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new FixOverlayAlphaManager
                    {
                        m_OccludingUIPassDict = new Dictionary<Camera, FixOverlayAlphaPass>()
                    };
                }

                return s_Instance;
            }
        }

        private bool m_Enable = true;
        private int m_StencilRef = 55;
        private int m_OverlayCount = 0;
        private Dictionary<Camera, FixOverlayAlphaPass> m_OccludingUIPassDict = null;

        public bool enable
        {
            get { return m_Enable; }
            set { m_Enable = value; }
        }

        public int stencilRef
        {
            get { return m_StencilRef; }
            set { m_StencilRef = value; }
        }

        public void RegisterOcclusionUI(GameObject overLay)
        {
            return;
            if (m_OverlayCount == 0)
            {
                m_Enable = true;
#if !UNITY_EDITOR
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#endif
            }

            m_OverlayCount++;
        }

        public void UnRegisterOcclusionUI(GameObject overLay)
        {
            return;
            m_OverlayCount--;
            if (m_OverlayCount == 0)
            {
                m_Enable = false;
                foreach (var keyValuePair in m_OccludingUIPassDict)
                {
                    keyValuePair.Value.enable = false;
                }
#if !UNITY_EDITOR
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
#endif
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            if (m_OccludingUIPassDict.TryGetValue(camera, out FixOverlayAlphaPass fixOverlayAlphaPass))
            {
                fixOverlayAlphaPass.enable = m_Enable;
            }
            else
            {
                m_OccludingUIPassDict.Add(camera, new FixOverlayAlphaPass(camera, m_Enable));
            }
        }
    }
}