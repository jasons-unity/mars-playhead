#if INCLUDE_MARS
using Unity.Labs.Utils;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Unity.Labs.MARS.Providers
{
    public static class ReferencePointExtensions
    {
        public static MRReferencePoint ToMRReferencePoint(this ARReferencePoint point)
        {
            var mrReferencePoint = new MRReferencePoint
            {
                id = point.trackableId.ToMarsId(),
                pose = point.transform.GetLocalPose()
            };

            switch (point.trackingState)
            {
                case TrackingState.Tracking:
                    mrReferencePoint.trackingState = MARSTrackingState.Tracking;
                    break;
                case TrackingState.Limited:
                    mrReferencePoint.trackingState = MARSTrackingState.Limited;
                    break;
                default:
                    mrReferencePoint.trackingState = MARSTrackingState.Unknown;
                    break;
            }

            return mrReferencePoint;
        }
    }
}
#endif
