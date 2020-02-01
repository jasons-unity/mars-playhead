using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS.Providers
{
    [ScriptableSettingsPath("XRSDKProviders/Runtime")]
    public class ARKitLandmarkMeshIndices : ScriptableSettings<ARKitLandmarkMeshIndices>
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
