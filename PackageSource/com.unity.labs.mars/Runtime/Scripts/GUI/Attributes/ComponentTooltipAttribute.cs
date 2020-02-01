using System;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Used to define the tooltip of a MARS Component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ComponentTooltipAttribute : Attribute
    {
        /// <summary>
        /// The tooltip to be displayed with the class label.
        /// </summary>
        public readonly string tooltip;

        public ComponentTooltipAttribute(string tooltip) { this.tooltip = tooltip; }
    }
}
