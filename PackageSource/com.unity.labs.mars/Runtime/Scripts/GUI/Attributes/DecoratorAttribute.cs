using System;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Used for Drawing custom decorator property drawers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DecoratorAttribute : Attribute
    {
        /// <summary>
        /// Type of attribute decorator is being applied to.
        /// </summary>
        public readonly Type attributeType;

        /// <summary>
        /// Used for Drawing custom decorator property drawers.
        /// </summary>
        /// <param name="attributeType"></param>
        public DecoratorAttribute(Type attributeType)
        {
            this.attributeType = attributeType;
        }
    }
}
