using System;
using UnityEngine;

namespace Unity.Labs.Utils
{
    /// <summary>
    /// Math utilities
    /// </summary>
    public static class MathUtility
    {
        /// <summary>
        /// Constrain a value between a minimum and a maximum
        /// </summary>
        /// <param name="input">The input number</param>
        /// <param name="min">The minimum output</param>
        /// <param name="max">The maximum output</param>
        /// <returns>The <paramref name="input"/> number, clamped between <paramref name="min"/> and <paramref name="max"/> </returns>
        public static double Clamp(double input, double min, double max)
        {
            if (input > max)
                return max;

            return input < min ? min : input;
        }

        /// <summary>
        /// Finds the shortest angle distance between two angle values
        /// </summary>
        /// <param name="start">The start value</param>
        /// <param name="end">The end value</param>
        /// <param name="halfMax">Half of the max angle</param>
        /// <param name="max">The max angle value</param>
        /// <returns>The angle distance between start and end</returns>
        public static double ShortestAngleDistance(double start, double end, double halfMax, double max)
        {
            var angleDelta = end - start;
            angleDelta = Math.Abs(angleDelta) % max;
            if (angleDelta > halfMax)
                angleDelta = -(max - angleDelta);

            return angleDelta;
        }

        /// <summary>
        /// Finds the shortest angle distance between two angle values
        /// </summary>
        /// <param name="start">The start value</param>
        /// <param name="end">The end value</param>
        /// <param name="halfMax">Half of the max angle</param>
        /// <param name="max">The max angle value</param>
        /// <returns>The angle distance between start and end</returns>
        public static float ShortestAngleDistance(float start, float end, float halfMax, float max)
        {
            var angleDelta = end - start;
            angleDelta = Math.Abs(angleDelta) % max;
            if (angleDelta > halfMax)
                angleDelta = -(max - angleDelta);

            return angleDelta;
        }

        /// <summary>
        /// Is the float value infinity or NaN?
        /// </summary>
        /// <param name="value">The float value</param>
        /// <returns>True if the value is infinity or NaN (not a number), otherwise false</returns>
        public static bool IsUndefined(this float value)
        {
            return float.IsInfinity(value) || float.IsNaN(value);
        }

        /// <summary>
        /// Checks if a vector is aligned with one of the axis vectors
        /// </summary>
        /// <param name="v"> The vector </param>
        /// <returns>True if the vector is aligned with any axis, otherwise false</returns>
        public static bool IsAxisAligned(this Vector3 v)
        {
            return Mathf.Approximately(v.x * v.y, 0) && Mathf.Approximately(v.y * v.z, 0) && Mathf.Approximately(v.z * v.x, 0);
        }
    }
}
