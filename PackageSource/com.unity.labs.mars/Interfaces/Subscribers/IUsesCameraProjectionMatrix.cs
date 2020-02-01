using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to a projection matrix that matches the physical camera
    /// </summary>
    public interface IUsesCameraProjectionMatrix : IFunctionalitySubscriber<IProvidesCameraProjectionMatrix>
    {
    }

    public static class IUsesCameraProjectionMatrixMethods
    {
        /// <summary>
        /// Get the current camera projection matrix
        /// </summary>
        /// <returns>The current camera projection matrix</returns>
        public static Matrix4x4? GetProjectionMatrix(this IUsesCameraProjectionMatrix obj)
        {
#if !FI_AUTOFILL
            return obj.provider.GetProjectionMatrix();
#else
            return default(Matrix4x4?);
#endif
        }
    }
}
