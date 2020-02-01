using System;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Enumerates the types of results used to filter MR hit tests
    /// </summary>
    [Flags]
    public enum MRHitTestResultTypes
    {
        /// <summary>
        /// 3D feature points like tracking anchors and point cloud points
        /// </summary>
        FeaturePoint             = 1 << 0,

        /// <summary>
        /// Horizontal planes
        /// </summary>
        HorizontalPlane          = 1 << 1,

        /// <summary>
        /// Vertical planes
        /// </summary>
        VerticalPlane            = 1 << 2,

        /// <summary>
        /// All plane types
        /// </summary>
        Plane = HorizontalPlane | VerticalPlane,

        /// <summary>
        /// Any type of result
        /// </summary>
        Any = FeaturePoint | Plane
    }
}
