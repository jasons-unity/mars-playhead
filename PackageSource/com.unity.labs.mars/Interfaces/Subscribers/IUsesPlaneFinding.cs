using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to plane finding features
    /// </summary>
    public interface IUsesPlaneFinding : IFunctionalitySubscriber<IProvidesPlaneFinding>
    {
    }

    public static class IUsesPlaneFindingMethods
    {
        /// <summary>
        /// Get the currently tracked planes
        /// </summary>
        /// <param name="planes">A list of MRPlane objects to which the currently tracked planes will be added</param>
        public static void GetPlanes(this IUsesPlaneFinding obj, List<MRPlane> planes)
        {
#if !FI_AUTOFILL
            obj.provider.GetPlanes(planes);
#endif
        }

        /// <summary>
        /// Subscribe to the planeAdded event, which is called when a plane becomes tracked for the first time
        /// </summary>
        /// <param name="planeAdded">The delegate to subscribe</param>
        public static void SubscribePlaneAdded(this IUsesPlaneFinding obj, Action<MRPlane> planeAdded)
        {
#if !FI_AUTOFILL
            obj.provider.planeAdded += planeAdded;
#endif
        }

        /// <summary>
        /// Unsubscribe from the planeAdded event, which is called when a plane becomes tracked for the first time
        /// </summary>
        /// <param name="planeAdded">The delegate to unsubscribe</param>
        public static void UnsubscribePlaneAdded(this IUsesPlaneFinding obj, Action<MRPlane> planeAdded)
        {
#if !FI_AUTOFILL
            obj.provider.planeAdded -= planeAdded;
#endif
        }

        /// <summary>
        /// Subscribe to the planeUpdated event, which is called when a tracked plane has new data
        /// </summary>
        /// <param name="planeUpdated">The delegate to subscribe</param>
        public static void SubscribePlaneUpdated(this IUsesPlaneFinding obj, Action<MRPlane> planeUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.planeUpdated += planeUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe from the planeUpdated event, which is called when a tracked plane has new data
        /// </summary>
        /// <param name="planeUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribePlaneUpdated(this IUsesPlaneFinding obj, Action<MRPlane> planeUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.planeUpdated -= planeUpdated;
#endif
        }

        /// <summary>
        /// Subscribe to the planeRemoved event, which is called when a tracked plane is removed (lost)
        /// </summary>
        /// <param name="planeRemoved">The delegate to subscribe</param>
        public static void SubscribePlaneRemoved(this IUsesPlaneFinding obj, Action<MRPlane> planeRemoved)
        {
#if !FI_AUTOFILL
            obj.provider.planeRemoved += planeRemoved;
#endif
        }

        /// <summary>
        /// Unsubscribe from the planeRemoved event, which is called when a tracked plane is removed (lost)
        /// </summary>
        /// <param name="planeRemoved">The delegate to unsubscribe</param>
        public static void UnsubscribePlaneRemoved(this IUsesPlaneFinding obj, Action<MRPlane> planeRemoved)
        {
#if !FI_AUTOFILL
            obj.provider.planeRemoved -= planeRemoved;
#endif
        }

        /// <summary>
        /// Stop detecting planes. This will happen automatically on destroying the session. It is only necessary to
        /// call this method to pause plane detection while maintaining camera tracking
        /// </summary>
        public static void StopDetectingPlanes(this IUsesPlaneFinding obj)
        {
#if !FI_AUTOFILL
            obj.provider.StopDetectingPlanes();
#endif
        }

        /// <summary>
        /// Start detecting planes. Plane detection is enabled on initialization, so this is only necessary after
        /// calling StopDetecting.
        /// </summary>
        public static void StartDetectingPlanes(this IUsesPlaneFinding obj)
        {
#if !FI_AUTOFILL
            obj.provider.StartDetectingPlanes();
#endif
        }
    }
}
