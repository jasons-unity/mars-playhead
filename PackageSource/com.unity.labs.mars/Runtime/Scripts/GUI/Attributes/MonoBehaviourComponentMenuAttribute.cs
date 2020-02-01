using System;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Used to define the menu item and they type of MonoBehaviour component it will add.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MonoBehaviourComponentMenuAttribute : ComponentMenuAttribute
    {
        /// <summary>
        /// Create an individual Menu item that is connected to MonoBehaviour component type.
        /// </summary>
        /// <param name="componentType">Type of MonoBehaviour component to add.</param>
        /// <param name="menuItem">Menu path for the item.</param>
        public MonoBehaviourComponentMenuAttribute(Type componentType, string menuItem)
            : base(componentType, menuItem) { }
    }
}
