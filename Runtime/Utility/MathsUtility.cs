//#define USE_BEZIER_MATHS

using System;
#if USE_BEZIER_MATHS
using System.Collections.Generic;
#endif
using System.Runtime.InteropServices;

using UnityEngine;

using SystemRandom = System.Random;
using UnityRandom = UnityEngine.Random;

namespace BlackTundra.Foundation.Utility {

    public static class MathsUtility {

        #region constant

        internal static readonly SystemRandom Random = new SystemRandom();

        #endregion

        #region WrapClamp

        public static float WrapClamp(this int value, in int min, in int max) {

            int width = max - min;
            return width == 0
                ? min
                : value - (((value - min) / width) * width);

        }

        /// <summary>
        /// Clamps a value between a minimum and maximum value but will wrap the value to do so.
        /// </summary>
        /// <param name="value">Value to wrap.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <returns>Clamped value.</returns>
        public static float WrapClamp(this float value, in float min, in float max) {

            float width = max - min;
            return Mathf.Approximately(width, 0.0f)
                ? min
                : value - (Mathf.Floor((value - min) / width) * width);

            /*
            if (value > min && value < max) return value;
            float difference = max - min;
            if (value < min) { do { value += difference; } while (value < min); } else { do { value -= difference; } while (value > max); }
            return value;
            */

        }

        #endregion

        #region Lerp

        /// <summary>
        /// Lerps towards a target value.
        /// </summary>
        /// <param name="value">Value to apply lerp to.</param>
        /// <param name="target">Target value to lerp towards.</param>
        /// <param name="delta">Difference to add to get the the target.</param>
        /// <returns>Value after lerp has been applied towards the target value.</returns>
        public static float Lerp(this float value, in float target, in float delta) {
            if (value == target) return value;
            if (value > target) { // greater than target value
                value -= delta;
                return value < target ? target : value;
            }
            value += delta;
            return value > target ? target : value;
        }

        #endregion

        #region Wrap

        /// <summary>
        /// Wraps a <paramref name="value"/> between <c>0.0</c> (inclusive) and <paramref name="max"/> (exclusive).
        /// </summary>
        public static float Wrap(in float value, in float max) => value < 0.0f ? max + value % max : value % max;

        /// <summary>
        /// Wraps a <paramref name="value"/> between <paramref name="min"/> (inclusive) and <paramref name="max"/> (exclusive).
        /// </summary>
        public static float Wrap(in float value, in float min, in float max) => value < min ? max - (min - value) % (max - min) : min + (value - min) % (max - min);

        #endregion

        #region ClosestPointOnLine

        /// <param name="lineStart">Start of the line.</param>
        /// <param name="lineEnd">End of the line.</param>
        /// <param name="point">Point to sample the closest point on the line to.</param>
        /// <returns>Returns the closest point to the specified <paramref name="point"/> on a finite line.</returns>
        public static Vector3 ClosestPointOnFiniteLine(in Vector3 lineStart, in Vector3 lineEnd, in Vector3 point) {
            Vector3 direction = lineEnd - lineStart;
            float sqrLength = direction.sqrMagnitude;
            if (sqrLength < Mathf.Epsilon) return lineStart;
            float length = Mathf.Sqrt(sqrLength);
            float projectedLength = Mathf.Clamp(
                Vector3.Dot(
                    point - lineStart,
                    direction * (1.0f / length)
                ),
                0.0f, length
            );
            return lineStart + (direction * projectedLength);
        }

        /// <param name="origin">Start of the line.</param>
        /// <param name="direction">Normalized direction of the line.</param>
        /// <param name="length">Length of the line.</param>
        /// <param name="point">Point to sample the closest point on the line to.</param>
        /// <returns>Returns the closest point to the specified <paramref name="point"/> on a finite line.</returns>
        public static Vector3 ClosestPointOnFiniteLine(in Vector3 origin, in Vector3 direction, in float length, in Vector3 point) {
            if (length < Mathf.Epsilon) return origin;
            float projectedLength = Mathf.Clamp(
                Vector3.Dot(
                    point - origin,
                    direction
                ),
                0.0f, length
            );
            return origin + (direction * projectedLength);
        }

        #endregion

        #region ClosestPointOnInfiniteLine

        /// <param name="origin">A random point on the line.</param>
        /// <param name="direction">Normalized direction that the line points in.</param>
        /// <param name="point">Point to sample the closest point on the line to.</param>
        /// <returns>Returns the closest point to the specified <paramref name="point"/> on an infinite line.</returns>
        public static Vector3 ClosestPointOnInfiniteLine(in Vector3 origin, in Vector3 direction, in Vector3 point) {
            float projectedLength = Vector3.Dot(
                point - origin,
                direction
            );
            return origin + (direction * projectedLength);
        }

        #endregion

        #region IsPointBetween

        /// <returns>
        /// Returns <c>true</c> if the <paramref name="sample"/> is between point <paramref name="p1"/> and <paramref name="p2"/>.
        /// </returns>
        public static bool IsPointBetween(in Vector3 sample, in Vector3 p1, in Vector3 p2) {
            Vector3 normalizedSample = sample - p1; // localize the sample relative to p1
            Vector3 direction = p2 - p1; // calculate the direction from p1 to p2
            float directionLength = direction.magnitude;
            direction *= 1.0f / directionLength; // normalize direction
            float projectionAmount = Vector3.Dot(normalizedSample, direction); // calculate how much p1Sample projects onto p1p2
            return projectionAmount >= 0.0f & projectionAmount < directionLength; // calculate if the sample is between p1 and p2
        }

        #endregion

        #region IsNormalized

        public static bool IsNormalized(this Vector2 v) { return Mathf.Approximately((v.x * v.x) + (v.y * v.y), 1.0f); }
        public static bool IsNormalized(this Vector3 v) { return Mathf.Approximately((v.x * v.x) + (v.y * v.y) + (v.z * v.z), 1.0f); }
        public static bool IsNormalized(this Vector4 v) { return Mathf.Approximately((v.x * v.x) + (v.y * v.y) + (v.z * v.z) + (v.w * v.w), 1.0f); }

        #endregion

        #region IsNaN

        public static bool IsNaN(this Vector2 v) { return float.IsNaN(v.x + v.y); }
        public static bool IsNaN(this Vector3 v) { return float.IsNaN(v.x + v.y + v.z); }
        public static bool IsNaN(this Vector4 v) { return float.IsNaN(v.x + v.y + v.z + v.w); }

        #endregion

        #region IsInsideCube

        /// <summary>
        /// Tests if a point is inside a cube.
        /// </summary>
        /// <param name="point">Point to test if inside a cube.</param>
        /// <param name="centre">Centre of the cube.</param>
        /// <param name="radius">Half the length of one side of the cube.</param>
        /// <returns>Returns true if the point is inside the cube.</returns>
        public static bool IsInsideCube(this Vector3 point, in Vector3 centre, in float radius) =>
            Mathf.Abs(point.x - centre.x) < radius
            && Mathf.Abs(point.y - centre.y) < radius
            && Mathf.Abs(point.z - centre.z) < radius;

        #endregion

        #region RandomPoint

        /// <returns>
        /// Returns a random point at <paramref name="range"/> distance (in meters) from the provided <paramref name="point"/>.
        /// </returns>
        public static Vector2 RandomPoint(in Vector2 point, in float range) {
            float x = UnityRandom.Range(-1.0f, 1.0f);
            float y = UnityRandom.Range(-1.0f, 1.0f);
            float c = range / Mathf.Sqrt(x * x + y * y);
            return new Vector2(x * c, y * c);
        }

        /// <returns>
        /// Returns a random point at <paramref name="range"/> distance (in meters) from the provided <paramref name="point"/>.
        /// </returns>
        public static Vector3 RandomPoint(in Vector3 point, in float range) {
            float x = UnityRandom.Range(-1.0f, 1.0f);
            float y = UnityRandom.Range(-1.0f, 1.0f);
            float z = UnityRandom.Range(-1.0f, 1.0f);
            float c = range / Mathf.Sqrt(x * x + y * y + z * z);
            return new Vector3(x * c, y * c, z * c);
        }

        /// <returns>
        /// Returns a random point at <paramref name="range"/> distance (in meters) from the provided <paramref name="point"/>.
        /// </returns>
        public static Vector4 RandomPoint(in Vector4 point, in float range) {
            float x = UnityRandom.Range(-1.0f, 1.0f);
            float y = UnityRandom.Range(-1.0f, 1.0f);
            float z = UnityRandom.Range(-1.0f, 1.0f);
            float w = UnityRandom.Range(-1.0f, 1.0f);
            float c = range / Mathf.Sqrt(x * x + y * y + z * z + w * w);
            return new Vector4(x * c, y * c, z * c, w * c);
        }

        #endregion

        #region UnitClamp

        /// <summary>
        /// Clamps a vector so it cannot have a magnitude greater than <c>1.0</c>.
        /// </summary>
        /// <param name="value">Vector to clamp.</param>
        /// <returns>Vector clamped in a way that the magnitude cannot be greater than <c>1.0</c>.</returns>
        public static Vector2 UnitClamp(this Vector2 value) {
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 1.0f ? value * (1.0f / Mathf.Sqrt(sqrMagnitude)) : value;
        }

        /// <inheritdoc cref="UnitClamp(Vector2)"/>
        public static Vector3 UnitClamp(this Vector3 value) {
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 1.0f ? value * (1.0f / Mathf.Sqrt(sqrMagnitude)) : value;
        }

        /// <inheritdoc cref="UnitClamp(Vector2)"/>
        public static Vector4 UnitClamp(this Vector4 value) {
            float sqrMagnitude = value.sqrMagnitude;
            return sqrMagnitude > 1.0f ? value * (1.0f / Mathf.Sqrt(sqrMagnitude)) : value;
        }

        #endregion

        #region Area

        public static float Area(in Vector3 v0, in Vector3 v1, in Vector3 v2) {

            float a = Vector3.Distance(v0, v1); // find the a length for area of triangle calculation
            float c = Vector3.Distance(v0, v2); // find the c length for area of triangle calculation
            float area = a * c * Mathf.Sin(Vector3.Angle(v1 - v0, v2 - v0) * Mathf.Deg2Rad) * 0.5f; // calculate the area of the triangle
            return area > 0.0f ? area : 0.0f;

        }

        #endregion

        #region Multiply

        public static Vector2 Multiply(this Vector2 v0, in Vector2 v1) {

            return new Vector2(
                v0.x * v1.x,
                v0.y * v1.y
            );

        }

        public static Vector3 Multiply(this Vector3 v0, in Vector3 v1) {

            return new Vector3(
                v0.x * v1.x,
                v0.y * v1.y,
                v0.z * v1.z
            );

        }

        public static Vector4 Multiply(this Vector4 v0, in Vector4 v1) {

            return new Vector4(
                v0.x * v1.x,
                v0.y * v1.y,
                v0.z * v1.z,
                v0.w * v1.w
            );

        }

        #endregion

        #region LargestComponent

        public static float LargestComponent(this Vector2 v) => v.x > v.y ? v.x : v.y;

        public static float LargestComponent(this Vector3 v) {

            float largestValue = Mathf.Abs(v.x);

            float value = Mathf.Abs(v.y);
            if (value > largestValue) largestValue = value;

            value = Mathf.Abs(v.z);
            return value > largestValue ? value : largestValue;

        }

        public static float LargestComponent(this Vector4 v) {

            float largestValue = Mathf.Abs(v.x);

            float value = Mathf.Abs(v.y);
            if (value > largestValue) largestValue = value;

            value = Mathf.Abs(v.z);
            if (value > largestValue) largestValue = value;

            value = Mathf.Abs(v.w);
            return value > largestValue ? value : largestValue;

        }

        #endregion

        #region LargestComponentIndex

        public static int LargestComponentIndex(this Vector2 v) => v.x > v.y ? 0 : 1;

        public static int LargestComponentIndex(this Vector3 v) {

            int largestIndex = 0;
            float largestValue = Mathf.Abs(v.x);

            float value = Mathf.Abs(v.y);
            if (value > largestValue) { largestIndex = 1; largestValue = value; }

            return Mathf.Abs(v.z) > largestValue ? 2 : largestIndex;

        }

        public static int LargestComponentIndex(this Vector4 v) {

            int largestIndex = 0;
            float largestValue = Mathf.Abs(v.x);

            float value = Mathf.Abs(v.y);
            if (value > largestValue) { largestIndex = 1; largestValue = value; }

            value = Mathf.Abs(v.z);
            if (value > largestValue) { largestIndex = 2; largestValue = value; }

            return Mathf.Abs(v.w) > largestValue ? 3 : largestIndex;

        }

        #endregion

        #region HasDirectionFlipped

        public static bool HasDirectionChanged(this Vector2 v0, in Vector2 v1) {

            switch (LargestComponentIndex(v0)) {

                case 0: return Mathf.Sign(v0.x) != Mathf.Sign(v1.x);
                case 1: return Mathf.Sign(v0.y) != Mathf.Sign(v1.y);
                default: throw new NotSupportedException();

            }

        }

        public static bool HasDirectionFlipped(this Vector3 v0, in Vector3 v1) {

            switch (LargestComponentIndex(v0)) {

                case 0: return Mathf.Sign(v0.x) != Mathf.Sign(v1.x);
                case 1: return Mathf.Sign(v0.y) != Mathf.Sign(v1.y);
                case 2: return Mathf.Sign(v0.z) != Mathf.Sign(v1.z);
                default: throw new NotSupportedException();

            }

        }

#if BLACK_TUNDRA_MATHS_4D_GEOMETRY

        public static bool HasDirectionChanged(this Vector4 v1, in Vector4 v2) {

            switch (LargestComponentIndex(v1)) {

                case 0: return Mathf.Sign(v1.x) != Mathf.Sign(v2.x);
                case 1: return Mathf.Sign(v1.y) != Mathf.Sign(v2.y);
                case 2: return Mathf.Sign(v1.z) != Mathf.Sign(v2.z);
                case 3: return Mathf.Sign(v1.w) != Mathf.Sign(v2.w);
                default: throw new NotSupportedException();

            }

        }

#endif

        #endregion

        #region CalculateBounds

        public static Bounds CalculateBounds(this Vector3[] points) {
            if (points == null) throw new ArgumentNullException(nameof(points));
            return GeometryUtility.CalculateBounds(
                points,
                Matrix4x4.identity
            );
        }

        public static Bounds CalculateBounds(this Vector3[] points, Matrix4x4 transform) {
            if (points == null) throw new ArgumentNullException(nameof(points));
            return GeometryUtility.CalculateBounds(
                points,
                transform
            );
        }

        public static Bounds CalculateBounds(this Vector3[] points, Transform transform) {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (transform == null) throw new ArgumentNullException(nameof(transform));
            return GeometryUtility.CalculateBounds(
                points,
                transform.localToWorldMatrix
            );
        }

        #endregion

        #region EvaluateQuadraticCurve
        /*
        public static Vector2 EvaluateQuadraticCurve(in Vector2 p0, in Vector2 p1, in Vector2 p2, in float t) {

            float omt = 1.0f - t;
            return (omt * omt * p0) + (t * ((2.0f * omt * p1) + (t * p2)));

        }

        public static Vector3 EvaluateQuadraticCurve(in Vector3 p0, in Vector3 p1, in Vector3 p2, in float t) {

            float omt = 1.0f - t;
            return (omt * omt * p0) + (t * ((2.0f * omt * p1) + (t * p2)));

        }

        #if BLACK_TUNDRA_MATHS_4D_GEOMETRY

        public static Vector4 EvaluateQuadraticCurve(in Vector4 a, in Vector4 b, in Vector4 c, in float t) {

            float omt = 1.0f - t;
            return (omt * omt * a) + (t * ((2.0f * omt * b) + (t * c)));

        }

        #endif
        */
        #endregion

        #region EvaluateBezierCurve
#if USE_BEZIER_MATHS

        /*
        /// <summary>
        /// Evaluates a 2D bezier curve.
        /// </summary>
        /// <param name="t">Position on the curve. This should be between 0.0 and 1.0 to remain on the curve. Any value outside of that range is extrapolated.</param>
        public static Vector2 EvaluateBezierCurve(in Vector2 p0, in Vector2 p1, in Vector2 p2, in Vector2 p3, in float t) {
            float omt = 1.0f - t;
            return (omt * omt * ((omt * p0) + (3.0f * t * p1))) + (t * t * ((3.0f * omt * p2) + (t * p3)));
        }
        */

        public static Vector3 EvaluateBezierCurve(in Vector3[] points, in float t) {
            if (points == null) throw new ArgumentNullException(nameof(points));
            return EvaluateBezierCurve(points[0], points[1], points[2], points[3], t);
        }

        /// <summary>
        /// Evaluates a 3D bezier curve.
        /// </summary>
        /// <param name="t">Position on the curve. This should be between 0.0 and 1.0 to remain on the curve. Any value outside of that range is extrapolated.</param>
        public static Vector3 EvaluateBezierCurve(in Vector3 p0, in Vector3 p1, in Vector3 p2, in Vector3 p3, in float t) {
            float omt = 1.0f - t;
            return (omt * omt * ((omt * p0) + (3.0f * t * p1))) + (t * t * ((3.0f * omt * p2) + (t * p3)));
        }

#endif
        #endregion

        #region EvaluateBezierCurveDerivative
#if USE_BEZIER_MATHS

        public static Vector3 EvaluateBezierCurveDerivative(in Vector3[] points, in float t) {
            if (points == null) throw new ArgumentNullException(nameof(points));
            return EvaluateBezierCurveDerivative(points[0], points[1], points[2], points[3], t);
        }

        public static Vector3 EvaluateBezierCurveDerivative(in Vector3 a0, in Vector3 c0, in Vector3 c1, in Vector3 a1, float t) {
            t = Mathf.Clamp01(t);
            float omt = 1.0f - t;
            return 3.0f * (omt * ((omt * (c0 - a0)) + (2.0f * t * (c1 - c0))) + (t * t * (a1 - c1)));
        }

#endif
        #endregion

        #region SplitBezierCurve

        public static Vector3[][] SplitBezierCurve(in Vector3 p0, in Vector3 p1, in Vector3 p2, in Vector3 p3, in float t) {

            Vector3 a0 = Vector3.Lerp(p0, p1, t);
            Vector3 a1 = Vector3.Lerp(p1, p2, t);
            Vector3 a2 = Vector3.Lerp(p2, p3, t);
            Vector3 b0 = Vector3.Lerp(a0, a1, t);
            Vector3 b1 = Vector3.Lerp(a1, a2, t);
            Vector3 p = Vector3.Lerp(b0, b1, t);

            return new Vector3[][] {
                new Vector3[] { p0, a0, b0, p },
                new Vector3[] { p, b1, a2, p3 }
            };

        }

        #endregion

        #region EstimateBezierCurveLength
#if USE_BEZIER_MATHS

        /*
        /// <summary>
        /// Estimates the length of a segment of a 2D bezier curve.
        /// </summary>
        public static float EstimateBezierCurveLength(in Vector2 p0, in Vector2 p1, in Vector2 p2, in Vector2 p3) => Vector3.Distance(p0, p3) + ((Vector3.Distance(p0, p1) + Vector3.Distance(p1, p2) + Vector3.Distance(p2, p3)) * 0.33333333333333333333f);
        */
        public static float EstimateBezierCurveLength(in Vector3[] points) {
            if (points == null) throw new ArgumentNullException(nameof(points));
            return EstimateBezierCurveLength(points[0], points[1], points[2], points[3]);
        }

        /// <summary>
        /// Estimates the length of a segment of a 3D bezier curve.
        /// </summary>
        public static float EstimateBezierCurveLength(in Vector3 p0, in Vector3 p1, in Vector3 p2, in Vector3 p3) => Vector3.Distance(p0, p3) + ((Vector3.Distance(p0, p1) + Vector3.Distance(p1, p2) + Vector3.Distance(p2, p3)) * 0.33333333333333333333f);

#endif
        #endregion

        #region CalculateBezierBounds
#if USE_BEZIER_MATHS

        public static Bounds CalculateBezierBounds(in Vector3[] points) {
            if (points == null) throw new ArgumentNullException(nameof(points));
            return CalculateBezierBounds(points[0], points[1], points[2], points[3]);
        }

        public static Bounds CalculateBezierBounds(in Vector3 p0, in Vector3 p1, in Vector3 p2, in Vector3 p3) {
            MinMaxVector3 minMax = new MinMaxVector3();
            minMax.Evaluate(p0);
            minMax.Evaluate(p3);
            IEnumerable<float> turningPoints = FindBezierTurningPoints(p0, p1, p2, p3);
            foreach (float t in turningPoints) minMax.Evaluate(EvaluateBezierCurve(p0, p1, p2, p3, t));
            return minMax.ToBounds();
        }

#endif
        #endregion

        #region FindBezierTurningPoints
#if USE_BEZIER_MATHS

        /// <summary>
        /// Finds the turning points on a 3D bezier curve.
        /// </summary>
        /// <returns>The progress on the curve of every turning point. Every value will be between 0.0 and 1.0.</returns>
        public static IEnumerable<float> FindBezierTurningPoints(in Vector3 p0, in Vector3 p1, in Vector3 p2, in Vector3 p3) {

            // coefficients of derivative function:
            Vector3 a = 3.0f * (-p0 + (3.0f * p1) - (3.0f * p2) + p3);
            Vector3 b = 6.0f * (p0 - (2.0f * p1) + p2);
            Vector3 c = 3.0f * (p1 - p0);

            List<float> turningPoints = new List<float>();
            turningPoints.AddRange(Find01TurningPoints(a.x, b.x, c.x));
            turningPoints.AddRange(Find01TurningPoints(a.y, b.y, c.y));
            turningPoints.AddRange(Find01TurningPoints(a.z, b.z, c.z));
            return turningPoints;

        }

#endif
        #endregion

        #region Find01TurningPoints
#if USE_BEZIER_MATHS

        /// <summary>
        /// Finds the turning points between 0.0 and 1.0 of a quadratic equation.
        /// This is used for finding the turning points on a bezier curve.
        /// </summary>
        /// <param name="a">Coefficient of x^2 term.</param>
        /// <param name="b">Coefficient of x^1 term.</param>
        /// <param name="c">Coefficient of x^0 term.</param>
        /// <returns>Solutions to quadratic equation.</returns>
        private static IEnumerable<float> Find01TurningPoints(in float a, in float b, in float c) {

            List<float> turningPoints = new List<float>();

            if (a != 0) {

                float discriminant = (b * b) - (4.0f * a * c);
                if (discriminant >= 0.0f) {

                    float sqrt = Mathf.Sqrt(discriminant);
                    float coefficient = 0.5f / a;
                    float solution = (-b + sqrt) * coefficient; // find the first solution
                    if (solution >= 0.0f && solution <= 1.0f) turningPoints.Add(solution); // add the solution
                    if (discriminant != 0.0f) {
                        solution = (-b - sqrt) * coefficient;
                        if (solution >= 0.0f && solution <= 1.0f) turningPoints.Add(solution);
                    }

                }

            }

            return turningPoints;

        }

#endif
        #endregion

        #region GetLineLength

        public static float GetLineLength(in Vector3[] line) {

            if (line == null) throw new ArgumentNullException(nameof(line));

            int segmentCount = line.Length;
            if (segmentCount < 2) return 0.0f;

            float length = 0.0f;
            int index = 0;
            segmentCount -= 1; // reduce by 1 (since otherwise an out of bounds exception will occur)
            while (index < segmentCount) length += Vector3.Distance(line[index++], line[index]);
            return length;

        }

        #endregion

        #region CheckBit

        /// <summary>
        /// Checks if a bit is true or not in the int.
        /// </summary>
        /// <param name="value">Integer to check the bits in.</param>
        /// <param name="bit">Index of the bit to check (starts at 0).</param>
        /// <returns>True if the bit is 1, otherwise false.</returns>
        public static bool CheckBit(this int value, in int bit) { return (value & (1 << bit)) != 0; }

        #endregion

        #region MinAngle

        /// <summary>
        /// Minimum angle (in degrees) between three points.
        /// This is never greater than 180deg.
        /// </summary>
        public static float MinAngle(in Vector3 p0, in Vector3 p1, in Vector3 p2) => Vector3.Angle(p0 - p1, p2 - p1);

        #endregion

        #region ToLocalPoint

        /// <summary>
        /// Converts a world-space <paramref name="point"/> into a local-space <paramref name="point"/> relative to the provided <paramref name="transform"/>.
        /// </summary>
        public static Vector3 ToLocalPoint(this Transform transform, in Vector3 point) {
            return transform != null ? transform.InverseTransformPoint(point) : point;
        }

        #endregion

        #region ToLocalPoint

        /// <summary>
        /// Converts a local-space <paramref name="point"/> into a world-space <paramref name="point"/> relative to the provided <paramref name="transform"/>.
        /// </summary>
        public static Vector3 ToWorldPoint(this Transform transform, in Vector3 point) {
            return transform != null ? transform.TransformPoint(point) : point;
        }

        #endregion

        #region ToLocalRotation

        /// <summary>
        /// Converts a world-space <paramref name="rotation"/> into a local-space rotation relative to the provided <paramref name="transform"/>.
        /// </summary>
        public static Quaternion ToLocalRotation(this Transform transform, in Quaternion rotation) {
            return transform != null ? Quaternion.Inverse(transform.rotation) * rotation : rotation;
        }

        #endregion

        #region ToWorldRotation

        /// <summary>
        /// Converts a local-space <paramref name="rotation"/> into a world-space rotation relative to the provided <paramref name="transform"/>.
        /// </summary>
        public static Quaternion ToWorldRotation(this Transform transform, in Quaternion rotation) {
            return transform != null ? transform.rotation * rotation : rotation;
        }

        #endregion

    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
    public struct NormalDistribution {

        #region variable

        /// <summary>
        /// Mean value of the <see cref="NormalDistribution"/>.
        /// </summary>
        [SerializeField]
        [FieldOffset(0)]
        public float mean;

        /// <summary>
        /// How far from the <see cref="mean"/> one standard deviation is.
        /// </summary>
        /// <remarks>
        /// This must be a non-zero positive number.
        /// </remarks>
        [SerializeField]
        [FieldOffset(4)]
        public float standardDeviation;

        #endregion

        #region constructor

        public NormalDistribution(in float mean, in float standardDeviation) {
            if (standardDeviation <= 0.0f) throw new ArgumentOutOfRangeException(nameof(standardDeviation));
            this.mean = mean;
            this.standardDeviation = standardDeviation;
        }

        #endregion

        #region logic

        #region PickRandom

        /// <summary>
        /// Picks a random value from this <see cref="NormalDistribution"/>.
        /// </summary>
        public float PickRandom() => BoxMullerTransform(UnityRandom.Range(0.0f, 1.0f), UnityRandom.Range(0.0f, 1.0f));

        #endregion

        #region BoxMullerTransform

        /// <summary>
        /// Takes two uniform <c>0.0f</c> to <c>1.0f</c> random numbers and produces a value from this <see cref="NormalDistribution"/> using a Box-Muller transform.
        /// </summary>
        /// <param name="u0">Uniform <c>0.0f</c> - <c>1.0f</c> random number.</param>
        /// <param name="u1">Uniform <c>0.0f</c> - <c>1.0f</c> random number.</param>
        /// <returns>Normally distributed value.</returns>
        private float BoxMullerTransform(in float u0, in float u1) {
            float standardNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u0)) * Mathf.Sin(2.0f * Mathf.PI * u1);
            return mean + (standardDeviation * standardNormal);
        }

        #endregion

        #region ProbabilityBelow

        public float ProbabilityBelow(in float value) => throw new NotImplementedException();

        #endregion

        #endregion

    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 16, Pack = 1)]
    public struct ClampedNormalDistribution {

        #region variable

        /// <summary>
        /// <see cref="NormalDistribution"/>.
        /// </summary>
        [SerializeField]
        [FieldOffset(0)]
        public NormalDistribution distribution;

        /// <summary>
        /// Lower clamp.
        /// </summary>
        [SerializeField]
        [FieldOffset(8)]
        public float lowerClamp;

        /// <summary>
        /// Upper clamp.
        /// </summary>
        [SerializeField]
        [FieldOffset(12)]
        public float upperClamp;

        #endregion

        #region constructor

        public ClampedNormalDistribution(in NormalDistribution distribution, in float lowerClamp, in float upperClamp) {
            this.distribution = distribution;
            if (lowerClamp < upperClamp) {
                this.lowerClamp = lowerClamp;
                this.upperClamp = upperClamp;
            } else {
                this.lowerClamp = upperClamp;
                this.upperClamp = lowerClamp;
            }
        }

        public ClampedNormalDistribution(in float mean, in float standardDeviation, in float lowerClamp, in float upperClamp) {
            distribution = new NormalDistribution(mean, standardDeviation);
            if (lowerClamp < upperClamp) {
                this.lowerClamp = lowerClamp;
                this.upperClamp = upperClamp;
            } else {
                this.lowerClamp = upperClamp;
                this.upperClamp = lowerClamp;
            }
        }

        #endregion

        #region logic

        #region PickRandom

        public float PickRandom() => Mathf.Clamp(distribution.PickRandom(), lowerClamp, upperClamp);

        #endregion

        #endregion

    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
    public struct SmoothFloat : IEquatable<SmoothFloat>, IEquatable<float> {

        #region variable

        /// <summary>
        /// Current value of the <see cref="SmoothFloat"/>.
        /// </summary>
        [FieldOffset(0)]
        public float value;

        #endregion

        #region constructor

        public SmoothFloat(in float value) {
            this.value = value;
        }

        #endregion

        #region logic

        #region Apply

        /// <summary>
        /// Applys a change towards the target value.
        /// </summary>
        /// <returns>Returns <c>true</c> if a change occurred.</returns>
        public bool Apply(in float target, in float deltaTime) {
            if (value == target) return false;
            if (value < target) {
                value += deltaTime;
                if (value > target)
                    value = target;
            } else {
                value -= deltaTime;
                if (value < target)
                    value = target;
            }
            return true;
        }

        #endregion

        #region Equals

        public bool Equals(SmoothFloat value) => this.value == value.value;

        public bool Equals(float value) => this.value == value;

        #endregion

        #endregion

        #region operators

        public static implicit operator float(SmoothFloat value) => value.value;
        public static implicit operator SmoothFloat(float value) => new SmoothFloat(value);

        #endregion

    }

    /// <summary>
    /// Describes a <see cref="Vector2"/> that can be smoothed.
    /// </summary>
    [Serializable]
    public struct SmoothVector2 : IEquatable<SmoothVector2>, IEquatable<Vector2> {

        #region variable

        public float x;
        public float y;
        [SerializeField, HideInInspector] private float _xSmoothing;
        [SerializeField, HideInInspector] private float _ySmoothing;
        private Vector2 _delta;

        #endregion

        #region property

#pragma warning disable IDE1006 // naming styles
        public float xSmoothing {
#pragma warning restore IDE1006 // naming styles
            get => _xSmoothing;
            set {
                if (value == _xSmoothing) return;
                if (value < 0.0f) throw new ArgumentException(string.Concat(nameof(xSmoothing), " must be positive."));
                if (value == float.NaN) throw new ArgumentException(string.Concat(nameof(xSmoothing), " is NaN."));
                _xSmoothing = value;
            }
        }

#pragma warning disable IDE1006 // naming styles
        public float ySmoothing {
#pragma warning restore IDE1006 // naming styles
            get => _ySmoothing;
            set {
                if (value == _ySmoothing) return;
                if (value < 0.0f) throw new ArgumentException(string.Concat(nameof(ySmoothing), " must be positive."));
                if (value == float.NaN) throw new ArgumentException(string.Concat(nameof(ySmoothing), " is NaN."));
                _ySmoothing = value;
            }
        }

#pragma warning disable IDE1006 // naming styles
        public Vector2 delta => _delta;
#pragma warning restore IDE1006 // naming styles

#pragma warning disable IDE1006 // naming styles
        public Vector2 value {
#pragma warning restore IDE1006 // naming styles
            get => new Vector2(x, y);
            set {
                x = value.x;
                y = value.y;
            }
        }

#pragma warning disable IDE1006 // naming styles
        public float magnitude => Mathf.Sqrt((x * x) + (y * y));
#pragma warning restore IDE1006 // naming styles

#pragma warning disable IDE1006 // naming styles
        public float sqrMagnitude => (x * x) + (y * y);
#pragma warning restore IDE1006 // naming styles

        #endregion

        #region constructor

        public SmoothVector2(in Vector2 value) : this(value.x, value.y, 1.0f, 1.0f) { }

        public SmoothVector2(in Vector2 value, in Vector2 smoothing) : this(value.x, value.y, smoothing.x, smoothing.y) { }

        public SmoothVector2(in float x, in float y) : this(x, y, 1.0f, 1.0f) { }

        public SmoothVector2(in float x, in float y, in float xSmoothing, in float ySmoothing) {

            if (xSmoothing < 0.0f) throw new ArgumentException(string.Concat(nameof(xSmoothing), " must be positive."));
            if (xSmoothing == float.NaN) throw new ArgumentException(string.Concat(nameof(xSmoothing), " is NaN."));

            if (ySmoothing < 0.0f) throw new ArgumentException(string.Concat(nameof(ySmoothing), " must be positive."));
            if (ySmoothing == float.NaN) throw new ArgumentException(string.Concat(nameof(ySmoothing), " is NaN."));

            this.x = x;
            this.y = y;
            _xSmoothing = xSmoothing;
            _ySmoothing = ySmoothing;
            _delta = Vector2.zero;

        }

        #endregion

        #region logic

        #region Apply

        public SmoothVector2 Apply(in Vector2 value, in float deltaTime) => Apply(value.x, value.y, deltaTime);

        public SmoothVector2 Apply(in float x, in float y, in float deltaTime) {
            float newX = Mathf.Lerp(this.x, x, _xSmoothing * deltaTime);
            float newY = Mathf.Lerp(this.y, y, _ySmoothing * deltaTime);
            _delta = new Vector2(newX - this.x, newY - this.y);
            this.x = newX;
            this.y = newY;
            return this;
        }

        #endregion

        #region Equals

        public bool Equals(Vector2 value) => value.x == x && value.y == y;

        public bool Equals(SmoothVector2 value) => value.x == x && value.y == y;

        #endregion

        #region ToString

        public override string ToString() => $"[{x}, {y}]";

        #endregion

        #endregion

        #region operators

        public static implicit operator Vector2(SmoothVector2 value) => new Vector2(value.x, value.y);
        public static implicit operator SmoothVector2(Vector2 value) => new SmoothVector2(value);

        #endregion

    }

    /// <summary>
    /// Describes a <see cref="Vector3"/> that can be smoothed.
    /// </summary>
    [Serializable]
    public struct SmoothVector3 : IEquatable<SmoothVector3>, IEquatable<SmoothVector2>, IEquatable<Vector3>, IEquatable<Vector2> {

        #region variable

        public float x;
        public float y;
        public float z;
        [SerializeField, HideInInspector] private float _xSmoothing;
        [SerializeField, HideInInspector] private float _ySmoothing;
        [SerializeField, HideInInspector] private float _zSmoothing;
        private Vector3 _delta;

        #endregion

        #region property

#pragma warning disable IDE1006 // naming styles
        public float xSmoothing {
#pragma warning restore IDE1006 // naming styles
            get => _xSmoothing;
            set {
                if (value == _xSmoothing) return;
                if (value < 0.0f) throw new ArgumentException(string.Concat(nameof(xSmoothing), " must be positive."));
                if (value == float.NaN) throw new ArgumentException(string.Concat(nameof(xSmoothing), " is NaN."));
                _xSmoothing = value;
            }
        }

#pragma warning disable IDE1006 // naming styles
        public float ySmoothing {
#pragma warning restore IDE1006 // naming styles
            get => _ySmoothing;
            set {
                if (value == _ySmoothing) return;
                if (value < 0.0f) throw new ArgumentException(string.Concat(nameof(ySmoothing), " must be positive."));
                if (value == float.NaN) throw new ArgumentException(string.Concat(nameof(ySmoothing), " is NaN."));
                _ySmoothing = value;
            }
        }

#pragma warning disable IDE1006 // naming styles
        public float zSmoothing {
#pragma warning restore IDE1006 // naming styles
            get => _zSmoothing;
            set {
                if (value == _zSmoothing) return;
                if (value < 0.0f) throw new ArgumentException(string.Concat(nameof(zSmoothing), " must be positive."));
                if (value == float.NaN) throw new ArgumentException(string.Concat(nameof(zSmoothing), " is NaN."));
                _zSmoothing = value;
            }
        }

#pragma warning disable IDE1006 // naming styles
        public Vector3 delta => _delta;
#pragma warning restore IDE1006 // naming styles

#pragma warning disable IDE1006 // naming styles
        public Vector3 value {
#pragma warning restore IDE1006 // naming styles
            get => new Vector3(x, y, z);
            set {
                x = value.x;
                y = value.y;
                z = value.z;
            }
        }

#pragma warning disable IDE1006 // naming styles
        public float magnitude => Mathf.Sqrt((x * x) + (y * y) + (z * z));
#pragma warning restore IDE1006 // naming styles

#pragma warning disable IDE1006 // naming styles
        public float sqrMagnitude => (x * x) + (y * y) + (z * z);
#pragma warning restore IDE1006 // naming styles

        #endregion

        #region constructor

        public SmoothVector3(in Vector3 value) : this(value.x, value.y, value.z, 1.0f, 1.0f, 1.0f) { }

        public SmoothVector3(in Vector3 value, in Vector3 smoothing) : this(value.x, value.y, value.z, smoothing.x, smoothing.y, smoothing.z) { }

        public SmoothVector3(in float x, in float y, in float z) : this(x, y, z, 1.0f, 1.0f, 1.0f) { }

        public SmoothVector3(in float x, in float y, in float z, in float xSmoothing, in float ySmoothing, in float zSmoothing) {

            if (xSmoothing < 0.0f) throw new ArgumentException(string.Concat(nameof(xSmoothing), " must be positive."));
            if (xSmoothing == float.NaN) throw new ArgumentException(string.Concat(nameof(xSmoothing), " is NaN."));

            if (ySmoothing < 0.0f) throw new ArgumentException(string.Concat(nameof(ySmoothing), " must be positive."));
            if (ySmoothing == float.NaN) throw new ArgumentException(string.Concat(nameof(ySmoothing), " is NaN."));

            if (zSmoothing < 0.0f) throw new ArgumentException(string.Concat(nameof(zSmoothing), " must be positive."));
            if (zSmoothing == float.NaN) throw new ArgumentException(string.Concat(nameof(zSmoothing), " is NaN."));

            this.x = x;
            this.y = y;
            this.z = z;
            _xSmoothing = xSmoothing;
            _ySmoothing = ySmoothing;
            _zSmoothing = zSmoothing;
            _delta = Vector3.zero;

        }

        #endregion

        #region logic

        #region Apply

        public SmoothVector3 Apply(in Vector3 value, in float deltaTime) => Apply(value.x, value.y, value.z, deltaTime);

        public SmoothVector3 Apply(in float x, in float y, in float z, in float deltaTime) {
            float newX = Mathf.Lerp(this.x, x, _xSmoothing * deltaTime);
            float newY = Mathf.Lerp(this.y, y, _ySmoothing * deltaTime);
            float newZ = Mathf.Lerp(this.z, z, _zSmoothing * deltaTime);
            _delta = new Vector3(newX - this.x, newY - this.y, newZ - this.z);
            this.x = newX;
            this.y = newY;
            this.z = newZ;
            return this;
        }

        #endregion

        #region Equals

        public bool Equals(Vector2 value) => value.x == x && value.y == y;

        public bool Equals(Vector3 value) => value.x == x && value.y == y && value.z == z;

        public bool Equals(SmoothVector2 value) => value.x == x && value.y == y;

        public bool Equals(SmoothVector3 value) => value.x == x && value.y == y && value.z == z;

        #endregion

        #region ToString

        public override string ToString() => string.Concat('[', x, ',', y, ',', z, ']');

        #endregion

        #endregion

        #region operators

        public static implicit operator Vector3(SmoothVector3 value) => new Vector3(value.x, value.y);
        public static implicit operator SmoothVector3(Vector3 value) => new SmoothVector3(value);

        #endregion

    }

}