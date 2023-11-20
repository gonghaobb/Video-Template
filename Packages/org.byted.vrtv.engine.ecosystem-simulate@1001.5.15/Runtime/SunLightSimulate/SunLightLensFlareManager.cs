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

using UnityEngine;
using System.Collections.Generic;

namespace Matrix.EcosystemSimulate
{
    public partial class SunLightManager
    {
        private Vector3 m_PreSunPos = Vector3.zero;

        private List<Vector4> m_WorldPos = new List<Vector4>();
        private List<Vector4> m_LfData = new List<Vector4>();
        private List<Vector4> m_FadeData = new List<Vector4>();
        private List<Color> m_Colors = new List<Color>();

        private void CreateLensFlare()
        {
            if (m_MainCamera == null || m_MainLight == null)
            {
                return;
            }

            if (!m_EnableLensFlare)
            {
                return;
            }
            if (m_MeshRenderer != null)
            {
                return;
            }

            GameObject go = GameObject.Find(LENS_FLARE_NAME);
            if (go == null)
            {
                go = new GameObject(LENS_FLARE_NAME);
            }

            m_MeshRenderer = go.GetComponent<MeshRenderer>();
            if (m_MeshRenderer == null)
            {
                m_MeshRenderer = go.AddComponent<MeshRenderer>();
            }

            m_MeshFilter = go.GetComponent<MeshFilter>();
            if (m_MeshFilter == null)
            {
                m_MeshFilter = go.AddComponent<MeshFilter>();
            }

            if (m_FlareList == null)
            {
                m_FlareList = new List<FlareSettings>();
            }

            if (m_MeshFilter.sharedMesh == null)
            {
                Mesh m = new Mesh();
                m.MarkDynamic();
                m_MeshFilter.mesh = m;
            }

            if (m_LensFlareTransform == null)
            {
                m_LensFlareTransform = go.transform;
            }

            UpdateGeometry();
            UpdateMaterials();

            enableLensFlare = m_EnableLensFlare;
        }

        private void UpdateLensFlarePos()
        {
            if(m_LensFlareTransform == null)
            {
                return;
            }
            if (m_MainCamera == null || m_MainLight == null)
            {
                //Debug.LogError("SunLightLensFlareManager : Main Camera 或者 Main Light 为空，已自动关闭 SunLensFlare");
                enableLensFlare = false;
                return;
            }


            Vector3 dir = m_LensFlareForward;
            if (dir == Vector3.zero)
            {
                dir = m_MainLight.transform.forward;
            }
            Vector3 sunWPos = m_MainCamera.transform.position - dir * m_MainCamera.farClipPlane;

            if (m_PreSunPos == sunWPos)
            {
                return;
            }

            m_LensFlareTransform.position = sunWPos;
            m_FarFadeEndDistance = m_MainCamera.farClipPlane;

            UpdateVaryingAttributes();

            m_PreSunPos = sunWPos;
        }

        private void DestroyLensFlare()
        {
            if (m_LensFlareTransform != null)
            {
                if(Application.isEditor)
                {
                    DestroyImmediate(m_LensFlareTransform.gameObject);
                }
                else
                {
                    Destroy(m_LensFlareTransform.gameObject);
                }
                m_MeshRenderer = null;
            }
        }

        private void EnableLensFlare()
        {
            CreateLensFlare();
            if (m_MeshRenderer != null)
            {
                m_MeshRenderer.enabled = true;
            }
        }

        private void DisableLensFlare()
        {
            if (m_MeshRenderer != null)
            {
                m_MeshRenderer.enabled = false;
            }
        }

        private void UpdateMaterials()
        {
            if (m_MeshRenderer == null)
            {
                return;
            }

            Material[] mats = new Material[m_FlareList.Count];

            int i = 0;
            foreach (FlareSettings f in m_FlareList)
            {
                mats[i] = f.Material;
                i++;
            }
            m_MeshRenderer.sharedMaterials = mats;
        }

        private void UpdateGeometry()
        {
            if (m_MeshFilter == null)
            {
                return;
            }

            Mesh m = m_MeshFilter.sharedMesh;

            List<Vector3> vertices = new List<Vector3>();
            foreach (FlareSettings item in m_FlareList)
            {
                vertices.Add(new Vector3(-1, -1, 0));
                vertices.Add(new Vector3(1, -1, 0));
                vertices.Add(new Vector3(1, 1, 0));
                vertices.Add(new Vector3(-1, 1));
            }
            m.SetVertices(vertices);

            List<Vector2> uvs = new List<Vector2>();
            foreach (FlareSettings item in m_FlareList)
            {
                uvs.Add(new Vector2(0, 1));
                uvs.Add(new Vector2(1, 1));
                uvs.Add(new Vector2(1, 0));
                uvs.Add(new Vector2(0, 0));
            }
            m.SetUVs(0, uvs);

            m.SetColors(GetLensFlareColor());
            m.SetUVs(1, GetLensFlareData());            // 射线位置，旋转，大小X，大小Y
            m.SetUVs(2, GetWorldPositionAndRadius());   // 位置 半径
            m.SetUVs(3, GetDistanceFadeData());         // 距离

            m.subMeshCount = m_FlareList.Count;

            for (int i = 0; i < m_FlareList.Count; i++)
            {
                int[] tris = new int[6];
                tris[0] = (i * 4) + 0;
                tris[1] = (i * 4) + 1;
                tris[2] = (i * 4) + 2;
                tris[3] = (i * 4) + 2;
                tris[4] = (i * 4) + 3;
                tris[5] = (i * 4) + 0;
                m.SetTriangles(tris, i);
            }

            Bounds b = m.bounds;
            b.extents = new Vector3(m_OcclusionRadius, m_OcclusionRadius, m_OcclusionRadius);
            m.bounds = b;
            m.UploadMeshData(false);
        }

        private void UpdateVaryingAttributes()
        {
            if (m_MeshFilter == null)
            {
                return;
            }

            Mesh m = m_MeshFilter.sharedMesh;
            m.SetColors(GetLensFlareColor());
            m.SetUVs(1, GetLensFlareData());
            m.SetUVs(2, GetWorldPositionAndRadius());
            m.SetUVs(3, GetDistanceFadeData());

            Bounds b = m.bounds;
            b.extents = new Vector3(m_OcclusionRadius, m_OcclusionRadius, m_OcclusionRadius);
            m.bounds = b;
        }

        private List<Color> GetLensFlareColor()
        {
            m_Colors.Clear();
            foreach (FlareSettings item in m_FlareList)
            {
                Color c = (item.MultiplyByLightColor && m_MainLight != null) ? item.Color * m_MainLight.color * m_MainLight.intensity : item.Color;

                m_Colors.Add(c);
                m_Colors.Add(c);
                m_Colors.Add(c);
                m_Colors.Add(c);
            }
            return m_Colors;
        }

        /// <summary>
        /// 射线位置，旋转，大小X，大小Y
        /// </summary>
        private List<Vector4> GetLensFlareData()
        {
            m_LfData.Clear();
            foreach (FlareSettings s in m_FlareList)
            {
                Vector4 data = new Vector4(s.RayPosition, s.AutoRotate ? -1 : Mathf.Abs(s.Rotation), s.Size.x, s.Size.y);
                m_LfData.Add(data);
                m_LfData.Add(data);
                m_LfData.Add(data);
                m_LfData.Add(data);
            }
            return m_LfData;
        }


        private List<Vector4> GetDistanceFadeData()
        {
            m_FadeData.Clear();

            foreach (FlareSettings s in m_FlareList)
            {
                Vector4 data = new Vector4(m_NearFadeStartDistance, m_NearFadeEndDistance, m_FarFadeStartDistance, m_FarFadeEndDistance);
                m_FadeData.Add(data);
                m_FadeData.Add(data);
                m_FadeData.Add(data);
                m_FadeData.Add(data);
            }
            return m_FadeData;
        }


        private List<Vector4> GetWorldPositionAndRadius()
        {
            m_WorldPos.Clear();
            Vector3 pos = m_LensFlareTransform.position;
            Vector4 value = new Vector4(pos.x, pos.y, pos.z, m_OcclusionRadius);
            foreach (FlareSettings s in m_FlareList)
            {
                m_WorldPos.Add(value);
                m_WorldPos.Add(value);
                m_WorldPos.Add(value);
                m_WorldPos.Add(value);
            }

            return m_WorldPos;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawSphere(m_LensFlareTransform.position, m_OcclusionRadius);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(m_LensFlareTransform.position, m_OcclusionRadius);
        }

        #region 外部接口

        public void OnValidateLensFlare()
        {
            if (Application.isPlaying)
            {
                enableLensFlare = enableLensFlare;
                UpdateGeometry();
                UpdateMaterials();
            }
        }

        public bool enableLensFlare
        {
            get { return m_EnableLensFlare; }
            set
            {
                m_EnableLensFlare = value;
                if (m_EnableLensFlare)
                {
                    EnableLensFlare();
                }
                else
                {
                    DisableLensFlare();
                }
            }
        }

        #endregion
    }
}