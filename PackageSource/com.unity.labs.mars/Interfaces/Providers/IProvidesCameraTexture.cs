using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Defines the API for a Camera Provider
    /// This functionality provider is responsible for access to a camera texture.
    /// </summary>
    public interface IProvidesCameraTexture : IFunctionalityProvider
    {
        /// <summary>
        /// Get the current camera texture
        /// </summary>
        /// <returns>The current camera texture</returns>
        Texture2D GetCameraTexture();
    }
}
