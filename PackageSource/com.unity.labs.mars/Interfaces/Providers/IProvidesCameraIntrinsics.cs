using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Defines the API for a Camera Intrinsics Provider
    /// This functionality provider is responsible for providing information about the intrinsics of the physical camera
    /// </summary>
    public interface IProvidesCameraIntrinsics : IFunctionalityProvider
    {
        float GetFOV();
    }
}
