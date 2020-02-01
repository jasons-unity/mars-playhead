using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public interface IProvidesFallbackLandmarks : IFunctionalityProvider
    {
        /// <summary>
        /// Gets the set of fallback poses
        /// </summary>
        /// <returns>Dictionary of landmark name to local pose</returns>
        Dictionary<MRFaceLandmark, Pose> GetFallbackFaceLandmarkPoses();
    }
}
