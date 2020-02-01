using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [Serializable]
    public class PlaneVoxelGenerationParams
    {
        [Tooltip("Seed with which to initialize the random number generator used to create rays")]
        public int raycastSeed;

        [Tooltip("Number of raycasts used to generate a point cloud")]
        public int raycastCount;

        [Tooltip("Maximum hit distance for each raycast")]
        public float maxHitDistance;

        [Tooltip("If the angle between a point's normal and a voxel grid direction is within this range, the point is added to the grid")]
        public float normalToleranceAngle;

        [Tooltip("Side length of each voxel")]
        public float voxelSize;

        [Tooltip("Points that are within this distance from the bounds outer side " +
                 "facing the same way as the point's normal will be ignored")]
        public float outerPointsThreshold;
    }
}
