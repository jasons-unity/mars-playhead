using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to camera intrinsics
    /// </summary>
    public interface IUsesCameraIntrinsics : IFunctionalitySubscriber<IProvidesCameraIntrinsics>
    {
    }

    public static class IUsesCameraIntrinsicsMethods
    {
        /// <summary>
        /// Get the field of view of the physical camera
        /// </summary>
        /// <returns>The camera field of view</returns>
        public static float GetFOV(this IUsesCameraIntrinsics obj)
        {
#if !FI_AUTOFILL
            return obj.provider.GetFOV();
#else
            return default(float);
#endif
        }
    }
}
