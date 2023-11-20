using UnityEngine;

namespace IxDE
{
    public class FovTransformer : MonoBehaviour
    {
        [SerializeField] private float m_Distance = 10.0f;
        [SerializeField] private float m_FromFov = 60.0f;
        [SerializeField] private float m_FromValue = 10.0f;

        public enum Axis
        {
            X,
            Y,
            Z
        }

        public float toFov => FovHelper.SizeToFov(m_FromValue, m_Distance);
        public float toValue => FovHelper.FovToSize(m_FromFov, m_Distance);

        public void GetDistance()
        {
            m_Distance = transform.localPosition.z;
        }

        public void SetScale(Axis axis)
        {
            var localScale = transform.localScale;
            localScale[(int)axis] = toValue;
            transform.localScale = localScale;
        }

        public void SetPosition(Axis axis)
        {
            var localPosition = transform.localPosition;
            localPosition[(int)axis] = (axis == Axis.Z) ? m_Distance : toValue;
            transform.localPosition = localPosition;
        }
    }
}