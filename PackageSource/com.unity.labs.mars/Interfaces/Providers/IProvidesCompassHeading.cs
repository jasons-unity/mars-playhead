using System;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Defines the API for a Compass Heading Provider
    /// This functionality provider is responsible for compass headings
    /// </summary>
    public interface IProvidesCompassHeading : IFunctionalityProvider
    {        
        /// <summary>
        /// Called when the compass heading changes
        /// </summary>
        event Action<float> headingUpdated;

        /// <summary>
        /// Get the current compass heading
        /// </summary>
        /// <returns>The compass heading</returns>
        float GetHeading();
    }
}
