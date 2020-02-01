using System;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to compass heading data
    /// </summary>
    public interface IUsesCompassHeading : IFunctionalitySubscriber<IProvidesCompassHeading>
    {
    }

    public static class IUsesCompassHeadingMethods
    {
        /// <summary>
        /// Get the current compass heading
        /// </summary>
        /// <returns>The compass heading</returns>
        public static float GetHeading(this IUsesCompassHeading obj)
        {
#if !FI_AUTOFILL
            return obj.provider.GetHeading();
#else
            return default(float);
#endif
        }

        /// <summary>
        /// Subscribe to the headingUpdated event, which is called when the compass heading changes
        /// </summary>
        /// <param name="headingUpdated">The delegate to subscribe</param>
        public static void SubscribeHeadingUpdated(this IUsesCompassHeading obj, Action<float> headingUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.headingUpdated += headingUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe from the headingUpdated event, which is called when the compass heading changes
        /// </summary>
        /// <param name="headingUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribeHeadingUpdated(this IUsesCompassHeading obj, Action<float> headingUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.headingUpdated -= headingUpdated;
#endif
        }
    }
}
