using UnityEditor;

namespace Unity.Labs.MARS
{
    [CustomEditor(typeof(MARSSceneModule))]
    public class MARSSceneModuleEditor : Editor
    {
        MARSSceneModuleDrawer m_SceneModuleDrawer;

        void OnEnable()
        {
            m_SceneModuleDrawer = new MARSSceneModuleDrawer(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            m_SceneModuleDrawer.OnInspectorGUI(serializedObject);
        }
    }

    public class MARSSceneModuleDrawer
    {
        SerializedProperty m_SimulateInPlayModeProperty;
        SerializedProperty m_SimulationIslandProperty;
        SerializedProperty m_SimulateDiscoveryProperty;
        SerializedProperty m_SimulatedDiscoveryIslandProperty;

        public MARSSceneModuleDrawer(SerializedObject serializedObject)
        {
            m_SimulateInPlayModeProperty = serializedObject.FindProperty("m_SimulateInPlayMode");
            m_SimulationIslandProperty = serializedObject.FindProperty("m_SimulationIsland");
            m_SimulateDiscoveryProperty = serializedObject.FindProperty("m_SimulateDiscovery");
            m_SimulatedDiscoveryIslandProperty = serializedObject.FindProperty("m_SimulatedDiscoveryIsland");
        }

        public void OnInspectorGUI(SerializedObject serializedObject)
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SimulateInPlayModeProperty);
            using (new EditorGUI.DisabledScope(!m_SimulateInPlayModeProperty.boolValue))
            {
                EditorGUILayout.PropertyField(m_SimulationIslandProperty);
                EditorGUILayout.PropertyField(m_SimulateDiscoveryProperty);
                using (new EditorGUI.DisabledScope(!m_SimulateDiscoveryProperty.boolValue))
                {
                    EditorGUILayout.PropertyField(m_SimulatedDiscoveryIslandProperty);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
