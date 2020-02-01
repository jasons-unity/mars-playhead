using UnityEditor;

namespace Unity.Labs.MARS
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(FallbackFace))]
    public class FallbackFaceEditor : Editor
    {
        FallbackFace m_FallbackFace;

        void OnEnable()
        {
            m_FallbackFace = (FallbackFace)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            FaceEditorUtils.LandmarkTransformFields(target, m_FallbackFace.landmarkTransforms);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
