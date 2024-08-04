using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Common;

namespace Matoya.Common.Geometry {
    public sealed class GeometryHelper {

        /// <summary>
        /// Returns true if <paramref name="testPoint"/> lies in the triangle defined by the points <paramref name="a"/>, <paramref name="b"/>, and <paramref name="c"/>.
        /// Assumes the points lie in a horizontal plane and that the vertices are ordered in a CCW order.
        /// </summary>
        /// <param name="testPoint">The point to test.</param>
        /// <param name="a">One vertex of the triangle.</param>
        /// <param name="b">One vertex of the triangle.</param>
        /// <param name="c">One vertex of the triangle.</param>
        /// <returns></returns>
        public static bool IsPointInHorizontalTriangle(Vector3 testPoint, Vector3 a, Vector3 b, Vector3 c) {
            Vector3 ab = b - a;
            Vector3 bc = c - b;
            Vector3 ca = a - c;

            Vector3 aToTest = testPoint - a;
            Vector3 bToTest = testPoint - b;
            Vector3 cToTest = testPoint - c;

            float aCross = Vector3.Cross(ab, aToTest).y;
            float bCross = Vector3.Cross(bc, bToTest).y;
            float cCross = Vector3.Cross(ca, cToTest).y;

            return (aCross >= 0.0f && bCross >= 0.0f && cCross >= 0.0f) || (aCross <= 0.0f && bCross <= 0.0f && cCross <= 0.0f);
        }

        /// <summary>
        /// Returns true if the triangle defined by the points <paramref name="a"/>, <paramref name="b"/>, and <paramref name="c"/> is wound CCW.
        /// </summary>
        /// <param name="a">One vertex of the triangle.</param>
        /// <param name="b">One vertex of the triangle.</param>
        /// <param name="c">One vertex of the triangle.</param>
        /// <returns></returns>
        public static bool IsTriangleCCW(Vector3 a, Vector3 b, Vector3 c) {
            Vector3 ab = b - a;
            Vector3 ac = c - a;

            float cross = Vector3.Cross(ab, ac).y;

            return cross < 0;
        }

        /// <summary>
        /// Returns true if the line defined by the points <paramref name="a"/> and <paramref name="b"/> at any point intersect at least one of the line segments formed by the elements of <paramref name="lineSegments"/>.
        /// </summary>
        /// <param name="a">One of the points defining the line.</param>
        /// <param name="b">One of the points defining the line.</param>
        /// <param name="lineSegments">A list of <see cref="Vector3"></see> in which consecutive elements each define a line segment.</param>
        /// <returns></returns>
        public static bool IsLineAndLineSegmentsIntersecting(Vector3 a, Vector3 b, IList<Vector3> lineSegments) {
            for(int i = 0, n = lineSegments.Count - 1; i < n; ++i) {
                Vector3 c = lineSegments[i];
                Vector3 d = lineSegments[i + 1];
                bool intersecting = IsHorizontalLineIntersection(a, b, c, d, out _);
                if(intersecting) {
                    return true;
                }
            }
            return false;
        }

        // Positive means CW, 0 means colinear, negative means CCW;
        /// <summary>
        /// Returns a positive float if the triangle defined by the points <paramref name="a"/>, <paramref name="b"/>, and <paramref name="c"/> is wound CW, 0 if it the points are colinear and a negative float if it is wound CCW.
        /// </summary>
        /// <param name="a">One vertex of the triangle.</param>
        /// <param name="b">One vertex of the triangle.</param>
        /// <param name="c">One vertex of the triangle.</param>
        /// <returns></returns>
        public static float TriangleDirection(Vector3 a, Vector3 b, Vector3 c) {
            Vector3 ab = b - a;
            Vector3 ac = c - a;
            return Vector3.Cross(ab, ac).y;
        }

        /// <summary>
        /// Generates a uniformly sampled random point in a triangle. Code from https://stackoverflow.com/questions/4778147/sample-random-point-in-triangle.
        /// </summary>
        /// <param name="a">One vertex of the triangle.</param>
        /// <param name="b">One vertex of the triangle.</param>
        /// <param name="c">One vertex of the triangle.</param>
        /// <returns></returns>
        public static Vector3 RandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c) {
            float randomValue1 = UnityEngine.Random.value;
            float randomValue2 = UnityEngine.Random.value;
            return (1 - Mathf.Sqrt(randomValue1)) * a + Mathf.Sqrt(randomValue1) * (1 - randomValue2) * b + Mathf.Sqrt(randomValue1) * randomValue2 * c;
        }

        /// <summary>
        /// Returns true if the lines defined by (<paramref name="a"/>, <paramref name="b"/>) and (<paramref name="c"/>, <paramref name="d"/>) intersect, ignoring the y-component of the line points.
        /// </summary>
        /// <param name="a">The start point of the first line.</param>
        /// <param name="b">The end point of the first line.</param>
        /// <param name="c">The start point of the second line.</param>
        /// <param name="d">The end point of the second line.</param>
        /// <param name="intersectionPoint">The intersection point that was found, as a <see cref="Vector2"/>.</param>
        /// <returns></returns>
        public static bool IsHorizontalLineIntersection(Vector3 a, Vector3 b, Vector3 c, Vector3 d, out Vector2 intersectionPoint) {
            return IsLineIntersection(a.SwizzleXZ(), b.SwizzleXZ(), c.SwizzleXZ(), d.SwizzleXZ(), out intersectionPoint);
        }

        /// <summary>
        /// Returns true if the lines defined by (<paramref name="a"/>, <paramref name="b"/>) and (<paramref name="c"/>, <paramref name="d"/>) intersect.
        /// </summary>
        /// <param name="a">The start point of the first line.</param>
        /// <param name="b">The end point of the first line.</param>
        /// <param name="c">The start point of the second line.</param>
        /// <param name="d">The end point of the second line.</param>
        /// <param name="intersectionPoint">The intersection point that was found.</param>
        public static bool IsLineIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 intersectionPoint) {
            Vector2 ab = b - a;
            Vector2 cd = d - c;
            intersectionPoint = Vector2.zero;

            float abCrossCd = Cross(ab, cd);
            if(abCrossCd == 0) {
                return false;
            }
            else {
                Vector2 ac = c - a;
                float t1 = Cross(ac, cd) / abCrossCd;
                float t2 = -Cross(ab, ac) / abCrossCd;
                if(t1 >= 0 && t1 <= 1 && t2 >= 0 && t2 <= 1) {
                    intersectionPoint = a + ab * t1;
                    return true;
                }
                else {
                    return false;
                }
            }
        }

        /// <summary>
        /// Returns true if the given point <paramref name="p"/> is on the line defined by <paramref name="a"/> and <paramref name="b"/>, ignoring the y-component of the line points.
        /// The <paramref name="tolerance"/> allows the point to lie slightly off the line. Values close to 1 means low tolerance, while values approaching 0 means a high tolerance.
        /// </summary>
        /// <param name="a">The start point of the line.</param>
        /// <param name="b">The end point of the line.</param>
        /// <param name="p">The point to test.</param>
        /// <param name="tolerance">The tolerance of the test.</param>
        /// <returns></returns>
        public static bool IsPointOnLine(Vector3 a, Vector3 b, Vector3 p, float tolerance = 0.99f) {
            return IsPointOnLine2d(a.SwizzleXZ(), b.SwizzleXZ(), p.SwizzleXZ(), tolerance);
        }

        /// <summary>
        /// Returns true if the given point <paramref name="p"/> is on the line defined by <paramref name="a"/> and <paramref name="b"/>.
        /// The <paramref name="tolerance"/> allows the point to lie slightly off the line. Values close to 1 means low tolerance, while values approaching 0 means a high tolerance.
        /// </summary>
        /// <param name="a">The start point of the line.</param>
        /// <param name="b">The end point of the line.</param>
        /// <param name="p">The point to test.</param>
        /// <param name="tolerance">The tolerance of the test.</param>
        /// <returns></returns>
        public static bool IsPointOnLine2d(Vector2 a, Vector2 b, Vector2 p, float tolerance = 0.99f) {
            Vector2 aToP = p - a;
            Vector2 aToB = b - a;
            return aToP.magnitude <= aToB.magnitude && Vector2.Dot(aToP.normalized, aToB.normalized) >= tolerance;
        }

        /// <summary>
        /// Returns the closest point to <paramref name="p"/> on the line defined by <paramref name="a"/> and <paramref name="b"/>, ignoring the y-component of the line points.
        /// </summary>
        /// <param name="a">The start point of the line.</param>
        /// <param name="b">The end point of the line.</param>
        /// <param name="p">The point to calculate the distance from.</param>
        /// <returns></returns>
        public static Vector3 ClosestPointOnLine(Vector3 a, Vector3 b, Vector3 p) {
            return ClosestPointOnLine2d(a.SwizzleXZ(), b.SwizzleXZ(), p.SwizzleXZ()).SwizzleX0Y();
        }

        /// <summary>
        /// Returns the closest point to <paramref name="p"/> on the line defined by <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The start point of the line.</param>
        /// <param name="b">The end point of the line.</param>
        /// <param name="p">The point to calculate the distance from.</param>
        /// <returns></returns>
        public static Vector2 ClosestPointOnLine2d(Vector2 a, Vector2 b, Vector2 p) {
            Vector2 closestPoint;
            SqrDistanceToLine(a, b, p, out closestPoint);
            return closestPoint;
        }

        /// <summary>
        /// Returns the area defined by the triangle formed by points <paramref name="pointA"/>, <paramref name="pointB"/>, and <paramref name="pointC"/>.
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <param name="pointC"></param>
        /// <returns></returns>
        public static float TriangleArea(Vector3 pointA, Vector3 pointB, Vector3 pointC) {
            return 0.5f * (pointA.x * (pointB.z - pointC.z) + pointB.x * (pointC.z - pointA.z) + pointC.x * (pointA.z - pointB.z));
        }

        /// <summary>
        /// Returns the squared distance to the closest point to <paramref name="p"/> on the line defined by <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The start point of the line.</param>
        /// <param name="b">The end point of the line.</param>
        /// <param name="p">The point to calculate the distance from.</param>
        /// <param name="projection">The projection of <paramref name="p"/> onto the line.</param>
        /// <returns></returns>
        public static float SqrDistanceToLine(Vector2 a, Vector2 b, Vector2 p, out Vector2 projection) {
            float length = Vector2.SqrMagnitude(a - b);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / length);
            projection = a + t * (b - a);
            return Vector2.SqrMagnitude(p - projection);
        }

        /// <summary>
        /// Returns the distance to the closest point to <paramref name="p"/> on the line defined by <paramref name="a"/> and <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The start point of the line.</param>
        /// <param name="b">The end point of the line.</param>
        /// <param name="p">The point to calculate the distance from.</param>
        /// <param name="projection">The projection of <paramref name="p"/> onto the line.</param>
        /// <returns></returns>
        public static float DistanceToLine(Vector2 a, Vector2 b, Vector2 p, out Vector2 projection) {
            return Mathf.Sqrt(SqrDistanceToLine(a, b, p, out projection));
        }

        /// <summary>
        /// Returns the point along the line that lies <paramref name="distance"/> units of distance away from <paramref name="a"/> in the direction towards <paramref name="b"/>.
        /// </summary>
        /// <param name="a">The start point of the line.</param>
        /// <param name="b">The end point of the line.</param>
        /// <param name="distance">The distance along the line to find the point of.</param>
        /// <returns></returns>
        public static Vector3 PointDistanceAlongLine(Vector3 a, Vector3 b, float distance) {
            float lineLength = Vector3.Distance(a, b);
            float t = distance / lineLength;
            return Vector3.LerpUnclamped(a, b, t);
        }

        /// <summary>
        /// Checks if the forward vector of <paramref name="transform"/> is colinear with <paramref name="direction"/> and sets the forward vector to <paramref name="direction"/> if not.
        /// If <paramref name="log"/> is true, a message is logged if they are not colinear.
        /// </summary>
        /// <param name="transform">The transform to correct the rotation of.</param>
        /// <param name="direction">The direction to correct to.</param>
        /// <param name="log">Whether or not to log a message if a correction was done.</param>
        public static void CorrectRotation(Transform transform, Vector3 direction, bool log = false) {
            if(Vector3.Dot(transform.forward, direction) != 0.0f) {
                Vector3 oldEuler = transform.eulerAngles;
                transform.forward = direction;
                if(log) {
                    Debug.Log($"Rotation was {oldEuler}, corrected to {transform.eulerAngles}.");
                }
            }
        }

        /// <summary>
        /// Rotates the vector <paramref name="v"/> 90 degrees clockwise in the horizontal plane.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 Rotate90DegCWHorizontal(Vector3 v) {
            return new(v.z, v.y, -v.x);
        }

        /// <summary>
        /// Rotates the vector <paramref name="v"/> 90 degrees counter-clockwise in the horizontal plane.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 Rotate90DegCCWHorizontal(Vector3 v) {
            return new(-v.z, v.y, v.x);
        }

        public static Vector3[] CalculateWorldBoxCorners(Bounds bounds) {
            Vector3 extent = bounds.extents;
            Vector3 center = bounds.center;

            Vector3 xComponent = Vector3.Project(extent, Vector3.right);
            Vector3 yComponent = Vector3.Project(extent, Vector3.up);
            Vector3 zComponent = Vector3.Project(extent, Vector3.forward);

            Vector3 v1 = center + extent;
            Vector3 v2 = center - xComponent + yComponent + zComponent;
            Vector3 v3 = center - xComponent + yComponent - zComponent;
            Vector3 v4 = center + xComponent + yComponent - zComponent;

            Vector3 v5 = center + xComponent - yComponent + zComponent;
            Vector3 v6 = center - xComponent - yComponent + zComponent;
            Vector3 v7 = center - xComponent - yComponent - zComponent;
            Vector3 v8 = center + xComponent - yComponent - zComponent;

            return new[] { v1, v2, v3, v4, v5, v6, v7, v8 };
        }

        private static float Cross(Vector2 a, Vector2 b) {
            return a.x * b.y - a.y * b.x;
        }

    }
}
