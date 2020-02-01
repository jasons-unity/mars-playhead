using UnityEditor;

namespace Unity.Labs.MARS
{
    [CustomEditor(typeof(SessionRecordingInfo))]
    public class SessionRecordingInfoEditor : Editor
    {
        SerializedProperty m_TimelineProperty;
        SerializedProperty m_DataRecordingsProperty;
        SerializedProperty m_SyntheticEnvironmentsProperty;

        void OnEnable()
        {
            m_TimelineProperty = serializedObject.FindProperty("m_Timeline");
            m_DataRecordingsProperty = serializedObject.FindProperty("m_DataRecordings");
            m_SyntheticEnvironmentsProperty = serializedObject.FindProperty("m_SyntheticEnvironments");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_TimelineProperty);
                EditorGUILayout.PropertyField(m_DataRecordingsProperty, true);
            }

            EditorGUILayout.PropertyField(m_SyntheticEnvironmentsProperty, true);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
