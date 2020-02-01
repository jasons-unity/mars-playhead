using UnityEngine;

namespace Unity.Labs.MARS
{
    public class MRLayerPlaneData : LayerPlaneData
    {
        public MRPlane MRPlane;
        public Color DebugColor;

        public MRLayerPlaneData(PlaneVoxel startingVoxel)
            : base(startingVoxel)
        {
            DebugColor = Random.ColorHSV(0f, 1f, 1f, 1f, 1f, 1f);
        }
    }
}
