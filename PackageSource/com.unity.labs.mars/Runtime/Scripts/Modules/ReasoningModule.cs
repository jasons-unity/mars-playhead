using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    [ModuleBehaviorCallbackOrder(ModuleOrders.ReasoningBehaviorOrder)]
    [ModuleUnloadOrder(ModuleOrders.DatabaseLoadOrder - 2)]
    public class ReasoningModule : ScriptableSettings<ReasoningModule>, IModuleBehaviorCallbacks, IModuleMarsUpdate,
        IModuleDependency<FunctionalityInjectionModule>, IUsesSlowTasks
    {
        [SerializeField]
        [Tooltip("All objects that interact with the Data API.")]
        List<ScriptableObject> m_ReasoningApis = new List<ScriptableObject>();

        FunctionalityInjectionModule m_FIModule;

        readonly HashSet<IReasoningAPI> m_ReasoningAPISet = new HashSet<IReasoningAPI>();

#if !FI_AUTOFILL
        IProvidesSlowTasks IFunctionalitySubscriber<IProvidesSlowTasks>.provider { get; set; }
#endif

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly HashSet<TraitDefinition> k_RequiredTraits = new HashSet<TraitDefinition>();
        static readonly HashSet<TraitDefinition> k_ProvidedTraits = new HashSet<TraitDefinition>();
        static readonly List<object> k_Subscribers = new List<object>();

        public void LoadModule()
        {
        }

        public void UnloadModule()
        {
            TearDownReasoningAPIs();
        }

        public void ConnectDependency(FunctionalityInjectionModule dependency)
        {
            m_FIModule = dependency;
        }

        void OnActiveIslandChanged(FunctionalityIsland island)
        {
            ResetReasoningAPIs();
        }

        internal void ResetReasoningAPIs()
        {
            TearDownReasoningAPIs();
            SetupReasoningAPIs();
        }

        void SetupReasoningAPIs()
        {
            var isPlaying = Application.isPlaying;
#if UNITY_EDITOR
            var session = isPlaying ?
                MARSSession.Instance :
                EditorOnlyDelegates.GetMARSSession(SceneManager.GetActiveScene());
#else
            var session = MARSSession.Instance;
#endif

            if (session == null)
                return;

            k_RequiredTraits.Clear();
            foreach (var requirement in session.requirements.TraitRequirements)
            {
                k_RequiredTraits.Add(requirement);
            }

            var activeIsland = m_FIModule.activeIsland;
            k_ProvidedTraits.Clear();
            activeIsland.GetProvidedTraits(k_ProvidedTraits);

            k_Subscribers.Clear();
            while (CheckMissingTraits(k_RequiredTraits, k_ProvidedTraits) > 0)
            {
                var clientAdded = false;
                foreach (var scriptableObject in m_ReasoningApis)
                {
                    var reasoningAPI = scriptableObject as IReasoningAPI;
                    if (reasoningAPI == null)
                    {
                        Debug.LogWarning(scriptableObject + " does not implement IReasoningAPI");
                        continue;
                    }

                    // force this reasoning api to be in the set for now - it doesn't provide traits,
                    // so it otherwise will not get spun up.
                    if (reasoningAPI.GetType() == typeof(GeoDuplicationReasoningAPI))
                    {
                        m_ReasoningAPISet.Add(reasoningAPI);
                        k_Subscribers.Add(reasoningAPI);
                        continue;
                    }

                    var providedTraits = reasoningAPI.GetProvidedTraits();
                    if (CheckForRequiredTraits(k_RequiredTraits, k_ProvidedTraits, providedTraits))
                    {
                        k_ProvidedTraits.UnionWith(providedTraits);

                        var traitUser = reasoningAPI as IRequiresTraits;
                        if (traitUser != null)
                        {
                            var requiredTraits = traitUser.GetRequiredTraits();
                            foreach (var requirement in requiredTraits)
                            {
                                k_RequiredTraits.Add(requirement);
                            }
                        }

                        m_ReasoningAPISet.Add(reasoningAPI);
                        k_Subscribers.Add(reasoningAPI);

                        clientAdded = true;
                    }

                }

                if (!clientAdded)
                    break;
            }

            // The active island will never be used in simulation, so only do provider setup in play mode
            if (isPlaying)
                activeIsland.SetupDefaultProviders(k_RequiredTraits);

            activeIsland.InjectFunctionality(k_Subscribers);
            foreach (var reasoningAPI in m_ReasoningAPISet)
            {
                reasoningAPI.Setup();
                this.AddMarsTimeSlowTask(reasoningAPI.ProcessScene, reasoningAPI.processSceneInterval);
            }
        }

        static int CheckMissingTraits(HashSet<TraitDefinition> requiredTraits, HashSet<TraitDefinition> providedTraits)
        {
            var count = 0;
            foreach (var trait in requiredTraits)
            {
                if (!providedTraits.Contains(trait))
                    count++;
            }

            return count;
        }

        static bool CheckForRequiredTraits(HashSet<TraitDefinition> requiredTraits, HashSet<TraitDefinition> providedTraits, TraitDefinition[] traitList)
        {
            foreach (var trait in traitList)
            {
                if (!providedTraits.Contains(trait) && requiredTraits.Contains(trait))
                    return true;
            }

            return false;
        }

        void TearDownReasoningAPIs()
        {
            foreach (var reasoningAPI in m_ReasoningAPISet)
            {
                this.RemoveMarsTimeSlowTask(reasoningAPI.ProcessScene);
                reasoningAPI.TearDown();
            }

            m_ReasoningAPISet.Clear();
        }

        /// <summary>
        /// Allows a reasoning API to be dynamically added to the scene
        /// </summary>
        /// <param name="reasoningAPI"></param>
        public void AddReasoningAPI(IReasoningAPI reasoningAPI)
        {
            m_ReasoningAPISet.Add(reasoningAPI);
            m_FIModule.activeIsland.InjectFunctionalitySingle(reasoningAPI);
            reasoningAPI.Setup();
            this.AddMarsTimeSlowTask(reasoningAPI.ProcessScene, reasoningAPI.processSceneInterval);
        }

        /// <summary>
        /// Removes a reasoning API from being processed in the scene
        /// </summary>
        public void RemoveReasoningAPI(IReasoningAPI reasoningAPI)
        {
            m_ReasoningAPISet.Remove(reasoningAPI);
            reasoningAPI.TearDown();
            this.RemoveMarsTimeSlowTask(reasoningAPI.ProcessScene);
        }

        /// <summary>
        /// Allows a reasoning API to change its interval at runtime
        /// </summary>
        /// <param name="reasoningAPI">The reasoning API to update the interval for</param>
        public void ChangeReasoningAPIInterval(IReasoningAPI reasoningAPI)
        {
            // just replace the task with a new one with a new interval
            this.AddMarsTimeSlowTask(reasoningAPI.ProcessScene, reasoningAPI.processSceneInterval, true);
        }

        internal void ProcessReasoningAPIScenes()
        {
            foreach (var reasoningAPI in m_ReasoningAPISet)
            {
                reasoningAPI.ProcessScene();
            }
        }

        internal void UpdateReasoningAPIData()
        {
            foreach (var reasoningAPI in m_ReasoningAPISet)
            {
                reasoningAPI.UpdateData();
            }
        }

        public void OnBehaviorAwake()
        {
            SetupReasoningAPIs();
        }

        public void OnBehaviorEnable()
        {
            m_FIModule.activeIslandChanged += OnActiveIslandChanged;
        }

        public void OnBehaviorStart() {}

        public void OnBehaviorUpdate() { }

        public void OnBehaviorDisable()
        {
            m_FIModule.activeIslandChanged -= OnActiveIslandChanged;
        }

        public void OnBehaviorDestroy() {}

        public void OnMarsUpdate()
        {
            UpdateReasoningAPIData();
        }
    }
}
