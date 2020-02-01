using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Defines the API for a Light Estimation Provider
    /// This functionality provider is responsible for light estimation
    /// </summary>
    public interface IProvidesLightEstimation : IFunctionalityProvider
    {
        /// <summary>
        /// Called when ambient light intensity changes
        /// </summary>
        event Action<float> ambientIntensityUpdated;

        /// <summary>
        /// Called when the color temperature changes
        /// </summary>
        event Action<float> colorTemperatureUpdated;

        /// <summary>
        /// Called when the light direction changes
        /// </summary>
        event Action<Vector3> lightDirectionUpdated;

        /// <summary>
        /// Try to get the ambient light intensity
        /// </summary>
        /// <param name="intensity">The ambient light intensity</param>
        /// <returns>True if the operation succeeded; false if the data is not available or the feature is not supported</returns>
        bool TryGetAmbientIntensity(out float intensity);

        /// <summary>
        /// Try to get the color temperature
        /// </summary>
        /// <param name="temperature">The color temperature</param>
        /// <returns>True if the operation succeeded; false if the data is not available or the feature is not supported</returns>
        bool TryGetColorTemperature(out float temperature);

        /// <summary>
        /// Try to get the light direction
        /// </summary>
        /// <param name="direction">The light direction</param>
        /// <returns>True if the operation succeeded; false if the data is not available or the feature is not supported</returns>
        bool TryGetLightDirection(out Vector3 direction);
    }
}
