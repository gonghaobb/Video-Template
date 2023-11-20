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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


namespace Matrix.OverlayUI
{
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class OverlayCanvasNotifier : MonoBehaviour
    {
        [NonSerialized]
        private OverlayCanvas m_ParentOverlayCanvas = null;

        public OverlayCanvas parentOverlayCanvas
        {
            get { return m_ParentOverlayCanvas; }
        }
        
        private Canvas m_Canvas;

        public Canvas canvas
        {
            get
            {
                if (m_Canvas == null)
                {
                    TryGetComponent(out m_Canvas);
                }

                return m_Canvas;
            }
        }
        
        [SerializeField]
        private bool m_IgnoreOverlay = false;
        
        public bool ignoreOverlay
        {
            get { return m_IgnoreOverlay; }
        }
        
        private void Awake()
        {
            RebindParentOverlayCanvas();
        }

        private void OnCanvasHierarchyChanged()
        {
            RebindParentOverlayCanvas();
        }

        private void OnTransformParentChanged()
        {
            RebindParentOverlayCanvas();
        }

        private void RebindParentOverlayCanvas()
        {
            if (transform.parent == null)
            {
                return;
            }

            OverlayCanvas overlayCanvas = gameObject.GetComponentInParent<OverlayCanvas>(true);

            if (m_ParentOverlayCanvas == overlayCanvas)
            {
                return;
            }

            if (m_ParentOverlayCanvas != null)
            {
                m_ParentOverlayCanvas.UnRegisterSubCanvasNotifier(this);
            }
                
            if (overlayCanvas != null)
            {
                overlayCanvas.RegisterSubCanvasNotifier(this);
            }
                
            m_ParentOverlayCanvas = overlayCanvas;
        }
    }
}