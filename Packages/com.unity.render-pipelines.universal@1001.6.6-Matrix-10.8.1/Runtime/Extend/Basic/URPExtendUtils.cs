using System;
using System.Collections.Generic;

namespace UnityEngine.Rendering.Universal
{
    public static class URPExtendUtils
    {
        public static void SetComputeBufferData<T>(CommandBuffer cmd, ComputeBuffer buffer, List<T> data) where T : struct
        {
            #if UNITY_2021_3_OR_NEWER
                cmd.SetBufferData(buffer, data);
            #else
                cmd.SetComputeBufferData(buffer, data);
            #endif
        }
    }
}