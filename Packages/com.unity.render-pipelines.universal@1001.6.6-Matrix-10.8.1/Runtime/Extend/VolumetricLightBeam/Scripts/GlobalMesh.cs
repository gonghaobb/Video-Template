using UnityEngine;
using System.Collections;

namespace VLB
{
    public static class GlobalMesh
    {
        public static Mesh Get()
        {
            //不再使用doubleside mesh ，统一在shader处理
            var needDoubleSided = Config.Instance.requiresDoubleSidedMesh;

            if (ms_Mesh == null)
            {
                Destroy();

                ms_Mesh = MeshGenerator.GenerateConeZ_Radius(
                    lengthZ: 1f,
                    radiusStart: 1f,
                    radiusEnd: 1f,
                    numSides: Config.Instance.sharedMeshSides,
                    numSegments: Config.Instance.sharedMeshSegments,
                    cap: true,
                    doubleSided: false);

                ms_Mesh.hideFlags = Consts.Internal.ProceduralObjectsHideFlags;
            }

            return ms_Mesh;
        }

        public static void Destroy()
        {
            if (ms_Mesh != null)
            {
                GameObject.DestroyImmediate(ms_Mesh);
                ms_Mesh = null;
            }
        }
        
        public static void DestroySingle()
        {
            if (ms_MeshSingleSide != null)
            {
                GameObject.DestroyImmediate(ms_MeshSingleSide);
                ms_MeshSingleSide = null;
            }
        }

        static Mesh ms_Mesh = null;
        static Mesh ms_MeshSingleSide = null;
    }
}
