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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;
using static UnityEngine.Experimental.Rendering.RayTracingAccelerationStructure;

namespace Matrix.EcosystemSimulate
{
    public class WaterPass : UserDefinedPass
    {
        private WaterManager m_Manager = null;
        private ProfilingSampler m_ProfilingSampler;
        private static readonly ShaderTagId s_ShaderTagId = new ShaderTagId("ForwardWater");

        public WaterPass(WaterManager manager, RenderPassEvent passevent, Camera camera, bool enablePass = true)
            : base(passevent, camera, enablePass)
        {
            m_Manager = manager;
            m_ProfilingSampler = new ProfilingSampler("Water");
        }

        public override int RequireFlag()
        {
            if (renderCamera != null && renderCamera.cullingMask == 0)
            {
                return 0;
            }

            if (m_Manager != null)
            {
                int flag = 0;
                if (m_Manager.realTimeRefractionColor)
                {
                    flag |= ForwardRendererExtend.REQUIRE_TRANSPARENT_TEXTURE | ForwardRendererExtend.REQUIRE_CREATE_COLOR_TEXTURE;
                }

                if (m_Manager.realTimeDepth)
                {
                    flag |= ForwardRendererExtend.REQUIRE_DEPTH_TEXTURE;
                }

                return flag;
            }

            return 0;
        }

        public override void Configure(CustomPass pass, CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {

        }

        public override void Execute(CustomPass pass, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            //如果开启实时折射，由于兼容性问题会破坏后续的深度测试，因此强制水在最后渲染
            if (m_Manager != null && m_Manager.realTimeRefractionColor)
            {
                CommandBuffer cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, m_ProfilingSampler))
                {
                    Camera camera = renderingData.cameraData.camera;
                    SortingSettings sortingSettings = new SortingSettings(renderCamera) { criteria = SortingCriteria.CommonTransparent };
                    DrawingSettings drawingSettings = new DrawingSettings(s_ShaderTagId, sortingSettings)
                    {
                        perObjectData = renderingData.perObjectData,
                        mainLightIndex = renderingData.lightData.mainLightIndex,
                        enableDynamicBatching = renderingData.supportsDynamicBatching,

                        // Disable instancing for preview cameras. This is consistent with the built-in forward renderer. Also fixes case 1127324.
                        enableInstancing = camera.cameraType == CameraType.Preview ? false : true,
                    };
                    FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                }
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        public override void Release()
        {
            base.Release();
        }
    }
}