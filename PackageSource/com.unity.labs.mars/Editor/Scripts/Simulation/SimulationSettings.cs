using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Settings for simulation of queries
    /// </summary>
    [ScriptableSettingsPath(MARSCore.UserSettingsFolder)]
    public class SimulationSettings : EditorScriptableSettings<SimulationSettings>, ISerializationCallbackReceiver
    {
        public const string AutoSyncTooltip = "When enabled, Simulation will automatically restart when changes are made to the active scene";

#pragma warning disable 649
        [SerializeField]
        EnvironmentMode m_EnvironmentMode;

        [SerializeField]
        GameObject m_EnvironmentPrefab;

        [SerializeField]
        VideoClip m_RecordedVideo;

        [SerializeField]
        bool m_UseEnvironmentRecording;

        [SerializeField]
        [Tooltip("When enabled, simulation will check for all data that would match a query, even if that data doesn't get used")]
        bool m_FindAllMatchingDataPerQuery;

        [SerializeField]
        [Tooltip("When enabled, the simulated device will reset back to its default starting pose after each simulation")]
        bool m_AutoResetDevicePose;
#pragma warning restore 649

        [SerializeField]
        [Tooltip(AutoSyncTooltip)]
        bool m_AutoSyncWithSceneChanges = true;

        [SerializeField]
        [Tooltip("Sets the amount of time to wait after changing query-related data before re-simulating. " +
            "If another change happens during this time then the timer is reset.")]
        float m_TimeToFinalizeQueryDataChange = 0.3f;

        [SerializeField]
        [Tooltip("When enabled, the simulated environment will be visualized")]
        bool m_ShowSimulatedEnvironment = true;

        [SerializeField]
        [Tooltip("When enabled, AR data for the simulated environment will be visualized")]
        bool m_ShowSimulatedData = true;

        [SerializeField]
        List<GameObject> m_Environments = new List<GameObject>();

        [SerializeField]
        List<SessionRecordingInfo> m_EnvironmentRecordings = new List<SessionRecordingInfo>();

        Dictionary<GameObject, SessionRecordingInfo> m_EnvironmentRecordingsMap = new Dictionary<GameObject, SessionRecordingInfo>();

        public static EnvironmentMode environmentMode
        {
            get { return instance.m_EnvironmentMode; }
            set { instance.m_EnvironmentMode = value; }
        }

        public static GameObject environmentPrefab
        {
            get { return instance.m_EnvironmentPrefab; }
            set { instance.m_EnvironmentPrefab = value; }
        }

        public static VideoClip recordedVideo
        {
            get { return instance.m_RecordedVideo; }
            set { instance.m_RecordedVideo = value; }
        }

        /// <summary>
        /// Checks whether the current environment is video-based.
        /// Future environment modes that support video controls (play/pause, etc) should be added here.
        /// </summary>
        public static bool isVideoEnvironment
        {
            get
            {
                return instance.m_EnvironmentMode == EnvironmentMode.Recorded ||
                    instance.m_EnvironmentMode == EnvironmentMode.Live;
            }
        }

        public static bool findAllMatchingDataPerQuery { get { return instance.m_FindAllMatchingDataPerQuery; } }

        public static float timeToFinalizeQueryDataChange { get { return instance.m_TimeToFinalizeQueryDataChange; } }

        public static bool showSimulatedData
        {
            get { return instance.m_ShowSimulatedData; }
            set
            {
                if (value == instance.m_ShowSimulatedData)
                    return;

                instance.m_ShowSimulatedData = value;
                MARSEnvironmentManager.SetSimDataVisibility(value);
            }
        }

        public static bool showSimulatedEnvironment
        {
            get { return instance.m_ShowSimulatedEnvironment; }
            set
            {
                if (value == instance.m_ShowSimulatedEnvironment)
                    return;

                instance.m_ShowSimulatedEnvironment = value;
                MARSEnvironmentManager.SetSimEnvironmentVisibility(value);
            }
        }

        public static bool autoResetDevicePose { get { return instance.m_AutoResetDevicePose; } }

        public bool AutoSyncWithSceneChanges
        {
            get
            {
                return m_AutoSyncWithSceneChanges;
            }
            set
            {
                m_AutoSyncWithSceneChanges = value;
                EditorUtility.SetDirty(this);
                if (value)
                {
                    var simObjectsManager = ModuleLoaderCore.instance.GetModule<SimulatedObjectsManager>();
                    var querySimulationModule = QuerySimulationModule.instance;
                    if (simObjectsManager != null && querySimulationModule != null && !simObjectsManager.SimulationSyncedWithScene)
                        querySimulationModule.RestartSimulationIfNeeded();
                }
            }
        }

        public bool UseEnvironmentRecording
        {
            get
            {
                return m_UseEnvironmentRecording;
            }
            set
            {
                m_UseEnvironmentRecording = value;
                EditorUtility.SetDirty(this);
            }
        }

        protected override void OnLoaded()
        {
            if (m_RecordedVideo == null)
                m_RecordedVideo = MARSUIResources.instance.DefaultRecordedVideo;
        }

        public void OnBeforeSerialize()
        {
            m_Environments.Clear();
            m_EnvironmentRecordings.Clear();
            foreach (var kvp in m_EnvironmentRecordingsMap)
            {
                m_Environments.Add(kvp.Key);
                m_EnvironmentRecordings.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            m_EnvironmentRecordingsMap.Clear();
            for (var i = 0; i < m_Environments.Count; i++)
            {
                m_EnvironmentRecordingsMap[m_Environments[i]] = m_EnvironmentRecordings[i];
            }
        }

        public void SetRecordingForCurrentEnvironment(SessionRecordingInfo recording)
        {
            m_EnvironmentRecordingsMap[environmentPrefab] = recording;
            EditorUtility.SetDirty(this);
        }

        public SessionRecordingInfo GetRecordingForCurrentEnvironment()
        {
            if (environmentPrefab == null)
                return null;

            return m_EnvironmentRecordingsMap.ContainsKey(environmentPrefab) ?
                m_EnvironmentRecordingsMap[environmentPrefab] : null;
        }
    }
}
