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
using Unity.XR.PXR;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace Matrix.OverlayUI
{
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    [DisallowMultipleComponent]
    public class OverlayCanvas : UIBehaviour
    {
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
        
        private Canvas m_HoleCanvas;

        public Canvas holeCanvas
        {
            get
            {
                if (m_HoleCanvas == null && m_Overlay != null)
                {
                    m_Overlay.TryGetComponent(out m_HoleCanvas);
                }

                return m_HoleCanvas;
            }
        }

        private RectTransform m_RectTransform;

        private RectTransform rectTransform
        {
            get
            {
                if (m_RectTransform == null)
                {
                    TryGetComponent(out m_RectTransform);
                }

                return m_RectTransform;
            }
        }
        
                
        private RawImage m_HoleImage;
        
        private RawImage holeImage
        {
            get
            {
                if (m_HoleImage == null && m_Overlay != null)
                {
                    m_Overlay.TryGetComponent(out m_HoleImage);
                }
                return m_HoleImage;
            }
        }

        private static Material s_DefaultUIMaterial = null;

        //用于处理各透传层之间存在物体的情况，可支持镂空UI，半透明UI颜色会变淡
        private static Material uiMaterialForUnderlayHole
        {
            get
            {
                if (s_DefaultUIMaterial == null)
                {
                    //透传-半透明混合修正方案2
                    s_DefaultUIMaterial = new Material(Shader.Find("OverlayUI/UnderlayHole_UI"))
                    {
                        name = "Underlay Hole UI"
                    };
#if UNITY_EDITOR
                    s_DefaultUIMaterial.SetFloat(s_SrcBlend, (float)BlendMode.One);
                    s_DefaultUIMaterial.SetFloat(s_DstBlend, (float)BlendMode.OneMinusSrcAlpha);
                    s_DefaultUIMaterial.SetFloat(s_BlendOp, (float)BlendOp.Add);
                    s_DefaultUIMaterial.SetFloat(s_SrcAlphaBlend, (float)BlendMode.One);
                    s_DefaultUIMaterial.SetFloat(s_DstAlphaBlend, (float)BlendMode.One);
                    s_DefaultUIMaterial.SetFloat(s_AlphaBlendOp, (float)BlendOp.ReverseSubtract);
#else
                    s_DefaultUIMaterial.SetFloat(s_SrcBlend, (float)BlendMode.Zero);
                    s_DefaultUIMaterial.SetFloat(s_DstBlend, (float)BlendMode.OneMinusSrcAlpha);
                    s_DefaultUIMaterial.SetFloat(s_BlendOp, (float)BlendOp.Add);
                    s_DefaultUIMaterial.SetFloat(s_SrcAlphaBlend, (float)BlendMode.One);
                    s_DefaultUIMaterial.SetFloat(s_DstAlphaBlend, (float)BlendMode.One);
                    s_DefaultUIMaterial.SetFloat(s_AlphaBlendOp, (float)BlendOp.ReverseSubtract);
#endif
                }

                return s_DefaultUIMaterial;
            }
        }

        private static Material s_DefaultUIMaterialNoOcclusion = null;

        //用于处理各透传层之间不存在物体的情况，可支持镂空UI和半透明UI
        private static Material uiMaterialForUnderlayHoleNoOcclusion
        {
            get
            {
                if (s_DefaultUIMaterialNoOcclusion == null)
                {
                    //透传-半透明混合修正方案2
                    s_DefaultUIMaterialNoOcclusion = new Material(Shader.Find("OverlayUI/UnderlayHole_UI"))
                    {
                        name = "Underlay Hole UI - NoOcclusion"
                    };
#if UNITY_EDITOR
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_SrcBlend, (float)BlendMode.One);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_DstBlend, (float)BlendMode.OneMinusSrcAlpha);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_BlendOp, (float)BlendOp.Add);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_SrcAlphaBlend, (float)BlendMode.Zero);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_DstAlphaBlend, (float)BlendMode.Zero);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_AlphaBlendOp, (float)BlendOp.Add);
#else
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_SrcBlend, (float)BlendMode.Zero);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_DstBlend, (float)BlendMode.OneMinusSrcAlpha);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_BlendOp, (float)BlendOp.Add);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_SrcAlphaBlend, (float)BlendMode.Zero);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_DstAlphaBlend, (float)BlendMode.Zero);
                    s_DefaultUIMaterialNoOcclusion.SetFloat(s_AlphaBlendOp, (float)BlendOp.Add);
#endif
                }

                return s_DefaultUIMaterialNoOcclusion;
            }
        }

        private int m_SortingLayerID = 0;

        public int sortingLayerID
        {
            get
            {
                return m_SortingLayerID;
            }
        }

        [SerializeField, Range(1, 4096)]
        private int m_Width = 1024;

        public int width
        {
            get
            {
                return m_Width;
            }
            set
            {
                if (m_Width != value)
                {
                    m_Width = value;
                    ResetRenderTexture();
                    rectTransform.hasChanged = true;
                }
            }
        }

        [SerializeField, Range(1, 4096)]
        private int m_Height = 1024;

        public int height
        {
            get
            {
                return m_Height;
            }
            set
            {
                if (m_Height != value)
                {
                    m_Height = value;
                    ResetRenderTexture();
                    rectTransform.hasChanged = true;
                }
            }
        }

        [SerializeField]
        private bool m_UseCustomSize = false;

        public bool useCustomSize
        {
            get
            {
                return m_UseCustomSize;
            }
            set
            {
                if (m_UseCustomSize != value)
                {
                    m_UseCustomSize = value;
                    ResetRenderTexture();
                    rectTransform.hasChanged = true;
                }
            }
        }

        [SerializeField]
        private Vector2 m_Offset = Vector2.zero;

        public Vector2 offset
        {
            get
            {
                return m_Offset;
            }
            set
            {
                if (m_Offset != value)
                {
                    m_Offset = value;
                    rectTransform.hasChanged = true;
                }
            }
        }

        private RenderTexture m_RenderTexture = null;

        private RenderTexture renderTexture
        {
            get
            {
                if (m_RenderTexture == null)
                {
                    ResetRenderTexture();
                }

                return m_RenderTexture;
            }
        }

        [SerializeField]
        private int m_OverlayDepth = -1;

        public int overlayDepth
        {
            get
            {
                return m_OverlayDepth;
            }
            set
            {
                m_OverlayDepth = value;
                if (m_Overlay != null)
                {
                    m_Overlay.layerDepth = m_OverlayDepth;
                }
            }
        }

        [SerializeField]
        private bool m_OcclusionBetweenOverlayLayers = true;

        public bool occlusionBetweenOverlayLayers
        {
            get
            {
                return m_OcclusionBetweenOverlayLayers;
            }
            set
            {
                if (m_OcclusionBetweenOverlayLayers != value)
                {
                    if (holeImage != null)
                    {
                        holeImage.material = m_OcclusionBetweenOverlayLayers
                            ? uiMaterialForUnderlayHole
                            : uiMaterialForUnderlayHoleNoOcclusion;
                    }

                    m_OcclusionBetweenOverlayLayers = value;
                }
            }
        }

        [SerializeField]
        private bool m_IsPerspective = false;

        public bool isPerspective
        {
            get
            {
                return m_IsPerspective;
            }
            set
            {
                m_IsPerspective = value;
            }
        }

        [SerializeField, Min(0)]
        private float m_Distance = 1f;

        public float distance
        {
            get
            {
                return m_Distance;
            }
            set
            {
                m_Distance = value;
            }
        }


        public Transform overlayTransform
        {
            get
            {
                return m_Overlay != null ? m_Overlay.transform : null;
            }
        }

        public Vector2 canvasSize
        {
            get
            {
                Rect rect = rectTransform.rect;
                Vector2 size = useCustomSize
                    ? new Vector3(width, height)
                    : new Vector3(rect.width, rect.height);
                Vector3 lossyScale = rectTransform.lossyScale;
                return new Vector2(size.x * Mathf.Abs(lossyScale.x), size.y * Mathf.Abs(lossyScale.y));
            }
        }

        [SerializeField, HideInInspector]
        private PXR_OverLay m_Overlay = null;

        private static readonly List<GraphicRaycaster> s_RaycasterList = new List<GraphicRaycaster>();
        private static readonly List<Canvas> s_SubCanvasList = new List<Canvas>();
        private static readonly List<Graphic> s_GraphicList = new List<Graphic>();
        
        private readonly HashSet<OverlayCanvasNotifier> m_SubCanvasNotifierSet = new HashSet<OverlayCanvasNotifier>();

        private static readonly int s_SrcBlend = Shader.PropertyToID("_SrcBlend");
        private static readonly int s_DstBlend = Shader.PropertyToID("_DstBlend");
        private static readonly int s_SrcAlphaBlend = Shader.PropertyToID("_SrcAlphaBlend");
        private static readonly int s_DstAlphaBlend = Shader.PropertyToID("_DstAlphaBlend");
        private static readonly int s_AlphaBlendOp = Shader.PropertyToID("_AlphaBlendOp");
        private static readonly int s_BlendOp = Shader.PropertyToID("_BlendOp");

        protected override void Start()
        {
            base.Start();
            DeployOverlayComponent();
            UpdateOverlayStatus();
        }

        protected override void OnEnable()
        {
            OverlayCanvasManager.instance.RegisterOverlayCanvas(this);
            m_SortingLayerID = OverlayCanvasManager.instance.GetSortingLayerID(this);
            Canvas.preWillRenderCanvases += UpdateOverlay;
            DeployOverlayComponent();
            ResetRenderTexture();
            DeployNotifierInSubCanvas();
            UpdateOverlayStatus();
        }

        protected override void OnDisable()
        {
            OverlayCanvasManager.instance.UnRegisterOverlayCanvas(this);
            Canvas.preWillRenderCanvases -= UpdateOverlay;
            UpdateOverlayStatus();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (Application.isPlaying)
            {
                if (m_Overlay != null && m_Overlay.gameObject != null)
                {
                    Destroy(m_Overlay.gameObject);
                }
            }
#if UNITY_EDITOR
            else
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (m_Overlay != null && m_Overlay.gameObject != null)
                    {
                        DestroyImmediate(m_Overlay.gameObject);
                    }
                };
            }
#endif
        }

        protected override void OnCanvasHierarchyChanged()
        {
            base.OnCanvasHierarchyChanged();
            if (m_Overlay != null)
            {
                UpdateOverlay();
                if (holeCanvas.isRootCanvas)
                {
                    holeCanvas.sortingOrder = canvas.sortingOrder;
                    m_Overlay.gameObject.layer = LayerMask.NameToLayer("UI");
                }
                else
                {
                    Canvas rootCanvas = canvas.rootCanvas;
                    holeCanvas.sortingOrder = rootCanvas.sortingOrder;
                    m_Overlay.gameObject.layer = rootCanvas.gameObject.layer;
                }
            }
        }

        private void DeployOverlayComponent()
        {
            if (m_Overlay == null)
            {
                GameObject obj = new GameObject(name + " Overlay")
                {
                    hideFlags = HideFlags.NotEditable
                };
                RectTransform rectTf = obj.AddComponent<RectTransform>();
                if (rectTransform.parent != null)
                {
                    if (rectTransform.parent != null && rectTransform.parent.gameObject.activeInHierarchy)
                    {
                        rectTf.SetParent(rectTransform.parent, false);
                    }

                    rectTf.localScale = rectTransform.parent.InverseTransformVector(rectTransform.lossyScale);
                }
                else
                {
                    rectTf.localScale = rectTransform.localScale;
                }

                rectTf.rotation = rectTransform.rotation;
                rectTf.localPosition = rectTransform.localPosition + new Vector3(offset.x, offset.y, 0);
                rectTf.SetSiblingIndex(rectTransform.GetSiblingIndex() + 1);
                Rect rect = rectTransform.rect;
                rectTf.sizeDelta = (useCustomSize
                    ? new Vector2(width, height)
                    : new Vector2(rect.width, rect.height));

                Canvas newHoleCanvas = obj.AddComponent<Canvas>();
                newHoleCanvas.renderMode = RenderMode.WorldSpace;
                if (newHoleCanvas.isRootCanvas)
                {
                    obj.layer = LayerMask.NameToLayer("UI");
                    newHoleCanvas.sortingOrder = canvas.sortingOrder;
                }
                else
                {
                    newHoleCanvas.sortingOrder = newHoleCanvas.rootCanvas.sortingOrder;
                    obj.layer = newHoleCanvas.rootCanvas.gameObject.layer;
                }

                RawImage newRawImage = obj.AddComponent<RawImage>();
                newRawImage.isMaskingGraphic = true;
                newRawImage.raycastTarget = false;

                m_Overlay = obj.AddComponent<PXR_OverLay>();
                m_Overlay.isDynamic = true;
                m_Overlay.overlayType = PXR_OverLay.OverlayType.Underlay;
                m_Overlay.textureType = PXR_OverLay.TextureType.DynamicTexture;
                m_Overlay.useLayerBlend = false;
            }

            if (holeImage != null)
            {
                holeImage.material = m_OcclusionBetweenOverlayLayers
                    ? uiMaterialForUnderlayHole
                    : uiMaterialForUnderlayHoleNoOcclusion;
                holeImage.texture = renderTexture;
            }

            if (m_Overlay != null)
            {
                m_Overlay.gameObject.hideFlags = HideFlags.NotEditable;
                m_Overlay.layerDepth = m_OverlayDepth;
                //透传-半透明混合修正方案2
                m_Overlay.useLayerBlend = true;
                m_Overlay.srcColor = PxrBlendFactor.PxrBlendFactorOne;
                m_Overlay.dstColor = PxrBlendFactor.PxrBlendFactorOneMinusSrcAlpha;
                m_Overlay.srcAlpha = PxrBlendFactor.PxrBlendFactorOne;
                m_Overlay.dstAlpha = PxrBlendFactor.PxrBlendFactorOneMinusSrcAlpha;
            }
        }

        private void DeployNotifierInSubCanvas()
        {
            GetComponentsInChildren(true, s_SubCanvasList);
            for (int i = 0; i < s_SubCanvasList.Count; i++)
            {
                Canvas subCanvas = s_SubCanvasList[i];
                if (subCanvas != canvas && !subCanvas.overrideSorting &&
                    !subCanvas.TryGetComponent(out OverlayCanvasNotifier notifier))
                {
                    subCanvas.gameObject.AddComponent<OverlayCanvasNotifier>();
                }
            }

            s_SubCanvasList.Clear();
        }

        private void SetRaycastBlockingMask(GameObject obj, bool overlayEnable, int overlayUILayer)
        {
            obj.GetComponents(s_RaycasterList);
            for (int i = 0; i < s_RaycasterList.Count; i++)
            {
                if (overlayEnable)
                {
                    s_RaycasterList[i].blockingMask |= 1 << overlayUILayer;
                }
                else
                {
                    s_RaycasterList[i].blockingMask ^= 1 << overlayUILayer;
                }
            }

            s_RaycasterList.Clear();

            if (obj.TryGetComponent(out TrackedDeviceGraphicRaycaster raycaster))
            {
                if (overlayEnable)
                {
                    raycaster.blockingMask |= 1 << overlayUILayer;
                }
                else
                {
                    raycaster.blockingMask ^= 1 << overlayUILayer;
                }
            }
        }

        private void UpdateOverlay()
        {
            SetCanvasSortingLayerOverride(true);
            if (m_Overlay != null)
            {
                RectTransform rectTf = m_Overlay.transform as RectTransform;
                if (rectTf != null)
                {
                    if (rectTf.parent != rectTransform.parent)
                    {
                        if (rectTransform.parent != null && rectTransform.parent.gameObject.activeInHierarchy)
                        {
                            rectTf.SetParent(rectTransform.parent, false);
                        }
                    }
#if UNITY_EDITOR
                    if (rectTransform.parent == rectTf)
                    {
                        rectTransform.SetParent(rectTf.parent);
                    }
#endif
                    int siblingIndex = rectTransform.GetSiblingIndex();
                    if (rectTf.GetSiblingIndex() != (siblingIndex + 1))
                    {
                        rectTf.SetSiblingIndex(siblingIndex + 1);
                    }

                    if (rectTransform.hasChanged)
                    {
                        rectTf.rotation = rectTransform.rotation;
                        rectTf.localScale = rectTransform.localScale;

                        Rect rect = rectTransform.rect;
                        rectTf.sizeDelta = (useCustomSize
                            ? new Vector2(width, height)
                            : new Vector2(rect.width, rect.height));
                        rectTf.position = rectTransform.TransformPoint(new Vector3(offset.x, offset.y, 0));

                        rectTransform.hasChanged = false;
                    }
                }
            }

            if (holeCanvas != null && canvas !=null)
            {
                holeCanvas.sortingOrder = canvas.sortingOrder;
            }
        }

        private void UpdateOverlayTexture()
        {
            if (m_Overlay != null && m_Overlay.layerTextures[0] != m_RenderTexture)
            {
#if PICOVEDIO_PXR
                m_Overlay.SetTexture(m_RenderTexture, true, true);
#else
                m_Overlay.SetTexture(m_RenderTexture, true);
#endif
            }
            
            Canvas.preWillRenderCanvases -= UpdateOverlayTexture;
        }

        public void RegisterSubCanvasNotifier(OverlayCanvasNotifier notifier)
        {
            if (m_SubCanvasNotifierSet.Contains(notifier))
            {
                return;
            }

            Canvas subCanvas = notifier.canvas;
            if (subCanvas != null)
            {
                if (subCanvas.overrideSorting)
                {
                    return;
                }

                m_SubCanvasNotifierSet.Add(notifier);
                bool overlayEnable = OverlayCanvasManager.instance.enable && isActiveAndEnabled && !notifier.ignoreOverlay;
                int overlayUILayer = OverlayCanvasManager.instance.overlayUILayer;
                if (overlayUILayer <= 32 && overlayUILayer >= 0)
                {
                    subCanvas.gameObject.layer = overlayEnable ? overlayUILayer : LayerMask.NameToLayer("UI");
                }
                
                SetRaycastBlockingMask(subCanvas.gameObject, overlayEnable, overlayUILayer);
                
                GetComponentsInChildren(s_GraphicList);
                foreach (var graphic in s_GraphicList)
                {
                    if (graphic.canvas == subCanvas)
                    {
                        graphic.SetMaterialDirty();
                    }
                }
            }
        }

        public void UnRegisterSubCanvasNotifier(OverlayCanvasNotifier notifier)
        {
            if (m_SubCanvasNotifierSet.Contains(notifier))
            {
                m_SubCanvasNotifierSet.Remove(notifier);
            }
        }

        public void UpdateOverlayStatus()
        {
            bool overlayEnable = OverlayCanvasManager.instance.enable && isActiveAndEnabled;
            if (canvas != null && m_Overlay != null)
            {
                int overlayUILayer = OverlayCanvasManager.instance.overlayUILayer;
                if (overlayUILayer <= 32 && overlayUILayer >= 0)
                {
                    GameObject obj = canvas.gameObject;
                    obj.layer = overlayEnable ? overlayUILayer : LayerMask.NameToLayer("UI");
                    SetRaycastBlockingMask(obj, overlayEnable, overlayUILayer);
                    foreach (OverlayCanvasNotifier notifier in m_SubCanvasNotifierSet)
                    {
                        bool subCanvasOverlay = overlayEnable && !notifier.ignoreOverlay;
                        if (notifier != null)
                        {
                            notifier.canvas.sortingLayerID = 0;
                            GameObject subObj = notifier.gameObject;
                            subObj.layer = subCanvasOverlay ? overlayUILayer : LayerMask.NameToLayer("UI");
                            SetRaycastBlockingMask(subObj, subCanvasOverlay, overlayUILayer);
                        }
                    }

                    GetComponentsInChildren(s_GraphicList);
                    foreach (var graphic in s_GraphicList)
                    {
                        graphic.SetMaterialDirty();
                    }

                    s_GraphicList.Clear();
                }

                canvas.overrideSorting = overlayEnable;
                canvas.sortingLayerID = 0;
                m_Overlay.gameObject.SetActive(overlayEnable);
                rectTransform.hasChanged = true;
            }
        }

        public void ResetRenderTexture()
        {
            if (!OverlayCanvasManager.instance.enable || !isActiveAndEnabled)
            {
                return;
            }

            Rect rect = rectTransform.rect;
            Vector2 size = useCustomSize
                ? new Vector2(width, height)
                : new Vector2(rect.width, rect.height);

            if (m_RenderTexture == null || m_RenderTexture.width != (int)size.x ||
                m_RenderTexture.height != (int)size.y)
            {
                if (m_RenderTexture != null)
                {
                    m_RenderTexture.Release();
                }
#if PICO_UGUI
                m_RenderTexture = new RenderTexture((int)size.x, (int)size.y, 24, GraphicsFormat.R8G8B8A8_UNorm)
#else
                m_RenderTexture = new RenderTexture((int)size.x, (int)size.y, 24, GraphicsFormat.R8G8B8A8_SRGB)
#endif
                {
                    name = name + " Overlay", filterMode = FilterMode.Bilinear
                };
            }
            
            if (holeImage != null)
            {
                holeImage.material = m_OcclusionBetweenOverlayLayers
                    ? uiMaterialForUnderlayHole
                    : uiMaterialForUnderlayHoleNoOcclusion;
                holeImage.texture = m_RenderTexture;
            }
            
            Canvas.preWillRenderCanvases -= UpdateOverlayTexture;
            Canvas.preWillRenderCanvases += UpdateOverlayTexture;
        }

        public bool TryGetRenderTexture(out RenderTexture rt)
        {
            rt = renderTexture;
            return rt != null;
        }

        public void SetCanvasSortingLayerOverride(bool enable)
        {
            bool overlayEnable = enable && OverlayCanvasManager.instance.enable && isActiveAndEnabled;
            if (canvas != null && m_Overlay != null && holeImage != null)
            {
                foreach (OverlayCanvasNotifier notifier in m_SubCanvasNotifierSet)
                {
                    bool subCanvasOverlay = overlayEnable && !notifier.ignoreOverlay;
                    if (notifier != null)
                    {
                        notifier.canvas.sortingLayerID = subCanvasOverlay ? sortingLayerID : 0;
                    }
                }

                canvas.sortingLayerID = overlayEnable ? sortingLayerID : 0;
            }
        }
    }
}