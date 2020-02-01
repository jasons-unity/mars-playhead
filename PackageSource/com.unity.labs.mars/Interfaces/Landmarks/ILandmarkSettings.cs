using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Interface used for components that provide extra settings to a landmark definition
    /// </summary>
    public interface ILandmarkSettings : ISimulatable
    {
        /// <summary>
        /// Event to call when the settings data has changed such that the landmark should recalculate
        /// </summary>
        event Action<ILandmarkSettings> dataChanged;
    }
}
