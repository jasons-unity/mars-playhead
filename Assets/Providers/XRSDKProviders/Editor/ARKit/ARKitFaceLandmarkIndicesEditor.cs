using UnityEditor;

namespace Unity.Labs.MARS.Providers
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ARKitLandmarkMeshIndices))]
    public class ARKitFaceLandmarkIndicesEditor : Editor
    {
        ARKitLandmarkMeshIndices m_LandmarkTriangleIndices;

        void OnEnable()
        {
            m_LandmarkTriangleIndices = (ARKitLandmarkMeshIndices)target;
        }

        public override void OnInspectorGUI()
        {
            ARKitFaceEditorUtils.ARKitLandmarkIndicesFields(m_LandmarkTriangleIndices.landmarkTriangleIndices);
        }
    }
}
