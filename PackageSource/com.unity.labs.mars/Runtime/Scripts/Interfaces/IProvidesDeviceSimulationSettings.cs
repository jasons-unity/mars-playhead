using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public interface IProvidesDeviceSimulationSettings : IFunctionalityProvider
    {
        /// <summary>
        /// World pose of the device at the start of simulation
        /// </summary>
        Pose DeviceStartingPose { get; }

        /// <summary>
        /// Bounds encapsulating the current environment, used to restrict device movement
        /// </summary>
        Bounds EnvironmentBounds { get; }

        /// <summary>
        /// Called when the simulation environment has changed
        /// </summary>
        event Action EnvironmentChanged;
    }
}
