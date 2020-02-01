using UnityEditor;

namespace Unity.Labs.MARS
{
    [CustomEditor(typeof(SessionRecordingSettings))]
    public class SessionRecordingSettingsEditor : Editor
    {
        SessionRecordingSettingsDrawer m_PreferencesDrawer;

        void OnEnable() { m_PreferencesDrawer = new SessionRecordingSettingsDrawer(serializedObject); }

        public override void OnInspectorGUI() { m_PreferencesDrawer.InspectorGUI(serializedObject); }
    }

    public class SessionRecordingSettingsDrawer
    {
        SerializedProperty m_CameraPoseIntervalProperty;

        public SessionRecordingSettingsDrawer(SerializedObject serializedObject)
        {
            m_CameraPoseIntervalProperty = serializedObject.FindProperty("m_CameraPoseInterval");
        }

        public void InspectorGUI(SerializedObject serializedObject)
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_CameraPoseIntervalProperty);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
