using System;
using System.Collections.Generic;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public class FallbackFace : MonoBehaviour
    {
        [SerializeField]
        List<Transform> m_LandmarkTransforms = new List<Transform>();

        internal List<Transform> landmarkTransforms { get { return m_LandmarkTransforms; } }

        void Reset()
        {
            m_LandmarkTransforms.Clear();
            foreach (var landmark in EnumValues<MRFaceLandmark>.Values)
            {
                var landmarkName = landmark.ToString();
                var landmarkTrans = transform.Find(landmarkName);
                m_LandmarkTransforms.Add(landmarkTrans);
            }
        }
    }
}
