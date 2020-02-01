using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [Serializable]
    public class VoxelPlaneFindingParams
    {
        [Tooltip("Voxel point density threshold that is independent of voxel size")]
        public int minPointsPerSqMeter;

        [Tooltip("A plane with x or y extent less than this value will be ignored")]
        public float minSideLength;

        [Tooltip("Planes within the same layer that are at most this distance from each other will be merged")]
        public float inLayerMergeDistance;

        [Tooltip("Planes in adjacent layers with an elevation difference of at most this much will be merged")]
        public float crossLayerMergeDistance;

        [Tooltip("When enabled, planes will only be created if they do not contain too much empty area")]
        public bool checkEmptyArea;

        [Tooltip("Curve that maps the area of a plane to the ratio of area that is allowed to be empty")]
        public AnimationCurve allowedEmptyAreaCurve;
    }
}
