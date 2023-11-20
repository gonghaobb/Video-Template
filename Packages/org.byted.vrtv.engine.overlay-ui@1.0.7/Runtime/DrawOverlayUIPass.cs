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
using UnityEngine.Rendering.Universal.Internal;

namespace Matrix.OverlayUI
{
    public class DrawOverlayUIPass : UserDefinedPass
    {
        private static readonly int s_DrawObjectPassDataPropID = Shader.PropertyToID("_DrawObjectPassData");
        private static readonly int s_ScaleBiasRtPropID = Shader.PropertyToID("_ScaleBiasRt");
        private const float CLIPPING_RANGE = 0.5f;
        private const string RENDER_IN_GAMMA = "RENDER_IN_GAMMA";

        private ProfilingSampler m_ProfilingSampler = null;
        private List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();
        private FilteringSettings m_FilteringSettings;
#if UNITY_EDITOR
        private Material m_BlitMaterial = null;
#endif

        public DrawOverlayUIPass(Camera camera, string[] shaderTags = null, bool enable = true) : base(
            RenderPassEvent.BeforeRendering, camera, enable)
        {
            m_ProfilingSampler = new ProfilingSampler("OverlayUIPass");
            if (shaderTags != null && shaderTags.Length > 0)
            {
                foreach (var passName in shaderTags)
                    m_ShaderTagIdList.Add(new ShaderTagId(passName));
            }
            else
            {
                m_ShaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForward"));
                m_ShaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
                m_ShaderTagIdList.Add(new ShaderTagId("LightweightForward"));
            }

            m_FilteringSettings = new FilteringSettings(RenderQueueRange.all);
        }

        public override void Configure(CustomPass pass, CommandBuffer cmd,
            RenderTextureDescriptor cameraTextureDescriptor)
        {
#if UNITY_EDITOR
            m_BlitMaterial = new Material(Shader.Find("Hidden/Universal Render Pipeline/Blit"));
#endif
        }

        public override int RequireFlag()
        {
            return 0x1 << 8;
        }

        public override void Execute(CustomPass pass, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            bool hasCullingResults = false;
            CullingResults cullingResults = renderingData.cullResults;
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Dictionary<OverlayCanvas, int> overlayCanvasDict = OverlayCanvasManager.instance.overlayCanvasDict;
                foreach (var overlayCanvas in overlayCanvasDict.Keys)
                {
                    if (overlayCanvas != null && overlayCanvas.TryGetRenderTexture(out RenderTexture rt) && overlayCanvas.overlayTransform != null)
                    {
                        Camera camera = renderingData.cameraData.camera;
                        
                        rt.DiscardContents();
                        CoreUtils.SetRenderTarget(cmd, rt);
                        CoreUtils.ClearRenderTarget(cmd, ClearFlag.All, Color.clear);
                        
                        Vector4 drawObjectPassData = Vector4.zero;
                        cmd.SetGlobalVector(s_DrawObjectPassDataPropID, drawObjectPassData);

                        float flipSign = (renderingData.cameraData.IsCameraProjectionMatrixFlipped()) ? -1.0f : 1.0f;
                        Vector4 scaleBias = (flipSign < 0.0f)
                            ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                            : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
                        cmd.SetGlobalVector(s_ScaleBiasRtPropID, scaleBias);

                        Vector2 canvasSize = overlayCanvas.canvasSize;
                        Matrix4x4 projectionMatrix;
                        float distance = 0;
                        if (overlayCanvas.isPerspective)
                        {
                            float fov = 2 * Mathf.Atan(canvasSize.y / (2 * overlayCanvas.distance)) * Mathf.Rad2Deg;
                            Matrix4x4 perspectiveMatrix = Matrix4x4.Perspective(fov, canvasSize.x / canvasSize.y,
                                0.00001f, overlayCanvas.distance + CLIPPING_RANGE);
                            projectionMatrix = GL.GetGPUProjectionMatrix(perspectiveMatrix, true);
                            distance = overlayCanvas.distance;
                        }
                        else
                        {
                            Matrix4x4 orthoMatrix = Matrix4x4.Ortho(canvasSize.x * -0.5f, canvasSize.x * 0.5f,
                                canvasSize.y * -0.5f, canvasSize.y * 0.5f,
                                0.00001f, CLIPPING_RANGE);
                            projectionMatrix = GL.GetGPUProjectionMatrix(orthoMatrix, true);
                            distance = CLIPPING_RANGE / 2;
                        }
                        
                        Transform canvasTransform = overlayCanvas.overlayTransform;
                        Vector3 canvasPosition = canvasTransform.position;
                        Vector3 viewPosition = canvasPosition - canvasTransform.forward * distance;
                        Matrix4x4 lookMatrix = Matrix4x4.LookAt(viewPosition, canvasPosition, canvasTransform.up);
                        Matrix4x4 scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
                        Matrix4x4 viewMatrix = scaleMatrix * lookMatrix.inverse;

                        RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, projectionMatrix, false);
                        cmd.EnableShaderKeyword(RENDER_IN_GAMMA);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();

                        if (!hasCullingResults)
                        {
                            camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters);
                            cullingParameters.cullingMask = (uint)(0 | 1 << OverlayCanvasManager.instance.overlayUILayer);
                            cullingResults = context.Cull(ref cullingParameters);
                            hasCullingResults = true;
                        }
                        
                        SortingCriteria sortFlags = SortingCriteria.CommonTransparent;
                        DrawingSettings drawSettings = pass.CreateDrawingSettings(m_ShaderTagIdList, ref renderingData, sortFlags);
                        
                        FilteringSettings filterSettings = m_FilteringSettings;
                        short id = (short)SortingLayer.GetLayerValueFromID(overlayCanvas.sortingLayerID);
                        filterSettings.sortingLayerRange = new SortingLayerRange(id, id);
                        
                        context.DrawRenderers(cullingResults, ref drawSettings, ref filterSettings);
                        cmd.DisableShaderKeyword(RENDER_IN_GAMMA);
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();

#if UNITY_EDITOR && PICO_UGUI
                        //仅用于编辑器下预览
                        RenderTargetHandle tempRT = new RenderTargetHandle();
                        tempRT.Init(rt.name);
                        cmd.GetTemporaryRT(tempRT.id, rt.descriptor, FilterMode.Point);
                        cmd.CopyTexture(rt,tempRT.Identifier());
                        cmd.EnableShaderKeyword("_SRGB_TO_LINEAR_CONVERSION");
                        cmd.SetGlobalTexture("_SourceTex",tempRT.Identifier());
                        cmd.Blit(tempRT.Identifier(), rt,m_BlitMaterial);
                        cmd.DisableShaderKeyword("_SRGB_TO_LINEAR_CONVERSION");
                        context.ExecuteCommandBuffer(cmd);
                        cmd.Clear();
#endif
                        overlayCanvas.SetCanvasSortingLayerOverride(false);
                    }
                }
            }

            RenderingUtils.SetViewAndProjectionMatrices(cmd, renderCamera.worldToCameraMatrix, renderCamera.projectionMatrix, false);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}