#if INCLUDE_MARS
using System.Collections.Generic;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Unity.Labs.MARS.Providers
{
    public static class ARFaceExtensions
    {
        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<Vector3> k_Vector3Buffer = new List<Vector3>();
        static readonly List<Vector2> k_Vector2Buffer = new List<Vector2>();
        static readonly List<int> k_IntBuffer = new List<int>();

        internal static void ToXRSDKFace(this ARFace face, XRFaceSubsystem xrFaceSubsystem, ref XRSDKFace xrsdkFace)
        {
            if (xrsdkFace == null)
                xrsdkFace = new XRSDKFace(face.trackableId.ToMarsId());

            xrsdkFace.pose = face.transform.GetWorldPose();

            var indices = face.indices;
            if (indices.Length > 0)
            {
                k_Vector3Buffer.Clear();
                foreach (var vertex in face.vertices)
                {
                    k_Vector3Buffer.Add(vertex);
                }

                xrsdkFace.Mesh.SetVertices(k_Vector3Buffer);

                k_Vector3Buffer.Clear();
                foreach (var normal in face.normals)
                {
                    k_Vector3Buffer.Add(normal);
                }

                xrsdkFace.Mesh.SetNormals(k_Vector3Buffer);

                k_Vector2Buffer.Clear();
                foreach (var uv in face.uvs)
                {
                    k_Vector2Buffer.Add(uv);
                }

                xrsdkFace.Mesh.SetUVs(0, k_Vector2Buffer);
                k_IntBuffer.Clear();
                foreach (var index in indices)
                {
                    k_IntBuffer.Add(index);
                }

                xrsdkFace.Mesh.SetTriangles(k_IntBuffer, 0);

#if !UNITY_EDITOR
#if UNITY_IOS
                // For iOS, we use ARKit Face Blendshapes to determine expressions
                xrsdkFace.GenerateLandmarks();
                xrsdkFace.CalculateExpressions(xrFaceSubsystem, face.trackableId);
#elif UNITY_ANDROID
                // For Android, we use the position of the face landmarks to determine expressions
                xrsdkFace.GenerateLandmarks();
                xrsdkFace.CalculateExpressions(ARCoreFaceLandmarksExtensions.LandmarkPositions);
#endif
#endif
            }
        }
    }
}
#endif
