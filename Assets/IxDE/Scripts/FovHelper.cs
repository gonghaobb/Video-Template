using UnityEngine;

namespace IxDE
{
    public struct FovHelper
    {
        public static float FovToSize(float fov, float distance)
        {
            return 2.0f * Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * distance;
        }

        public static float SizeToFov(float size, float distance)
        {
            return 2.0f *Mathf.Atan(size * 0.5f / distance) * Mathf.Rad2Deg;
        }
    }
}
