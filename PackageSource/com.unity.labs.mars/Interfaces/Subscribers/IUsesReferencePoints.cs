using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to tracked world reference points
    /// </summary>
    public interface IUsesReferencePoints : IFunctionalitySubscriber<IProvidesReferencePoints>
    {
    }

    public static class IUsesReferencePointsMethods
    {
        /// <summary>
        /// Get all registered reference points
        /// </summary>
        /// <param name="referencePoints">A list of MRReferencePoint objects to which the currently tracked reference points will be added</param>
        public static void GetAllReferencePoints(this IUsesReferencePoints obj, List<MRReferencePoint> referencePoints)
        {
#if !FI_AUTOFILL
            obj.provider.GetAllReferencePoints(referencePoints);
#endif
        }

        /// <summary>
        /// Add a reference point
        /// </summary>
        /// <returns>true if adding the point succeeded, false otherwise</returns>
        public static bool TryAddReferencePoint(this IUsesReferencePoints obj, Pose pose, out MarsTrackableId referencePointId)
        {
#if !FI_AUTOFILL
            return obj.provider.TryAddReferencePoint(pose, out referencePointId);
#else
            referencePointId = default(string);
            return default(bool);
#endif
        }

        /// <summary>
        /// Get a reference point
        /// </summary>
        /// <returns>true if getting the point succeeded, false otherwise</returns>
        public static bool TryGetReferencePoint(this IUsesReferencePoints obj, MarsTrackableId referencePointId, out MRReferencePoint point)
        {
#if !FI_AUTOFILL
            return obj.provider.TryGetReferencePoint(referencePointId, out point);
#else
            point = default(TReferencePoint);
            return default(bool);
#endif
        }

        /// <summary>
        /// Remove a reference point
        /// </summary>
        /// <returns>true if removing the point succeeded, false otherwise</returns>
        public static bool TryRemoveReferencePoint(this IUsesReferencePoints obj, MarsTrackableId referencePointId)
        {
#if !FI_AUTOFILL
            return obj.provider.TryRemoveReferencePoint(referencePointId);
#else
             return default(bool);
#endif
        }

        /// <summary>
        /// Subscribe to the pointAdded event, which is called when a reference point is first added
        /// </summary>
        /// <param name="pointAdded">The delegate to subscribe</param>
        public static void SubscribePointAdded(this IUsesReferencePoints obj, Action<MRReferencePoint> pointAdded)
        {
#if !FI_AUTOFILL
            obj.provider.pointAdded += pointAdded;
#endif
        }

        /// <summary>
        /// Unsubscribe from the pointAdded event, which is called when a reference point is first added
        /// </summary>
        /// <param name="pointAdded">The delegate to unsubscribe</param>
        public static void UnsubscribePointAdded(this IUsesReferencePoints obj, Action<MRReferencePoint> pointAdded)
        {
#if !FI_AUTOFILL
            obj.provider.pointAdded -= pointAdded;
#endif
        }

        /// <summary>
        /// Subscribe to the pointUpdated event, which is called when a reference point is updated
        /// </summary>
        /// <param name="pointUpdated">The delegate to subscribe</param>
        public static void SubscribePointUpdated(this IUsesReferencePoints obj, Action<MRReferencePoint> pointUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.pointUpdated += pointUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe from the pointUpdated event, which is called when a reference point is updated
        /// </summary>
        /// <param name="pointUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribePointUpdated(this IUsesReferencePoints obj, Action<MRReferencePoint> pointUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.pointUpdated -= pointUpdated;
#endif
        }

        /// <summary>
        /// Subscribe to the pointRemoved event, which is called when a reference point is removed
        /// </summary>
        /// <param name="pointRemoved">The delegate to subscribe</param>
        public static void SubscribePointRemoved(this IUsesReferencePoints obj, Action<MRReferencePoint> pointRemoved)
        {
#if !FI_AUTOFILL
            obj.provider.pointRemoved += pointRemoved;
#endif
        }

        /// <summary>
        /// Unsubscribe from the pointRemoved event, which is called when a reference point is removed
        /// </summary>
        /// <param name="pointRemoved">The delegate to unsubscribe</param>
        public static void UnsubscribePointRemoved(this IUsesReferencePoints obj, Action<MRReferencePoint> pointRemoved)
        {
#if !FI_AUTOFILL
            obj.provider.pointRemoved -= pointRemoved;
#endif
        }

        /// <summary>
        /// Stop tracking reference points. This will happen automatically on destroying the session. It is only necessary to
        /// call this method to pause plane detection while maintaining camera tracking
        /// </summary>
        public static void StopTrackingReferencePoints(this IUsesReferencePoints obj)
        {
#if !FI_AUTOFILL
            obj.provider.StopTrackingReferencePoints();
#endif
        }

        /// <summary>
        /// Start tracking reference points. Point cloud detection is enabled on initialization, so this is only necessary after
        /// calling StopDetecting.
        /// </summary>
        public static void StartTrackingReferencePoints(this IUsesReferencePoints obj)
        {
#if !FI_AUTOFILL
            obj.provider.StartTrackingReferencePoints();
#endif
        }
    }
}
