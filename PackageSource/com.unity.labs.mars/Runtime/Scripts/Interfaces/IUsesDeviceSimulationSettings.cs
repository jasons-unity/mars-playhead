using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public interface IUsesDeviceSimulationSettings : IFunctionalitySubscriber<IProvidesDeviceSimulationSettings>
    {
    }

    public static class IUsesDeviceSimulationSettingsMethods
    {
        /// <summary>
        /// Gets the world pose of the device at the start of simulation
        /// </summary>
        public static Pose GetDeviceStartingPose(this IUsesDeviceSimulationSettings obj)
        {
#if !FI_AUTOFILL
            return obj.provider.DeviceStartingPose;
#else
            return default(Pose);
#endif
        }

        /// <summary>
        /// Gets the bounds encapsulating the current environment, used to restrict device movement
        /// </summary>
        public static Bounds GetEnvironmentBounds(this IUsesDeviceSimulationSettings obj)
        {
#if !FI_AUTOFILL
            return obj.provider.EnvironmentBounds;
#else
            return default(Bounds);
#endif
        }

        /// <summary>
        /// Subscribe to the EnvironmentChanged event, which is called when the simulation environment changes
        /// </summary>
        /// <param name="environmentChanged">The delegate to subscribe</param>
        public static void SubscribeEnvironmentChanged(this IUsesDeviceSimulationSettings obj, Action environmentChanged)
        {
#if !FI_AUTOFILL
            obj.provider.EnvironmentChanged += environmentChanged;
#endif
        }

        /// <summary>
        /// Unsubscribe from the EnvironmentChanged event, which is called when the simulation environment changes
        /// </summary>
        /// <param name="environmentChanged">The delegate to unsubscribe</param>
        public static void UnsubscribeEnvironmentChanged(this IUsesDeviceSimulationSettings obj, Action environmentChanged)
        {
#if !FI_AUTOFILL
            obj.provider.EnvironmentChanged -= environmentChanged;
#endif
        }
    }
}
