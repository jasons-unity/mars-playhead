using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides a template for hit test results
    /// </summary>
    public struct MRHitTestResult
    {
        /// <summary>
        /// The position and orientation of the surface that was hit
        /// </summary>
        public Pose pose { get; set; }
    }
}
