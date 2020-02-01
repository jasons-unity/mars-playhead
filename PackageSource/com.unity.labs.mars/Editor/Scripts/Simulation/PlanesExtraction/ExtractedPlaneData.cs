using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public struct ExtractedPlaneData
    {
        public List<Vector3> vertices;
        public Pose pose;
        public Vector3 center;
        public Vector2 extents;
    }
}
