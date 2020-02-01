using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to point cloud features
    /// </summary>
    public interface IUsesPointCloud : IFunctionalitySubscriber<IProvidesPointCloud>
    {
    }

    public static class IUsesPointCloudMethods
    {
        /// <summary>
        /// Get the latest available point cloud data
        /// </summary>
        /// <returns>The point cloud data</returns>
        public static Dictionary<MarsTrackableId, PointCloudData> GetPoints(this IUsesPointCloud obj)
        {
#if FI_AUTOFILL
            return default;
#else
            return obj.provider.GetPoints();
#endif
        }

        /// <summary>
        /// Subscribe to the pointCloudUpdated event, which is called whenever the point cloud is updated
        /// </summary>
        /// <param name="pointCloudUpdated">The delegate to subscribe</param>
        public static void SubscribePointCloudUpdated(this IUsesPointCloud obj, Action<Dictionary<MarsTrackableId, PointCloudData>> pointCloudUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.PointCloudUpdated += pointCloudUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe a delegate from the pointCloudUpdated event
        /// </summary>
        /// <param name="pointCloudUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribePointCloudUpdated(this IUsesPointCloud obj, Action<Dictionary<MarsTrackableId, PointCloudData>> pointCloudUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.PointCloudUpdated -= pointCloudUpdated;
#endif
        }

        /// <summary>
        /// Stop detecting point clouds. This will happen automatically on destroying the session. It is only necessary to
        /// call this method to pause plane detection while maintaining camera tracking
        /// </summary>
        public static void StopDetectingPoints(this IUsesPointCloud obj)
        {
#if !FI_AUTOFILL
            obj.provider.StopDetectingPoints();
#endif
        }

        /// <summary>
        /// Start detecting point clouds. Point cloud detection is enabled on initialization, so this is only necessary after
        /// calling StopDetecting.
        /// </summary>
        public static void StartDetectingPoints(this IUsesPointCloud obj)
        {
#if !FI_AUTOFILL
            obj.provider.StartDetectingPoints();
#endif
        }
    }
}
