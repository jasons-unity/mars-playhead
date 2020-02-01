using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Defines the API for a Camera Projection Matrix Provider
    /// This functionality provider is responsible for providing a projection matrix to match the device's physical camera
    /// </summary>
    public interface IProvidesCameraProjectionMatrix : IFunctionalityProvider
    {
        /// <summary>
        /// Get the current camera projection matrix
        /// </summary>
        /// <returns>The current projection matrix-- will be null if no frames have been received yet</returns>
        Matrix4x4? GetProjectionMatrix();
    }
}
