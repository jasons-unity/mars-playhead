using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Interface for interacting with a class that controls a landmark
    /// </summary>
    public interface ILandmarkController
    {
        /// <summary>
        /// The definition of the landmark that is being controlled
        /// </summary>
        LandmarkDefinition landmarkDefinition { get; set; }

        /// <summary>
        /// The output to store resulting landmark data
        /// </summary>
        ILandmarkOutput output { get; set; }

        /// <summary>
        /// The settings specified for this landmark
        /// </summary>
        ILandmarkSettings settings { get; set; }
    }
}
