using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS.Providers
{
    [ScriptableSettingsPath("Providers/XRSDKProviders/Runtime")]
    public class ARCoreLandmarkMeshIndices : ScriptableSettings<ARCoreLandmarkMeshIndices>
    {
        [SerializeField]
        int[] m_LandmarkTriangleIndexes;

        public int[] landmarkTriangleIndices
        {
            get { return m_LandmarkTriangleIndexes; }
            set { m_LandmarkTriangleIndexes = value; }
        }
    }
}
