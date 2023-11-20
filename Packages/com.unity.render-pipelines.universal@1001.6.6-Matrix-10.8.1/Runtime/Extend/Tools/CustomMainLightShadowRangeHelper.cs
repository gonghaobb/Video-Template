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
using UnityEngine.Rendering.Universal;

namespace Matrix.CommonShader
{
    public class CustomMainLightShadowRangeHelper
    {
        public static void Enable(bool isEnable)
        {
            UniversalRenderPipeline.asset.enableCustomMainLightShadowRange = isEnable;
        }

        public static void SetRange(float minX, float maxX, float minY, float maxY,
            float minZ, float maxZ)
        {
            UniversalRenderPipeline.asset.customMainLightShadowRange.minX = minX;
            UniversalRenderPipeline.asset.customMainLightShadowRange.maxX = maxX;
            UniversalRenderPipeline.asset.customMainLightShadowRange.minY = minY;
            UniversalRenderPipeline.asset.customMainLightShadowRange.maxY = maxY;
            UniversalRenderPipeline.asset.customMainLightShadowRange.minZ = minZ;
            UniversalRenderPipeline.asset.customMainLightShadowRange.maxZ = maxZ;
        }

        public static void ClearRange()
        {
            UniversalRenderPipeline.asset.customMainLightShadowRange.minX = Mathf.Infinity;
            UniversalRenderPipeline.asset.customMainLightShadowRange.maxX = Mathf.NegativeInfinity;
            UniversalRenderPipeline.asset.customMainLightShadowRange.minY = Mathf.Infinity;
            UniversalRenderPipeline.asset.customMainLightShadowRange.maxY = Mathf.NegativeInfinity;
            UniversalRenderPipeline.asset.customMainLightShadowRange.minZ = Mathf.Infinity;
            UniversalRenderPipeline.asset.customMainLightShadowRange.maxZ = Mathf.NegativeInfinity;
        }

        public static void AddBounds(Bounds bouds)
        {
            AddBoundBox(bouds.min.x, bouds.max.x, bouds.min.y, bouds.max.y, bouds.min.z, bouds.max.z);
        }

        public static void AddBoundBox(Vector3 center, Vector3 size)
        {
            Vector3 extent = size * 0.5f;
            AddBoundBox(center.x - extent.x, center.x + extent.x, center.y - extent.y, center.y + extent.y, center.z - extent.z, center.z + extent.z);
        }

        public static void AddBoundBox(float minX, float maxX, float minY, float maxY,
            float minZ, float maxZ)
        {
            UniversalRenderPipeline.asset.customMainLightShadowRange.minX = Mathf.Min(minX, UniversalRenderPipeline.asset.customMainLightShadowRange.minX);
            UniversalRenderPipeline.asset.customMainLightShadowRange.maxX = Mathf.Max(maxX, UniversalRenderPipeline.asset.customMainLightShadowRange.maxX);
            UniversalRenderPipeline.asset.customMainLightShadowRange.minY = Mathf.Min(minY, UniversalRenderPipeline.asset.customMainLightShadowRange.minY);
            UniversalRenderPipeline.asset.customMainLightShadowRange.maxY = Mathf.Max(maxY, UniversalRenderPipeline.asset.customMainLightShadowRange.maxY);
            UniversalRenderPipeline.asset.customMainLightShadowRange.minZ = Mathf.Min(minZ, UniversalRenderPipeline.asset.customMainLightShadowRange.minZ);
            UniversalRenderPipeline.asset.customMainLightShadowRange.maxZ = Mathf.Max(maxZ, UniversalRenderPipeline.asset.customMainLightShadowRange.maxZ);
        }

        public static void AddBoundsList(Bounds[] boundsList)
        {
            if (boundsList == null)
            {
                return;
            }

            for (int i = 0; i < boundsList.Length; ++i)
            {
                AddBounds(boundsList[i]);
            }
        }

        public static void AddSphere(Vector3 center, float radius)
        {
            AddBoundBox(center, new Vector3(radius, radius, radius));
        }

        public static void AddBoxCollider(BoxCollider boxCollider)
        {
            if (boxCollider == null)
            {
                Debug.LogError("CustomMainLightShadowRangeHelper.AddBoxCollider boxCollider is null.");
                return;
            }
            AddBoundBox(boxCollider.center, boxCollider.size);
        }

        public static void AddSphereCollider(SphereCollider sphereCollider)
        {
            if (sphereCollider == null)
            {
                Debug.LogError("CustomMainLightShadowRangeHelper.AddSphereCollider sphereCollider is null.");
                return;
            }

            Vector3 center = sphereCollider.center;
            float radius = sphereCollider.radius;
            AddBoundBox(center, new Vector3(radius, radius, radius));
        }
    }
}