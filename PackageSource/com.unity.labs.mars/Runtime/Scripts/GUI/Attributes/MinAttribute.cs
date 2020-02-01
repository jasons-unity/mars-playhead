using System;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Attribute used to clamp a float or in variable to a min value in a property decorator.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class MinAttribute : Attribute
    {
        /// <summary>
        /// Min value that can be set in a property decorator.
        /// </summary>
        public float min;

        /// <summary>
        /// Attribute used to clamp a float or in variable to a min value in a property decorator.
        /// </summary>
        /// <param name="min">Min value that can be set in a property decorator.</param>
        public MinAttribute(float min)
        {
            this.min = min;
        }
    }
}
