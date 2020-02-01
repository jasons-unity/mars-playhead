using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public class LayerPlaneData
    {
        /// <summary>
        /// XZ vertices of the plane, relative to the layer origin
        /// </summary>
        public readonly List<Vector3> Vertices;

        /// <summary>
        /// Y offset of the plane, relative to the layer origin
        /// </summary>
        public float YOffsetFromLayer;

        /// <summary>
        /// Coordinates of voxels that contribute to this plane
        /// </summary>
        public readonly HashSet<Vector2Int> Voxels;

        /// <summary>
        /// Is this plane included in the layer above it due to a cross-layer merge?
        /// </summary>
        public bool CrossLayer;

        public LayerPlaneData(PlaneVoxel startingVoxel)
        {
            Voxels = new HashSet<Vector2Int> { startingVoxel.LayerCoordinates };
            YOffsetFromLayer = startingVoxel.PointYOffset;
            Vertices = new List<Vector3>();
        }

        public void AddVoxel(PlaneVoxel voxel)
        {
            var numVoxelsInPlane = Voxels.Count;
            Voxels.Add(voxel.LayerCoordinates);
            YOffsetFromLayer = (YOffsetFromLayer * numVoxelsInPlane + voxel.PointYOffset) / Voxels.Count;
        }
    }
}
