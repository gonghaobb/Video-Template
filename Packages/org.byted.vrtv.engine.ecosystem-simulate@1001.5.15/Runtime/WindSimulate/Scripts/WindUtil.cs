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
    public static class WindUtil
    {
        public static Vector3 TransferAngleToVector(float angle)
        {
            return Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        }

        public static void DrawSphere(Vector3 pos, float radius)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(pos , radius);
        }

        public static void DrawSemicircle(Vector3 origin, Vector3 direction, float radius, int angle)
        {
            //DrawSemicircle(origin, direction, radius, angle, Vector3.up);
            //DrawSemicircle(origin, direction, radius, angle, Vector3.Cross(direction, Vector3.up));
            Gizmos.color = Color.white;

            Vector3 left = Quaternion.AngleAxis(-angle / 2, Vector3.up) * direction;
            Vector3 right = Quaternion.AngleAxis(angle / 2, Vector3.up) * direction;

            Vector3 top = Quaternion.AngleAxis(-angle / 2, Vector3.Cross(direction, Vector3.up)) * direction;
            Vector3 bottom = Quaternion.AngleAxis(angle / 2, Vector3.Cross(direction, Vector3.up)) * direction;

            if (angle != 360)
            {
                Gizmos.DrawLine(origin, origin + left * radius);
                Gizmos.DrawLine(origin, origin + right * radius);
                Gizmos.DrawLine(origin, origin + top * radius);
                Gizmos.DrawLine(origin, origin + bottom * radius);
            }

            DrawCurve(origin, top, radius, angle, Vector3.Cross(direction, Vector3.up));
            DrawCurve(origin, left, radius, angle, Vector3.up);
            DrawCurve(origin, left, radius , Mathf.Max(angle , 90) , direction);
            DrawCurve(origin, left, radius , Mathf.Max(angle, 90), -direction);
            DrawCurve(origin, right, radius , Mathf.Max(angle, 90), -direction);
            DrawCurve(origin, right, radius , Mathf.Max(angle, 90), direction);
        }

        public static void DrawCurve(Vector3 origin, Vector3 from , float radius, int angle, Vector3 axis)
        {
            Vector3 currentP = origin + from * radius;
            for (int i = 0; i <= angle / 10; i++)
            {
                Vector3 dir = Quaternion.AngleAxis(10 * i, axis) * from;
                Vector3 oldP = currentP;
                currentP = origin + dir * radius;
                Gizmos.DrawLine(oldP, currentP);
            }
        }

        public static void DrawRect(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, Color color)
        {
            Gizmos.color = color;
            Gizmos.DrawLine(p1, p2);
            Gizmos.DrawLine(p2, p3);
            Gizmos.DrawLine(p3, p4);
            Gizmos.DrawLine(p4, p1);
        }

        public static void DrawBox(Vector3[] points, Color color)
        {
            /*

                y z
                |/      
                --->x
                    (3) /--------------/(7)
                       / |            / |  
                      /  |           /  |
                  (2)/--------------/(6)|
                     |   |          |   | 
                     |(1)|--------- |-- /(5)
                     |  /           |  /   
                     |/--------------/
                    (0)             (4)

            */

            Gizmos.color = color;
            DrawRect(points[0], points[1], points[3], points[2], color);
            DrawRect(points[4], points[5], points[7], points[6], color);
            Gizmos.DrawLine(points[0], points[4]);
            Gizmos.DrawLine(points[2], points[6]);
            Gizmos.DrawLine(points[1], points[5]);
            Gizmos.DrawLine(points[3], points[7]);
        }

        public static void DrawArrow(Vector3 position, Vector3 direction, Vector3 right, float size, Color color)
        {
            Gizmos.color = color;
            Vector3 top = position + direction * size / 2;
            Vector3 bottom = position - direction * size / 2;
            Gizmos.DrawLine(top, bottom);
            Gizmos.DrawLine(top, position + right * 2f);
            Gizmos.DrawLine(top, position - right * 2f);
        }
    }
}
