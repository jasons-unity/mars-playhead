using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to face tracking features
    /// </summary>
    public interface IUsesFaceTracking : IFunctionalitySubscriber<IProvidesFaceTracking>, IFaceFeature
    {
    }

    public static class IUsesFaceTrackingMethods
    {
        /// <summary>
        /// Get the total number of faces that can be tracked by this provider simultaneously
        /// </summary>
        /// <returns>The maximum possible number of simultaneously tracked faces, -1 if there is no theoretical limit</returns>
        public static int GetMaxFaceCount(this IUsesFaceTracking obj)
        {
#if !FI_AUTOFILL
            return obj.provider.GetMaxFaceCount();
#else
            return default(int);
#endif
        }

        /// <summary>
        /// Get the currently tracked faces
        /// </summary>
        /// <param name="faces">A list of IMRFace objects to which the currently tracked planes will be added</param>
        public static void GetFaces(this IUsesFaceTracking obj, List<IMRFace> faces)
        {
#if !FI_AUTOFILL
            obj.provider.GetFaces(faces);
#endif
        }

        /// <summary>
        /// Subscribe to the faceAdded event, which is called whenever a face becomes tracked for the first time
        /// </summary>
        /// <param name="faceAdded">The delegate to subscribe</param>
        public static void SubscribeFaceAdded(this IUsesFaceTracking obj, Action<IMRFace> faceAdded)
        {
#if !FI_AUTOFILL
            obj.provider.FaceAdded += faceAdded;
#endif
        }

        /// <summary>
        /// Unsubscribe a delegate from the faceAdded event
        /// </summary>
        /// <param name="faceAdded">The delegate to unsubscribe</param>
        public static void UnsubscribeFaceAdded(this IUsesFaceTracking obj, Action<IMRFace> faceAdded)
        {
#if !FI_AUTOFILL
            obj.provider.FaceAdded -= faceAdded;
#endif
        }

        /// <summary>
        /// Subscribe to the faceUpdated event, which is called when a tracked face has updated data
        /// </summary>
        /// <param name="faceUpdated">The delegate to subscribe</param>
        public static void SubscribeFaceUpdated(this IUsesFaceTracking obj, Action<IMRFace> faceUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.FaceUpdated += faceUpdated;
#endif
        }

        /// <summary>
        /// Unsubscribe a delegate from the faceUpdated event
        /// </summary>
        /// <param name="faceUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribeFaceUpdated(this IUsesFaceTracking obj, Action<IMRFace> faceUpdated)
        {
#if !FI_AUTOFILL
            obj.provider.FaceUpdated -= faceUpdated;
#endif
        }

        /// <summary>
        /// Subscribe to the faceRemoved event, which is called whenever a tracked face is removed (lost)
        /// </summary>
        /// <param name="faceRemoved">The delegate to subscribe</param>
        public static void SubscribeFaceRemoved(this IUsesFaceTracking obj, Action<IMRFace> faceRemoved)
        {
#if !FI_AUTOFILL
            obj.provider.FaceRemoved += faceRemoved;
#endif
        }

        /// <summary>
        /// Unsubscribe a delegate from the faceRemoved event
        /// </summary>
        /// <param name="faceRemoved">The delegate to unsubscribe</param>
        public static void UnsubscribeFaceRemoved(this IUsesFaceTracking obj, Action<IMRFace> faceRemoved)
        {
#if !FI_AUTOFILL
            obj.provider.FaceRemoved -= faceRemoved;
#endif
        }
    }
}
