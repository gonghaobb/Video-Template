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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Matrix.EcosystemSimulate
{
    [CreateAssetMenu(fileName = "WaterSimulate", menuName = "EcosystemSimulate/WaterSimulate")]
    public class WaterManager : SubEcosystem
    {
        [Header("实时折射颜色")]
        public bool realTimeRefractionColor;

        [Header("实时深度")]
        public bool realTimeDepth;

        private WaterPass m_WaterPass;

#if UNITY_EDITOR
        private WaterPass m_SceneWaterPass;
#endif

        public override bool SupportInCurrentPlatform()
        {
            return true;
        }

        public override bool SupportRunInEditor()
        {
            return true;
        }

        public override void Enable()
        {
            CreateWaterPass();
        }

        public override void Disable()
        {
            ReleaseWaterPass();
        }

        public override void Update()
        {
            base.Update();

#if UNITY_EDITOR
            CreateWaterPass();
#endif
        }

        private bool ShouldCreateWaterPass()
        {
            return realTimeRefractionColor || realTimeDepth;
        }

        private void CreateWaterPass()
        {
            if (!ShouldCreateWaterPass())
            {
                return;
            }

            if (m_WaterPass == null)
            {
                Camera cam = EcosystemManager.instance.mainCamera;
                if (cam == null || cam.isActiveAndEnabled == false)
                {
                    cam = Camera.main;
                }

                if (cam != null)
                {
                    m_WaterPass = new WaterPass(this, realTimeRefractionColor ? RenderPassEvent.BeforeRenderingPostProcessing : RenderPassEvent.BeforeRenderingTransparents, cam, true);
                }
            }

#if UNITY_EDITOR
            if (m_SceneWaterPass == null)
            {
                var sceneView = UnityEditor.SceneView.lastActiveSceneView;
                if (sceneView != null)
                {
                    m_SceneWaterPass = new WaterPass(this, realTimeRefractionColor ? RenderPassEvent.BeforeRenderingPostProcessing : RenderPassEvent.BeforeRenderingTransparents, sceneView.camera, true);
                }
            }
#endif
        }

        private void ReleaseWaterPass()
        {
            if (m_WaterPass != null)
            {
                if (m_WaterPass.renderCamera != null)
                {
                    m_WaterPass.Release();
                }
                m_WaterPass = null;
            }

#if UNITY_EDITOR
            if (m_SceneWaterPass != null)
            {
                if (m_SceneWaterPass.renderCamera != null)
                {
                    m_SceneWaterPass.Release();
                }
                m_SceneWaterPass = null;
            }
#endif
        }
    }
}