using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [CustomEditor(typeof(SimulationSettings))]
    public class SimulationSettingsInspector : Editor
    {
        SimulationSettingsDrawer m_SettingsDrawer;

        [MenuItem(MenuConstants.MenuPrefix + "Simulation Settings", priority = MenuConstants.SimSettingsPriority)]
        static void SelectSimulationSettings()
        {
            Selection.activeObject = SimulationSettings.instance;
        }

        void OnEnable()
        {
            m_SettingsDrawer = new SimulationSettingsDrawer(serializedObject);

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload += Repaint;
            AssemblyReloadEvents.beforeAssemblyReload += Repaint;
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            AssemblyReloadEvents.afterAssemblyReload -= Repaint;
            AssemblyReloadEvents.beforeAssemblyReload -= Repaint;
        }

        void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
        {
            Repaint();
        }

        public override void OnInspectorGUI()
        {
            m_SettingsDrawer.InspectorGUI(serializedObject);
        }
    }

    public class SimulationSettingsDrawer
    {
        SerializedProperty m_EnvironmentModeProperty;
        SerializedProperty m_EnvironmentPrefabProperty;
        SerializedProperty m_RecordedVideoProperty;
        SerializedProperty m_SessionRecordingProperty;
        SerializedProperty m_FindAllMatchingDataPerQueryProperty;
        SerializedProperty m_TimeToFinalizeQueryDataChangeProperty;
        SerializedProperty m_ShowSimulatedDataProperty;
        SerializedProperty m_ShowSimulatedEnvironmentProperty;
        SerializedProperty m_AutoResetDevicePoseProperty;
        SerializedProperty m_AutoSyncWithSceneChangesProperty;

        SimulationSettings m_SimulationSettings;

        static readonly GUIContent k_SaveViewContent = new GUIContent("Save Environment View",
            "This will save the default camera settings for this environment scene to those of your current Sim View camera");

        static readonly GUIContent k_SaveSimStartingPoseContent = new GUIContent("Save Simulation Starting Pose",
            "This will save the simulated device starting pose for this environment to the pose of your current Sim View camera");

        static readonly GUIContent k_RecordingContent = new GUIContent("Recording");

        public SimulationSettingsDrawer(SerializedObject serializedObject)
        {
            m_SimulationSettings = serializedObject.targetObject as SimulationSettings;
            m_EnvironmentModeProperty = serializedObject.FindProperty("m_EnvironmentMode");
            m_EnvironmentPrefabProperty = serializedObject.FindProperty("m_EnvironmentPrefab");
            m_RecordedVideoProperty = serializedObject.FindProperty("m_RecordedVideo");
            m_SessionRecordingProperty = serializedObject.FindProperty("m_SessionRecording");
            m_FindAllMatchingDataPerQueryProperty = serializedObject.FindProperty("m_FindAllMatchingDataPerQuery");
            m_TimeToFinalizeQueryDataChangeProperty = serializedObject.FindProperty("m_TimeToFinalizeQueryDataChange");
            m_ShowSimulatedEnvironmentProperty = serializedObject.FindProperty("m_ShowSimulatedEnvironment");
            m_ShowSimulatedDataProperty = serializedObject.FindProperty("m_ShowSimulatedData");
            m_AutoResetDevicePoseProperty = serializedObject.FindProperty("m_AutoResetDevicePose");
            m_AutoSyncWithSceneChangesProperty = serializedObject.FindProperty("m_AutoSyncWithSceneChanges");
        }

        public void InspectorGUI(SerializedObject serializedObject)
        {
            serializedObject.Update();

            var previousEnvironmentMode = (EnvironmentMode)m_EnvironmentModeProperty.enumValueIndex;
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(m_EnvironmentModeProperty, new GUIContent("Environment Mode"));
                if (EditorGUI.EndChangeCheck())
                {
                    if ((int)previousEnvironmentMode == m_EnvironmentModeProperty.enumValueIndex)
                        return;

                    serializedObject.ApplyModifiedProperties();
                    MARSEnvironmentManager.instance.RefreshEnvironmentAndRestartSimulation(SimulationSettings.isVideoEnvironment);
                }

                switch (m_EnvironmentModeProperty.enumValueIndex)
                {
                    case (int)EnvironmentMode.Synthetic:
                        DrawSyntheticEnvironmentGUI(serializedObject);
                        break;

                    case (int)EnvironmentMode.Live:
                        DrawLiveVideoGUI();
                        break;

                    case (int)EnvironmentMode.Recorded:
                        DrawCapturedVideoGUI(serializedObject);
                        break;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(m_FindAllMatchingDataPerQueryProperty);
            EditorGUILayout.PropertyField(m_TimeToFinalizeQueryDataChangeProperty);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ShowSimulatedEnvironmentProperty);
            EditorGUILayout.PropertyField(m_ShowSimulatedDataProperty);
            if (EditorGUI.EndChangeCheck())
            {
                MARSEnvironmentManager.SetSimDataVisibility(m_ShowSimulatedDataProperty.boolValue);
                MARSEnvironmentManager.SetSimEnvironmentVisibility(m_ShowSimulatedEnvironmentProperty.boolValue);
            }

            EditorGUILayout.PropertyField(m_AutoResetDevicePoseProperty);
            EditorGUILayout.PropertyField(m_AutoSyncWithSceneChangesProperty);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawSyntheticEnvironmentGUI(SerializedObject serializedObject)
        {
            var environmentManager = MARSEnvironmentManager.instance;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_EnvironmentPrefabProperty, new GUIContent("Prefab"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                environmentManager.RefreshEnvironmentAndRestartSimulation();
            }

            if (!environmentManager.SyntheticEnvironmentsExist)
                EditorGUILayout.HelpBox("Mark prefab in your project with the 'Environment' asset label " +
                    "for them to appear as simulation scenes. These prefabs should contain objects with " +
                    "SimulatedPlane components, or another simulated data type.", MessageType.Info);

            if (GUILayout.Button("Update Simulated Prefab"))
                environmentManager.UpdateSimulatedEnvironmentCandidates();

            var recordingManager = ModuleLoaderCore.instance.GetModule<SimulationRecordingManager>();
            if (recordingManager == null)
                return;

            recordingManager.ValidateRecordings();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var index = EditorGUILayout.Popup(k_RecordingContent, recordingManager.CurrentRecordingOptionIndex,
                    recordingManager.RecordingOptionContents);

                if (check.changed)
                    recordingManager.SetRecordingOptionAndRestartSimulation(index);
            }
        }

        static void DrawLiveVideoGUI()
        {
            DrawCommonVideoGUI();
        }

        void DrawCapturedVideoGUI(SerializedObject serializedObject)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_RecordedVideoProperty, new GUIContent("Video"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                MARSEnvironmentManager.instance.RefreshEnvironmentAndRestartSimulation();
            }

            DrawCommonVideoGUI();
        }

        static void DrawCommonVideoGUI()
        {
            if (!SceneWatchdogModule.instance.currentSceneIsFaceScene)
            {
                EditorGUILayout.HelpBox("To use the WebCam, open a scene with face subscribers.", MessageType.Info);
            }
            else
            {
                var querySimulationModule = QuerySimulationModule.instance;
                if (querySimulationModule.simulatingTemporal)
                {
                    if (GUILayout.Button("Stop"))
                        querySimulationModule.StopTemporalSimulation();
                }
                else
                {
                    if (GUILayout.Button("Start"))
                        querySimulationModule.StartTemporalSimulation();
                }
            }
        }
    }
}
