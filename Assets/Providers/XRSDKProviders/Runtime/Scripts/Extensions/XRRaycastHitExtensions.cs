#if INCLUDE_MARS
using UnityEngine.XR.ARFoundation;

namespace Unity.Labs.MARS.Providers
{
    public static class XRRaycastHitExtensions
    {
        public static MRHitTestResult ToMRHitTestResult(this ARRaycastHit hit)
        {
            return new MRHitTestResult
            {
                pose = hit.pose
            };
        }
    }
}
#endif
