using System.Collections.Generic;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public static class PlaneUtils
    {
        static readonly Vector3 k_Up = Vector3.up;

        /// <summary>
        /// Triangulates the polygon and tiles the UV data correctly from the polygon center.
        /// Sets normals to local up.
        /// </summary>
        /// <param name="pose">Input Pose of the source plane</param>
        /// <param name="vertices">Input Vertices of the polygon.</param>
        /// <param name="indices">Output Index buffer to fill for triangulation</param>
        /// <param name="normals">Output for vertex normals</param>
        /// <param name="texCoords">Output uv coordinates.</param>
        public static void TriangulatePlaneFromVerties(in Pose pose, List<Vector3> vertices, List<int> indices, List<Vector3> normals, List<Vector2> texCoords)
        {
            var uvPose = GeometryUtils.PolygonUVPoseFromPlanePose(pose);
            var vertsCount = vertices.Count;
            GeometryUtils.TriangulatePolygon(indices, vertsCount);

            for (var i = 0; i < vertsCount; i++)
            {
                var uvCoord = GeometryUtils.PolygonVertexToUV(vertices[i], pose, uvPose);
                texCoords.Add(uvCoord);
                normals.Add(k_Up);
            }
        }
    }
}
