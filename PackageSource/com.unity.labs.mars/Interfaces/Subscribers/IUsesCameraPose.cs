using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to 3dof/6dof camera tracking features
    /// </summary>
    public interface IUsesCameraPose : IFunctionalitySubscriber<IProvidesCameraPose>
    {
    }

    public static class IUsesCameraPoseMethods
    {
        /// <summary>
        /// Get the current camera pose
        /// </summary>
        /// <returns>The current camera pose</returns>
        public static Pose GetPose(this IUsesCameraPose obj)
        {
#if !FI_AUTOFILL
            return obj.provider.GetCameraPose();
#else
            return default(Pose);
#endif
        }

        /// <summary>
        /// Subscribe to the poseUpdated event, which is called when the camera pose changes
        /// </summary>
        /// <param name="poseUpdated">The delegate to subscribe</param>
        public static void SubscribePoseUpdated(this IUsesCameraPose obj, Action<Pose> poseUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.poseUpdated += poseUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe from the poseUpdated event, which is called when the camera pose changes
        /// </summary>
        /// <param name="poseUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribePoseUpdated(this IUsesCameraPose obj, Action<Pose> poseUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.poseUpdated -= poseUpdated;
#endif
        }

        /// <summary>
        /// Subscribe to the trackingTypeChanged event, which is called when the camera tracking type changes
        /// </summary>
        /// <param name="trackingTypeChanged">The delegate to subscribe</param>
        public static void SubscribeTrackingTypeChanged(this IUsesCameraPose obj, Action<MRCameraTrackingState> trackingTypeChanged)
        {
#if !FI_AUTOFILL
            obj.provider.trackingStateChanged += trackingTypeChanged;
#endif
        }

        /// <summary>
        /// Unsubscribe from the trackingTypeChanged event, which is called when the camera tracking type changes
        /// </summary>
        /// <param name="trackingTypeChanged">The delegate to unsubscribe</param>
        public static void UnsubscribeTrackingTypeChanged(this IUsesCameraPose obj, Action<MRCameraTrackingState> trackingTypeChanged)
        {
#if !FI_AUTOFILL
            obj.provider.trackingStateChanged -= trackingTypeChanged;
#endif
        }
    }
}
