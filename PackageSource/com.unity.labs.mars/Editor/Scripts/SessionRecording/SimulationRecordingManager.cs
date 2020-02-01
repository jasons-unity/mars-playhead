using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Module responsible for setting up MR session recordings as data sources for simulation
    /// </summary>
    public class SimulationRecordingManager : IModuleDependency<SessionRecordingModule>, IModuleDependency<QuerySimulationModule>,
        IModuleDependency<MARSEnvironmentManager>, IModuleAssetCallbacks
    {
        const string k_SessionRecordingFilter = "t:SessionRecordingInfo";
        const string k_StubProvidersName = "Stub Providers";

        SessionRecordingModule m_SessionRecordingModule;
        QuerySimulationModule m_QuerySimulationModule;
        MARSEnvironmentManager m_EnvironmentManager;

        readonly Dictionary<GameObject, List<SessionRecordingInfo>> m_RecordingsPerEnvironment =
            new Dictionary<GameObject, List<SessionRecordingInfo>>();

        readonly List<SessionRecordingInfo> m_CurrentRecordings = new List<SessionRecordingInfo>();

        internal bool DisableRecordingPlayback { get; set; }

        internal GUIContent[] RecordingOptionContents { get; private set; }

        public int CurrentRecordingOptionIndex { get; private set; }

        public int CurrentRecordingsCount { get { return m_CurrentRecordings.Count; } }

        public string CurrentRecordingName
        {
            get
            {
                if (CurrentRecordingOptionIndex < 1 || CurrentRecordingOptionIndex > CurrentRecordingsCount)
                    return string.Empty;

                var currentRecording = m_CurrentRecordings[CurrentRecordingOptionIndex - 1];
                return currentRecording != null ? currentRecording.name : string.Empty;
            }
        }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        // Reference type collections must also be cleared after use
        static readonly List<GameObject> k_EnvironmentPrefabs = new List<GameObject>();
        static readonly List<DataRecording> k_DataRecordings = new List<DataRecording>();
        static readonly List<Object> k_NewAssets = new List<Object>();

        public void ConnectDependency(SessionRecordingModule dependency) { m_SessionRecordingModule = dependency; }

        public void ConnectDependency(QuerySimulationModule dependency) { m_QuerySimulationModule = dependency; }

        public void ConnectDependency(MARSEnvironmentManager dependency) { m_EnvironmentManager = dependency; }

        public void LoadModule()
        {
            QuerySimulationModule.addCustomProviders += AddCustomProviders;
            MARSEnvironmentManager.onEnvironmentSetup += UpdateCurrentEnvironmentRecordings;
            GetAllRecordings();
            if (m_EnvironmentManager.EnvironmentSetup)
                UpdateCurrentEnvironmentRecordings();
        }

        public void UnloadModule()
        {
            QuerySimulationModule.addCustomProviders -= AddCustomProviders;
            MARSEnvironmentManager.onEnvironmentSetup -= UpdateCurrentEnvironmentRecordings;
            m_RecordingsPerEnvironment.Clear();
            m_CurrentRecordings.Clear();
            RecordingOptionContents = null;
        }

        public void OnWillCreateAsset(string path) { }

        public string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                var recording = AssetDatabase.LoadAssetAtPath<SessionRecordingInfo>(path);
                if (recording != null)
                {
                    RefreshSessionRecordings();
                    break;
                }
            }

            return paths;
        }

        public AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options) { return AssetDeleteResult.DidNotDelete; }

        /// <summary>
        /// Prompts to save a Timeline of the current session recording and add it to the current synthetic environment
        /// </summary>
        public void TrySaveSyntheticRecording()
        {
            var environmentPrefab = SimulationSettings.environmentPrefab;
            var defaultName = $"{environmentPrefab.name} Recording";
            var recordingPath = EditorUtility.SaveFilePanelInProject("Save Recording", defaultName, "playable", "");
            if (recordingPath.Length == 0)
                return;

            k_DataRecordings.Clear();
            k_NewAssets.Clear();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, recordingPath);
            var recordingInfo = ScriptableObject.CreateInstance<SessionRecordingInfo>();
            var recordingName = timeline.name;
            recordingInfo.name = recordingName;
            recordingInfo.Timeline = timeline;
            AssetDatabase.AddObjectToAsset(recordingInfo, timeline);
            m_SessionRecordingModule.CreateDataRecordings(timeline, k_DataRecordings, k_NewAssets);
            foreach (var dataRecording in k_DataRecordings)
            {
                recordingInfo.AddDataRecording(dataRecording);
                dataRecording.name = dataRecording.GetType().Name;
                AssetDatabase.AddObjectToAsset(dataRecording, timeline);
            }

            foreach (var newAsset in k_NewAssets)
            {
                AssetDatabase.AddObjectToAsset(newAsset, timeline);
            }

            recordingInfo.AddSyntheticEnvironment(environmentPrefab);
            AssetDatabase.SaveAssets();

            m_CurrentRecordings.Add(recordingInfo);
            if (m_RecordingsPerEnvironment.TryGetValue(environmentPrefab, out var recordings))
                recordings.Add(recordingInfo);
            else
                m_RecordingsPerEnvironment[environmentPrefab] = new List<SessionRecordingInfo> { recordingInfo };

            var newOptionsCount = RecordingOptionContents.Length + 1;
            var newOptionContents = new GUIContent[newOptionsCount];
            RecordingOptionContents.CopyTo(newOptionContents, 0);
            newOptionContents[newOptionsCount - 1] = new GUIContent(recordingName);
            RecordingOptionContents = newOptionContents;

            k_DataRecordings.Clear();
            k_NewAssets.Clear();
        }

        /// <summary>
        /// Sets an optional recording to use with the current environment and triggers simulation restart
        /// </summary>
        /// <param name="optionIndex">Index of the recording to use, where 0 means no recording should be used</param>
        public void SetRecordingOptionAndRestartSimulation(int optionIndex)
        {
            m_QuerySimulationModule.StopTemporalSimulation();

            if (optionIndex < 0 || optionIndex > CurrentRecordingsCount)
                optionIndex = 0;

            // An optionIndex of 0 means no recording is used, so the index into current recordings is optionIndex - 1
            var useRecording = optionIndex > 0;
            CurrentRecordingOptionIndex = optionIndex;
            var simulationSettings = SimulationSettings.instance;
            if (useRecording)
            {
                simulationSettings.UseEnvironmentRecording = true;
                simulationSettings.SetRecordingForCurrentEnvironment(m_CurrentRecordings[optionIndex - 1]);
                m_QuerySimulationModule.ShouldSimulateTemporal = true;
            }
            else
            {
                simulationSettings.UseEnvironmentRecording = false;
            }

            m_QuerySimulationModule.RestartSimulationIfNeeded();
        }

        /// <summary>
        /// Sets up the previous recording for the current environment and triggers simulation restart
        /// </summary>
        public void SetupPrevRecordingAndRestartSimulation()
        {
            var simulationSettings = SimulationSettings.instance;
            var recordingsCount = m_CurrentRecordings.Count;
            if (recordingsCount <= 1 || !simulationSettings.UseEnvironmentRecording)
                return;

            m_QuerySimulationModule.StopTemporalSimulation();

            if (CurrentRecordingOptionIndex == 1)
                CurrentRecordingOptionIndex = recordingsCount;
            else
                CurrentRecordingOptionIndex--;

            simulationSettings.SetRecordingForCurrentEnvironment(m_CurrentRecordings[CurrentRecordingOptionIndex - 1]);
            m_QuerySimulationModule.ShouldSimulateTemporal = true;
            m_QuerySimulationModule.RestartSimulationIfNeeded();
        }

        /// <summary>
        /// Sets up the next recording for the current environment and triggers simulation restart
        /// </summary>
        public void SetupNextRecordingAndRestartSimulation()
        {
            var simulationSettings = SimulationSettings.instance;
            var recordingsCount = m_CurrentRecordings.Count;
            if (recordingsCount <= 1 || !simulationSettings.UseEnvironmentRecording)
                return;

            m_QuerySimulationModule.StopTemporalSimulation();

            if (CurrentRecordingOptionIndex >= recordingsCount)
                CurrentRecordingOptionIndex = 1;
            else
                CurrentRecordingOptionIndex++;

            simulationSettings.SetRecordingForCurrentEnvironment(m_CurrentRecordings[CurrentRecordingOptionIndex - 1]);
            m_QuerySimulationModule.ShouldSimulateTemporal = true;
            m_QuerySimulationModule.RestartSimulationIfNeeded();
        }

        public void ValidateRecordings()
        {
            if (RecordingOptionContents == null || AnyRecordingsDestroyed())
            {
                var simulationSettings = SimulationSettings.instance;
                var wasUsingRecording = simulationSettings.UseEnvironmentRecording;
                GetAllRecordings();
                UpdateCurrentEnvironmentRecordings();
                if (wasUsingRecording && !simulationSettings.UseEnvironmentRecording)
                {
                    // The current recording was invalidated while it was being used, so stop simulating and trigger a one-shot
                    m_QuerySimulationModule.StopTemporalSimulation();
                    m_QuerySimulationModule.RestartSimulationIfNeeded();
                }
            }
        }

        bool AnyRecordingsDestroyed()
        {
            foreach (var kvp in m_RecordingsPerEnvironment)
            {
                foreach (var recordingInfo in kvp.Value)
                {
                    if (recordingInfo == null)
                        return true;
                }
            }

            return false;
        }

        [MenuItem(MenuConstants.DevMenuPrefix + "Refresh Session Recordings", priority = MenuConstants.RefreshSessionRecordingsPriority)]
        public static void RefreshSessionRecordings()
        {
            var recordingManager = ModuleLoaderCore.instance.GetModule<SimulationRecordingManager>();
            recordingManager.GetAllRecordings();
            recordingManager.UpdateCurrentEnvironmentRecordings();
        }

        void GetAllRecordings()
        {
            m_RecordingsPerEnvironment.Clear();
            var recordingGUIDs = AssetDatabase.FindAssets(k_SessionRecordingFilter);
            foreach (var guid in recordingGUIDs)
            {
                k_EnvironmentPrefabs.Clear();
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var recording = AssetDatabase.LoadAssetAtPath<SessionRecordingInfo>(path);
                recording.GetSyntheticEnvironments(k_EnvironmentPrefabs);
                foreach (var environmentPrefab in k_EnvironmentPrefabs)
                {
                    if (m_RecordingsPerEnvironment.TryGetValue(environmentPrefab, out var recordings))
                        recordings.Add(recording);
                    else
                        m_RecordingsPerEnvironment[environmentPrefab] = new List<SessionRecordingInfo> { recording };
                }
            }

            k_EnvironmentPrefabs.Clear();
        }

        void UpdateCurrentEnvironmentRecordings()
        {
            m_CurrentRecordings.Clear();
            if (SimulationSettings.environmentMode != EnvironmentMode.Synthetic)
                return;

            if (SimulationSettings.environmentPrefab != null && m_RecordingsPerEnvironment.TryGetValue(SimulationSettings.environmentPrefab, out var recordings))
                m_CurrentRecordings.AddRange(recordings);

            var recordingsCount = m_CurrentRecordings.Count;
            RecordingOptionContents = new GUIContent[recordingsCount + 1];
            RecordingOptionContents[0] = new GUIContent("No Recording (Manual)");
            for (var i = 0; i < recordingsCount; i++)
            {
                RecordingOptionContents[i + 1] = new GUIContent(m_CurrentRecordings[i].name);
            }

            var simulationSettings = SimulationSettings.instance;
            var currentRecording = simulationSettings.GetRecordingForCurrentEnvironment();
            if (currentRecording == null)
            {
                if (recordingsCount > 0)
                {
                    // The current environment has recordings but it does not have a valid recording assigned
                    // in simulation settings, so assign one now
                    currentRecording = m_CurrentRecordings[0];
                    simulationSettings.SetRecordingForCurrentEnvironment(currentRecording);
                }
                else if (simulationSettings.UseEnvironmentRecording)
                {
                    // The current environment does not have recordings but the previous environment was using
                    // a recording, so make sure the next simulation is one-shot
                    m_QuerySimulationModule.ShouldSimulateTemporal = false;
                    simulationSettings.UseEnvironmentRecording = false;
                }
            }

            CurrentRecordingOptionIndex = simulationSettings.UseEnvironmentRecording ?
                1 + m_CurrentRecordings.IndexOf(currentRecording) : 0;
        }

        void AddCustomProviders(List<IFunctionalityProvider> providers)
        {
            var simulationSettings = SimulationSettings.instance;
            if (DisableRecordingPlayback || SimulationSettings.environmentMode != EnvironmentMode.Synthetic ||
                !simulationSettings.UseEnvironmentRecording || !m_QuerySimulationModule.simulatingTemporal)
            {
                return;
            }

            k_DataRecordings.Clear();
            var sessionDirectorGO = GameObjectUtils.Create("Session Director");
            var director = sessionDirectorGO.AddComponent<SimulatableDirector>().Director;
            var recordingInfo = simulationSettings.GetRecordingForCurrentEnvironment();
            director.playableAsset = recordingInfo.Timeline;
            recordingInfo.GetDataRecordings(k_DataRecordings);
            var hasPlanesRecording = false;
            var hasPointCloudRecording = false;
            foreach (var recording in k_DataRecordings)
            {
                recording.SetupDataProviders(director, providers);

                if (recording is PlaneFindingRecording)
                    hasPlanesRecording = true;
                else if (recording is PointCloudRecording)
                    hasPointCloudRecording = true;
            }

            // If there are planes or point cloud recordings, we need to set up stub providers to make sure
            // the simulated discovery provider game object doesn't get created.
            if (hasPlanesRecording || hasPointCloudRecording)
            {
                var stubProvidersObj = GameObjectUtils.Create(k_StubProvidersName);
                var sessionProvider = stubProvidersObj.AddComponent<StubSessionProvider>();
                providers.Add(sessionProvider);
                if (!hasPlanesRecording)
                {
                    var planesProvider = stubProvidersObj.AddComponent<StubPlanesProvider>();
                    providers.Add(planesProvider);
                }
                else if (!hasPointCloudRecording)
                {
                    var pointCloudProvider = stubProvidersObj.AddComponent<StubPointCloudProvider>();
                    providers.Add(pointCloudProvider);
                }
            }

            k_DataRecordings.Clear();
        }
    }
}
