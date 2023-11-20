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
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering.Universal
{
    public static class HistoryRTSystem
    {
        private static Dictionary<Camera, RTBufferContainer> m_RTBufferDict = new Dictionary<Camera, RTBufferContainer>();
        public static RTBufferContainer Get(Camera camera)
        {
            RTBufferContainer container = null;
            if (!m_RTBufferDict.TryGetValue(camera, out container))
            {
                container = new RTBufferContainer();
                m_RTBufferDict.Add(camera, container);
            }

            return container;
        }

        // 销毁所有history rt，by mjk
        public static void Cleanup()
        {
            foreach (var container in m_RTBufferDict)
            {
                container.Value.ReleaseBuffer();
            }
        }
    }

    public class RTBufferContainer
    {
        private Dictionary<int, RTHandle[]> m_RTHandleDict = new Dictionary<int, RTHandle[]>();

        public RTHandle AllocHistoryFrameRT(CameraFrameHistoryType type, Func<int, RTHandle> allocator, int bufferCount)
        {
            return AllocHistoryFrameRT((int)type, allocator, bufferCount);
        }
        public RTHandle AllocHistoryFrameRT(int id, Func<int, RTHandle> allocator, int bufferCount)
        {
            AllocBuffer(id, (i) => allocator(i), bufferCount);
            return GetFrameRT(id, 0);
        }
        public RTHandle GetPreviousFrameRT(CameraFrameHistoryType type)
        {
            return GetPreviousFrameRT((int)type);
        }
        public RTHandle GetPreviousFrameRT(int id)
        {
            return GetFrameRT(id, 1);
        }
        public RTHandle GetCurrentFrameRT(CameraFrameHistoryType type)
        {
            return GetCurrentFrameRT((int)type);
        }
        public RTHandle GetCurrentFrameRT(int id)
        {
            return GetFrameRT(id, 0);
        }

        public void Init()
        {
            Swap();
            Resize();
        }

        private void AllocBuffer(int bufferId, Func<int, RTHandle> allocator, int bufferCount)
        {
            if (bufferCount > 0 && allocator != null)
            {
                var buffers = new RTHandle[bufferCount];
                m_RTHandleDict.Add(bufferId, buffers);

                for (int i = 0; i < bufferCount; ++i)
                {
                    buffers[i] = allocator(i);
                }
            }
        }

        public RTHandle GetFrameRT(int bufferId, int frameIndex)
        {
            RTHandle[] handles = null;
            if (!m_RTHandleDict.TryGetValue(bufferId, out handles))
            {
                return null;
            }
            if (frameIndex < handles.Length && frameIndex >= 0)
            {
                return handles[frameIndex];
            }
            return null;
        }

        public void ReleaseBuffer(int bufferId)
        {
            if (m_RTHandleDict.TryGetValue(bufferId, out var buffers))
            {
                for (int i = 0; i < buffers.Length; ++i)
                {
                    buffers[i].Release();
                }
            }

            m_RTHandleDict.Remove(bufferId);
        }

        public void ReleaseBuffer()
        {
            var enumerator = m_RTHandleDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                for (int i = 0; i < enumerator.Current.Value.Length; ++i)
                {
                    enumerator.Current.Value[i].Release();
                }
            }

            m_RTHandleDict.Clear();
        }

        public void Swap()
        {
            var enumerator = m_RTHandleDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var curVar = enumerator.Current.Value;
                if (curVar.Length > 1)
                {
                    var nextFirst = curVar[curVar.Length - 1];
                    for (int i = 0, c = curVar.Length - 1; i < c; ++i)
                    {
                        curVar[i + 1] = curVar[i];

                    }
                    curVar[0] = nextFirst;
                }
            }
        }

        public void Resize()
        {
            var enumerator = m_RTHandleDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                for (int i = 0; i < enumerator.Current.Value.Length; ++i)
                {
                    enumerator.Current.Value[i].Resize();
                }
            }
        }

        // 手动调整分辨率的时候，我们也要重新设置buffer大小。 by mjk
        public void SetReferenceSize(int width, int height)
        {
            var enumerator = m_RTHandleDict.GetEnumerator();
            while (enumerator.MoveNext())
            {
                for (int i = 0; i < enumerator.Current.Value.Length; ++i)
                {
                    enumerator.Current.Value[i].Resize(width, height);
                }
            }
        }
    }
}
