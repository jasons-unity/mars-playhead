using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to MR hit testing features
    /// </summary>
    public interface IUsesMRHitTesting : IFunctionalitySubscriber<IProvidesMRHitTesting>
    {
    }

    public static class IUsesHitTestingMethods
    {
        /// <summary>
        /// Perform a screen-based hit test against MR data
        /// </summary>
        /// <param name="screenPosition">The screen position from which test will originate</param>
        /// <param name="result">The result of the hit test</param>
        /// <param name="types">The types of results to test against</param>
        /// <returns>Whether the test succeeded</returns>
        public static bool ScreenHitTest(this IUsesMRHitTesting obj, Vector2 screenPosition,
            out MRHitTestResult result, MRHitTestResultTypes types = MRHitTestResultTypes.Any)
        {
#if !FI_AUTOFILL
            return obj.provider.ScreenHitTest(screenPosition, out result, types);
#else
            return default(bool);
#endif
        }

        /// <summary>
        /// Perform a world-based hit test against MR feature points.
        /// </summary>
        /// <param name="ray">The ray to test</param>
        /// <param name="result">The result of the hit test</param>
        /// <param name="types">The types of results to test against</param>
        /// <returns>Whether the test succeeded</returns>
        public static bool WorldHitTestHitTest(this IUsesMRHitTesting obj, Ray ray,
            out MRHitTestResult result, MRHitTestResultTypes types = MRHitTestResultTypes.Any)
        {
#if !FI_AUTOFILL
            return obj.provider.WorldHitTest(ray, out result, types);
#else
            return default(bool);
#endif
        }

        /// <summary>
        /// Stop performing hit tests. This will happen automatically on destroying the session. It is only necessary to
        /// call this method to pause plane detection while maintaining camera tracking
        /// </summary>
        public static void StopHitTesting(this IUsesMRHitTesting obj)
        {
#if !FI_AUTOFILL
            obj.provider.StopHitTesting();
#endif
        }

        /// <summary>
        /// Start performing hit tests. Hit test support is enabled on initialization, so this is only necessary after
        /// calling StopDetecting.
        /// </summary>
        public static void StartHitTesting(this IUsesMRHitTesting obj)
        {
#if !FI_AUTOFILL
            obj.provider.StartHitTesting();
#endif
        }

    }
}
