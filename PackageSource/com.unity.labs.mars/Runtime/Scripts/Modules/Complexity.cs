using System;
using UnityEngine;

namespace Unity.Labs.MARS.Query
{
    /// <summary>
    /// Represents the minimum real-world data needed in order to start fulfilling conditions.
    /// </summary>
    [Serializable]
    public struct Complexity
    {
        /// <summary>
        /// Bounds that define the minimum volume of spatial data needed.
        /// </summary>
        public Bounds minBounds;
    }
}
