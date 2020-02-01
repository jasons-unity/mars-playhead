using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

namespace Unity.Labs.MARS
{
    public enum EnvironmentMode
    {
        Synthetic,
        Recorded,
        Live,
        Remote
    }

    /// <summary>
    /// Module responsible for setting up and switching between simulation environments of different types
    /// </summary>
    [RequiresLayer(k_EnvironmentLayerName)]
    public class MARSEnvironmentManager : EditorScriptableSettings<MARSEnvironmentManager>,
        IModuleDependency<MARSVideoModule>, IModuleDependency<QuerySimulationModule>,
        IModuleDependency<SimulationSceneModule>, IModuleDependency<MARSWorldScaleModule>,
        IModuleDependency<FunctionalityInjectionModule>, IModuleDependency<SimulatedObjectsManager>,
        IProvidesDeviceSimulationSettings
    {
        enum SimulatedRendererType
        {
            Environment,
            Data
        }

        const string k_EnvironmentLayerName = "Environment";
        const string k_EnvironmentLabel = "Environment";
        const string k_DefaultEnvironmentLabel = "DefaultEnv";
        const string k_PrefabSearchPrefix = "t:prefab l:";
        const string k_EnvironmentPrefabFilter = k_PrefabSearchPrefix + k_EnvironmentLabel;
        const string k_DefaultEnvironmentPrefabFilter = k_PrefabSearchPrefix + k_DefaultEnvironmentLabel;
        const string k_SampleVideosFilter = "t:VideoClip l:" + k_EnvironmentLabel;
        const string k_OnDestroyNotifierName = "OnDestroyNotifier";
        const string k_NoSyntheticEnvironmentsTitle = "No Synthetic Simulation Environments";
        const string k_NoSyntheticEnvironmentsMessage =
            "No synthetic simulation environments found. Create a prefab to use as an environment, and label the " +
            "asset as 'Environment'. See the MARS documentation for more detail.";

        static IProvidesCameraIntrinsics s_CameraIntrinsicsProvider;
        static OnDestroyNotifier s_OnDestroyNotifier;

        readonly HashSet<ISimulationView> m_VideoSimulationViews = new HashSet<ISimulationView>();
        readonly HashSet<Camera> m_VideoCameras = new HashSet<Camera>();

        int m_CurrentSyntheticEnvironmentIndex = -1;
        int m_CurrentSampleVideoIndex = -1;

        MARSVideoModule m_VideoModule;
        QuerySimulationModule m_QuerySimulationModule;
        MARSWorldScaleModule m_WorldScaleModule;
        SimulationSceneModule m_SimulationSceneModule;
        FunctionalityInjectionModule m_FIModule;
        SimulatedObjectsManager m_SimulatedObjectsManager;

        GUIContent[] m_EnvironmentGUIContents;
        GUIContent[] m_VideoGUIContents;

        readonly List<string> m_EnvironmentPrefabPaths = new List<string>();
        readonly List<string> m_EnvironmentPrefabNames = new List<string>();
        readonly List<string> m_SampleVideoPaths = new List<string>();
        readonly List<string> m_SampleVideoNames = new List<string>();

        internal GUIContent[] EnvironmentGUIContents { get { return m_EnvironmentGUIContents; } }
        internal GUIContent[] VideoGUIContents { get { return m_VideoGUIContents; } }

        public static event Action onEnvironmentSetup;

        public static int EnvironmentObjectsLayer { get; private set; }

        public Pose DeviceStartingPose { get; private set; }

        /// <summary>
        /// Whether there is currently a simulation environment setup
        /// </summary>
        public bool EnvironmentSetup { get; private set; }

        /// <summary>
        /// Metadata about the current synthetic environment scene
        /// </summary>
        public MARSEnvironmentInfo SyntheticEnvironmentInfo { get; private set; }

        /// <summary>
        /// List of the synthetic environment prefab paths
        /// </summary>
        public List<string> EnvironmentPrefabPaths { get { return m_EnvironmentPrefabPaths; } }

        /// <summary>
        /// List of the synthetic environment prefab names
        /// </summary>
        public List<string> EnvironmentPrefabNames { get { return m_EnvironmentPrefabNames; } }

        /// <summary>
        /// List of the capture environment scene paths
        /// </summary>
        public List<string> SampleVideoPaths { get { return m_SampleVideoPaths; } }

        /// <summary>
        /// List of the capture environment scene names
        /// </summary>
        public List<string> SampleVideoNames { get { return m_SampleVideoNames; } }

        public Bounds EnvironmentBounds
        {
            get { return SyntheticEnvironmentInfo != null ? SyntheticEnvironmentInfo.EnvironmentBounds : default(Bounds); }
        }

        /// <summary>
        /// Name of the current synthetic environment prefab
        /// </summary>
        public string SyntheticEnvironmentName
        {
            get
            {
                return currentSyntheticEnvironmentIndexIsValid
                    ? EnvironmentPrefabNames[CurrentSyntheticEnvironmentIndex]
                    : string.Empty;
            }
        }

        /// <summary>
        /// Name of the current recorded environment scene
        /// </summary>
        public string RecordedEnvironmentName
        {
            get
            {
                if (CurrentSampleVideoIndex < 0 || CurrentSampleVideoIndex >= SampleVideoNames.Count)
                    return string.Empty;
                return SampleVideoNames[CurrentSampleVideoIndex];
            }
        }

        bool currentSyntheticEnvironmentIndexIsValid
        {
            get
            {
                return
                    SimulationSettings.environmentMode == EnvironmentMode.Synthetic &&
                    CurrentSyntheticEnvironmentIndex >= 0 && CurrentSyntheticEnvironmentIndex < EnvironmentPrefabNames.Count;
            }
        }

        public Pose DefaultDeviceStartingPose
        {
            get { return SyntheticEnvironmentInfo != null ? SyntheticEnvironmentInfo.SimulationStartingPose : default(Pose); }
        }

        /// <summary>
        /// The root object that environment objects are added to
        /// </summary>
        public GameObject EnvironmentParent { get; private set; }

        /// <summary>
        /// Checks whether any synthetic environment prefab have been found
        /// </summary>
        public bool SyntheticEnvironmentsExist { get { return m_EnvironmentPrefabPaths.Count != 0; } }

        /// <summary>
        /// Checks whether any recorded video environments have been found
        /// </summary>
        public bool RecordedVideosExist { get { return m_SampleVideoPaths.Count != 0; } }

        /// <summary>
        /// Index of the currently active synthetic environment in the m_EnvironmentPrefabsPaths list
        /// </summary>
        public int CurrentSyntheticEnvironmentIndex { get { return m_CurrentSyntheticEnvironmentIndex; } }

        /// <summary>
        /// Index of the currently active capture environment in the m_SampleVideoPaths list
        /// </summary>
        public int CurrentSampleVideoIndex { get { return m_CurrentSampleVideoIndex; } }

        public event Action EnvironmentChanged;

        public void ConnectDependency(MARSVideoModule dependency) { m_VideoModule = dependency; }

        public void ConnectDependency(QuerySimulationModule dependency) { m_QuerySimulationModule = dependency; }

        public void ConnectDependency(SimulationSceneModule dependency) { m_SimulationSceneModule = dependency; }

        public void ConnectDependency(FunctionalityInjectionModule dependency) { m_FIModule = dependency; }

        void IModuleDependency<MARSWorldScaleModule>.ConnectDependency(MARSWorldScaleModule dependency) { m_WorldScaleModule = dependency; }

        public void ConnectDependency(SimulatedObjectsManager dependency) { m_SimulatedObjectsManager = dependency; }

        public void LoadModule()
        {
            EnvironmentObjectsLayer = LayerMask.NameToLayer(k_EnvironmentLayerName);
            SimulationSceneModule.SimulationSceneCreated += SetupEnvironment;
            SimulationSceneModule.SimulationSceneDestroyed += TearDownEnvironment;
            EditorOnlyDelegates.IsEnvironmentPrefab = IsEnvironmentPrefab;
            GetSimulatedEnvironmentCandidates();
            EditorOnlyDelegates.CullEnvironmentFromSceneLights = CullEnvironmentFromSceneLights;
            EditorOnlyDelegates.GetEnvironmentLayer = () => EnvironmentObjectsLayer;
            EditorOnlyDelegates.IsEnvironmentSetup = () => EnvironmentSetup;
            EditorOnlyDelegates.SwitchToNextEnvironment = SetupNextEnvironment;
            onEnvironmentSetup += EditorOnlyEvents.OnEnvironmentSetup;
        }

        public void UnloadModule()
        {
            SimulationSceneModule.SimulationSceneCreated -= SetupEnvironment;
            SimulationSceneModule.SimulationSceneDestroyed -= TearDownEnvironment;
            EditorOnlyDelegates.IsEnvironmentPrefab = null;

            TearDownEnvironment();

            EditorOnlyDelegates.CullEnvironmentFromSceneLights = null;
            EditorOnlyDelegates.GetEnvironmentLayer = null;
            EditorOnlyDelegates.IsEnvironmentSetup = null;
            EditorOnlyDelegates.SwitchToNextEnvironment = null;
            onEnvironmentSetup -= EditorOnlyEvents.OnEnvironmentSetup;

            // Do not reset current environment/video index to avoid changing environments on domain reload
            m_EnvironmentPrefabPaths.Clear();
            m_EnvironmentPrefabNames.Clear();
            m_SampleVideoPaths.Clear();
            m_SampleVideoNames.Clear();
            TearDownFOVOverride();
            DeviceStartingPose = default(Pose);
            EnvironmentSetup = false;
            SyntheticEnvironmentInfo = null;
            m_EnvironmentGUIContents = null;
            m_VideoGUIContents = null;

            if (s_OnDestroyNotifier)
            {
                s_OnDestroyNotifier.destroyed = null;
                UnityObjectUtils.Destroy(s_OnDestroyNotifier.gameObject);
            }
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var deviceSimulationSettingsSubscriber = obj as IUsesDeviceSimulationSettings;
            if (deviceSimulationSettingsSubscriber != null)
                deviceSimulationSettingsSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        void OnCameraPreRender(Camera camera)
        {
            if (!m_VideoCameras.Contains(camera))
                return;

            var fov = s_CameraIntrinsicsProvider.GetFOV();
            if (fov <= 0)
                return;

            camera.fieldOfView = fov;

            camera.projectionMatrix = Matrix4x4.Perspective(fov, camera.aspect, camera.nearClipPlane, camera.farClipPlane);
        }

        void OnCameraPostRender(Camera camera)
        {
            if (!m_VideoCameras.Contains(camera))
                return;

            camera.ResetProjectionMatrix();
        }

        static bool IsEnvironmentPrefab(GameObject prefab)
        {
            if (!PrefabUtility.IsPartOfPrefabAsset(prefab))
                return false;

            var outerPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(prefab);
            if (outerPrefab == null)
                return false;

            var path = AssetDatabase.GetAssetPath(outerPrefab);
            if (string.IsNullOrEmpty(path))
                return false;

            var labels = AssetDatabase.GetLabels(outerPrefab);
            return labels.Contains(k_EnvironmentLabel) || labels.Contains(k_DefaultEnvironmentLabel);
        }

        /// <summary>
        /// Tears down the current environment (if there is one) and sets up a new one based on current simulation settings,
        /// and then triggers simulation restart
        /// </summary>
        /// <param name="forceTemporal">If true, the next simulation will be temporal</param>
        internal void RefreshEnvironmentAndRestartSimulation(bool forceTemporal = false)
        {
            if (forceTemporal)
                m_QuerySimulationModule.ShouldSimulateTemporal = true;

            RefreshEnvironment();
            ModuleLoaderCore.instance.GetModule<SimulatedObjectsManager>().DirtySimulatableScene();
            m_QuerySimulationModule.RestartSimulationIfNeeded();
        }

        /// <summary>
        /// Tears down the current environment (if there is one) and sets up a new one based on current simulation settings
        /// </summary>
        internal void RefreshEnvironment()
        {
            TearDownEnvironment();
            SetupEnvironment();
        }

        /// <summary>
        /// Sets up an environment based on current simulation settings
        /// </summary>
        internal void SetupEnvironment()
        {
            if (EnvironmentSetup)
            {
                Debug.LogWarning("There is already an environment setup. Please use RefreshEnvironment if you wish to " +
                    "tear down the current environment and setup a new one.");
                return;
            }

            if (!m_SimulationSceneModule.IsSimulationReady)
                return;

            var notifiers = FindObjectsOfType<OnDestroyNotifier>();
            if (notifiers.Length > 0)
            {
                s_OnDestroyNotifier = notifiers.First(notifier => notifier.gameObject.name.Equals(k_OnDestroyNotifierName));
            }

            if (s_OnDestroyNotifier == null)
            {
                s_OnDestroyNotifier = new GameObject(k_OnDestroyNotifierName).AddComponent<OnDestroyNotifier>();
                s_OnDestroyNotifier.gameObject.hideFlags = HideFlags.HideInHierarchy;
            }

            s_OnDestroyNotifier.destroyed = OnNotifierDestroyed;

            EnvironmentParent = new GameObject("Simulated Environment");
            EditorOnlyDelegates.GetSimulatedEnvironmentRoot = () => EnvironmentParent;
            m_SimulationSceneModule.AddEnvironmentGameObject(EnvironmentParent);

            switch (SimulationSettings.environmentMode)
            {
                case EnvironmentMode.Synthetic:
                    var settings = OpenSyntheticEnvironment();
                    FinishSetupEnvironment();
#if UNITY_POST_PROCESSING_STACK_V2
                    if (EditorApplication.isPlayingOrWillChangePlaymode && settings != null)
                        settings.SetupPostProcessingVolume();
#endif
                    break;
                case EnvironmentMode.Live:
                    m_VideoModule.SetVideoClip(null);
                    FinishSetupEnvironment();
                    break;
                case EnvironmentMode.Recorded:
                    m_VideoModule.SetVideoClip(SimulationSettings.recordedVideo);
                    FinishSetupEnvironment();
                    break;
                default:
                    FinishSetupEnvironment();
                    break;
            }
        }

        void FinishSetupEnvironment()
        {
            SetSimEnvironmentVisibility(SimulationSettings.showSimulatedEnvironment);
            SetSimDataVisibility(SimulationSettings.showSimulatedData);
            EnvironmentParent.SetLayerRecursively(EnvironmentObjectsLayer);
            // Cull lighting to only simulated environment objects
            foreach (var light in EnvironmentParent.GetComponentsInChildren<Light>())
            {
                light.cullingMask = 1 << EnvironmentObjectsLayer;
            }

            var environmentTrans = EnvironmentParent.transform;
            environmentTrans.localScale = Vector3.one * MARSWorldScaleModule.GetWorldScale();
            // Need to match the scalar values for components that have parameters
            // that are not effected by world scale.
            m_WorldScaleModule.ApplyWorldScaleToEnvironment();

            var session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            if (session != null)
            {
                var sessionTrans = session.transform;
                environmentTrans.position = sessionTrans.position;
                environmentTrans.rotation = sessionTrans.rotation.ConstrainYaw();
            }

            EnvironmentSetup = true;
            if (onEnvironmentSetup != null)
                onEnvironmentSetup();

            EnvironmentChanged?.Invoke();
        }

        static void OnNotifierDestroyed(OnDestroyNotifier obj)
        {
            // If the notifier is destroyed and it is not due to switching scenes, in play mode, or building, we know we are discarding changes
            if (!(ModuleLoaderCore.isSwitchingScenes || EditorApplication.isPlaying))
            {
                // Delay call to ReloadModules so that it happens after MARSSession gets destroyed and triggers UnloadModules
                EditorApplication.delayCall += ModuleLoaderCore.instance.ReloadModules;
            }
        }

        /// <summary>
        /// Tears down the current environment (if there is one)
        /// </summary>
        internal void TearDownEnvironment()
        {
            if (!EnvironmentSetup)
                return;

            EnvironmentSetup = false;

            var activeIsland = m_FIModule.activeIsland;
            if (activeIsland != null)
            {
                IFunctionalityProvider provider;
                if (activeIsland.providers.TryGetValue(typeof(IProvidesSessionControl), out provider))
                    ((IProvidesSessionControl)provider).ResetSession();
            }

            m_QuerySimulationModule.CleanupSimulation();

            SyntheticEnvironmentInfo = null;

            m_WorldScaleModule.ClearEnvironmentRangeScaledComponents();

            if (EnvironmentParent != null && !m_SimulatedObjectsManager.WillDestroyAfterAssemblyReload(EnvironmentParent))
                UnityObjectUtils.Destroy(EnvironmentParent);

            DeviceStartingPose = default(Pose);
        }

        public SimulationRenderSettings RenderSettings { get; private set; }

        MARSEnvironmentSettings OpenSyntheticEnvironment()
        {
            var currentEnvironment = SimulationSettings.environmentPrefab;
            if (currentEnvironment == null)
            {
                if (!SyntheticEnvironmentsExist)
                    return null;

                SetSyntheticEnvironment(0);
            }
            else if (AssetDatabase.GetAssetPath(currentEnvironment) != m_EnvironmentPrefabPaths[m_CurrentSyntheticEnvironmentIndex])
            {
                SetSyntheticEnvironment(m_CurrentSyntheticEnvironmentIndex);
            }

            // Copy the objects under a deactivated gameobject root. This allows all the objects to be copied before any
            // behaviour gets OnEnable called. When the root is activated again, the script with the lowest execution
            // order, MARSEnvironmentSettings, will wake up first and fire an event that injects functionality before the
            // other scripts are awake.
            EnvironmentParent.SetActive(false);
            var envPrefab = SimulationSettings.environmentPrefab;
            // TODO envPrefab is being set to HideInHierarchy somewhere. Not coping flags to allow editing and selecting.
            var envPrefabCopy = Instantiate(envPrefab, EnvironmentParent.transform);
            envPrefabCopy.name = envPrefab.name; // Removes "(clone)" from the name
            m_WorldScaleModule.GetEnvironmentRangeScaledComponents(envPrefabCopy);

            // This will create a MARSEnvironmentSettings on the prefab copy if none exits
            MARSEnvironmentSettings settings;
            if (!MARSEnvironmentSettings.GetOrCreateSettings(envPrefabCopy, out settings))
            {
                Debug.LogWarning($"Environment Settings created for {envPrefabCopy.name}'s Instance." +
                    "\nYou Should create an Environment Settings on the original to use custom values!");
            }

            settings.UpdatePrefabInfo();
            SyntheticEnvironmentInfo = settings.EnvironmentInfo;
            DeviceStartingPose = SyntheticEnvironmentInfo.SimulationStartingPose;
            RenderSettings = settings.RenderSettings;

            EnvironmentParent.SetActive(true);
            FrameAllSimViewsOnSyntheticEnvironment();
            return settings;
        }

        internal void UpdateDeviceStartingPose()
        {
            var simulatedObjectsManager = ModuleLoaderCore.instance.GetModule<SimulatedObjectsManager>();
            if (simulatedObjectsManager != null)
            {
                var simulatedCamera = simulatedObjectsManager.SimulatedCamera;
                if (simulatedCamera != null)
                    DeviceStartingPose = simulatedObjectsManager.SimulatedCamera.transform.GetLocalPose();
            }
        }

        internal void ResetDeviceStartingPose()
        {
            DeviceStartingPose = DefaultDeviceStartingPose;
        }

        /// <summary>
        /// Offsets and scales the parent transform of the environment and adjusts the simulation view camera
        /// such that the environment appears to have been unchanged
        /// </summary>
        internal void OffsetEnvironment(Vector3 positionOffset, Quaternion rotationOffset, Vector3 scaleOffset)
        {
            Debug.Assert(EnvironmentParent != null);

            var environmentParentTrans = EnvironmentParent.transform;
            var inversePreviousOffset = environmentParentTrans.worldToLocalMatrix;
            var previousScale = environmentParentTrans.localScale.x;
            var differenceScale = scaleOffset.x / previousScale;
            var differenceRotation = rotationOffset * inversePreviousOffset.rotation;

            environmentParentTrans.position = positionOffset;
            environmentParentTrans.rotation = rotationOffset;
            environmentParentTrans.localScale = scaleOffset;

            foreach (var simView in SimulationView.SimulationViews)
            {
                if (simView == null)
                    continue;

                // Offset cached scene locations.
                simView.OffsetViewCachedData(environmentParentTrans, inversePreviousOffset, differenceRotation, differenceScale);

                var size = simView.size * differenceScale;
                var previousLocalPivot = inversePreviousOffset.MultiplyPoint3x4(simView.pivot);
                var pivot = environmentParentTrans.TransformPoint(previousLocalPivot);
                var rotation = differenceRotation * simView.rotation;
                simView.LookAt(pivot, rotation, size, simView.orthographic, true);
                simView.Repaint();
            }
        }

        internal void SetupFOVOverride(IProvidesCameraIntrinsics camera)
        {
            s_CameraIntrinsicsProvider = camera;
            Camera.onPreRender += OnCameraPreRender;
            Camera.onPostRender += OnCameraPostRender;
        }

        internal void TearDownFOVOverride()
        {
            s_CameraIntrinsicsProvider = null;
            Camera.onPreRender -= OnCameraPreRender;
            Camera.onPostRender -= OnCameraPostRender;

            foreach (var videoCamera in m_VideoCameras)
            {
                if (videoCamera == null)
                    continue;

                videoCamera.ResetProjectionMatrix();
            }

            m_VideoSimulationViews.Clear();
            m_VideoCameras.Clear();
        }

        bool GetSimulatedEnvironmentCandidates()
        {
            m_EnvironmentPrefabPaths.Clear();
            m_EnvironmentPrefabNames.Clear();
            var envPrefabs = AssetDatabase.FindAssets(k_EnvironmentPrefabFilter);
            if (envPrefabs.Length == 0)
                envPrefabs = AssetDatabase.FindAssets(k_DefaultEnvironmentPrefabFilter);

            foreach (var guid in envPrefabs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                m_EnvironmentPrefabPaths.Add(path);
                m_EnvironmentPrefabNames.Add(Path.GetFileNameWithoutExtension(path));
            }

            m_EnvironmentGUIContents = m_EnvironmentPrefabNames.ConvertAll(prefabName => new GUIContent(prefabName)).ToArray();

            m_SampleVideoPaths.Clear();
            m_SampleVideoNames.Clear();
            var sampleVideos = AssetDatabase.FindAssets(k_SampleVideosFilter);
            foreach (var guid in sampleVideos)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                m_SampleVideoPaths.Add(path);
                m_SampleVideoNames.Add(Path.GetFileNameWithoutExtension(path));
            }

            m_VideoGUIContents = m_SampleVideoNames.ConvertAll(videoName => new GUIContent(videoName)).ToArray();

            var currentPrefabAssetPath = AssetDatabase.GetAssetPath(SimulationSettings.environmentPrefab);
            m_CurrentSyntheticEnvironmentIndex = Mathf.Max(m_EnvironmentPrefabPaths.IndexOf(currentPrefabAssetPath), 0);
            var currentVideoAssetPath = AssetDatabase.GetAssetPath(SimulationSettings.recordedVideo);
            m_CurrentSampleVideoIndex = Mathf.Max(m_SampleVideoPaths.IndexOf(currentVideoAssetPath), 0);

            return m_EnvironmentPrefabPaths.Count > 0;
        }

        public void UpdateSimulatedEnvironmentCandidates()
        {
            var noSynthEnvironmentsPreviously = !SyntheticEnvironmentsExist;
            GetSimulatedEnvironmentCandidates();

            if (SimulationSettings.environmentMode == EnvironmentMode.Synthetic && SyntheticEnvironmentsExist && noSynthEnvironmentsPreviously)
                SetSyntheticEnvironment(0);
        }

        [MenuItem(MenuConstants.DevMenuPrefix + "Update Simulation Environments", priority = MenuConstants.UpdateSimEnvironmentsPriority)]
        public static void UpdateSimulationEnvironmentsMenuItem()
        {
            instance.UpdateSimulatedEnvironmentCandidates();
        }

        /// <summary>
        /// Sets up the next environment of the current type and then triggers simulation restart
        /// </summary>
        /// <param name="forward">Direction in which to cycle through environments</param>
        /// <param name="forceTemporal">If true, the next simulation will be temporal</param>
        public void SetupNextEnvironmentAndRestartSimulation(bool forward, bool forceTemporal = false)
        {
            NextEnvironment(forward);
            RefreshEnvironmentAndRestartSimulation(forceTemporal);
        }

        /// <summary>
        /// Sets up the specified environment based on index in environment list of the current type and then
        /// triggers simulation restart.
        /// </summary>
        /// <param name="index">Specified index of environment</param>
        /// <param name="forceTemporal">If true, the next simulation will be temporal</param>
        public void SetupEnvironmentAndRestartSimulation(int index, bool forceTemporal = false)
        {
            switch (SimulationSettings.environmentMode)
            {
                case EnvironmentMode.Synthetic:
                    if (!SyntheticEnvironmentsExist)
                        return;

                    SetSyntheticEnvironment(index);
                    break;

                case EnvironmentMode.Recorded:
                    if (!RecordedVideosExist ||!SyntheticEnvironmentsExist)
                        return;

                    SetCaptureEnvironment(index);
                    break;
            }
            RefreshEnvironmentAndRestartSimulation(forceTemporal);
        }

        /// <summary>
        /// Sets up the specified environment, if it is in environment list of the current type, then
        /// triggers simulation restart.
        /// </summary>
        /// <param name="path">Path of the desired environment</param>
        /// <param name="forceTemporal">If true, the next simulation will be temporal</param>
        public void SetupEnvironmentAndRestartSimulation(string path, bool forceTemporal = false)
        {
            var index = -1;
            switch (SimulationSettings.environmentMode)
            {
                case EnvironmentMode.Synthetic:
                    index = m_EnvironmentPrefabPaths.IndexOf(path);
                    break;
                case EnvironmentMode.Recorded:
                    index = m_SampleVideoPaths.IndexOf(path);
                    break;
            }

            if (index != -1)
                SetupEnvironmentAndRestartSimulation(index, forceTemporal);
        }

        /// <summary>
        /// Sets up the next environment of the current type
        /// </summary>
        /// <param name="forward">Direction in which to cycle through environments</param>
        public void SetupNextEnvironment(bool forward)
        {
            NextEnvironment(forward);
            RefreshEnvironment();
        }

        /// <summary>
        /// Updates simulation settings to reference the next environment of the current type
        /// </summary>
        /// <param name="forward">Direction in which to cycle through environments</param>
        public void NextEnvironment(bool forward)
        {
            int index;
            switch (SimulationSettings.environmentMode)
            {
                case EnvironmentMode.Synthetic:
                    if (!SyntheticEnvironmentsExist)
                    {
                        DisplayNoEnvironmentsDialog();
                        return;
                    }

                    index = forward ? m_CurrentSyntheticEnvironmentIndex + 1 : m_CurrentSyntheticEnvironmentIndex - 1;
                    SetSyntheticEnvironment(index);
                    break;

                case EnvironmentMode.Recorded:
                    if (!RecordedVideosExist ||!SyntheticEnvironmentsExist)
                        return;

                    index = forward ? m_CurrentSampleVideoIndex + 1 : m_CurrentSampleVideoIndex - 1;
                    SetCaptureEnvironment(index);
                    break;
            }
        }

        /// <summary>
        /// Updates simulation settings to reference the synthetic environment at the given index
        /// </summary>
        /// <param name="index">Synthetic environment index</param>
        public void SetSyntheticEnvironment(int index)
        {
            if (index < 0)
                index = m_EnvironmentPrefabPaths.Count - 1;
            else if (index >= m_EnvironmentPrefabPaths.Count)
                index = 0;

            if (!SyntheticEnvironmentsExist && !GetSimulatedEnvironmentCandidates())
            {
                DisplayNoEnvironmentsDialog();
                return;
            }

            m_CurrentSyntheticEnvironmentIndex = index;

            var environmentToLoad = AssetDatabase.LoadAssetAtPath<GameObject>(m_EnvironmentPrefabPaths[m_CurrentSyntheticEnvironmentIndex]);

            if (environmentToLoad == null)
            {
                GetSimulatedEnvironmentCandidates();
                SetSyntheticEnvironment(index);
                return;
            }

            SimulationSettings.environmentPrefab = environmentToLoad;
        }

        /// <summary>
        /// Updates simulation settings to reference the capture environment at the given index
        /// </summary>
        /// <param name="index">Capture environment index</param>
        public void SetCaptureEnvironment(int index)
        {
            if (index < 0)
                index = m_SampleVideoPaths.Count - 1;
            else if (index >= m_SampleVideoPaths.Count)
                index = 0;

            m_CurrentSampleVideoIndex = index;

            SimulationSettings.recordedVideo =
                AssetDatabase.LoadAssetAtPath<VideoClip>(m_SampleVideoPaths[m_CurrentSampleVideoIndex]);
        }

        void DisplayNoEnvironmentsDialog()
        {
            EditorUtility.DisplayDialog(k_NoSyntheticEnvironmentsTitle, k_NoSyntheticEnvironmentsMessage, "Ok");
        }

        public void FrameAllSimViewsOnSyntheticEnvironment()
        {
            if (SimulationSettings.environmentMode != EnvironmentMode.Synthetic)
            {
                Debug.LogWarning(
                    "Cannot frame sim view on a synthetic environment because the current environment mode is not Synthetic.");
                return;
            }

            Debug.Assert(SyntheticEnvironmentInfo != null);

            foreach (var simView in SimulationView.SimulationViews)
            {
                var activeView = simView == SimulationView.ActiveSimulationView;
                FrameSimViewOnSyntheticEnvironment(simView, SyntheticEnvironmentInfo, activeView, true);
            }
        }

        public Quaternion FrameSimViewOnSyntheticEnvironment(ISimulationView simView, bool rotateView, bool instant)
        {
            if (SimulationSettings.environmentMode != EnvironmentMode.Synthetic)
            {
                Debug.LogWarning(
                    "Cannot frame sim view on a synthetic environment because the current environment mode is not Synthetic.");
                return Quaternion.identity;
            }

            Debug.Assert(SyntheticEnvironmentInfo != null);
            return FrameSimViewOnSyntheticEnvironment(simView, SyntheticEnvironmentInfo, rotateView, instant);
        }

        public static Quaternion FrameSimViewOnSyntheticEnvironment(ISimulationView simView, MARSEnvironmentInfo envInfo, bool rotateView, bool instant)
        {
            var session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            var cameraPose = envInfo.DefaultCameraWorldPose;
            float worldScale;
            Vector3 positionOffset;
            Quaternion rotationOffset;
            if (session != null)
            {
                var sessionTrans = session.transform;
                worldScale = sessionTrans.localScale.x;
                positionOffset = sessionTrans.position;
                rotationOffset = sessionTrans.rotation.ConstrainYawNormalized();
            }
            else
            {
                worldScale = 1f;
                positionOffset = Vector3.zero;
                rotationOffset = Quaternion.identity;
            }

            var rawDistance = envInfo.DefaultCameraSize;
            var existingPivot = envInfo.DefaultCameraPivot;

            var rawPivot = existingPivot != Vector3.zero ? existingPivot : cameraPose.position + cameraPose.forward * rawDistance;
            var pivot = rotationOffset * rawPivot * worldScale + positionOffset;
            var size = rawDistance * worldScale;

            var rawRotation = rotateView ? cameraPose.rotation : simView.rotation;
            var rotation = rotationOffset * rawRotation;

            if (simView.sceneType == ViewSceneType.Simulation)
            {
                simView.LookAt(pivot, rotation, size, simView.orthographic, instant);
                simView.isRotationLocked = false;
                simView.Repaint();
            }

            if (simView is SimulationView simulationView)
                simulationView.CacheLookAt(pivot, rotation, size);

            return rotation;
        }

        internal void FrameSimViewOnVideo(IProvidesCameraPreview cameraPreview)
        {
            var videoSceneView = SimulationView.ActiveSimulationView;

            foreach (var simView in SimulationView.SimulationViews)
            {
                if (simView == videoSceneView)
                {
                    var videoSimView = (ISimulationView)simView;
                    m_VideoSimulationViews.Add(videoSimView);
                    continue;
                }

                simView.camera.ResetProjectionMatrix();
                simView.isRotationLocked = false;
            }

            foreach (var videoSimView in m_VideoSimulationViews)
            {
                m_VideoCameras.Add(videoSimView.camera);
                videoSimView.LookAt(cameraPreview.GetPreviewObjectPosition(), Quaternion.identity, 0, false, true);
                videoSimView.pivot = Vector3.zero;
                videoSimView.isRotationLocked = true;
            }
        }

        public void StartVideoIfNeeded()
        {
            if (SimulationSettings.environmentMode == EnvironmentMode.Recorded)
                m_VideoModule.SetVideoClip(SimulationSettings.recordedVideo);
        }

        static IEnumerable<Renderer> GetSimRenderers(SimulatedRendererType seeking)
            {
            var results = new List<Renderer>();
            if (instance == null || instance.EnvironmentParent == null)
                return results;

            var allRenderers = instance.EnvironmentParent.GetComponentsInChildren<Renderer>();
            foreach (var renderer in allRenderers)
            {
                // Renderers on synthesized data are visualizing invisible data,
                // and not actually seen as part of the environment
                var synthesizedRenderer = renderer.GetComponent<SynthesizedTrait>() != null
                    || renderer.GetComponent<SynthesizedTrackable>() != null;

                if (synthesizedRenderer && seeking == SimulatedRendererType.Data)
                    results.Add(renderer);
                else if (!synthesizedRenderer && seeking == SimulatedRendererType.Environment)
                    results.Add(renderer);
            }

            return results;
        }

        internal static void SetSimDataVisibility(bool enabled)
        {
            foreach (var renderer in GetSimRenderers(SimulatedRendererType.Data))
                renderer.enabled = enabled;
        }

        internal static void SetSimEnvironmentVisibility(bool enabled)
        {
            foreach (var renderer in GetSimRenderers(SimulatedRendererType.Environment))
                renderer.enabled = enabled;
        }

        static void CullEnvironmentFromSceneLights(Scene scene)
        {
            var mask = ~(1 << EnvironmentObjectsLayer);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var light in root.GetComponentsInChildren<Light>())
                {
                    light.cullingMask = light.cullingMask & mask;
                }
            }
        }
    }
}
