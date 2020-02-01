using UnityEditor;

namespace Unity.Labs.MARS.Providers
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(ARCoreLandmarkMeshIndices))]
    public class ARCoreFaceLandmarkIndicesEditor : Editor
    {
        ARCoreLandmarkMeshIndices m_LandmarkTriangleIndices;

        void OnEnable()
        {
            m_LandmarkTriangleIndices = (ARCoreLandmarkMeshIndices)target;
        }

        public override void OnInspectorGUI()
        {
            ARCoreFaceEditorUtils.ARCoreLandmarkIndicesFields(m_LandmarkTriangleIndices.landmarkTriangleIndices);
        }
    }
}
