using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to light estimation features
    /// </summary>
    public interface IUsesLightEstimation : IFunctionalitySubscriber<IProvidesLightEstimation>
    {
    }

    public static class IUsesLightEstimationMethods
    {
        /// <summary>
        /// Try to get the ambient light intensity
        /// </summary>
        /// <param name="intensity">The ambient light intensity</param>
        /// <returns>True if the operation succeeded; false if the data is not available or the feature is not supported</returns>
        public static bool TryGetAmbientIntensity(this IUsesLightEstimation obj, out float intensity)
        {
#if !FI_AUTOFILL
            return obj.provider.TryGetAmbientIntensity(out intensity);
#else
            intensity = default(float);
            return false;
#endif
        }

        /// <summary>
        /// Try to get the color temperature
        /// </summary>
        /// <param name="temperature">The color temperature</param>
        /// <returns>True if the operation succeeded; false if the data is not available or the feature is not supported</returns>
        public static bool GetColorTemperature(this IUsesLightEstimation obj, out float temperature)
        {
#if !FI_AUTOFILL
            return obj.provider.TryGetColorTemperature(out temperature);
#else
            temperature = default(float);
            return false;
#endif
        }

        /// <summary>
        /// Try to get the light direction
        /// </summary>
        /// <param name="direction">The light direction</param>
        /// <returns>True if the operation succeeded; false if the data is not available or the feature is not supported</returns>
        public static bool GetLightDirection(this IUsesLightEstimation obj, out Vector3 direction)
        {
#if !FI_AUTOFILL
            return obj.provider.TryGetLightDirection(out direction);
#else
            direction = default(Vector3);
            return false;
#endif
        }

        /// <summary>
        /// Subscribe to the ambientIntensityUpdated event, which is called when ambient light intensity changes
        /// </summary>
        /// <param name="ambientIntensityUpdated">The delegate to subscribe</param>
        public static void SubscribeAmbientIntensityUpdated(this IUsesLightEstimation obj, Action<float> ambientIntensityUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.ambientIntensityUpdated += ambientIntensityUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe from the ambientIntensityUpdated event, which is called when ambient light intensity changes
        /// </summary>
        /// <param name="ambientIntensityUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribeAmbientIntensityUpdated(this IUsesLightEstimation obj, Action<float> ambientIntensityUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.ambientIntensityUpdated -= ambientIntensityUpdated;
#endif
        }

        /// <summary>
        /// Subscribe to the colorTemperatureUpdated event, which is called when the color temperature changes
        /// </summary>
        /// <param name="colorTemperatureUpdated">The delegate to subscribe</param>
        public static void SubscribeColorTemperatureUpdated(this IUsesLightEstimation obj, Action<float> colorTemperatureUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.colorTemperatureUpdated += colorTemperatureUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe from the colorTemperatureUpdated event, which is called when the color temperature changes
        /// </summary>
        /// <param name="colorTemperatureUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribeColorTemperatureUpdated(this IUsesLightEstimation obj, Action<float> colorTemperatureUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.colorTemperatureUpdated -= colorTemperatureUpdated;
#endif
        }

        /// <summary>
        /// Subscribe to the lightDirectionUpdated event, which is called when the light direction changes
        /// </summary>
        /// <param name="lightDirectionUpdated">The delegate to subscribe</param>
        public static void SubscribePointCloudUpdated(this IUsesLightEstimation obj, Action<Vector3> lightDirectionUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.lightDirectionUpdated += lightDirectionUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe from the lightDirectionUpdated event, which is called when the light direction changes
        /// </summary>
        /// <param name="lightDirectionUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribeLightDirectionUpdated(this IUsesLightEstimation obj, Action<Vector3> lightDirectionUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.lightDirectionUpdated -= lightDirectionUpdated;
#endif
        }
    }
}
