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

namespace Matrix.EcosystemSimulate
{
    public static class WindMath
    {
        public static bool IntersectBoxAndCircle(Vector2 circleCenter, float circleRadius, Vector2 boxCenter,
            Vector2 boxRadius)
        {
            Vector2 d = circleCenter - boxCenter;
            d.x = Mathf.Abs(d.x);
            d.y = Mathf.Abs(d.y);

            Vector2 u = d - boxRadius;
            u.x = Mathf.Max(u.x, 0);
            u.y = Mathf.Max(u.y, 0);

            return Vector2.Dot(u, u) <= circleRadius * circleRadius;
        }

        public static bool IntersectBoxAndBox(Vector2 boxCenter1, Vector2 boxRadius1, Vector2 boxCenter2,
            Vector2 boxRadius2)
        {
            Vector2 d = boxCenter1 - boxCenter2;
            d.x = Mathf.Abs(d.x);
            d.y = Mathf.Abs(d.y);

            Vector2 u = boxRadius1 + boxRadius2;

            return d.x < u.x && d.y < u.y;
        }
    }
}

