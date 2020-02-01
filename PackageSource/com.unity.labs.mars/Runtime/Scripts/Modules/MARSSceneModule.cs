using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    [ModuleBehaviorCallbackOrder(ModuleOrders.SceneBehaviorOrder)]
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    public class MARSSceneModule : ScriptableSettings<MARSSceneModule>, IModuleBehaviorCallbacks, IUsesCameraOffset,
        IModuleDependency<FunctionalityInjectionModule>
    {
        [SerializeField]
        [Tooltip("When enabled, the simulated environment will be added in play mode, and a simulation functionality island will be used.")]
        bool m_SimulateInPlayMode = true;

#pragma warning disable 649
        [SerializeField]
        [Tooltip("When SimulateInPlayMode is enabled and SimulateDiscovery is disabled, this island will be used for functionality.")]
        FunctionalityIsland m_SimulationIsland;
#pragma warning restore 649

        [SerializeField]
        [Tooltip("When enabled and SimulateInPlayMode is enabled, the simulated discovery functionality island will be used.")]
        bool m_SimulateDiscovery = true;

#pragma warning disable 649
        [SerializeField]
        [Tooltip("When SimulateInPlayMode and SimulateDiscovery are enabled, this island will be used for functionality.")]
        FunctionalityIsland m_SimulatedDiscoveryIsland;
#pragma warning restore 649

        FunctionalityInjectionModule m_FIModule;

        public bool simulateInPlaymode { get { return m_SimulateInPlayMode; } set { m_SimulateInPlayMode = value; } }

        public bool simulateDiscovery { get { return m_SimulateDiscovery; } set { m_SimulateDiscovery = value; } }

        public static bool simulatedDiscoveryInPlayMode
        {
            get { return instance != null && instance.m_SimulateInPlayMode && instance.m_SimulateDiscovery; }
        }

#if !FI_AUTOFILL
        public IProvidesCameraOffset provider { get; set; }
#endif

        static readonly List<MonoBehaviour> k_MonoBehaviours = new List<MonoBehaviour>();
        static readonly List<object> k_MonoBehaviourObjects = new List<object>();

        public void LoadModule() { }

        public void UnloadModule() { }

        public void ConnectDependency(FunctionalityInjectionModule dependency)
        {
            m_FIModule = dependency;
            // TODO: Collect all scenes and add islands if modules persist between scene loads
#if UNITY_EDITOR
            var session = Application.isPlaying ?
                MARSSession.Instance :
                EditorOnlyDelegates.GetMARSSession(SceneManager.GetActiveScene());
#else
            var session = MARSSession.Instance;
#endif

            // TODO: Don't load modules for non-MARS scenes
            if (session == null)
                return;

            FunctionalityIsland island = null;

            if (session.island)
                island = session.island;

#if UNITY_EDITOR
            if (Application.isPlaying && m_SimulateInPlayMode)
            {
                if (m_SimulateDiscovery)
                {
                    if (m_SimulatedDiscoveryIsland == null)
                    {
                        Debug.LogWarning("There is no simulated discovery island set in the MARSSceneModule to be used for play mode simulation");
                        return;
                    }

                    island = m_SimulatedDiscoveryIsland;
                }
                else
                {
                    if (m_SimulationIsland == null)
                    {
                        Debug.LogWarning("There is no simulation island set in the MARSSceneModule to be used for play mode simulation");
                        return;
                    }

                    island = m_SimulationIsland;
                }
            }
#endif
            if (island)
                dependency.AddIsland(island);
        }

        /// <summary>
        /// Gets all MonoBehaviours in the scene, including ones on inactive objects
        /// </summary>
        static void GetAllMonoBehaviors()
        {
            k_MonoBehaviours.Clear();
            k_MonoBehaviourObjects.Clear();
            var activeScene = SceneManager.GetActiveScene();
            foreach (var gameObject in activeScene.GetRootGameObjects())
            {
                GetMonoBehaviorsRecursively(gameObject);
            }
        }

        static void GetMonoBehaviorsRecursively(GameObject gameObject)
        {
            gameObject.GetComponents(k_MonoBehaviours);
            foreach (var behaviour in k_MonoBehaviours)
            {
                k_MonoBehaviourObjects.Add(behaviour);
            }

            foreach (Transform child in gameObject.transform)
            {
                GetMonoBehaviorsRecursively(child.gameObject);
            }
        }

        public void OnBehaviorAwake()
        {
            if (!Application.isPlaying)
                return;

            var session = MARSSession.Instance;
            if (session == null)
                return;

            GetAllMonoBehaviors();
            var behaviors = k_MonoBehaviourObjects;
            var providers = new List<IFunctionalityProvider>();
            var subscribers = new List<IFunctionalitySubscriber>();
            var providerTypes = new HashSet<Type>();
            var subscriberTypes = new HashSet<Type>();
            subscriberTypes.Add(typeof(MARSSceneModule));
            foreach (var behavior in behaviors)
            {
                // Exclude modules as they have already been set up for FI
                if (behavior is IModule)
                    continue;

                var functionalityProvider = behavior as IFunctionalityProvider;
                if (functionalityProvider != null)
                {
                    providers.Add(functionalityProvider);
                    providerTypes.Add(functionalityProvider.GetType());
                }

                var subscriber = behavior as IFunctionalitySubscriber;
                if (subscriber != null)
                {
                    subscribers.Add(subscriber);
                    subscriberTypes.Add(subscriber.GetType());
                }
            }

            if (MARSDebugSettings.sceneModuleLogging)
            {
                Debug.Log(string.Format("Scene Module found {0} providers with types: {1}", providers.Count,
                    string.Join(",", providerTypes.Select(type => type.Name).ToArray())));
                Debug.Log(string.Format("Scene Module found {0} subscribers with types: {1}", subscribers.Count,
                    string.Join(",", subscriberTypes.Select(type => type.Name).ToArray())));
            }

            // TODO: notify other modules (i.e. backend) that the active island has changed
#if UNITY_EDITOR
            var useSimulationIsland = Application.isPlaying && simulateInPlaymode;
            if (useSimulationIsland)
            {
                var simulationIsland = m_SimulateDiscovery ? m_SimulatedDiscoveryIsland : m_SimulationIsland;
                if (simulationIsland != null)
                    m_FIModule.SetActiveIsland(simulationIsland);
            }
            else
            {
                var island = session.island;
                if (island != null)
                    m_FIModule.SetActiveIsland(island);
            }
#else
            var island = session.island;
            if (island)
                m_FIModule.SetActiveIsland(island);
#endif
            var activeIsland = m_FIModule.activeIsland;
            activeIsland.AddProviders(providers);

            var newProviders = new List<IFunctionalityProvider>();
            activeIsland.SetupDefaultProviders(subscriberTypes, newProviders);

            var definitions = new HashSet<TraitDefinition>();
            foreach (var requirement in session.requirements.TraitRequirements)
            {
                definitions.Add(requirement);
            }

            activeIsland.SetupDefaultProviders(definitions, newProviders);
            activeIsland.RequireProviders(definitions, newProviders);
            behaviors.AddRange(newProviders);
            activeIsland.InjectFunctionality(behaviors);
            activeIsland.InjectFunctionalitySingle(this);

#if UNITY_EDITOR
            if (EditorOnlyDelegates.CullEnvironmentFromSceneLights != null)
            {
                for (var i = 0; i < SceneManager.sceneCount; i++)
                {
                    EditorOnlyDelegates.CullEnvironmentFromSceneLights(SceneManager.GetSceneAt(i));
                }
            }
#endif

            // Update the scale provider to the scene's session scale in case it needs to cache this value
            this.SetCameraScale(session.transform.localScale.x);

#if UNITY_EDITOR
            var openSimulationScene = EditorOnlyDelegates.OpenSimulationScene;
            if (openSimulationScene != null && simulateInPlaymode)
                openSimulationScene();
#endif
        }

        public void OnBehaviorEnable() { }
        public void OnBehaviorDestroy() { }
        public void OnBehaviorStart() { }
        public void OnBehaviorUpdate() { }
        public void OnBehaviorDisable() { }
    }
}
