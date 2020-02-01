using System;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Attribute used to clamp a float or in variable between a min and max value in a property decorator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinMaxAttribute : Attribute
    {
        /// <summary>
        /// Min value that can be set in a property decorator.
        /// </summary>
        public readonly float min;

        /// <summary>
        /// Max value that can be set in a property decorator.
        /// </summary>
        public readonly float max;

        /// <summary>
        /// Attribute used to clamp a float or in variable between a min and max value in a property decorator.
        /// </summary>
        /// <param name="min">Min value that can be set in a property decorator.</param>
        /// <param name="max">Max value that can be set in a property decorator.</param>
        public MinMaxAttribute(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
    }
}
