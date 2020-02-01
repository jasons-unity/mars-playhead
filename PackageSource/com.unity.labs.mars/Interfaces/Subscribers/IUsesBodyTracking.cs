using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to body tracking features
    /// </summary>
    public interface IUsesBodyTracking : IFunctionalitySubscriber<IProvidesBodyTracking>
    {
    }

    public static class IUsesBodyTrackingMethods
    {
        /// <summary>
        /// Track body at specified position
        /// </summary>
        /// <param name="center">The position (center) at which to track a body</param>
        public static void TrackBody(this IUsesBodyTracking obj, Vector2 center)
        {
#if !FI_AUTOFILL
            obj.provider.TrackBody(center);
#endif
        }

        /// <summary>
        /// Get the currently tracked bodies
        /// </summary>
        /// <param name="bodies">A list of MRRect objects to which the currently tracked planes will be added</param>
        public static void GetBodies(this IUsesBodyTracking obj, List<MRBody> bodies)
        {
#if !FI_AUTOFILL
            obj.provider.GetBodies(bodies);
#endif
        }

        /// <summary>
        /// Subscribe to the bodyAdded event, which is called whenever a body becomes tracked for the first time
        /// </summary>
        /// <param name="bodyAdded">The delegate to subscribe</param>
        public static void SubscribeBodyAdded(this IUsesBodyTracking obj, Action<MRBody> bodyAdded)
        {
#if !FI_AUTOFILL
            obj.provider.bodyAdded += bodyAdded;
#endif
        }

        /// <summary>
        /// Unsubscribe a delegate from the bodyAdded event
        /// </summary>
        /// <param name="bodyAdded">The delegate to unsubscribe</param>
        public static void UnsubscribeBodyAdded(this IUsesBodyTracking obj, Action<MRBody> bodyAdded)
        {
#if !FI_AUTOFILL
            obj.provider.bodyAdded -= bodyAdded;
#endif
        }

        /// <summary>
        /// Subscribe to the bodyUpdated event, which is called when a tracked body has updated data
        /// </summary>
        /// <param name="bodyUpdated">The delegate to subscribe</param>
        public static void SubscribeBodyUpdated(this IUsesBodyTracking obj, Action<MRBody> bodyUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.bodyUpdated += bodyUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe a delegate from the bodyUpdated event
        /// </summary>
        /// <param name="bodyUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribeBodyUpdated(this IUsesBodyTracking obj, Action<MRBody> bodyUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.bodyUpdated -= bodyUpdated;
#endif
        }

        /// <summary>
        /// Subscribe to the bodyRemoved event, which is called whenever a tracked body is removed (lost)
        /// </summary>
        /// <param name="bodyRemoved">The delegate to subscribe</param>
        public static void SubscribeBodyRemoved(this IUsesBodyTracking obj, Action<MRBody> bodyRemoved)
        {
#if !FI_AUTOFILL
            obj.provider.bodyRemoved += bodyRemoved;
#endif
        }

        /// <summary>
        /// Unsubscribe a delegate from the bodyRemoved event
        /// </summary>
        /// <param name="bodyRemoved">The delegate to unsubscribe</param>
        public static void UnsubscribeBodyRemoved(this IUsesBodyTracking obj, Action<MRBody> bodyRemoved)
        {
#if !FI_AUTOFILL
            obj.provider.bodyRemoved -= bodyRemoved;
#endif
        }
    }
}
