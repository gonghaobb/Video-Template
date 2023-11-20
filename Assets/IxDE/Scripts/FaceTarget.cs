using UnityEngine;

namespace IxDE
{
    public class FaceTarget : MonoBehaviour
    {
        private enum Direction
        {
            Up,
            Left,
            Right,
            Down
        }
        
        [SerializeField] private Transform m_Target;
        [Space] [SerializeField] private RectTransform m_CenterRect;
        [SerializeField] private RectTransform m_ContentRect;
        [SerializeField] private Vector3 m_Margin = new Vector3(32.0f, 32.0f, 0f);
        [Space]
        [SerializeField] private Direction m_Direction = Direction.Down;
        [SerializeField] private float m_Rotation = 30.0f;
        
        private float m_Sign => m_Direction == Direction.Right || m_Direction == Direction.Up ? 1 : -1;

        public void SetFlat()
        {
            if (m_Direction == Direction.Left || m_Direction == Direction.Right)
            {
                var x = m_Sign * ((m_CenterRect.sizeDelta.x + m_ContentRect.sizeDelta.x) / 2.0f + m_Margin.x);
                transform.localPosition = new Vector3(x, 0, m_Margin.z);
            }
            else if (m_Direction == Direction.Up || m_Direction == Direction.Down)
            {
                var y = m_Sign * ((m_CenterRect.sizeDelta.y + m_ContentRect.sizeDelta.y) / 2.0f + m_Margin.y);
                transform.localPosition = new Vector3(0, y, m_Margin.z);
            }
            transform.localRotation = Quaternion.identity;
        }
        
        public void GetRotation()
        {
            var direction = transform.position - m_Target.position;
            var angle = Vector3.Angle(direction, m_Target.forward);
            m_Rotation = angle;
        }

        public void Face()
        {
            if (m_Direction == Direction.Left || m_Direction == Direction.Right)
            {
                transform.localEulerAngles = new Vector3(0, m_Sign * m_Rotation, 0);
                var offset = GetOffset(m_ContentRect.sizeDelta.x);
                var x = offset.x + m_Sign * ((m_CenterRect.sizeDelta.x + m_ContentRect.sizeDelta.x) / 2.0f + m_Margin.x);
                var z = offset.y + m_Margin.z;
                transform.localPosition = new Vector3(x, 0, z);
            } else if (m_Direction == Direction.Up || m_Direction == Direction.Down)
            {
                transform.localEulerAngles = new Vector3(-m_Sign * m_Rotation, 0, 0);
                var offset = GetOffset(m_ContentRect.sizeDelta.y);
                var y = offset.x + m_Sign * ((m_CenterRect.sizeDelta.y + m_ContentRect.sizeDelta.y) / 2.0f + m_Margin.y);
                var z = offset.y + m_Margin.z;
                transform.localPosition = new Vector3(0, y, z);
            }
        }

        private Vector2 GetOffset(float size)
        {
            var theta = m_Sign * m_Rotation * Mathf.Deg2Rad;
            var h = size / 2.0f;
            var a = Mathf.Sin(theta) * h;
            var b = Mathf.Cos(theta) * h;
            var offset = -m_Sign * (h - b);
            var z = -Mathf.Abs(a);
            return new Vector2(offset, z);
        }
    }
}