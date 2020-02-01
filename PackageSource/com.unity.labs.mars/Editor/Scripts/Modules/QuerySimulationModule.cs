using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.MARS.CodeGen;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using SlowTask = Unity.Labs.MARS.SlowTaskModule.SlowTask;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Module responsible for simulating queries in edit mode
    /// </summary>
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    public class QuerySimulationModule : EditorScriptableSettings<QuerySimulationModule>, IModuleDependency<MARSDatabase>,
        IModuleDependency<MARSQueryBackend>, IModuleDependency<ReasoningModule>, IModuleDependency<SlowTaskModule>,
        IModuleDependency<SimulatedObjectsManager>, IModuleDependency<MARSEnvironmentManager>,
        IModuleDependency<MARSEntityEditorModule>, IModuleDependency<FunctionalityInjectionModule>,
        IModuleDependency<SimulationSceneModule>, IModuleDependency<SceneWatchdogModule>, IModuleDependency<QueryPipelinesModule>,
        IModuleDependency<EvaluationSchedulerModule>,
        IUsesDatabaseQuerying, IUsesCameraOffset
    {
        static readonly Action<QueryMatchID, int> k_OnQueryMatchFound = OnQueryMatchFound;
        static readonly Action<QueryMatchID, Dictionary<int, float>> k_OnQueryMatchesFound = OnQueryMatchesFound;
        static readonly Action<QueryMatchID, Dictionary<IMRObject, int>> k_OnSetQueryMatchFound = OnSetQueryMatchFound;

        [SerializeField]
        float m_SimulationPollTime = 0.25f;

#pragma warning disable 649
        [SerializeField]
        FunctionalityIsland m_SimulationIsland;

        [SerializeField]
        FunctionalityIsland m_SimulatedDiscoveryIsland;
#pragma warning restore 649

        MARSDatabase m_Database;
        MARSQueryBackend m_QueryBackend;
        QueryPipelinesModule m_QueryPipelinesModule;
        ReasoningModule m_ReasoningModule;
        SlowTaskModule m_SlowTaskModule;
        SimulatedObjectsManager m_SimulatedObjectsManager;
        MARSEnvironmentManager m_EnvironmentManager;
        MARSEntityEditorModule m_EntityEditorModule;
        SimulationSceneModule m_SimulationSceneModule;
        SceneWatchdogModule m_SceneWatchdogModule;
        FunctionalityInjectionModule m_FIModule;
        EvaluationSchedulerModule m_SchedulerModule;

        bool m_SimulationRestartNeeded;
        SlowTask m_SimulationPollTask;
        QueryResult m_TempQueryResult;
        SetQueryResult m_TempSetQueryResult;
        SetMatchData m_TempSetMatchData;
        FunctionalityIsland m_PreviousIsland;
        readonly SimulationContext m_SimulationContext = new SimulationContext();

        readonly List<IFunctionalityProvider> m_Providers = new List<IFunctionalityProvider>();
        readonly List<MonoBehaviour> m_ProviderBehaviours = new List<MonoBehaviour>();
        readonly List<GameObject> m_ProviderGameObjects = new List<GameObject>();

#if !FI_AUTOFILL
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }
#endif

        public bool simulating { get; private set; }

        public bool simulatingTemporal { get; private set; }

        internal FunctionalityIsland functionalityIsland { get; private set; }
        public GameObject providersRoot { get; private set; }

        public bool simulatedDataAvailable { get; private set; }

        internal bool ShouldSimulateTemporal { get; set; }

        internal static bool TestMode { private get; set; }

        public static event Action onTemporalSimulationStart;
        public static event Action onTemporalSimulationStop;

        public static bool sceneIsSimulatable
        {
            get
            {
                return SceneWatchdogModule.instance.anyEntitiesInScene ||
                    SceneWatchdogModule.instance.anySubscribersInScene;
            }
        }

        public static event Action simulationDone;

        /// <summary>
        /// Called right before default providers are setup when starting simulation.
        /// The list should be filled out with providers that you want this module to add before it sets up default providers.
        /// Provider game objects created in this callback should be created using GameObjectUtils.Create so that they get
        /// added to the simulation scene.
        /// </summary>
        public static event Action<List<IFunctionalityProvider>> addCustomProviders;

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<IFunctionalitySubscriber> k_FunctionalitySubscriberObjects = new List<IFunctionalitySubscriber>();

        public void ConnectDependency(MARSDatabase dependency) { m_Database = dependency; }

        public void ConnectDependency(MARSQueryBackend dependency) { m_QueryBackend = dependency; }

        public void ConnectDependency(QueryPipelinesModule dependency) { m_QueryPipelinesModule = dependency; }

        public void ConnectDependency(ReasoningModule dependency) { m_ReasoningModule = dependency; }

        public void ConnectDependency(SlowTaskModule dependency) { m_SlowTaskModule = dependency; }

        public void ConnectDependency(SimulatedObjectsManager dependency) { m_SimulatedObjectsManager = dependency; }

        public void ConnectDependency(MARSEnvironmentManager dependency) { m_EnvironmentManager = dependency; }

        public void ConnectDependency(MARSEntityEditorModule dependency) { m_EntityEditorModule = dependency; }

        public void ConnectDependency(SimulationSceneModule dependency) { m_SimulationSceneModule = dependency; }

        public void ConnectDependency(SceneWatchdogModule dependency) { m_SceneWatchdogModule = dependency; }

        public void ConnectDependency(EvaluationSchedulerModule dependency) { m_SchedulerModule = dependency; }

        public void ConnectDependency(FunctionalityInjectionModule dependency)
        {
            if (!m_SimulationIsland)
            {
                Debug.LogWarning("You need to set the simulation island", this);
                return;
            }

            if (!m_SimulatedDiscoveryIsland)
            {
                Debug.LogWarning("You need to set the simulated discovery island", this);
                return;
            }

            if (EditorApplication.isPlaying)
                return;

            m_FIModule = dependency;
            m_FIModule.AddIsland(m_SimulationIsland);
            m_FIModule.AddIsland(m_SimulatedDiscoveryIsland);
        }

        public void LoadModule()
        {
            functionalityIsland = m_SimulationIsland;
            EditorApplication.update += Update;
            SimulationSceneModule.SimulationSceneCreated += OnSimulationSceneCreated;
            SimulationSceneModule.SimulationSceneDestroyed += OnSimulationSceneDestroyed;

            m_TempQueryResult = new QueryResult(QueryMatchID.Generate());
            m_TempSetQueryResult = new SetQueryResult(QueryMatchID.Generate(), new List<IMRObject>());
            m_TempSetMatchData = new SetMatchData
            {
                dataAssignments = new Dictionary<IMRObject, int>(),
                exclusivities = new Dictionary<IMRObject, Exclusivity>()
            };

            // do not poll for simulation if we are running tests.  It has caused
            // hard-to-debug test failures from simulation running at unexpected times.
            m_SimulationPollTask = new SlowTask
            {
                sleepTime = m_SimulationPollTime,
                lastExecutionTime = Time.time,
                task = CheckIfSimulationRestartNeeded
            };

            // Start simulation if any objects are using the sim scene
            RestartSimulationIfNeeded();
        }

        public void UnloadModule()
        {
            m_SlowTaskModule.ClearTasks();
            m_SimulationPollTask = null;
            QueryObjectMapping.Map.Clear();
            EditorApplication.update -= Update;
            SimulationSceneModule.SimulationSceneCreated -= OnSimulationSceneCreated;
            SimulationSceneModule.SimulationSceneDestroyed -= OnSimulationSceneDestroyed;

            m_SimulationRestartNeeded = false;
            ShouldSimulateTemporal = false;
            m_SimulationPollTask = null;
            m_TempQueryResult = null;
            m_TempSetMatchData = default(SetMatchData);
            m_PreviousIsland = null;

            CleanupProviders();

            simulating = false;
            simulatingTemporal = false;
            functionalityIsland = null;
            providersRoot = null;
            simulatedDataAvailable = false;

            m_SimulationContext.Clear();
        }

        /// <summary>
        /// Runs simulation over a single frame.
        /// This method repeatedly checks for query matches until there are no more to be found.
        /// </summary>
        public void SimulateOneShot()
        {
            if (simulatingTemporal || !TryStartSimulation())
                return;

            // Run database processing jobs
            m_Database.OnMarsUpdate();
            m_ReasoningModule.UpdateReasoningAPIData();
            m_SlowTaskModule.SyncAddRemoveBuffers();
            m_SlowTaskModule.SyncMarsTimeAddRemoveBuffers();

            // We keep running the task loop until it doesn't find any matches.
            // This is to ensure that relations dependent on other entities have a chance to be matched.
            // Ideally we could avoid this by sorting queries based on a dependency graph.
            bool matchFoundThisIteration;
            do
            {
                matchFoundThisIteration = m_QueryBackend.RunAllQueries();
            }
            while (matchFoundThisIteration);

            StopSimulation();

            // Avoid triggering OnDisable until module unload or the next simulation, so that simulatables keep
            // their state at the end of simulation. We still need to set runInEditMode to false - this happens in
            // a double delay call so that it doesn't interfere with Start, which happens on a delay.
            EditorApplication.delayCall += DelayStopRunningOneShot;
        }

        void DelayStopRunningOneShot()
        {
            EditorApplication.delayCall += StopRunningOneShot;
        }

        void StopRunningOneShot()
        {
            m_SimulatedObjectsManager.StopRunningSimulatablesOneShot();
            if (simulationDone != null)
                simulationDone();
        }

        /// <summary>
        /// Starts query simulation that runs frame-to-frame
        /// </summary>
        public void StartTemporalSimulation()
        {
            if (simulatingTemporal)
                return;

            simulatingTemporal = true;

            if (!TryStartSimulation())
            {
                simulatingTemporal = false;
                return;
            }

            TriggerOnTemporalSimulationStart();

            m_EnvironmentManager.StartVideoIfNeeded();
        }

        /// <summary>
        /// Stops query simulation that runs frame-to-frame
        /// </summary>
        public void StopTemporalSimulation()
        {
            if (!simulatingTemporal)
                return;

            // Make it so that temporal simulation starts up again if it was interrupted by module unloading or assembly reloading
            if (ModuleLoaderCore.isUnloadingModules || SimulationSceneModule.isAssemblyReloading)
                ShouldSimulateTemporal = true;

            StopSimulation();
            m_SimulatedObjectsManager.StopRunningSimulatables();
            StopRunningProviders();
            simulatingTemporal = false;
            TriggerOnTemporalSimulationStop();

            m_QueryPipelinesModule.StandalonePipeline.ClearData();
        }

        void RestartTemporalSimulationKeepData()
        {
            m_EnvironmentManager.UpdateDeviceStartingPose();

            StopSimulation();
            m_SimulatedObjectsManager.StopRunningSimulatables();
            TriggerOnTemporalSimulationStop();
            if (m_QueryPipelinesModule != null)
                m_QueryPipelinesModule.StandalonePipeline.ClearData();

            if (!TryStartSimulation(true))
            {
                simulatingTemporal = false;
                return;
            }

            TriggerOnTemporalSimulationStart();
        }

        static void TriggerOnTemporalSimulationStart()
        {
#if UNITY_EDITOR
            EditorOnlyEvents.OnTemporalSimulationStart();
#endif
            if (onTemporalSimulationStart != null)
                onTemporalSimulationStart();
        }

        static void TriggerOnTemporalSimulationStop()
        {
            if (onTemporalSimulationStop != null)
                onTemporalSimulationStop();
        }

        bool TryStartSimulation(bool useExistingData = false)
        {
            if (!TraitCodeGenerator.HasGenerated)
                return false;

            m_SimulationRestartNeeded = false;
            ShouldSimulateTemporal = false;

            // Make sure the watchdog is up-to-date before we check if the scene has entities or subscribers
            m_SceneWatchdogModule.ExecutePollingTask();
            if (!sceneIsSimulatable)
                return false;

            if (!m_SimulationSceneModule.IsSimulationReady)
                return false;

            // if this method is null, that means something has tried to start simulation when the database isn't setup
            if (IUsesQueryResultsMethods.RegisterQuery == null)
                return false;

            var marsSession = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            if (marsSession == null)
                return false;

            Profiler.BeginSample(ProfilerLabels.TryStartSimulation); // only profile after safety checks
            if (MARSDebugSettings.querySimulationModuleLogging)
                Debug.Log("Start simulation");

            // Cancel delay calls in case the previous simulation was one-shot. Otherwise we can end up stopping behaviors
            // in the middle of temporal simulation or doubling up on delay calls for one-shot simulation.
            EditorApplication.delayCall -= StopRunningOneShot;
            EditorApplication.delayCall -= DelayStopRunningOneShot;

            m_SlowTaskModule.ClearTasks();
            QueryObjectMapping.Map.Clear();
            k_FunctionalitySubscriberObjects.Clear();
            var shouldStartVideo = SimulationSettings.isVideoEnvironment && simulatingTemporal;
            m_SimulatedObjectsManager.SetupSimulatables(shouldStartVideo, k_FunctionalitySubscriberObjects);

            simulating = true;

            var providersPreserved = useExistingData;
            if (!useExistingData)
            {
                if (m_SimulationContext.Update(marsSession, k_FunctionalitySubscriberObjects, simulatingTemporal, shouldStartVideo))
                {
                    if (MARSDebugSettings.querySimulationModuleLogging)
                        Debug.Log("Simulation context changed. Recreating providers.");

                    CleanupProviders();

                    functionalityIsland = SimulationSettings.environmentMode == EnvironmentMode.Synthetic && simulatingTemporal ?
                        m_SimulatedDiscoveryIsland : m_SimulationIsland;

                    providersRoot = new GameObject("Providers");
                    m_SimulationSceneModule.AddContentGameObject(providersRoot);
                    GameObjectUtils.gameObjectInstantiated += OnProviderInstantiated;

                    if (addCustomProviders != null)
                    {
                        addCustomProviders(m_Providers);
                        functionalityIsland.AddProviders(m_Providers);
                    }

                    functionalityIsland.SetupDefaultProviders(m_SimulationContext.SceneSubscriberTypes, m_Providers);
                    var definitions = new HashSet<TraitDefinition>();
                    foreach (var requirement in m_SimulationContext.SceneRequirements)
                    {
                        definitions.Add(requirement);
                    }

                    functionalityIsland.SetupDefaultProviders(definitions, m_Providers);
                    functionalityIsland.RequireProviders(definitions, m_Providers);

                    ModuleLoaderCore.instance.InjectFunctionalityInModules(functionalityIsland);
                    GameObjectUtils.gameObjectInstantiated -= OnProviderInstantiated;
                    providersRoot.SetHideFlagsRecursively(SimulatedObjectsManager.SimulatedObjectHideFlags);

                    foreach (var provider in m_Providers)
                    {
                        var providerBehaviour = provider as MonoBehaviour;
                        if (providerBehaviour != null)
                        {
                            m_ProviderBehaviours.Add(providerBehaviour);
                        }
                    }

                    foreach (var gameObject in m_ProviderGameObjects)
                    {
                        foreach (var simulatable in gameObject.GetComponentsInChildren<ISimulatable>())
                        {
                            var providerBehaviour = simulatable as MonoBehaviour;
                            if (providerBehaviour != null)
                            {
                                m_ProviderBehaviours.Add(providerBehaviour);
                            }
                        }
                    }
                }
                else
                {
                    providersPreserved = true;
                    StopRunningProviders();
                }
            }

            // When using providers from the last simulation, ensure they stay at the bottom of the hierarchy
            if (providersPreserved)
                providersRoot.transform.SetAsLastSibling();

            this.SetCameraScale(MARSWorldScaleModule.GetWorldScale());

            // Set active island now that providers have been setup
            m_PreviousIsland = m_FIModule.activeIsland;
            m_FIModule.SetActiveIsland(functionalityIsland);
            functionalityIsland.InjectFunctionalitySingle(marsSession);
            marsSession.StartRunInEditMode();

            if (!useExistingData)
            {
                // Clear the database and then run providers in edit mode so we start getting data.
                m_Database.Clear();
                foreach (var providerBehaviour in m_ProviderBehaviours)
                {
                    providerBehaviour.StartRunInEditMode();

                    var cameraPreviewProvider = providerBehaviour as IProvidesCameraPreview;
                    if (cameraPreviewProvider != null)
                        cameraPreviewProvider.previewReady += m_EnvironmentManager.FrameSimViewOnVideo;

                    var cameraIntrinsicsProvider = providerBehaviour as IProvidesCameraIntrinsics;
                    if (cameraIntrinsicsProvider != null)
                        m_EnvironmentManager.SetupFOVOverride(cameraIntrinsicsProvider);
                }
            }

            m_ReasoningModule.ResetReasoningAPIs();
            simulatedDataAvailable = true;

            // We must also reset the backend's query management state each time, otherwise there is no guarantee that
            // the its collections will be ordered the same way each time, even if the ordering of clients in the scene
            // stays the same. This also ensures its slow tasks are registered using edit mode time rather than play mode time.
            m_QueryBackend.ResetQueryManagement();

            GameObjectUtils.gameObjectInstantiated += m_SimulatedObjectsManager.AddSpawnedObjectToSimulation;
            m_QueryBackend.onQueryMatchFound += k_OnQueryMatchFound;
            m_QueryBackend.onSetQueryMatchFound += k_OnSetQueryMatchFound;
            if (SimulationSettings.findAllMatchingDataPerQuery)
                m_QueryBackend.onQueryMatchesFound += k_OnQueryMatchesFound;

            // inject functionality on all copied functionality subscribers
            functionalityIsland.InjectPreparedFunctionality(k_FunctionalitySubscriberObjects);

            GeoLocationModule.instance.AddOrUpdateLocationTrait();

            m_SchedulerModule.ResetTime();
            m_SimulatedObjectsManager.StartRunningSimulatables();

            Profiler.EndSample();
            return true;
        }

        void StopSimulation()
        {
            GameObjectUtils.gameObjectInstantiated -= m_SimulatedObjectsManager.AddSpawnedObjectToSimulation;
            m_QueryBackend.onQueryMatchFound -= k_OnQueryMatchFound;
            m_QueryBackend.onQueryMatchesFound -= k_OnQueryMatchesFound;
            m_QueryBackend.onSetQueryMatchFound -= k_OnSetQueryMatchFound;
            m_SlowTaskModule.ClearTasks();

            foreach (var simView in SimulationView.SimulationViews)
            {
                var camera = simView.camera;
                camera.ResetProjectionMatrix();
                simView.isRotationLocked = false;
            }

            m_EnvironmentManager.TearDownFOVOverride();

            var marsSession = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            if (marsSession != null)
                marsSession.StopRunInEditMode();

            if (m_FIModule.islands.Contains(m_PreviousIsland))
                m_FIModule.SetActiveIsland(m_PreviousIsland);

            simulating = false;
        }

        void StopRunningProviders()
        {
            simulatedDataAvailable = false;
            foreach (var behaviour in m_ProviderBehaviours)
            {
                if (behaviour != null)
                    behaviour.StopRunInEditMode();

                var cameraPreviewProvider = behaviour as IProvidesCameraPreview;
                if (cameraPreviewProvider != null)
                    cameraPreviewProvider.previewReady -= m_EnvironmentManager.FrameSimViewOnVideo;
            }
        }

        void CleanupProviders()
        {
            StopRunningProviders();

            functionalityIsland.RemoveProviders(m_Providers);
            if (!m_SimulatedObjectsManager.WillDestroyAfterAssemblyReload(providersRoot))
                UnityObjectUtils.Destroy(providersRoot);

            m_Providers.Clear();
            m_ProviderBehaviours.Clear();
            m_ProviderGameObjects.Clear();
        }

        void OnProviderInstantiated(GameObject providerObject)
        {
            // test cleanup can lead to providers getting destroyed before this callback runs
            if (providersRoot == null)
                return;

            m_ProviderGameObjects.Add(providerObject);
            providerObject.transform.SetParent(providersRoot.transform);
        }

        static void OnQueryMatchFound(QueryMatchID queryMatchID, int bestDataID)
        {
            if (MARSDebugSettings.querySimulationModuleLogging)
            {
                const string str = "<color=lime>Query match found</color> - <b>{0}</b>, " +
                    "data ID used: <b>{1}</b>";

                Debug.LogFormat(str, queryMatchID, bestDataID);
            }
        }

        static void OnQueryMatchesFound(QueryMatchID queryMatchID, Dictionary<int, float> allDataMatches)
        {
            if (MARSDebugSettings.querySimulationModuleLogging)
            {
                var allMatchingDataString = allDataMatches.Aggregate(
                    "--All matching data IDs: ", (current, kvp) => current + (kvp.Key + ", "));
                Debug.Log(allMatchingDataString);
            }
        }

        static void OnSetQueryMatchFound(QueryMatchID queryMatchID, Dictionary<IMRObject, int> matchChildren)
        {
            if (MARSDebugSettings.querySimulationModuleLogging)
            {
                var allMatchingDataString = matchChildren.Aggregate(
                    "--Child data IDs: ", (current, kvp) => current + (kvp.Value + ", "));
                Debug.Log(allMatchingDataString);
            }
        }

        void Update()
        {
            if (Application.isPlaying || ModuleLoaderCore.isBuilding)
                return;

            // QueuePlayerLoopUpdate is necessary not only for temporal simulation but to update Time.time for the slow task
            EditorApplication.QueuePlayerLoopUpdate();

            m_SimulationPollTask?.Update(Time.time);

            if (simulatingTemporal)
                m_SimulatedObjectsManager.UpdateLandmarkChildren();
        }

        /// <summary>
        /// If any objects are using the simulation scene, this method sets a flag that a new simulation should happen as soon as possible
        /// </summary>
        /// <param name="forceTemporal">If true, the next simulation will be temporal</param>
        public void RestartSimulationIfNeeded(bool forceTemporal = false)
        {
            if (SimulationSceneModule.UsingSimulation)
            {
                m_SimulationRestartNeeded = true;
                if (forceTemporal)
                    ShouldSimulateTemporal = true;

                if (MARSDebugSettings.querySimulationModuleLogging)
                    Debug.Log("simulation restart needed");
            }
        }

        void CheckIfSimulationRestartNeeded()
        {
            if (TestMode)
                return;

            if (m_SimulationSceneModule.IsSimulationReady && m_SimulationRestartNeeded && !m_EntityEditorModule.adjustingInSimView)
            {
                if (simulatingTemporal)
                    RestartTemporalSimulationKeepData();
                else if (ShouldSimulateTemporal)
                    StartTemporalSimulation();
                else
                    SimulateOneShot();
            }
        }

        void OnSimulationSceneCreated()
        {
            RestartSimulationIfNeeded();
        }

        void OnSimulationSceneDestroyed()
        {
            CleanupProviders();

            // Reset the simulation context so that it will register an update next time we start simulation
            m_SimulationContext.Clear();
        }

        internal void CleanupSimulation()
        {
            StopTemporalSimulation();
            m_SimulatedObjectsManager.CleanupSimulation();
            CleanupProviders();
            m_SimulationContext.Clear();
        }
    }
}
