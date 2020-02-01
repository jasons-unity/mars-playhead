using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides a template for tracked object data
    /// </summary>
    public interface IMRTrackable
    {
        /// <summary>
        /// The id of this tracked object
        /// </summary>
        MarsTrackableId id { get; }

        /// <summary>
        /// The pose of this tracked object
        /// </summary>
        Pose pose { get; }
    }
}
