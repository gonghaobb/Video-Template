﻿/*******************************************************************************
Copyright © 2015-2022 PICO Technology Co., Ltd.All rights reserved.  

NOTICE：All information contained herein is, and remains the property of 
PICO Technology Co., Ltd. The intellectual and technical concepts 
contained herein are proprietary to PICO Technology Co., Ltd. and may be 
covered by patents, patents in process, and are protected by trade secret or 
copyright law. Dissemination of this information or reproduction of this 
material is strictly forbidden unless prior written permission is obtained from
PICO Technology Co., Ltd. 
*******************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR;
using Debug = UnityEngine.Debug;

namespace Unity.XR.PXR
{
    public class PXR_OverLay : MonoBehaviour, IComparable<PXR_OverLay>
    {
        private const string TAG = "[PXR_CompositeLayers]";
        public static List<PXR_OverLay> Instances = new List<PXR_OverLay>();

        private static int overlayID = 0;
        [NonSerialized]
        public int overlayIndex;
        public int layerDepth;
        public int imageIndex = 0;
        public OverlayType overlayType = OverlayType.Overlay;
        public OverlayShape overlayShape = OverlayShape.Quad;
        public TextureType textureType = TextureType.ExternalSurface;
        public Transform overlayTransform;
        public Camera xrRig;

        public Texture[] layerTextures = new Texture[2] { null, null };
        public bool isDynamic = false;
        public int[] overlayTextureIds = new int[2];
        public Matrix4x4[] mvMatrixs = new Matrix4x4[2];
        public Vector3[] modelScales = new Vector3[2];
        public Quaternion[] modelRotations = new Quaternion[2];
        public Vector3[] modelTranslations = new Vector3[2];
        public Quaternion[] cameraRotations = new Quaternion[2];
        public Vector3[] cameraTranslations = new Vector3[2];
        public Camera[] overlayEyeCamera = new Camera[2];

        public bool overrideColorScaleAndOffset = false;
        public Vector4 colorScale = Vector4.one;
        public Vector4 colorOffset = Vector4.zero;

        // Eac
        public Vector3 offsetPosLeft = Vector3.one;
        public Vector3 offsetPosRight = Vector3.one;
        public Vector4 offsetRotLeft = Vector4.one;
        public Vector4 offsetRotRight = Vector4.one;
        public DegreeType degreeType = DegreeType.Eac360;
        public float overlapFactor = 0;

        private Vector4 overlayLayerColorScaleDefault = Vector4.one;
        private Vector4 overlayLayerColorOffsetDefault = Vector4.zero;

        public bool isExternalAndroidSurface = false;
        public bool isExternalAndroidSurfaceDRM = false;
        public Surface3DType externalAndroidSurface3DType = Surface3DType.Single;
        public IntPtr externalAndroidSurfaceObject = IntPtr.Zero;
        public delegate void ExternalAndroidSurfaceObjectCreated();
        public ExternalAndroidSurfaceObjectCreated externalAndroidSurfaceObjectCreated = null;

        // 360 
        public float radius = 0; // >0

        // ImageRect
        public bool useImageRect = false;
        public TextureRect textureRect = TextureRect.StereoScopic;
        public DestinationRect destinationRect = DestinationRect.Default;
        public Rect srcRectLeft = new Rect(0, 0, 1, 1);
        public Rect srcRectRight = new Rect(0, 0, 1, 1);
        public Rect dstRectLeft = new Rect(0, 0, 1, 1);
        public Rect dstRectRight = new Rect(0, 0, 1, 1);

        public PxrRecti imageRectLeft;
        public PxrRecti imageRectRight;

        //PicoVideo;PICO VIDEO Modify;Begin
        public UInt32 Width;
        public UInt32 Height;
        // 是否在awake的时候初始化RT，为了兼容以前旧了浏览器的逻辑，模式是不初始化的
        public bool initBuffer = false;
        public bool forceSRGB = false;
        private bool m_IsGamma = false;
        private bool isSRGBTexture = false;
        
        /// <summary>
        /// 是否支持ExternalRT接口（只有GLES3才支持）
        /// </summary>
        private static bool isSupportExternalRT = false;
        /// <summary>
        /// 是否使用ExternalRT
        /// </summary>
        private bool isUseExternalRT = false;
        
        private int srcX, srcY, copyWidth, copyHeight;
        private bool useFifaCopy = false;
        private bool isOES = false;
        private static Material OESM;

        private List<int> m_UsedLayerIDList = new List<int>(); //PicoVideo;LayerDestroyFixed;wujunlin
        //PicoVideo;PICO VIDEO Modify;End

        // LayerBlend
        public bool useLayerBlend = false;
        public PxrBlendFactor srcColor = PxrBlendFactor.PxrBlendFactorOne;
        public PxrBlendFactor dstColor = PxrBlendFactor.PxrBlendFactorOne;
        public PxrBlendFactor srcAlpha = PxrBlendFactor.PxrBlendFactorOne;
        public PxrBlendFactor dstAlpha = PxrBlendFactor.PxrBlendFactorOne;
        public float[] colorMatrix = new float[18] {
            1,0,0, // left
            0,1,0,
            0,0,1,
            1,0,0, // right
            0,1,0,
            0,0,1,
        };

        public bool isClones = false;
        public bool isClonesToNew = false;
        public PXR_OverLay originalOverLay;

        private bool toCreateSwapChain = false;
        private bool toCopyRT = false;
        private bool copiedRT = false;
        private int eyeCount = 2;
        private UInt32 imageCounts = 0;
        private PxrLayerParam overlayParam = new PxrLayerParam();
        private struct NativeTexture
        {
            public Texture[] textures;
        };
        private NativeTexture[] nativeTextures;
        private static Material cubeM;
        
        //PicoVideo;SRGB_Fixed;wujunlin;Begin
        private static Material s_BlitMaterial;
        private const  string SRGB_TO_LINEAR_CONVERSION = "_SRGB_TO_LINEAR_CONVERSION";
        private static readonly int s_SourceTex = Shader.PropertyToID("_SourceTex");
        //PicoVideo;SRGB_Fixed;wujunlin;End
        
        private IntPtr leftPtr = IntPtr.Zero;
        private IntPtr rightPtr = IntPtr.Zero;


        public int CompareTo(PXR_OverLay other)
        {
            return layerDepth.CompareTo(other.layerDepth);
        }

        protected void Awake()
        {
            //PicoVideo;ExternalRT;lidonghong;Begin
#if PICO_VIDEO_EXTERNAL_RENDER_TEXTURE
            isSupportExternalRT = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3;
#endif
            //PicoVideo;ExternalRT;lidonghong;End
            
            xrRig = Camera.main;
            Instances.Add(this);
            if (null == xrRig.gameObject.GetComponent<PXR_OverlayManager>())
            {
                xrRig.gameObject.AddComponent<PXR_OverlayManager>();
            }

            overlayEyeCamera[0] = xrRig;
            overlayEyeCamera[1] = xrRig;

            overlayTransform = GetComponent<Transform>();
#if UNITY_ANDROID && !UNITY_EDITOR
            if (overlayTransform != null)
            {
                MeshRenderer render = overlayTransform.GetComponent<MeshRenderer>();
                if (render != null)
                {
                    render.enabled = false;
                }
            }
#endif

            if (!isClones && initBuffer)  //PicoVideo;PICO VIDEO Modify;
            {
                InitializeBuffer();
            }
        }

        private void Start()
        {
            if (isClones && initBuffer)  //PicoVideo;PICO VIDEO Modify;
            {
                InitializeBuffer();
            }

            if (PXR_Manager.Instance == null)
            {
                return;
            }

            Camera[] cam = PXR_Manager.Instance.GetEyeCamera();
            if (cam[0] != null && cam[0].enabled)
            {
                RefreshCamera(cam[0], cam[0]);
            }
            else if (cam[1] != null && cam[2] != null)
            {
                RefreshCamera(cam[1], cam[2]);
            }
        }

        public void RefreshCamera(Camera leftCamera,Camera rightCamera)
        {
            overlayEyeCamera[0] = leftCamera;
            overlayEyeCamera[1] = rightCamera;
        }

        public void InitializeBuffer()
        {
            overlayID++;
            overlayIndex = overlayID;
            if (0 == overlayShape)
            {
                overlayShape = OverlayShape.Quad;
            }

            m_UsedLayerIDList.Add(overlayIndex);//PicoVideo;LayerDestroyFixed;wujunlin
            overlayParam.layerId = overlayIndex;
            overlayParam.layerShape = overlayShape;
            overlayParam.layerType = overlayType;

            
            if (GraphicsDeviceType.Vulkan == SystemInfo.graphicsDeviceType)
            {
                overlayParam.format = QualitySettings.activeColorSpace == ColorSpace.Linear ? (UInt64)ColorForamt.VK_FORMAT_R8G8B8A8_SRGB : (UInt64)RenderTextureFormat.Default;
            }
            else
            {
                overlayParam.format = QualitySettings.activeColorSpace == ColorSpace.Linear ? (UInt64)ColorForamt.GL_SRGB8_ALPHA8 : (UInt64)RenderTextureFormat.Default;
            }

            if (null == layerTextures[0] && null != layerTextures[1])
            {
                layerTextures[0] = layerTextures[1];
            }

            if (layerTextures[1] != null)
            {
                overlayParam.width = (uint)layerTextures[1].width;
                overlayParam.height = (uint)layerTextures[1].height;
            }
            else
            {
                overlayParam.width = (uint)PXR_Plugin.System.UPxr_GetConfigInt(ConfigType.RenderTextureWidth);
                overlayParam.height = (uint)PXR_Plugin.System.UPxr_GetConfigInt(ConfigType.RenderTextureHeight);
            }

            //PicoVideo;SRGB_Fixed;wujunlin;Begin
            isSRGBTexture = false;
            if (layerTextures[1] is RenderTexture rt)
            {
                isSRGBTexture = rt.sRGB;
            }
#if PICO_VIDEO_TEXTURE2D_SRGB_IDENTIFIER
            else if (layerTextures[1] is Texture2D t2d)
            {
                isSRGBTexture = t2d.sRGB;
            }
#endif
            //PicoVideo;SRGB_Fixed;wujunlin;End

            overlayParam.sampleCount = 1;

            if (OverlayShape.Cubemap == overlayShape)
            {
                overlayParam.faceCount = 6;
                if (cubeM == null)
                    cubeM = new Material(Shader.Find("PXR_SDK/PXR_CubemapBlit"));
            }
            else
            {
                overlayParam.faceCount = 1;
                
                //PicoVideo;DirectOES;baoqinshun;Begin
                if (OESM == null)
                {
                    OESM = new Material(Shader.Find("PXR_SDK/PXR_OESBlit"));
                }
                //PicoVideo;DirectOES;baoqinshun;End
            }

            //PicoVideo;SRGB_Fixed;wujunlin;Begin
            if (s_BlitMaterial == null)
            {
                s_BlitMaterial = new Material(Shader.Find("Hidden/Universal Render Pipeline/Blit"));
            }
            //PicoVideo;SRGB_Fixed;wujunlin;End

            overlayParam.arraySize = 1;
            overlayParam.mipmapCount = 1;
            overlayParam.layerFlags = 0;

            if (isClones)
            {
                if (null != originalOverLay)
                {
                    overlayParam.layerFlags |= (UInt32)PxrLayerCreateFlags.PxrLayerFlagSharedImagesBetweenLayers;
                    leftPtr = Marshal.AllocHGlobal(Marshal.SizeOf(originalOverLay.overlayIndex));
                    rightPtr = Marshal.AllocHGlobal(Marshal.SizeOf(originalOverLay.overlayIndex));
                    Marshal.WriteInt64(leftPtr, originalOverLay.overlayIndex);
                    Marshal.WriteInt64(rightPtr, originalOverLay.overlayIndex);
                    overlayParam.leftExternalImages = leftPtr;
                    overlayParam.rightExternalImages = rightPtr;
                    isExternalAndroidSurface = originalOverLay.isExternalAndroidSurface;
                    isDynamic = originalOverLay.isDynamic;
                    overlayParam.width = (UInt32)Mathf.Min(overlayParam.width, originalOverLay.overlayParam.width);
                    overlayParam.height = (UInt32)Mathf.Min(overlayParam.height, originalOverLay.overlayParam.height);
                }
                else
                {
                    PLog.e(TAG, "In clone state, originalOverLay cannot be empty!");
                }
            }

            if (isExternalAndroidSurface)
            {
                //PicoVideo;PICO VIDEO Modify;Begin
                overlayParam.width = initBuffer ? 1024 : Width;
                overlayParam.height = initBuffer ? 1024 : Height;
                //PicoVideo;PICO VIDEO Modify;End
                
                if (isExternalAndroidSurfaceDRM)
                {
                    overlayParam.layerFlags |= (UInt32)(PxrLayerCreateFlags.PxrLayerFlagAndroidSurface | PxrLayerCreateFlags.PxrLayerFlagProtectedContent);
                }
                else
                {
                    overlayParam.layerFlags |= (UInt32)PxrLayerCreateFlags.PxrLayerFlagAndroidSurface;
                }

                if (Surface3DType.LeftRight == externalAndroidSurface3DType)
                {
                    overlayParam.layerFlags |= (UInt32)PxrLayerCreateFlags.PxrLayerFlag3DLeftRightSurface;
                }
                else if (Surface3DType.TopBottom == externalAndroidSurface3DType)
                {
                    overlayParam.layerFlags |= (UInt32)PxrLayerCreateFlags.PxrLayerFlag3DTopBottomSurface;
                }

                overlayParam.layerLayout = LayerLayout.Mono;
                IntPtr layerParamPtr = Marshal.AllocHGlobal(Marshal.SizeOf(overlayParam));
                Marshal.StructureToPtr(overlayParam, layerParamPtr, false);
                PXR_Plugin.Render.UPxr_CreateLayer(layerParamPtr);
                Marshal.FreeHGlobal(layerParamPtr);
            }
            else
            {
                if (isDynamic)
                {
                    overlayParam.layerFlags = 0;
                }
                else
                {
                    overlayParam.layerFlags |= (UInt32)PxrLayerCreateFlags.PxrLayerFlagStaticImage;
                }

                if ((layerTextures[0] != null && layerTextures[1] != null && layerTextures[0] == layerTextures[1]) || null == layerTextures[1])
                {
                    eyeCount = 1;
                    overlayParam.layerLayout = LayerLayout.Mono;
                }
                else
                {
                    eyeCount = 2;
                    overlayParam.layerLayout = LayerLayout.Stereo;
                }

                PXR_Plugin.Render.UPxr_CreateLayerParam(overlayParam);
                toCreateSwapChain = true;
                CreateTexture();
            }
        }

        public void CreateExternalSurface(PXR_OverLay overlayInstance)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (IntPtr.Zero != overlayInstance.externalAndroidSurfaceObject)
            {
                return;
            }

            PXR_Plugin.Render.UPxr_GetLayerAndroidSurface(overlayInstance.overlayIndex, 0, ref overlayInstance.externalAndroidSurfaceObject);
            PLog.i(TAG, string.Format("CreateExternalSurface: Overlay Type:{0}, LayerDepth:{1}, SurfaceObject:{2}", overlayInstance.overlayType, overlayInstance.overlayIndex, overlayInstance.externalAndroidSurfaceObject));
            
            if (IntPtr.Zero == overlayInstance.externalAndroidSurfaceObject || null == overlayInstance.externalAndroidSurfaceObjectCreated)
            {
                return;
            }
            
            overlayInstance.externalAndroidSurfaceObjectCreated();
#endif
        }

        public void UpdateCoords()
        {
            if (null == overlayTransform || !overlayTransform.gameObject.activeSelf || null == overlayEyeCamera[0] || null == overlayEyeCamera[1])
            {
                return;
            }

            for (int i = 0; i < mvMatrixs.Length; i++)
            {
                mvMatrixs[i] = overlayEyeCamera[i].worldToCameraMatrix * overlayTransform.localToWorldMatrix;
                if (overlayTransform is RectTransform uiTransform)
                {
                    var rect = uiTransform.rect;
                    var lossyScale = overlayTransform.lossyScale;
                    modelScales[i] = new Vector3(rect.width * lossyScale.x,
                        rect.height * lossyScale.y, 1);
                    modelTranslations[i] = uiTransform.TransformPoint(rect.center);
                }
                else
                {
                    modelScales[i] = overlayTransform.localScale;   //PicoVideo;Transform_Fixed;baoqinshun;
                    modelTranslations[i] = overlayTransform.position;
                }
                modelRotations[i] = overlayTransform.rotation;
                cameraRotations[i] = overlayEyeCamera[i].transform.rotation;
                cameraTranslations[i] = overlayEyeCamera[i].transform.position;
            }
        }

        public bool CreateTexture()
        {
            if (!toCreateSwapChain)
            {
                return false;
            }

            if (null == nativeTextures)
                nativeTextures = new NativeTexture[eyeCount];

            for (int i = 0; i < eyeCount; i++)
            {
                int ret = PXR_Plugin.Render.UPxr_GetLayerImageCount(overlayIndex, (EyeType)i, ref imageCounts);
                if (ret != 0 || imageCounts < 1)
                {
                    return false;
                }

                if (null == nativeTextures[i].textures)
                {
                    nativeTextures[i].textures = new Texture[imageCounts];
                }

                for (int j = 0; j < imageCounts; j++)
                {
                    IntPtr ptr = IntPtr.Zero;
                    PXR_Plugin.Render.UPxr_GetLayerImagePtr(overlayIndex, (EyeType)i, j, ref ptr);

                    if (IntPtr.Zero == ptr)
                    {
                        return false;
                    }

                    Texture texture;
                    if (OverlayShape.Cubemap == overlayShape)
                    {
                        texture = Cubemap.CreateExternalTexture((int)overlayParam.width, TextureFormat.RGBA32, false, ptr);
                    }
                    else
                    {
                        //PicoVideo;ExternalRT;lidonghong;Begin
#if PICO_VIDEO_EXTERNAL_RENDER_TEXTURE
                        if (isSupportExternalRT && isDynamic)
                        {
                            isUseExternalRT = true;
                            texture = RenderTexture.CreateExternalRenderTexture((int)overlayParam.width,
                                (int)overlayParam.height, ptr);
                        }
                        else
#endif
                        {
                            texture = Texture2D.CreateExternalTexture((int)overlayParam.width, (int)overlayParam.height, TextureFormat.RGBA32, false, true, ptr);
                        }
                        
                        //PicoVideo;ExternalRT;lidonghong;End
                    }

                    if (null == texture)
                    {
                        return false;
                    }

                    nativeTextures[i].textures[j] = texture;
                }
            }

            toCreateSwapChain = false;
            toCopyRT = true;
            copiedRT = false;

            FreePtr();

            return true;
        }

        //PicoVideo;PICO VIDEO Modify;Begin
        public bool CopyRT()
        {
            if (useFifaCopy)
            {
                return CopyRTFifa();
            }
            else
            {
                return CopyRTOrigin();
            }
        }

        public bool CopyRTOrigin()
        {
            
            if (isClones)
            {
                return true;
            }
            
            if (!toCopyRT)
            {
                return copiedRT;
            }

            if (!isDynamic && copiedRT)
            {
                return copiedRT;
            }

            if (null == nativeTextures)
            {
                return false;
            }

            for (int i = 0; i < eyeCount; i++)
            {
                Texture nativeTexture = nativeTextures[i].textures[imageIndex];

                if (null == nativeTexture || null == layerTextures[i])
                    continue;

                RenderTexture texture = layerTextures[i] as RenderTexture;

                if (OverlayShape.Cubemap == overlayShape && null == layerTextures[i] as Cubemap)
                {
                    return false;
                }

                for (int f = 0; f < (int)overlayParam.faceCount; f++)
                {
                    if (QualitySettings.activeColorSpace == ColorSpace.Gamma && texture != null && texture.format == RenderTextureFormat.ARGB32)
                    {
                        Graphics.CopyTexture(layerTextures[i], f, 0, nativeTexture, f, 0);
                    }
                    else
                    {
                        //PicoVideo;ExternalRT;lidonghong;Begin
                        if (!isUseExternalRT)
                        {
                            RenderTextureDescriptor rtDes = new RenderTextureDescriptor((int)overlayParam.width, (int)overlayParam.height, RenderTextureFormat.ARGB32, 0);
                            rtDes.msaaSamples = (int)overlayParam.sampleCount;
                            rtDes.useMipMap = true;
                            rtDes.autoGenerateMips = false;
                            //PicoVideo;SRGB_Fixed;wujunlin
                            rtDes.sRGB = (!isOES && isSRGBTexture) || forceSRGB;//OESTexture颜色为Srgb但标记为Linear，因此rt也不需要勾选srgb

                            RenderTexture renderTexture = RenderTexture.GetTemporary(rtDes);

                            if (!renderTexture.IsCreated())
                            {
                                renderTexture.Create();
                            }
                            renderTexture.DiscardContents();

                            if (OverlayShape.Cubemap == overlayShape)
                            {
                                cubeM.SetInt("_d", f);
                                Graphics.Blit(layerTextures[i], renderTexture, cubeM);
                            }
                            else
                            {
                                Graphics.Blit(layerTextures[i], renderTexture);
                            }
                            Graphics.CopyTexture(renderTexture, 0, 0, nativeTexture, f, 0);
                            RenderTexture.ReleaseTemporary(renderTexture);
                        }
                        else
                        {
                            RenderTexture rt = nativeTexture as RenderTexture;
                            if (m_IsGamma)
                            {
                                s_BlitMaterial.EnableKeyword(SRGB_TO_LINEAR_CONVERSION);
                            }

                            s_BlitMaterial.SetTexture(s_SourceTex, layerTextures[i]);
                            Graphics.Blit(layerTextures[i], rt, s_BlitMaterial);
                            s_BlitMaterial.DisableKeyword(SRGB_TO_LINEAR_CONVERSION);
                        }
                        //PicoVideo;ExternalRT;lidonghong;End
                    }
                }
                copiedRT = true;
            }

            return copiedRT;
        }

        public void SetTexture(Texture texture, bool dynamic, bool isGamma = false)
        {
            if (isExternalAndroidSurface)
            {
                PLog.w(TAG, "Not support setTexture !");
                return;
            }

            if (isClones)
            {
                return;
            }
            else
            {
                foreach (PXR_OverLay overlay in PXR_OverLay.Instances)
                {
                    if (overlay.isClones && null != overlay.originalOverLay && overlay.originalOverLay.overlayIndex == overlayIndex)
                    {
                        overlay.DestroyLayer();
                        overlay.isClonesToNew = true;
                    }
                }
            }

            toCopyRT = false;
            DestroyLayer();
            for (int i = 0; i < layerTextures.Length; i++)
            {
                layerTextures[i] = texture;
            }
            
            isOES = false;          //PicoVideo;DirectOES;baoqinshun
            useFifaCopy = false;    //PicoVideo;DirectOES;baoqinshun
            m_IsGamma = isGamma;    //PicoVideo;SRGB_Fixed;wujunlin

            isDynamic = dynamic;
            InitializeBuffer();

            if (!isClones)
            {
                foreach (PXR_OverLay overlay in PXR_OverLay.Instances)
                {
                    if (overlay.isClones && overlay.isClonesToNew)
                    {
                        overlay.isOES = false;          //PicoVideo;DirectOES;baoqinshun      
                        overlay.useFifaCopy = false;    //PicoVideo;DirectOES;baoqinshun
                        overlay.m_IsGamma = isGamma;    //PicoVideo;SRGB_Fixed;wujunlin
                        
                        overlay.originalOverLay = this;
                        overlay.InitializeBuffer();
                        overlay.isClonesToNew = false;
                    }
                }
            }
        }

        public bool CopyRTFifa()
        {
            if (!toCopyRT)
            {
                return copiedRT;
            }

            if (!isDynamic && copiedRT)
            {
                return copiedRT;
            }

            for (int i = 0; i < eyeCount; i++)
            {
                Texture nativeTexture = nativeTextures[i].textures[imageIndex];

                if (null == nativeTexture)
                    continue;

                RenderTexture texture = layerTextures[i] as RenderTexture;

                if (OverlayShape.Cubemap == overlayShape && null == layerTextures[i] as Cubemap)
                {
                    return false;
                }

                for (int f = 0; f < (int)overlayParam.faceCount; f++)
                {
                    if (QualitySettings.activeColorSpace == ColorSpace.Gamma && texture != null && texture.format == RenderTextureFormat.ARGB32)
                    {
                        Graphics.CopyTexture(layerTextures[i], f, 0, nativeTexture, f, 0);
                    }
                    else
                    {
                        //PicoVideo;ExternalRT;lidonghong;Begin
                        if (!isUseExternalRT)
                        {
                            RenderTextureDescriptor rtDes = new RenderTextureDescriptor((int)overlayParam.width, (int)overlayParam.height, RenderTextureFormat.ARGB32, 0);
                            rtDes.msaaSamples = (int)overlayParam.sampleCount;
                            rtDes.useMipMap = true;
                            rtDes.autoGenerateMips = false;
                            rtDes.sRGB = (!isOES && isSRGBTexture) || forceSRGB;//OESTexture颜色为Srgb但标记为Linear，因此rt也不需要勾选srgb

                            RenderTexture renderTexture = RenderTexture.GetTemporary(rtDes);

                            if (!renderTexture.IsCreated())
                            {
                                renderTexture.Create();
                            }
                            renderTexture.DiscardContents();

                            if (OverlayShape.Cubemap == overlayShape)
                            {
                                cubeM.SetInt("_d", f);
                                Graphics.Blit(layerTextures[i], renderTexture, cubeM);
                            }
                            else
                            {
                                OESM.SetInt("texWidth", layerTextures[i].width);
                                OESM.SetInt("texHeight", layerTextures[i].height);
                                OESM.SetInt("dstWidth", (int)overlayParam.width);
                                OESM.SetInt("dstHeight", (int)overlayParam.height);
                                OESM.SetInt("offsetX", srcX);
                                OESM.SetInt("offsetY", srcY);
                                OESM.SetTexture("_ExternalOESTex", layerTextures[i]);
                                Graphics.Blit(layerTextures[i], renderTexture, OESM);
                            }
                            Graphics.CopyTexture(renderTexture, 0, 0, nativeTexture, f, 0);
                            RenderTexture.ReleaseTemporary(renderTexture);
                        }
                        else
                        {
                            RenderTexture rt = nativeTexture as RenderTexture;;
                            if (isOES)
                            {
                                OESM.SetInt("texWidth", layerTextures[i].width);
                                OESM.SetInt("texHeight", layerTextures[i].height);
                                OESM.SetInt("dstWidth", (int)overlayParam.width);
                                OESM.SetInt("dstHeight", (int)overlayParam.height);
                                OESM.SetInt("offsetX", srcX);
                                OESM.SetInt("offsetY", srcY);
                                OESM.SetTexture("_ExternalOESTex", layerTextures[i]);

                                bool keywordsEnable = OESM.IsKeywordEnabled(SRGB_TO_LINEAR_CONVERSION);
                                OESM.EnableKeyword(SRGB_TO_LINEAR_CONVERSION);
                                Graphics.Blit(layerTextures[i], rt, OESM);
                                if (!keywordsEnable)
                                {
                                    OESM.DisableKeyword(SRGB_TO_LINEAR_CONVERSION);
                                }
                            }
                            else
                            {
                                Graphics.Blit(layerTextures[i], rt);
                            }
                        }
                        //PicoVideo;ExternalRT;lidonghong;End
                    }
                }
                copiedRT = true;
            }

            return copiedRT;
        }
        
        public void SetTextureForFifa(Texture texture, bool dynamic, int srcX = 0,
            int srcY = 0, int width = 0, int height = 0)
        {
            if (isExternalAndroidSurface)
            {
                PLog.w(TAG, "Not support setTexture !");
                return;
            }   

            if (isClones)
            {
                return;
            }
            else
            {
                foreach (PXR_OverLay overlay in PXR_OverLay.Instances)
                {
                    if (overlay.isClones && null != overlay.originalOverLay && overlay.originalOverLay.overlayIndex == overlayIndex)
                    {
                        overlay.DestroyLayer();
                        overlay.isClonesToNew = true;
                    }
                }
            }
            
            toCopyRT = false;
            DestroyLayer();
            for (int i = 0; i < layerTextures.Length; i++)
            {
                layerTextures[i] = texture;
            }

            isOES = true;
            isDynamic = dynamic;
            useFifaCopy = true;
            this.srcX = srcX;
            this.srcY = srcY;
            copyWidth = width;
            copyHeight = height;
            InitializeBufferForFifa();
            
            if (!isClones)
            {
                foreach (PXR_OverLay overlay in PXR_OverLay.Instances)
                {
                    if (overlay.isClones && overlay.isClonesToNew)
                    {
                        overlay.isOES = false;   
                        overlay.useFifaCopy = false;
                        overlay.srcX = srcX;
                        overlay.srcY = srcY;
                        overlay.copyWidth = width;
                        overlay.copyHeight = height;
                        
                        overlay.originalOverLay = this;
                        overlay.InitializeBufferForFifa();
                        overlay.isClonesToNew = false;
                    }
                }
            }
        }
        
        public void InitializeBufferForFifa()
        {
            Debug.Log("InitializeBufferForFifa");
            overlayID++;
            overlayIndex = overlayID;
            if (0 == overlayShape)
            {
                overlayShape = OverlayShape.Quad;
            }

            m_UsedLayerIDList.Add(overlayIndex); //PicoVideo;LayerDestroyFixed;wujunlin
            overlayParam.layerId = overlayIndex;
            overlayParam.layerShape = overlayShape;
            overlayParam.layerType = overlayType;
            // overlayParam.format = (UInt64)RenderTextureFormat.Default;
            // overlayParam.format = (UInt64)RenderTextureFormat.Default;
            if (GraphicsDeviceType.Vulkan == SystemInfo.graphicsDeviceType)
            {
                //overlayParam.format = (UInt64)(QualitySettings.activeColorSpace == ColorSpace.Linear ? ColorForamt.VK_FORMAT_R8G8B8A8_SRGB : ColorForamt.VK_FORMAT_R8G8B8A8_UNORM);
                overlayParam.format = QualitySettings.activeColorSpace == ColorSpace.Linear ? (UInt64)ColorForamt.VK_FORMAT_R8G8B8A8_SRGB : (UInt64)RenderTextureFormat.Default;
            }
            else
            {
                //overlayParam.format = (UInt64)(QualitySettings.activeColorSpace == ColorSpace.Linear ? ColorForamt.GL_SRGB8_ALPHA8 : ColorForamt.GL_RGBA8);
                overlayParam.format = QualitySettings.activeColorSpace == ColorSpace.Linear ? (UInt64)ColorForamt.GL_SRGB8_ALPHA8 : (UInt64)RenderTextureFormat.Default;
            }

            
            if (null == layerTextures[0] && null != layerTextures[1])
            {
                layerTextures[0] = layerTextures[1];
            }

            if (useFifaCopy)
            {
                overlayParam.width = (uint)copyWidth;
                overlayParam.height = (uint)copyHeight;
            }
            else
            {
                if (layerTextures[1] != null)
                {
                    overlayParam.width = (uint)layerTextures[1].width;
                    overlayParam.height = (uint)layerTextures[1].height;
                }
                else
                {
                    overlayParam.width = (uint)PXR_Plugin.System.UPxr_GetConfigInt(ConfigType.RenderTextureWidth);
                    overlayParam.height = (uint)PXR_Plugin.System.UPxr_GetConfigInt(ConfigType.RenderTextureHeight);
                }    
            }
            

            overlayParam.sampleCount = 1;

            if (OverlayShape.Cubemap == overlayShape)
            {
                overlayParam.faceCount = 6;
                if (cubeM == null)
                    cubeM = new Material(Shader.Find("PXR_SDK/PXR_CubemapBlit"));
            }
            else
            {
                overlayParam.faceCount = 1;
                
                //PicoVideo;DirectOES;baoqinshun;Begin
                if (OESM == null)
                {
                    OESM = new Material(Shader.Find("PXR_SDK/PXR_OESBlit"));
                }
                //PicoVideo;DirectOES;baoqinshun;End
            }

            overlayParam.arraySize = 1;
            overlayParam.mipmapCount = 1;
            overlayParam.layerFlags = 0;

            if (isClones)
            {
                if (null != originalOverLay)
                {
                    overlayParam.layerFlags |= (UInt32)PxrLayerCreateFlags.PxrLayerFlagSharedImagesBetweenLayers;
                    leftPtr = Marshal.AllocHGlobal(Marshal.SizeOf(originalOverLay.overlayIndex));
                    rightPtr = Marshal.AllocHGlobal(Marshal.SizeOf(originalOverLay.overlayIndex));
                    Marshal.WriteInt64(leftPtr, originalOverLay.overlayIndex);
                    Marshal.WriteInt64(rightPtr, originalOverLay.overlayIndex);
                    overlayParam.leftExternalImages = leftPtr;
                    overlayParam.rightExternalImages = rightPtr;
                    isExternalAndroidSurface = originalOverLay.isExternalAndroidSurface;
                    isDynamic = originalOverLay.isDynamic;
                    overlayParam.width = (UInt32)Mathf.Min(overlayParam.width, originalOverLay.overlayParam.width);
                    overlayParam.height = (UInt32)Mathf.Min(overlayParam.height, originalOverLay.overlayParam.height);
                }
                else
                {
                    PLog.e(TAG, "In clone state, originalOverLay cannot be empty!");
                }
            }

            if (isExternalAndroidSurface)
            {
                //PicoVideo;PICO VIDEO Modify;Begin
                overlayParam.width = initBuffer ? 1024 : Width;
                overlayParam.height = initBuffer ? 1024 : Height;
                //PicoVideo;PICO VIDEO Modify;End
                
                if (isExternalAndroidSurfaceDRM)
                {
                    overlayParam.layerFlags = (UInt32)(PxrLayerCreateFlags.PxrLayerFlagAndroidSurface | PxrLayerCreateFlags.PxrLayerFlagProtectedContent);
                }
                else
                {
                    overlayParam.layerFlags = (UInt32)PxrLayerCreateFlags.PxrLayerFlagAndroidSurface;
                }

                if (Surface3DType.LeftRight == externalAndroidSurface3DType)
                {
                    overlayParam.layerFlags |= (UInt32)PxrLayerCreateFlags.PxrLayerFlag3DLeftRightSurface;
                }
                else if (Surface3DType.TopBottom == externalAndroidSurface3DType)
                {
                    overlayParam.layerFlags |= (UInt32)PxrLayerCreateFlags.PxrLayerFlag3DTopBottomSurface;
                }

                overlayParam.layerLayout = LayerLayout.Mono;
                IntPtr layerParamPtr = Marshal.AllocHGlobal(Marshal.SizeOf(overlayParam));
                Marshal.StructureToPtr(overlayParam, layerParamPtr, false);
                PXR_Plugin.Render.UPxr_CreateLayer(layerParamPtr);
                Marshal.FreeHGlobal(layerParamPtr);
            }
            else
            {
                if (isDynamic)
                {
                    overlayParam.layerFlags = 0;
                }
                else
                {
                    overlayParam.layerFlags = (UInt32)PxrLayerCreateFlags.PxrLayerFlagStaticImage;
                }

                if ((layerTextures[0] != null && layerTextures[1] != null && layerTextures[0] == layerTextures[1]) || null == layerTextures[1])
                {
                    eyeCount = 1;
                    overlayParam.layerLayout = LayerLayout.Mono;
                }
                else
                {
                    eyeCount = 2;
                    overlayParam.layerLayout = LayerLayout.Stereo;
                }

                PXR_Plugin.Render.UPxr_CreateLayerParam(overlayParam);
                toCreateSwapChain = true;
                CreateTexture();
            }
        }
        //PicoVideo;PICO VIDEO Modify;End

        private void FreePtr()
        {
            if (leftPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(leftPtr);
                leftPtr = IntPtr.Zero;
            }

            if (rightPtr != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(rightPtr);
                rightPtr = IntPtr.Zero;
            }
        }

        //PicoVideo;LayerDestroyFixed;wujunlin;Start
        private void DestroyUsedLayer()
        {
            //把已使用的Layer销毁，避免因为异步创建销毁的时序问题导致有过多Layer残留
            for (int i = 0; i < m_UsedLayerIDList.Count; i++)
            {
                if (PXR_Plugin.Render.UPxr_GetLayerImageCount(m_UsedLayerIDList[i], EyeType.EyeLeft, ref imageCounts) == 0)
                {
                    PXR_Plugin.Render.UPxr_DestroyLayerByRender(m_UsedLayerIDList[i]);
                }
            }
            m_UsedLayerIDList.Clear();
        }
        //PicoVideo;LayerDestroyFixed;wujunlin;End

        public void OnDestroy()
        {
            Debug.LogWarning($"OnDestroy : overlayIndex { overlayIndex }");
            DestroyUsedLayer(); //PicoVideo;LayerDestroyFixed;wujunlin
            DestroyLayer();
            Instances.Remove(this);
        }

        public void DestroyLayer()
        {
            Debug.LogWarning($"DestroyLayer : overlayIndex { overlayIndex } stacktrace {new StackTrace()}");
            
            if (!isClones)
            {
                List<PXR_OverLay> toDestroyClones = new List<PXR_OverLay>();
                foreach (PXR_OverLay overlay in Instances)
                {
                    if (overlay.isClones && null != overlay.originalOverLay && overlay.originalOverLay.overlayIndex == overlayIndex)
                    {
                        toDestroyClones.Add(overlay);
                    }
                }

                foreach (PXR_OverLay overLay in toDestroyClones)
                {
                    //PicoVideo;AvoidDestroyBeforeInit;Begin
                    if (overLay.overlayIndex > 0)
                    {
                        PXR_Plugin.Render.UPxr_DestroyLayerByRender(overLay.overlayIndex);
                        overLay.overlayIndex = 0;
                    }
                    //PicoVideo;AvoidDestroyBeforeInit;End
                    
                    ClearTexture();
                }
            }

            //PicoVideo;AvoidDumplicateDestroy;Begin
            if (overlayIndex > 0)
            {
                PXR_Plugin.Render.UPxr_DestroyLayerByRender(overlayIndex);
                overlayIndex = 0;
            }
            //PicoVideo;AvoidDumplicateDestroy;End
            
            ClearTexture();
        }

        private void ClearTexture()
        {
            FreePtr();

            if (isExternalAndroidSurface || null == nativeTextures || isClones)
            {
                return;
            }
            
            for (int i = 0; i < eyeCount; i++)
            {
                if (null == nativeTextures[i].textures)
                {
                    continue;
                }

                for (int j = 0; j < imageCounts; j++)
                    DestroyImmediate(nativeTextures[i].textures[j]);
            }

            nativeTextures = null;
        }

        public void SetLayerColorScaleAndOffset(Vector4 scale, Vector4 offset)
        {
            colorScale = scale;
            colorOffset = offset;
        }

        public void SetEACOffsetPosAndRot(Vector3 leftPos, Vector3 rightPos, Vector4 leftRot, Vector4 rightRot, float factor)
        {
            offsetPosLeft = leftPos;
            offsetPosRight = rightPos;
            offsetRotLeft = leftRot;
            offsetRotRight = rightRot;
            overlapFactor = factor;
        }

        public Vector4 GetLayerColorScale()
        {
            if (!overrideColorScaleAndOffset)
            {
                return overlayLayerColorScaleDefault;
            }
            return colorScale;
        }

        public Vector4 GetLayerColorOffset()
        {
            if (!overrideColorScaleAndOffset)
            {
                return overlayLayerColorOffsetDefault;
            }
            return colorOffset;
        }

        public PxrRecti getPxrRectiLeft(bool left)
        {
            if (left)
            {
                imageRectLeft.x = (int)(overlayParam.width * srcRectLeft.x);
                imageRectLeft.y = (int)(overlayParam.height * srcRectLeft.y);
                imageRectLeft.width = (int)(overlayParam.width * Mathf.Min(srcRectLeft.width, 1 - srcRectLeft.x));
                imageRectLeft.height = (int)(overlayParam.height * Mathf.Min(srcRectLeft.height, 1 - srcRectLeft.y));
                return imageRectLeft;
            }
            else
            {
                imageRectRight.x = (int)(overlayParam.width * srcRectRight.x);
                imageRectRight.y = (int)(overlayParam.height * srcRectRight.y);
                imageRectRight.width = (int)(overlayParam.width * Mathf.Min(srcRectRight.width, 1 - srcRectRight.x));
                imageRectRight.height = (int)(overlayParam.height * Mathf.Min(srcRectRight.height, 1 - srcRectRight.y));
                return imageRectRight;
            }
        }

        public enum OverlayShape
        {
            Quad = 1,
            Cylinder = 2,
            Equirect = 3,
            Cubemap = 5,
            Eac = 6
        }

        public enum OverlayType
        {
            Overlay = 0,
            Underlay = 1
        }

        public enum TextureType
        {
            ExternalSurface,
            DynamicTexture,
            StaticTexture
        }

        public enum LayerLayout
        {
            Stereo = 0,
            DoubleWide = 1,
            Array = 2,
            Mono = 3
        }

        public enum Surface3DType
        {
            Single = 0,
            LeftRight,
            TopBottom
        }

        public enum TextureRect
        {
            MonoScopic,
            StereoScopic,
            Custom
        }

        public enum DestinationRect
        {
            Default,
            Custom
        }
        
        public enum DegreeType
        {
            Eac360 = 0,
            Eac180 = 4,
        }

        public enum ColorForamt
        {
            VK_FORMAT_R8G8B8A8_UNORM = 37,
            VK_FORMAT_R8G8B8A8_SRGB = 43,
            GL_SRGB8_ALPHA8 = 0x8c43,
            GL_RGBA8 = 0x8058
        }
    }
}