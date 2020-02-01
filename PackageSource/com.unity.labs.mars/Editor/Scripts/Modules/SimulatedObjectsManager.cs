using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using SlowTask = Unity.Labs.MARS.SlowTaskModule.SlowTask;
using UnityObject = UnityEngine.Object;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Module responsible for copying simulatable objects to the simulation scene, mapping between these copies and their originals,
    /// and checking for changes to the original objects
    /// </summary>
    [ScriptableSettingsPath(MARSCore.UserSettingsFolder)]
    public class SimulatedObjectsManager : EditorScriptableSettings<SimulatedObjectsManager>, IModuleDependency<QuerySimulationModule>,
        IModuleDependency<MARSEnvironmentManager>, IModuleDependency<SimulationSceneModule>,
        IModuleDependency<SceneWatchdogModule>
    {
        public const HideFlags SimulatedObjectHideFlags = HideFlags.DontSave;

        const string k_TemporaryRootName = "DELETE ME | temporary root used by SimulatedObjectsManager";
        const string k_SimulatedContentRootName = "Augmented Objects";

        // Using built in layer 6 which cannot be renamed
        const int k_SimulatedObjectsLayerNumber = 6;

        static LayerMask s_SimulatedObjectsLayer = -1;
        static int s_SimulatedLightingMask = -1;

        static readonly Dictionary<Type, MethodInfo> k_OnDisableMethods = new Dictionary<Type, MethodInfo>();

        [SerializeField]
        List<GameObject> m_DestroyAfterReloadObjects = new List<GameObject>();

        readonly Dictionary<ISimulatable, ISimulatable> m_CopiedToOriginalSimulatables = new Dictionary<ISimulatable, ISimulatable>();
        readonly Dictionary<ISimulatable, ISimulatable> m_OriginalToCopiedSimulatables = new Dictionary<ISimulatable, ISimulatable>();
        readonly Dictionary<Transform, Transform> m_OriginalToCopiedTransforms = new Dictionary<Transform, Transform>();
        readonly Dictionary<Transform, Transform> m_CopiedToOriginalTransforms = new Dictionary<Transform, Transform>();

        QuerySimulationModule m_QuerySimulationModule;
        MARSEnvironmentManager m_EnvironmentManager;
        SimulationSceneModule m_SimulationSceneModule;
        SceneWatchdogModule m_SceneWatchdogModule;

        SlowTask m_SimulatableChangeFinalizedTask;
        SlowTask m_OffsetChangeFinalizedTask;
        bool m_SimulatableSceneDirty;
        bool m_PreviousVideoSimulation;

        readonly List<MonoBehaviour> m_SimulatableBehaviours = new List<MonoBehaviour>();
        readonly List<MonoBehaviour> m_SpawnedSimulatableBehaviours = new List<MonoBehaviour>();
        readonly List<GameObject> m_SpawnedSimulatableObjects = new List<GameObject>();
        readonly Dictionary<LandmarkController, LandmarkController> m_LandmarkControllers = new Dictionary<LandmarkController, LandmarkController>();

        public static int SimulatedObjectsLayer
        {
            get
            {
                if (s_SimulatedObjectsLayer == -1)
                    s_SimulatedObjectsLayer = k_SimulatedObjectsLayerNumber;

                return s_SimulatedObjectsLayer;
            }
        }

        static int SimulatedLightingMask
        {
            get
            {
                if (s_SimulatedLightingMask == -1)
                    s_SimulatedLightingMask = ~(1 << MARSEnvironmentManager.EnvironmentObjectsLayer);

                return s_SimulatedLightingMask;
            }
        }

        public GameObject SimulatedContentRoot { get; private set; }

        public Camera SimulatedCamera { get; private set; }

        public bool SimulationSyncedWithScene { get; private set; }

        public event Action<Camera> OnSimulatedCameraCreated;

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<GameObject> k_ActiveSceneGameObjects = new List<GameObject>();
        static readonly List<ISimulatable> k_OriginalSimulatables = new List<ISimulatable>();
        static readonly List<ISimulatable> k_CopiedSimulatables = new List<ISimulatable>();
        static readonly List<Light> k_CopiedLights = new List<Light>();
        static readonly List<Transform> k_OriginalTransforms = new List<Transform>();
        static readonly List<Transform> k_CopiedTransforms = new List<Transform>();
        static readonly List<IFunctionalitySubscriber> k_Subscribers = new List<IFunctionalitySubscriber>();
        static readonly HashSet<GameObject> k_UniqueDestroyAfterReloadObjects = new HashSet<GameObject>();
#if UNITY_POST_PROCESSING_STACK_V2
        static readonly List<PostProcessVolume> k_ContentPostProcessVolumes = new List<PostProcessVolume>();
        static readonly Dictionary<PostProcessVolume, int> k_ContentPostProcessVolumeLayers = new Dictionary<PostProcessVolume, int>();
#endif

        public void ConnectDependency(QuerySimulationModule dependency) { m_QuerySimulationModule = dependency; }

        public void ConnectDependency(MARSEnvironmentManager dependency) { m_EnvironmentManager = dependency; }

        public void ConnectDependency(SimulationSceneModule dependency) { m_SimulationSceneModule = dependency; }

        public void ConnectDependency(SceneWatchdogModule dependency) { m_SceneWatchdogModule = dependency; }

        public void LoadModule()
        {
            EditorOnlyDelegates.TryGetSimulatedCamera = TryGetSimulatedCamera;
            EditorOnlyDelegates.AddSpawnedTransformToSimulationManager = AddSpawnedTransformToSimulationManager;
            EditorOnlyDelegates.AddSpawnedSimulatableToSimulationManager = AddSpawnedSimulatableToSimulationManager;
            EditorOnlyDelegates.DirtySimulatableScene = DirtySimulatableScene;
            Undo.postprocessModifications += OnPostprocessModifications;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
            EditorApplication.update += Update;
            m_SceneWatchdogModule.prefabInstanceReverted += PrefabInstanceReverted;
            SimulationSceneModule.SimulationSceneDestroyed += OnSimulationSceneDestroyed;
            AddSimulationLayerToParticleSystemPreview();

            var logging = MARSDebugSettings.SimObjectsManagerLogging;
            foreach (var gameObject in m_DestroyAfterReloadObjects)
            {
                if (gameObject == null)
                    continue;

                if (logging)
                    Debug.Log($"Destroying old simulated object '{gameObject.name}'");

                DestroyImmediate(gameObject);
            }

            m_DestroyAfterReloadObjects.Clear();
        }

        public void UnloadModule()
        {
            CleanupSimulation();
            m_SimulatableSceneDirty = false;
            m_PreviousVideoSimulation = false;
            SimulationSyncedWithScene = false;

            EditorOnlyDelegates.TryGetSimulatedCamera = null;
            EditorOnlyDelegates.AddSpawnedTransformToSimulationManager = null;
            EditorOnlyDelegates.AddSpawnedSimulatableToSimulationManager = null;
            EditorOnlyDelegates.DirtySimulatableScene = null;
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            EditorApplication.update -= Update;
            m_SceneWatchdogModule.prefabInstanceReverted -= PrefabInstanceReverted;
            SimulationSceneModule.SimulationSceneDestroyed -= OnSimulationSceneDestroyed;
        }

        static void AddSimulationLayerToParticleSystemPreview()
        {
            var particleSystemEditorUtils = typeof(EditorApplication).Assembly.GetType(
                "UnityEditor.ParticleSystemEditorUtils", true, true);

            var previewLayersProperty = particleSystemEditorUtils.GetProperty("previewLayers", BindingFlags.NonPublic | BindingFlags.Static);
            if (previewLayersProperty != null)
            {
                var currentLayers = (uint)previewLayersProperty.GetMethod.Invoke(null, null);
                var newLayers = currentLayers | (uint)(1 << SimulatedObjectsLayer);
                var setMethod = previewLayersProperty.SetMethod;
                setMethod.Invoke(null, new[] { (object)newLayers });
            }
        }

        Camera TryGetSimulatedCamera() { return m_QuerySimulationModule.simulating ? SimulatedCamera : null; }

        /// <summary>
        /// Checks whether an object is a simulated object.
        /// </summary>
        /// <param name="obj"> The gameobject to check. </param>
        /// <returns></returns>
        public static bool IsSimulatedObject(GameObject obj) { return obj != null && obj.layer == SimulatedObjectsLayer; }

        /// <summary>
        /// Returns the original version of an ISimulatable copied to the simulation scene, if there is one
        /// </summary>
        /// <param name="copy">The ISimulatable in the simulation scene</param>
        /// <returns></returns>
        public ISimulatable GetOriginalSimulatable(ISimulatable copy)
        {
            ISimulatable original;
            return m_CopiedToOriginalSimulatables.TryGetValue(copy, out original) ? original : null;
        }

        /// <summary>
        /// Returns the simulation scene copy of an ISimulatable, if there is one
        /// </summary>
        /// <param name="original">The original version of a copied ISimulatable</param>
        /// <returns></returns>
        public ISimulatable GetCopiedSimulatable(ISimulatable original)
        {
            ISimulatable copy;
            return m_OriginalToCopiedSimulatables.TryGetValue(original, out copy) ? copy : null;
        }

        /// <summary>
        /// Returns the simulation scene copy of a Transform, if there is one
        /// </summary>
        /// <param name="original">The original version of a copied Transform</param>
        /// <returns></returns>
        public Transform GetCopiedTransform(Transform original)
        {
            Transform copy;
            return m_OriginalToCopiedTransforms.TryGetValue(original, out copy) ? copy : null;
        }

        /// <summary>
        /// Returns the original scene copy of a simulation scene Transform, if there is one
        /// </summary>
        /// <param name="copy">The copied version of a Transform</param>
        /// <returns></returns>
        public Transform GetOriginalTransform(Transform copy)
        {
            Transform original;
            return m_CopiedToOriginalTransforms.TryGetValue(copy, out original) ? original : null;
        }


        /// <summary>
        /// Returns the original scene copy of a simulation scene Object, if there is one being managed
        /// </summary>
        /// <param name="copy">The copied version of an object</param>
        /// <returns></returns>
        public UnityObject GetOriginalObject(UnityObject copy)
        {
            UnityObject originalObj = null;
            var simulatable = copy as ISimulatable;
            if (simulatable != null)
                originalObj = (UnityObject)GetOriginalSimulatable(simulatable);

            if (originalObj == null)
            {
                var tf = copy as Transform;
                if (tf != null)
                    originalObj = GetOriginalTransform(tf);
            }

            if (originalObj == null)
            {
                var go = copy as GameObject;
                if (go != null)
                {
                    var originalTransform = GetOriginalTransform(go.transform);
                    if (originalTransform != null)
                        originalObj = originalTransform.gameObject;
                }
            }

            if (originalObj == null)
            {
                var component = copy as Component;
                if (component != null)
                {
                    var type = component.GetType();
                    var index = component.GetComponents(type).ToList().IndexOf(component);
                    var originalTransform = GetOriginalTransform(component.transform);
                    if (originalTransform != null)
                        originalObj = originalTransform.GetComponents(type)[index];
                }
            }

            return originalObj;
        }

        /// <summary>
        /// Sets flags that the active scene has been modified and simulation should restart.
        /// This guarantees that the scene contents will be copied to the simulation scene for the next simulation.
        /// </summary>
        public void DirtySimulatableScene()
        {
            m_SimulatableSceneDirty = true;
            SimulationSyncedWithScene = false;
            if (SimulationSettings.instance.AutoSyncWithSceneChanges)
                m_QuerySimulationModule.RestartSimulationIfNeeded();
        }

        /// <summary>
        /// Cleans up objects from the last simulation and sets up objects for a new simulation.
        /// If the active scene and its contents have not changed since the last simulation, this will preserve objects
        /// from the last simulation (aside from objects spawned during simulation). Otherwise the previous objects will
        /// be destroyed and replaced with new copies from the active scene.
        /// <param name="videoSimulation">Is this a simulation with a video environment?</param>
        /// <param name="subscribers">List that will be populated with functionality subscribers among the simulated objects</param>
        /// </summary>
        internal void SetupSimulatables(bool videoSimulation, List<IFunctionalitySubscriber> subscribers)
        {
            foreach (var behaviour in m_SimulatableBehaviours)
            {
                CleanupBehaviour(behaviour);
            }

            foreach (var behaviour in m_SpawnedSimulatableBehaviours)
            {
                CleanupBehaviour(behaviour);

                if (behaviour != null)
                    m_CopiedToOriginalSimulatables.Remove((ISimulatable)behaviour);
            }

            foreach (var gameObject in m_SpawnedSimulatableObjects)
            {
                if (gameObject == null)
                    continue;

                m_CopiedToOriginalTransforms.Remove(gameObject.transform);
                UnityObject.DestroyImmediate(gameObject);
            }

            var copySimulatables = m_SimulatableSceneDirty || m_SimulatableBehaviours.Count == 0 ||
                videoSimulation && !m_PreviousVideoSimulation; // Always copy simulatables when switching to video sim, so face features run

            m_PreviousVideoSimulation = videoSimulation;
            if (!copySimulatables)
            {
                foreach (var behaviour in m_SimulatableBehaviours)
                {
                    if (behaviour == null)
                    {
                        // Cannot reuse simulatables that have been destroyed
                        copySimulatables = true;
                        break;
                    }
                }
            }

            var logging = MARSDebugSettings.SimObjectsManagerLogging;
            if (copySimulatables)
            {
                if (logging)
                    Debug.Log("Destroy and copy simulatables");

                m_SimulatableSceneDirty = false;
                CopySimulatablesToSimulationScene();
            }
            else
            {
                if (logging)
                    Debug.Log("Preserve simulatables");

                var startingPose = m_EnvironmentManager.DeviceStartingPose;
                SimulatedCamera.transform.SetLocalPose(startingPose);
            }

            m_SpawnedSimulatableObjects.Clear();
            m_SpawnedSimulatableBehaviours.Clear();
            SimulatedContentRoot.GetComponentsInChildren(subscribers);
            if (!videoSimulation)
            {
                // Ignore face subscribers if not using video environment to avoid activating face tracking, which takes over the view
                for (var i = subscribers.Count - 1; i >= 0; --i)
                {
                    var subscriber = subscribers[i];
                    if (subscriber is IFaceFeature)
                        subscribers.RemoveAt(i);
                }

                for (var i = m_SimulatableBehaviours.Count - 1; i >= 0; --i)
                {
                    var behaviour = m_SimulatableBehaviours[i];
                    if (behaviour is IFaceFeature)
                        m_SimulatableBehaviours.RemoveAt(i);
                }
            }

            SimulationSyncedWithScene = true;
        }

        /// <summary>
        /// Starts running the current simulated behaviours in edit mode. This triggers OnEnable for the behaviours.
        /// </summary>
        internal void StartRunningSimulatables()
        {
            // We disable each simulatable before running it, and then enable it if it was enabled. This prevents
            // the possibility of OnDisable being called right before OnEnable.
            foreach (var behaviour in m_SimulatableBehaviours)
            {
                var wasEnabled = behaviour.enabled;
                if (wasEnabled)
                    behaviour.enabled = false;

                behaviour.runInEditMode = true;
                if (wasEnabled)
                    behaviour.enabled = true;
            }
        }

        /// <summary>
        /// Adds the given Game Object to the current simulation and starts running its ISimulatable behaviours
        /// </summary>
        /// <param name="gameObject">The Game Object to add to simulation</param>
        public void AddSpawnedObjectToSimulation(GameObject gameObject)
        {
            if (gameObject.transform.parent == null && SimulatedContentRoot != null)
            {
                m_SimulationSceneModule.AddContentGameObject(gameObject);
                gameObject.transform.SetParent(SimulatedContentRoot.transform, true);
                gameObject.SetLayerAndAddToHideFlagsRecursively(SimulatedObjectsLayer, SimulatedObjectHideFlags);
            }

            k_Subscribers.Clear();
            gameObject.GetComponentsInChildren(k_Subscribers);
            m_QuerySimulationModule.functionalityIsland.InjectPreparedFunctionality(k_Subscribers);

            // Because GameObjects may be created in Awake, AddSpawnedObjectToSimulation can be called recursively, thus
            // we cannot re-use the list of simulatables as it may be modified while iterating
            var simulatablesList = CollectionPool<List<ISimulatable>, ISimulatable>.GetCollection();
            gameObject.GetComponentsInChildren(simulatablesList);
            foreach (var simulatable in simulatablesList.Cast<MonoBehaviour>())
            {
                m_SpawnedSimulatableBehaviours.Add(simulatable);
                simulatable.StartRunInEditMode();
            }

            m_SpawnedSimulatableObjects.Add(gameObject);
            CollectionPool<List<ISimulatable>, ISimulatable>.RecycleCollection(simulatablesList);
        }

        /// <summary>
        /// Stops running the current simulated behaviours in edit mode. This triggers OnDisable for the behaviours.
        /// </summary>
        internal void StopRunningSimulatables()
        {
            foreach (var behaviour in m_SimulatableBehaviours)
            {
                if (behaviour != null)
                    behaviour.StopRunInEditMode();
            }

            foreach (var behaviour in m_SpawnedSimulatableBehaviours)
            {
                if (behaviour != null)
                    behaviour.StopRunInEditMode();
            }
        }

        /// <summary>
        /// Stops running the current simulated behaviours in edit mode.
        /// Unlike <see cref="StopRunningSimulatables"/> this does not trigger OnDisable for the behaviours.
        /// </summary>
        internal void StopRunningSimulatablesOneShot()
        {
            foreach (var behaviour in m_SimulatableBehaviours)
            {
                StopRunningBehaviourOneShot(behaviour);
            }

            foreach (var behaviour in m_SpawnedSimulatableBehaviours)
            {
                StopRunningBehaviourOneShot(behaviour);
            }
        }

        internal void CleanupSimulation()
        {
            foreach (var behaviour in m_SimulatableBehaviours)
            {
                CleanupBehaviour(behaviour);
            }

            foreach (var behaviour in m_SpawnedSimulatableBehaviours)
            {
                CleanupBehaviour(behaviour);
            }

            DestroySimulatedObjectsRoot();

            m_SimulatableBehaviours.Clear();
            m_SpawnedSimulatableBehaviours.Clear();
            m_SpawnedSimulatableObjects.Clear();
            m_CopiedToOriginalSimulatables.Clear();
            m_OriginalToCopiedSimulatables.Clear();
            m_CopiedToOriginalTransforms.Clear();
            m_OriginalToCopiedTransforms.Clear();
        }

        static void CleanupBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour == null)
                return;

            if (behaviour.runInEditMode)
            {
                behaviour.StopRunInEditMode();
                return;
            }

            if (!behaviour.enabled)
                return;

            TryCallOnDisable(behaviour);
        }

        static void TryCallOnDisable(MonoBehaviour behaviour)
        {
            var type = behaviour.GetType();

            MethodInfo method;
            if (!k_OnDisableMethods.TryGetValue(type, out method))
            {
                method = type.GetMethod("OnDisable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                k_OnDisableMethods[type] = method;
            }

            if (method != null)
                method.Invoke(behaviour, null);
        }

        static void StopRunningBehaviourOneShot(MonoBehaviour behaviour)
        {
            if (behaviour == null)
                return;

            // Force queries to have a state of Unavailable if they are not tracking, so that it is clear
            // which queries did not find a match in the one-shot simulation.
            var realWorldObject = behaviour as Proxy;
            if (realWorldObject != null && realWorldObject.queryState != QueryState.Tracking)
                realWorldObject.UpdateQueryState(QueryState.Unavailable, null, true);

            var set = behaviour as ProxyGroup;
            if (set != null && set.queryState != QueryState.Tracking)
                set.UpdateQueryState(QueryState.Unavailable, true);

            behaviour.StopAllCoroutines();
            behaviour.runInEditMode = false;
        }

        void CopySimulatablesToSimulationScene()
        {
            Profiler.BeginSample(ProfilerLabels.CopySimulatablesToSimulationScene);
            // Destroy old copied objects and objects spawned by queries.
            DestroySimulatedObjectsRoot();

            // If anything about the session changes in EnsureRuntimeState, it tries to dirty the simulatable scene
            // which would trigger another simulation. To prevent that we temporarily clear the delegate.
            EditorOnlyDelegates.DirtySimulatableScene = null;
            MARSSession.EnsureRuntimeState();
            EditorOnlyDelegates.DirtySimulatableScene = DirtySimulatableScene;
            var session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());

            // Find all the root game objects in the active scene and move them under a root transform.
            var activeScene = SceneManager.GetActiveScene();
            k_ActiveSceneGameObjects.Clear();
            activeScene.GetRootGameObjects(k_ActiveSceneGameObjects);
            SimulatedContentRoot = new GameObject(k_TemporaryRootName);
            foreach (var gameObject in k_ActiveSceneGameObjects)
            {
                gameObject.transform.SetParent(SimulatedContentRoot.transform, true);
            }

            k_OriginalSimulatables.Clear();
            SimulatedContentRoot.GetComponentsInChildren(k_OriginalSimulatables);

            k_OriginalTransforms.Clear();
            SimulatedContentRoot.GetComponentsInChildren(k_OriginalTransforms);
            k_OriginalTransforms.RemoveAt(0); // Ignore the root

            // Copy the root transform to the environment scene. The reason we copy simulatables this way rather than
            // copying them individually is so that their references to other simulatables stay intact.
            var copiedSimulatedContentRoot = GameObjectUtils.CloneWithHideFlags(SimulatedContentRoot);
            copiedSimulatedContentRoot.name = k_SimulatedContentRootName;

            // After copying, we need to move original gameobjects back to the scene root.
            foreach (var gameObject in k_ActiveSceneGameObjects)
            {
                gameObject.transform.SetParent(null, true);
            }

            // Even though we call EnsureRuntimeState above, it's possible the session could end up not being the first sibling
            // after we move simulatables back to their original parents (if the session was not the first sibling initially).
            if (session.transform.GetSiblingIndex() != 0)
                session.transform.SetAsFirstSibling();

            UnityObjectUtils.Destroy(SimulatedContentRoot);
            SimulatedContentRoot = copiedSimulatedContentRoot;

            // Remove the copied MARS Session to avoid conflicts
            var copiedSession = SimulatedContentRoot.GetComponentInChildren(typeof(MARSSession));
            UnityObjectUtils.Destroy(copiedSession);

            m_SimulationSceneModule.AddContentGameObject(SimulatedContentRoot);

            k_CopiedTransforms.Clear();
            SimulatedContentRoot.GetComponentsInChildren(k_CopiedTransforms);
            k_CopiedTransforms.RemoveAt(0); // Ignore the root
            var copiedTransformsCount = k_CopiedTransforms.Count;
            Debug.Assert(k_OriginalTransforms.Count == copiedTransformsCount,
                "Copied Transforms in the simulation scene do not map 1:1 with original Transforms in the query scene.");

            m_OriginalToCopiedTransforms.Clear();
            m_CopiedToOriginalTransforms.Clear();
            for (var i = 0; i < copiedTransformsCount; ++i)
            {
                AddTransformCopy(k_CopiedTransforms[i], k_OriginalTransforms[i]);
            }

            m_SimulatableBehaviours.Clear();
            k_CopiedSimulatables.Clear();
            SimulatedContentRoot.GetComponentsInChildren(k_CopiedSimulatables);
            var copiedSimulatablesCount = k_CopiedSimulatables.Count;
            Debug.Assert(k_OriginalSimulatables.Count == copiedSimulatablesCount,
                "Copied ISimulatables in the simulation scene do not map 1:1 with original ISimulatables in the query scene.");

            m_CopiedToOriginalSimulatables.Clear();
            m_OriginalToCopiedSimulatables.Clear();
            m_LandmarkControllers.Clear();
            for (var i = 0; i < copiedSimulatablesCount; i++)
            {
                var originalSimulatable = k_OriginalSimulatables[i];
                var copiedSimulatable = k_CopiedSimulatables[i];
                var behaviour = copiedSimulatable as MonoBehaviour;
                if (behaviour != null)
                    m_SimulatableBehaviours.Add(behaviour);

                var landmarkController = originalSimulatable as LandmarkController;
                if (landmarkController)
                {
                    m_LandmarkControllers[landmarkController] = (LandmarkController)copiedSimulatable;
                }

                AddSimulatedCopy(copiedSimulatable, originalSimulatable);
            }

            k_CopiedLights.Clear();
            SimulatedContentRoot.GetComponentsInChildren(k_CopiedLights);
            // Content scene lights on shouldn't cast on the simulated environment
            foreach (var light in k_CopiedLights)
                light.cullingMask &= SimulatedLightingMask;

            // Set up simulation camera at the device starting position
            // with the correct settings for rendering the sim scene
            var originalCamera = session.cameraReference.gameObject;
            var copiedCameraTrans = GetCopiedTransform(originalCamera.transform);

            var startingPose = m_EnvironmentManager.DeviceStartingPose;
            copiedCameraTrans.SetLocalPose(startingPose);
            SimulatedCamera = copiedCameraTrans.GetComponent<Camera>();
            SimulatedCamera.cameraType = CameraType.SceneView;
            SimulatedCamera.tag = "Untagged"; // Copied camera should not be marked as the main camera
            SimulationSceneModule.instance.AssignCameraToSimulation(SimulatedCamera);

            // Disallow MSAA on copied camera to avoid tiled GPU perf warning
            // https://issuetracker.unity3d.com/issues/tiled-gpu-perf-warning-appears-when-multiple-cameras-with-allow-msaa-are-present-in-the-scene-and-viewport-rect-is-not-default
            SimulatedCamera.allowMSAA = false;

            if (OnSimulatedCameraCreated != null)
                OnSimulatedCameraCreated(SimulatedCamera);

#if UNITY_POST_PROCESSING_STACK_V2
            k_ContentPostProcessVolumeLayers.Clear();

            // Need to maintain the layer for post processing volumes in the sim scene
            SimulatedContentRoot.GetComponentsInChildren(k_ContentPostProcessVolumes);
            foreach (var volume in k_ContentPostProcessVolumes)
            {
                k_ContentPostProcessVolumeLayers.Add(volume, volume.gameObject.layer);
            }
#endif

            SimulatedContentRoot.SetLayerAndAddToHideFlagsRecursively(SimulatedObjectsLayer, SimulatedObjectHideFlags);

#if UNITY_POST_PROCESSING_STACK_V2
            // Restore the layer of post processing volumes
            foreach (var volume in k_ContentPostProcessVolumeLayers)
            {
                volume.Key.gameObject.layer = volume.Value;
            }

            k_ContentPostProcessVolumes.Clear();
            k_ContentPostProcessVolumeLayers.Clear();
#endif

            EntityVisualsModule.instance.simEntitiesRoot = SimulatedContentRoot.transform;

            foreach (var contentHierarchy in Resources.FindObjectsOfTypeAll<ContentHierarchyPanel>())
            {
                contentHierarchy.RestoreState();
            }

            Profiler.EndSample();
        }

        internal void AddSimulatedCopy(ISimulatable copiedSimulatable, ISimulatable originalSimulatable)
        {
            m_CopiedToOriginalSimulatables[copiedSimulatable] = originalSimulatable;
            m_OriginalToCopiedSimulatables[originalSimulatable] = copiedSimulatable;
        }

        void AddTransformCopy(Transform copiedTransform, Transform originalTransform)
        {
            m_CopiedToOriginalTransforms[copiedTransform] = originalTransform;
            m_OriginalToCopiedTransforms[originalTransform] = copiedTransform;
        }

        void AddSpawnedTransformToSimulationManager(Transform spawnedTransform, Transform originalTransform)
        {
            m_CopiedToOriginalTransforms[spawnedTransform] = GetOriginalTransform(originalTransform);
        }

        void AddSpawnedSimulatableToSimulationManager(ISimulatable spawnedSimulatable, ISimulatable originalsSimulatable)
        {
            m_CopiedToOriginalSimulatables[spawnedSimulatable] = GetOriginalSimulatable(originalsSimulatable);
        }

        void DestroySimulatedObjectsRoot()
        {
            if (SimulatedContentRoot == null)
                return;

            if (SimulatedCamera != null)
                SimulationSceneModule.instance.RemoveCameraFromSimulation(SimulatedCamera);

            foreach (var contentHierarchy in Resources.FindObjectsOfTypeAll<ContentHierarchyPanel>())
            {
                contentHierarchy.CacheState();
            }

            UnityObjectUtils.Destroy(SimulatedContentRoot);
            SimulatedContentRoot = null;
        }

        internal void UpdateLandmarkChildren()
        {
            foreach (var kvp in m_LandmarkControllers)
            {
                var originalLandmark = kvp.Key;
                var copyLandmark = kvp.Value;

                if (originalLandmark == null || copyLandmark == null)
                    continue;

                var copyChildCount = copyLandmark.transform.childCount;
                var originalChildCount = originalLandmark.transform.childCount;

                if (originalChildCount != copyChildCount)
                {
                    DirtySimulatableScene();
                    continue;
                }

                for (var j = 0; j < copyChildCount; j++)
                {
                    var originalChild = originalLandmark.transform.GetChild(j);
                    var copyChild = copyLandmark.transform.GetChild(j);

                    if (copyChild.GetComponent<ILandmarkOutput>() != null)
                        continue;

                    copyChild.localPosition = originalChild.localPosition;
                    copyChild.localRotation = originalChild.localRotation;
                    copyChild.localScale = originalChild.localScale;
                }
            }
        }

        UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (!SimulationSceneModule.UsingSimulation)
                return modifications;

            foreach (var modification in modifications)
            {
                var gameObject = modification.currentValue.target as GameObject;
                if (gameObject == null)
                {
                    var component = modification.currentValue.target as Component;
                    if (component != null)
                        gameObject = component.gameObject;
                }
                else if (gameObject.GetComponent<ISimulatable>() != null)
                {
                    // GetComponentInParent ignores inactive GameObjects, so we need this check to
                    // handle the case in which a simulatable's GameObject has been deactivated.
                    if (OnSimulatableObjectChanged(gameObject))
                        break;
                }

                // Don't bother checking for other modifications if we have already reset the timer for simulatable changes.
                if (gameObject != null && gameObject.GetComponentInParent<ISimulatable>() != null && OnSimulatableObjectChanged(gameObject))
                    break;

                // Camera offset comes from the Transform of the MARSSession, so if we're targeting it that means
                // this is a camera offset modification
                var transformTarget = modification.currentValue.target as Transform;
                if (transformTarget == null)
                    continue;

                if (MARSSession.Instance == null || transformTarget.gameObject != MARSSession.Instance.gameObject)
                    continue;

                StartOffsetChangeTask();
            }
            return modifications;
        }

        void OnUndoRedoPerformed()
        {
            if (!SimulationSceneModule.UsingSimulation)
                return;

            var gameObject = Selection.activeGameObject;
            if (gameObject != null && gameObject.GetComponentInParent<ISimulatable>() != null && OnSimulatableObjectChanged(gameObject))
                return;

            if (MARSSession.Instance != null && gameObject == MARSSession.Instance.gameObject)
                StartOffsetChangeTask();
        }

        bool OnSimulatableObjectChanged(GameObject simulatableObject)
        {
            // Don't re-evaluate queries if the object is a child of a LandmarkController
            if (simulatableObject.GetComponentInParent<LandmarkController>())
                return false;

            // Don't re-evaluate queries if it was an object in the simulation scene that was touched
            if (simulatableObject.layer != SimulatedObjectsLayer)
            {
                StartSimulatableChangeTask();
                return true;
            }

            return false;
        }

        void StartSimulatableChangeTask()
        {
            // We only re-evaluate queries if a certain amount of time has passed without any change happening.
            // This keeps us from rapidly re-evaluating queries as, for example, a slider is dragged.
            SimulationSyncedWithScene = false;
            m_SimulatableChangeFinalizedTask = new SlowTask
            {
                sleepTime = SimulationSettings.timeToFinalizeQueryDataChange,
                lastExecutionTime = Time.time,
                task = OnSimulatableChangeFinalized
            };
        }

        void OnSimulatableChangeFinalized()
        {
            m_SimulatableChangeFinalizedTask = null;
            DirtySimulatableScene();
        }

        void StartOffsetChangeTask()
        {
            SimulationSyncedWithScene = false;
            m_OffsetChangeFinalizedTask = new SlowTask
            {
                sleepTime = SimulationSettings.timeToFinalizeQueryDataChange,
                lastExecutionTime = Time.time,
                task = OnOffsetChangeFinalized
            };
        }

        void OnOffsetChangeFinalized()
        {
            m_OffsetChangeFinalizedTask = null;
            DirtySimulatableScene();
            Vector3 translation;
            Quaternion rotation;
            Vector3 scale;
            var session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            if (session != null)
            {
                var sessionTrans = session.transform;
                translation = sessionTrans.position;
                rotation = sessionTrans.rotation.ConstrainYawNormalized();
                scale = Vector3.one * sessionTrans.localScale.x;
            }
            else
            {
                translation = Vector3.zero;
                rotation = Quaternion.identity;
                scale = Vector3.one;
            }

            m_EnvironmentManager.OffsetEnvironment(translation, rotation, scale);
        }

        void Update()
        {
            // Must update Time.time for slow tasks
            EditorApplication.QueuePlayerLoopUpdate();

            if (m_SimulatableChangeFinalizedTask != null)
                m_SimulatableChangeFinalizedTask.Update(Time.time);

            if (m_OffsetChangeFinalizedTask != null)
                m_OffsetChangeFinalizedTask.Update(Time.time);
        }

        void PrefabInstanceReverted()
        {
            StartSimulatableChangeTask();
        }

        void OnSimulationSceneDestroyed()
        {
            if (SimulationSceneModule.isAssemblyReloading)
            {
                // When a simulated object is destroyed during assembly reload, if it has an open Editor that Editor will
                // stick around after reload and it will get null reference exceptions in OnEnable.
                // To avoid this we temporarily move the object to the active scene (so it doesn't get destroyed with the
                // sim scene) and delay destroying it until after reload.
                var logging = MARSDebugSettings.SimObjectsManagerLogging;
                k_UniqueDestroyAfterReloadObjects.Clear();
                foreach (var editor in Resources.FindObjectsOfTypeAll<Editor>())
                {
                    var targets = editor.targets;
                    foreach (var target in targets)
                    {
                        var targetGameObject = target as GameObject;
                        if (targetGameObject == null || k_UniqueDestroyAfterReloadObjects.Contains(targetGameObject))
                            continue;

                        if (targetGameObject.scene != m_SimulationSceneModule.ContentScene &&
                            targetGameObject.scene != m_SimulationSceneModule.EnvironmentScene)
                        {
                            continue;
                        }

                        if (logging)
                        {
                            Debug.Log($"Found open Editor for simulated object '{targetGameObject.name}'. " +
                                "Temporarily moving to active scene before assembly reloads.");
                        }

                        targetGameObject.transform.parent = null;
                        SceneManager.MoveGameObjectToScene(targetGameObject, SceneManager.GetActiveScene());
                        m_DestroyAfterReloadObjects.Add(targetGameObject);
                        k_UniqueDestroyAfterReloadObjects.Add(targetGameObject);
                    }
                }

                k_UniqueDestroyAfterReloadObjects.Clear();
                CleanupSimulation();
                return; 
            }

            CleanupSimulation();
        }

        internal bool WillDestroyAfterAssemblyReload(GameObject gameObject)
        {
            return m_DestroyAfterReloadObjects.Contains(gameObject);
        }
    }
}
