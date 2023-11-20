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
#if UNITY_EDITOR
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
#endif

namespace Matrix.OverlayUI
{
    public class OverlayCanvasManager
    {
        private static OverlayCanvasManager s_Instance = null;

        public static OverlayCanvasManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new OverlayCanvasManager
                    {
                        m_DrawOverlayUIPassDict = new Dictionary<Camera, DrawOverlayUIPass>(),
                        m_OverlayCanvasDict = new Dictionary<OverlayCanvas, int>(),
                        m_UnusedID = new Queue<int>()
                    };
                    s_Instance.Init();
                }

                return s_Instance;
            }
        }

        private void Init()
        {
            foreach (SortingLayer layer in SortingLayer.layers)
            {
                if (layer.name.Contains("OverlayCanvas"))
                {
                    m_UnusedID.Enqueue(layer.id);
                }
            }
        }
        
        private bool m_Enable = true;
        public bool enable
        {
            get { return m_Enable; }
            set
            {
                if (m_Enable != value)
                {
                    m_Enable = value;
                    foreach (OverlayCanvas overlayCanvas in m_OverlayCanvasDict.Keys)
                    {
                        overlayCanvas.UpdateOverlayStatus();
                    }
                }
            }
        }

        private int m_OverlayUILayer = LayerMask.NameToLayer("OverlayUI");
        public int overlayUILayer
        {
            get { return m_OverlayUILayer; }
            set
            {
                m_OverlayUILayer = value;
            }
        }

        private Dictionary<OverlayCanvas,int> m_OverlayCanvasDict = null;
        public Dictionary<OverlayCanvas,int> overlayCanvasDict
        {
            get { return m_OverlayCanvasDict; }
        }
        
        private int m_OverlayCanvasCount = 0;
        private Dictionary<Camera, DrawOverlayUIPass> m_DrawOverlayUIPassDict = null;
        private Queue<int> m_UnusedID = null;

        public void Reset()
        {
            m_UnusedID.Clear();
            foreach (SortingLayer layer in SortingLayer.layers)
            {
                if (layer.name.Contains("OverlayCanvas"))
                {
                    m_UnusedID.Enqueue(layer.id);
                }
            }
            
            Dictionary<OverlayCanvas, int> newDict = new Dictionary<OverlayCanvas, int>();
            foreach (KeyValuePair<OverlayCanvas, int> keypair in m_OverlayCanvasDict)
            {
                OverlayCanvas overlayCanvas = keypair.Key;
                if (overlayCanvas != null)
                {
                    if (m_UnusedID.Count == 0)
                    {
                        Debug.LogError("Unused SortingLayer is not enough, please add more SortingLayers for OverlayCanvas.");
                        newDict.Add(overlayCanvas, 0);
                    }
                    else
                    {
                        newDict.Add(overlayCanvas, m_UnusedID.Dequeue());
                    }
                }
            }
            m_OverlayCanvasDict.Clear();
            m_OverlayCanvasDict = newDict;
        }

        public void RegisterOverlayCanvas(OverlayCanvas overlayCanvas)
        {
            if (m_OverlayCanvasCount == 0)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            }

            if (!m_OverlayCanvasDict.ContainsKey(overlayCanvas))
            {
                if (m_UnusedID.Count == 0)
                {
                    Debug.LogError("Unused SortingLayer is not enough, please add more SortingLayers for OverlayCanvas.");
                    m_OverlayCanvasDict.Add(overlayCanvas, 0);
                }
                else
                {
                    m_OverlayCanvasDict.Add(overlayCanvas, m_UnusedID.Dequeue());
                }
            }

            m_OverlayCanvasCount++;
        }

        public void UnRegisterOverlayCanvas(OverlayCanvas overlayCanvas)
        {
            if (m_OverlayCanvasDict.TryGetValue(overlayCanvas, out int index))
            {
                m_OverlayCanvasDict.Remove(overlayCanvas);
                m_OverlayCanvasCount--;
                if (index != 0)
                {
                    m_UnusedID.Enqueue(index);
                }
            }
            
            if (m_OverlayCanvasCount == 0)
            {
                foreach (KeyValuePair<Camera, DrawOverlayUIPass> keyValuePair in m_DrawOverlayUIPassDict)
                {
                    if (keyValuePair.Key != null)
                    {
                        keyValuePair.Value.enable = false;
                    }
                }
                
                RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
        {
#if UNITY_EDITOR
            if (!(StageUtility.GetCurrentStage() is PrefabStage) && camera.cameraType == CameraType.SceneView)
            {
                camera.cullingMask &= ~(1 << overlayUILayer);
            }
#endif
            if (camera != Camera.main)
            {
                return;
            }

            bool overlayEnable = m_OverlayCanvasCount > 0 && m_Enable;
            if (m_DrawOverlayUIPassDict.TryGetValue(camera, out DrawOverlayUIPass fixOverlayAlphaPass))
            {
                fixOverlayAlphaPass.enable = overlayEnable;
            }
            else
            {
                m_DrawOverlayUIPassDict.Add(camera, new DrawOverlayUIPass(camera, null, overlayEnable));
            }
        }

        public int GetSortingLayerID(OverlayCanvas overlayCanvas)
        {
            if (m_OverlayCanvasDict.TryGetValue(overlayCanvas, out int index))
            {
                return index;
            }
            return 0;
        }
    }
}