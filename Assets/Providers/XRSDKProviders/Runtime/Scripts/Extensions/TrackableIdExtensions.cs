#if INCLUDE_MARS
using UnityEngine.XR.ARSubsystems;

namespace Unity.Labs.MARS.Providers
{
    public static class TrackableIdExtensions
    {
        public static MarsTrackableId ToMarsId(this TrackableId id)
        {
            MarsTrackableId marsId;
            unsafe
            {
                marsId = *(MarsTrackableId*) (&id);
            }

            return marsId;
        }
    }
}
#endif
