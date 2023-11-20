namespace UnityEngine.UI
{
    internal class RectangularVertexClipper
    {
        readonly Vector3[] m_WorldCorners = new Vector3[4];
        readonly Vector3[] m_CanvasCorners = new Vector3[4];

        public Rect GetCanvasRect(RectTransform t, Canvas c, bool allowInverse = false)//PicoVideo;RectMask2DAllowInverse;WuJunLin
        {
            if (c == null)
                return new Rect();

            t.GetWorldCorners(m_WorldCorners);
            var canvasTransform = c.GetComponent<Transform>();
            for (int i = 0; i < 4; ++i)
                m_CanvasCorners[i] = canvasTransform.InverseTransformPoint(m_WorldCorners[i]);
            
            //PicoVideo;RectMask2DAllowInverse;WuJunLin;Start
            if (allowInverse)
            {
                float xMin = m_CanvasCorners[0].x;
                float yMin = m_CanvasCorners[0].y;
                for (int i = 0; i < 4; ++i)
                {
                    if (xMin > m_CanvasCorners[i].x)
                    {
                        xMin = m_CanvasCorners[i].x;
                    }

                    if (yMin > m_CanvasCorners[i].y)
                    {
                        yMin = m_CanvasCorners[i].y;
                    }
                }

                return new Rect(xMin, yMin, Mathf.Abs(m_CanvasCorners[2].x - m_CanvasCorners[0].x),
                    Mathf.Abs(m_CanvasCorners[2].y - m_CanvasCorners[0].y));
            }
            else
            {
                return new Rect(m_CanvasCorners[0].x, m_CanvasCorners[0].y, m_CanvasCorners[2].x - m_CanvasCorners[0].x,
                    m_CanvasCorners[2].y - m_CanvasCorners[0].y);
            }
            //PicoVideo;RectMask2DAllowInverse;WuJunLin;End
        }
    }
}