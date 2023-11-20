namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Copy the given color target to the current camera target
    ///
    /// You can use this pass to copy the result of rendering to
    /// the camera target. The pass takes the screen viewport into
    /// consideration.
    /// </summary>
    public class DebugPass : ScriptableRenderPass
    {
        private Material m_BlitMaterial = null;
        private static string[] DEBUG_MODE_KEYWORDS = new string[]
        {
            "_DEBUG_MODE_1",
            "_DEBUG_MODE_2",
            "_DEBUG_MODE_3",
            "_DEBUG_MODE_4"
        };
        
        public DebugPass(RenderPassEvent evt, PostProcessData data)
        {
            profilingSampler = new ProfilingSampler("DebugPass");
            if (data.shaders.debugPassPS != null)
            {
                m_BlitMaterial = CoreUtils.CreateEngineMaterial(data.shaders.debugPassPS);
            }
            renderPassEvent = evt;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_BlitMaterial == null)
            {
                return;
            }

            ref CameraData cameraData = ref renderingData.cameraData;
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.DebugPass)))
            {
                if (cameraData.xr.enabled)
                {
                    cameraData.xr.StartSinglePass(cmd);
                }
                bool yflip = !cameraData.xr.renderTargetIsRenderTexture && SystemInfo.graphicsUVStartsAtTop;
                Vector4 scaleBias = yflip ? new Vector4(1, -1, 0, 1) : new Vector4(1, 1, 0, 0);
                cmd.SetGlobalVector(ShaderPropertyId.scaleBias, scaleBias);
                for (int i = 0; i < DEBUG_MODE_KEYWORDS.Length; ++i)
                {
                    m_BlitMaterial.DisableKeyword(DEBUG_MODE_KEYWORDS[(int)UniversalRenderPipeline.asset.debugMode - 1]);
                }
                m_BlitMaterial.EnableKeyword(DEBUG_MODE_KEYWORDS[(int)UniversalRenderPipeline.asset.debugMode - 1]);
                cmd.DrawProcedural(Matrix4x4.identity, m_BlitMaterial, 0, MeshTopology.Quads, 4);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}
