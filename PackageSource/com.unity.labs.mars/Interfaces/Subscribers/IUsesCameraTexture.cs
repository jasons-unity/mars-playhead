using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to a camera texture
    /// </summary>
    public interface IUsesCameraTexture : IFunctionalitySubscriber<IProvidesCameraTexture>
    {
    }

    public static class IUsesCameraTextureMethods
    {
        /// <summary>
        /// Get the current camera texture
        /// </summary>
        /// <returns>The current camera texture</returns>
        public static Texture2D GetCameraTexture(this IUsesCameraTexture obj)
        {
#if !FI_AUTOFILL
            return obj.provider.GetCameraTexture();
#else
            return default(RenderTexture);
#endif
        }
    }
}
