using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS.Behaviors
{
    [RequireComponent(typeof(MeshFilter))]
    public class MARSPointCloudVisualizer : MonoBehaviour, IUsesPointCloud, IUsesCameraOffset, ISimulatable
    {
        // This will often grow up to 500+ at runtime on core / kit
        const int k_StartingPointCapacity = 250;
        static readonly Vector3 k_InfinityVector = Vector3.one * Mathf.Infinity;
        static readonly Vector3 k_NegativeInfinityVector = Vector3.one * Mathf.NegativeInfinity;

        [SerializeField]
        Color m_LowConfidenceColor = Color.red;

        [SerializeField]
        Color m_HighConfidenceColor = Color.green;

        Mesh m_Mesh;
        Vector3[] m_Vertices = new Vector3[k_StartingPointCapacity];
        int[] m_Indices = new int[k_StartingPointCapacity];
        Color[] m_Colors = new Color[k_StartingPointCapacity];

#if !FI_AUTOFILL
        IProvidesPointCloud IFunctionalitySubscriber<IProvidesPointCloud>.provider { get; set; }
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }
#endif

        public void Start()
        {
            m_Mesh = new Mesh();
            GetComponent<MeshFilter>().sharedMesh = m_Mesh;
        }

        void OnEnable()
        {
            this.SubscribePointCloudUpdated(OnPointCloudUpdated);
        }

        void OnDisable()
        {
            this.UnsubscribePointCloudUpdated(OnPointCloudUpdated);
        }

        void OnPointCloudUpdated(Dictionary<MarsTrackableId, PointCloudData> data)
        {
            if (MARSCore.instance.paused)
                return;

            var vertexCount = m_Vertices.Length;
            var pointCount = 0;
            foreach (var kvp in data)
            {
                var pointCloud = kvp.Value;
                var positions = pointCloud.Positions;
                if (!positions.HasValue)
                    continue;

                pointCount += positions.Value.Length;
            }

            if (vertexCount < pointCount || m_Vertices == null)
            {
                m_Vertices = new Vector3[pointCount];
                m_Indices = new int[pointCount];
                m_Colors = new Color[pointCount];
            }

            var scale = this.GetCameraScale();
            var positionOffset = this.GetCameraPositionOffset();
            var rotationOffset = Quaternion.AngleAxis(this.GetCameraYawOffset(), Vector3.up);
            var boundsMin = k_InfinityVector;
            var boundsMax = k_NegativeInfinityVector;
            pointCount = 0;
            foreach (var kvp in data)
            {
                var pointCloud = kvp.Value;
                var positions = pointCloud.Positions;
                if (!positions.HasValue)
                    continue;

                var positionsValue = positions.Value;
                var confidenceValues = pointCloud.ConfidenceValues;
                var length = positionsValue.Length;
                if (confidenceValues.HasValue)
                {
                    var confidenceValuesValue = confidenceValues.Value;
                    for (var i = 0; i < length; i++)
                    {
                        var scaledPosition = positionsValue[i] * scale;
                        var index = i + pointCount;
                        m_Vertices[index] = positionOffset + rotationOffset * scaledPosition;
                        m_Colors[index] = Color.Lerp(m_LowConfidenceColor, m_HighConfidenceColor, confidenceValuesValue[i]);
                        m_Indices[index] = index;
                        GrowBounds(scaledPosition, ref boundsMax, ref boundsMin);
                    }
                }
                else
                {
                    for (var i = 0; i < length; i++)
                    {
                        var scaledPosition = positionsValue[i] * scale;
                        var index = i + pointCount;
                        m_Vertices[index] = positionOffset + rotationOffset * scaledPosition;
                        m_Colors[index] = m_HighConfidenceColor;
                        m_Indices[index] = index;
                        GrowBounds(scaledPosition, ref boundsMax, ref boundsMin);
                    }
                }

                pointCount += length;
            }

            for (var i = pointCount; i < vertexCount; i++)
            {
                m_Indices[i] = 0;
            }

            m_Mesh.vertices = m_Vertices;
            m_Mesh.colors = m_Colors;
            m_Mesh.SetIndices(m_Indices, MeshTopology.Points, 0);

            if (pointCount == 0)
            {
                m_Mesh.bounds = new Bounds();
            }
            else
            {
                var size = boundsMax - boundsMin;
                m_Mesh.bounds = new Bounds(boundsMin + size * 0.5f, size);
            }
        }

        static void GrowBounds(Vector3 position, ref Vector3 boundsMax, ref Vector3 boundsMin)
        {
            if (position.x > boundsMax.x)
                boundsMax.x = position.x;

            if (position.y > boundsMax.y)
                boundsMax.y = position.y;

            if (position.z > boundsMax.z)
                boundsMax.z = position.z;

            if (position.x < boundsMin.x)
                boundsMin.x = position.x;

            if (position.y < boundsMin.y)
                boundsMin.y = position.y;

            if (position.z < boundsMin.z)
                boundsMin.z = position.z;
        }
    }
}
