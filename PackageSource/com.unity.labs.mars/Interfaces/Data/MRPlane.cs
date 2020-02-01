using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides a template for tracked plane data
    /// </summary>
    [Serializable]
    public struct MRPlane : IMRTrackable, IEquatable<MRPlane>
    {
        [SerializeField]
        MarsTrackableId m_ID;

        [SerializeField]
        MarsPlaneAlignment m_Alignment;

        [SerializeField]
        Pose m_Pose;

        [SerializeField]
        Vector3 m_Center;

        [SerializeField]
        Vector2 m_Extents;

        /// <summary>
        /// The id of this plane as determined by the provider
        /// </summary>
        public MarsTrackableId id
        {
            get { return m_ID; }
            set { m_ID = value; }
        }

        /// <summary>
        /// The alignment of this plane (e.g. Horizontal, Vertical)
        /// </summary>
        public MarsPlaneAlignment alignment
        {
            get { return m_Alignment; }
            set { m_Alignment = value; }
        }

        /// <summary>
        /// The pose of this plane
        /// </summary>
        public Pose pose
        {
            get { return m_Pose; }
            set { m_Pose = value; }
        }

        /// <summary>
        /// The center of this plane, in local space
        /// </summary>
        public Vector3 center
        {
            get { return m_Center; }
            set { m_Center = value; }
        }

        /// <summary>
        /// The extents of this plane
        /// </summary>
        public Vector2 extents
        {
            get { return m_Extents; }
            set { m_Extents = value; }
        }

        /// <summary>
        /// (Optional) vertices for polygon extents
        /// </summary>
        public List<Vector3> vertices;

        /// <summary>
        /// (Optional) texture coordinates (UVs) for polygon extents
        /// Will be present if vertices are present
        /// </summary>
        public List<Vector2> textureCoordinates;

        /// <summary>
        /// (Optional) normal vectors for polygon extents
        /// Will be present if vertices are present
        /// </summary>
        public List<Vector3> normals;

        /// <summary>
        /// (Optional) indices (triangles) for polygon extents
        /// Will be present if vertices are present
        /// </summary>
        public List<int> indices;

        public override string ToString()
        {
            const string str = "extents: {0}\npose: {1}";
            return String.Format(str, m_Extents, m_Pose);
        }

        public override int GetHashCode() { return m_ID.GetHashCode(); }

        public bool Equals(MRPlane other) { return m_ID.Equals(other.m_ID); }
    }
}
