using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to camera intrinsics
    /// </summary>
    public interface IUsesCameraPreview : IFunctionalitySubscriber<IProvidesCameraPreview>
    {
    }

    public static class IUsesCameraPreviewMethods
    {
        /// <summary>
        /// Get the field of view of the physical camera
        /// </summary>
        /// <returns>The camera field of view</returns>
        public static Vector3 GetPreviewObjectPosition(this IUsesCameraPreview obj)
        {
#if !FI_AUTOFILL
            return obj.provider.GetPreviewObjectPosition();
#else
            return default(Vector3);
#endif
        }

        /// <summary>
        /// Subscribe a given action to the previewReady event, which is called when the preview object exists and is ready
        /// </summary>
        /// <param name="previewReady">The action to subscribe to the previewReady event</param>
        public static void SubscribeToPreviewReady(this IUsesCameraPreview obj, Action<IProvidesCameraPreview> previewReady)
        {
            obj.provider.previewReady += previewReady;
        }

        /// <summary>
        /// Unsubscribe a given action to the previewReady event
        /// </summary>
        /// <param name="previewReady">The action to unsubscribe to the previewReady event</param>
        public static void UnsubscribeToPreviewReady(this IUsesCameraPreview obj, Action<IProvidesCameraPreview> previewReady)
        {
            obj.provider.previewReady -= previewReady;
        }
    }
}
