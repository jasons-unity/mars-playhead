#if INCLUDE_AR_FOUNDATION
using Unity.Labs.Utils;
using UnityEngine.XR.ARFoundation;

namespace Unity.Labs.MARS.Providers
{
    public static class ARTrackedImageExtensions
    {
        public static MRMarker ToMRMarker(this ARTrackedImage trackedImage)
        {
            var mrMarker = new MRMarker
            {
                id = trackedImage.trackableId.ToMarsId(),
                pose = trackedImage.transform.GetWorldPose(),
                markerId = trackedImage.referenceImage.guid,
                extents = trackedImage.extents
            };

            return mrMarker;
        }
    }
}
#endif
