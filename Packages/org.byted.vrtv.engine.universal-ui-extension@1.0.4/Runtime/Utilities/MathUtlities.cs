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
using UnityEngine;

namespace Matrix.UniversalUIExtension
{
    public static class MathUtilities
    {
        public static bool TRSApproximately(Matrix4x4 matrix1, Matrix4x4 matrix2, float sqrTolerance)
        {
            for (int i = 0; i < 4; i++)
            {
                if ((matrix1.GetRow(i) - matrix2.GetRow(i)).sqrMagnitude > sqrTolerance)
                {
                    return false;
                }
            }

            return true;
        }

        public static Vector4 ToVector(Rect r)
        {
            return new Vector4(r.xMin, r.yMin, r.xMax, r.yMax);
        }

        public static Vector4 Div(Vector4 v, Vector2 s)
        {
            return new Vector4(v.x / s.x, v.y / s.y, v.z / s.x, v.w / s.y);
        }

        public static Vector2 Div(Vector2 v, Vector2 s)
        {
            return new Vector2(v.x / s.x, v.y / s.y);
        }

        public static Vector4 Mul(Vector4 v, Vector2 s)
        {
            return new Vector4(v.x * s.x, v.y * s.y, v.z * s.x, v.w * s.y);
        }

        public static Vector2 Size(Vector4 r)
        {
            return new Vector2(r.z - r.x, r.w - r.y);
        }

        public static Vector4 Move(Vector4 v, Vector2 o)
        {
            return new Vector4(v.x + o.x, v.y + o.y, v.z + o.x, v.w + o.y);
        }

        public static Vector4 BorderOf(Vector4 outer, Vector4 inner)
        {
            return new Vector4(inner.x - outer.x, inner.y - outer.y, outer.z - inner.z, outer.w - inner.w);
        }

        public static Vector4 ApplyBorder(Vector4 v, Vector4 b)
        {
            return new Vector4(v.x + b.x, v.y + b.y, v.z - b.z, v.w - b.w);
        }

        public static Vector2 Min(Vector4 r)
        {
            return new Vector2(r.x, r.y);
        }

        public static Vector2 Max(Vector4 r)
        {
            return new Vector2(r.z, r.w);
        }

        public static Vector2 Remap(Vector2 c, Vector4 r1, Vector4 r2)
        {
            var r1Size = Max(r1) - Min(r1);
            var r2Size = Max(r2) - Min(r2);
            return Vector2.Scale(Div((c - Min(r1)), r1Size), r2Size) + Min(r2);
        }

        public static bool Inside(Vector2 v, Vector4 r)
        {
            return v.x >= r.x && v.y >= r.y && v.x <= r.z && v.y <= r.w;
        }
    }
}