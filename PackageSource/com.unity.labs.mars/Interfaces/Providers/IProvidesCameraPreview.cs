using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Defines the API for a Camera Preview Provider
    /// This functionality provider is responsible for providing information about the in-scene object for a camera preview
    /// </summary>
    public interface IProvidesCameraPreview : IFunctionalityProvider
    {
        /// <summary>
        /// Get the world position of the camera preview object in the scene
        /// </summary>
        /// <returns>The world position of the camera preview object</returns>
        Vector3 GetPreviewObjectPosition();

        /// <summary>
        /// Called when the camera preview object exists and is ready
        /// </summary>
        event Action<IProvidesCameraPreview> previewReady;
    }
}
