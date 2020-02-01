using UnityEngine.XR.ARSubsystems;

namespace Unity.Labs.MARS.Providers
{
    public static class PlaneAlignmentExtensions
    {
        public static MarsPlaneAlignment ToMarsPlaneAlignment(this PlaneAlignment arPlaneAlignment)
        {
            switch (arPlaneAlignment)
            {
                case PlaneAlignment.HorizontalDown:
                    return MarsPlaneAlignment.HorizontalDown;
                case PlaneAlignment.HorizontalUp:
                    return MarsPlaneAlignment.HorizontalUp;
                case PlaneAlignment.Vertical:
                    return MarsPlaneAlignment.Vertical;
                case PlaneAlignment.NotAxisAligned:
                    return MarsPlaneAlignment.NonAxis;
            }

            return MarsPlaneAlignment.None;
        }
    }
}
