using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [CustomEditor(typeof(MARSEnvironmentSettings))]
    public class MARSEnvironmentSettingsEditor : Editor
    {
        static readonly GUIContent k_SaveViewContent = new GUIContent("Save Environment View",
            "Save the default camera settings for this environment scene to match your current scene or sim view camera.");

        static readonly GUIContent k_SaveSimStartingPoseContent = new GUIContent("Save Simulation Starting Pose",
            "Save the simulated device starting pose for this environment to match your current scene or sim view camera.");

        static readonly GUIContent k_SavePlanesFromSimContent = new GUIContent("Save Planes From Simulation",
            "Overwrite generated planes with planes from the current simulation.");

        SerializedProperty m_EnvironmentInfoProperty;
        SerializedProperty m_DefaultCameraWorldPoseProperty;
        SerializedProperty m_DefaultCameraPivotProperty;
        SerializedProperty m_DefaultCameraSizeProperty;
        SerializedProperty m_SimulationStartingPoseProperty;
        SerializedProperty m_EnvironmentBoundsProperty;

        SerializedProperty m_RenderSettingsProperty;
#if UNITY_POST_PROCESSING_STACK_V2
        SerializedProperty m_PostProcessProfileProperty;
#endif

        void OnEnable()
        {
            m_EnvironmentInfoProperty = serializedObject.FindProperty("m_EnvironmentInfo");
            m_DefaultCameraWorldPoseProperty = m_EnvironmentInfoProperty.FindPropertyRelative("m_DefaultCameraWorldPose");
            m_DefaultCameraPivotProperty = m_EnvironmentInfoProperty.FindPropertyRelative("m_DefaultCameraPivot");
            m_DefaultCameraSizeProperty = m_EnvironmentInfoProperty.FindPropertyRelative("m_DefaultCameraSize");
            m_SimulationStartingPoseProperty = m_EnvironmentInfoProperty.FindPropertyRelative("m_SimulationStartingPose");
            m_EnvironmentBoundsProperty = m_EnvironmentInfoProperty.FindPropertyRelative("m_EnvironmentBounds");

            m_RenderSettingsProperty = serializedObject.FindProperty("m_RenderSettings");
#if UNITY_POST_PROCESSING_STACK_V2
            m_PostProcessProfileProperty = serializedObject.FindProperty("m_PostProcessProfile");
#endif
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var settings = target as MARSEnvironmentSettings;

            var sceneView = SceneView.lastActiveSceneView;
            var isSimView = false;
            if (sceneView != null)
                isSimView = sceneView is SimulationView;

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_EnvironmentInfoProperty.isExpanded = EditorGUILayout.Foldout(m_EnvironmentInfoProperty.isExpanded,
                    m_EnvironmentInfoProperty.displayName, true);

                if (m_EnvironmentInfoProperty.isExpanded)
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.PropertyField(m_DefaultCameraWorldPoseProperty, true);
                        EditorGUILayout.PropertyField(m_DefaultCameraPivotProperty);
                        EditorGUILayout.PropertyField(m_DefaultCameraSizeProperty);
                        using (new EditorGUI.DisabledScope(sceneView == null))
                        {
                            if (GUILayout.Button(k_SaveViewContent))
                            {
                                settings.SetDefaultEnvironmentCamera(sceneView, isSimView);
                                EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
                            }
                        }

                        EditorGUILayout.PropertyField(m_SimulationStartingPoseProperty, true);
                        EditorGUILayout.PropertyField(m_EnvironmentBoundsProperty);
                        using (new EditorGUI.DisabledScope(sceneView == null))
                        {
                            if (GUILayout.Button(k_SaveSimStartingPoseContent))
                            {
                                settings.SetSimulationStartingPose(sceneView.camera.transform.GetWorldPose(), isSimView);
                                EditorSceneManager.MarkSceneDirty(settings.gameObject.scene);
                            }
                        }
                    }
                }

                EditorGUILayout.PropertyField(m_RenderSettingsProperty, true);

#if UNITY_POST_PROCESSING_STACK_V2
                EditorGUILayout.PropertyField(m_PostProcessProfileProperty, true);
#endif

                var environmentObject = settings.gameObject;
                var currentEnvironmentPath = AssetDatabase.GetAssetPath(SimulationSettings.environmentPrefab);
                var environmentPrefabStage = PrefabStageUtility.GetPrefabStage(environmentObject);
                var environmentIsInSimulation = SimulationSceneModule.UsingSimulation &&
                                                SimulationSettings.environmentMode == EnvironmentMode.Synthetic &&
                                                environmentPrefabStage != null &&
                                                currentEnvironmentPath == environmentPrefabStage.prefabAssetPath;

                if (!environmentIsInSimulation)
                {
                    EditorGUILayout.HelpBox(
                        "Planes from simulation can only be saved to this environment if it is the active simulation environment.",
                        MessageType.Info);
                }

                using (new EditorGUI.DisabledScope(!environmentIsInSimulation))
                {
                    if (GUILayout.Button(k_SavePlanesFromSimContent))
                        ModuleLoaderCore.instance.GetModule<PlaneGenerationModule>().SavePlanesFromSimulation(environmentObject);
                }

                if (change.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
