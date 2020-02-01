using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Base class that must be used to build up any members of MultiConditions
    /// </summary>
    [System.Serializable]
    public class SubCondition
    {
        /// <summary>
        /// Refers to the MonoBehaviour that is a MultiCondition hosting this SubCondition
        /// </summary>
        [HideInInspector]
        public MonoBehaviour Host;

        public bool enabled { get { return (Host != null) ? Host.enabled : false; } }
    }
}
