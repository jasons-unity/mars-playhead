using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides a template for tracked marker data
    /// </summary>
    [Serializable]
    public struct MRMarker : IMRTrackable, IEquatable<MRMarker>
    {
        [SerializeField]
        MarsTrackableId m_TrackableId;

        [SerializeField]
        Pose m_Pose;

        [SerializeField]
        Guid m_MarkerId;

        [SerializeField]
        Vector2 m_Extents;

        /// <summary>
        /// The id of this tracked marker as determined by the provider
        /// </summary>
        public MarsTrackableId id
        {
            get { return m_TrackableId; }
            set { m_TrackableId = value; }
        }

        /// <summary>
        /// The pose of this marker
        /// </summary>
        public Pose pose
        {
            get { return m_Pose; }
            set { m_Pose = value; }
        }

        /// <summary>
        /// The guid of this marker
        /// </summary>
        public Guid markerId
        {
            get { return m_MarkerId; }
            set { m_MarkerId = value; }
        }

        /// <summary>
        /// The extents of this marker
        /// </summary>
        public Vector2 extents
        {
            get { return m_Extents; }
            set { m_Extents = value; }
        }

        public override string ToString()
        {
            const string str = "marker: {0}\npose: {1}";
            return String.Format(str, markerId, m_Pose);
        }

        public override int GetHashCode() { return id.GetHashCode(); }

        public bool Equals(MRMarker other) { return id.Equals(other.id); }
    }
}
