#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Labs.MARS.Data.Tests
{
    /// <summary>
    /// Tests the sequence of creating a Context, waiting until it is tracking, deactivating it, and re-activating it on the next frame
    /// </summary>
    public class QueryRegistrationTest : MonoBehaviour, IMonoBehaviourTest
    {
        const int k_FrameTimeOut = 3000;

        FunctionalityInjectionModule m_FIModule;
        MARSSession m_MARSSession;
        Transform m_InactiveParent;
        GameObject m_Context;

        // Local method use only -- created here to reduce garbage collection
        static readonly List<Component> k_ComponentList = new List<Component>();
        QueryPipelinesModule m_PipelinesModule;
        SlowTaskModule m_SlowTaskModule;
        bool m_Completed;
        int m_FramesSinceCompleted;
        bool m_WasSimulatingInPlaymode;
        bool m_WasSimulatingDiscovery;

        public bool IsTestFinished { get; private set; }

        protected virtual void OnEnable()
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var sceneModule = MARSSceneModule.instance;
            m_WasSimulatingInPlaymode = sceneModule.simulateInPlaymode;
            m_WasSimulatingDiscovery = sceneModule.simulateDiscovery;
            sceneModule.simulateInPlaymode = true;
            sceneModule.simulateDiscovery = false;

            var inactiveObject = new GameObject();
            inactiveObject.SetActive(false);
            m_InactiveParent = inactiveObject.transform;
            m_Context = new GameObject("QueryRegistrationTest_Proxy");
            m_Context.transform.parent = m_InactiveParent;
            k_ComponentList.Clear();
            k_ComponentList.Add(m_Context.AddComponent<Proxy>());
            k_ComponentList.Add(m_Context.AddComponent<ShowChildrenOnTrackingAction>());
            k_ComponentList.Add(m_Context.AddComponent<SetPoseAction>());
            k_ComponentList.Add(m_Context.AddComponent<PlaneSizeCondition>());

            MARSSession.TestMode = true;
            MARSSession.EnsureRuntimeState();
            m_MARSSession = MARSSession.Instance;
            m_FIModule = moduleLoader.GetModule<FunctionalityInjectionModule>();

            var activeIsland = m_FIModule.activeIsland;
            moduleLoader.InjectFunctionalityInModules(activeIsland);
            m_MARSSession.CheckCapabilities();
            var definitions = new HashSet<TraitDefinition>();
            foreach (var requirement in m_MARSSession.requirements.TraitRequirements)
            {
                definitions.Add(requirement);
            }

            activeIsland.SetupDefaultProviders(definitions);
            foreach (var currentComponent in k_ComponentList)
            {
                activeIsland.InjectFunctionalitySingle(currentComponent);
            }

            k_ComponentList.Clear();
            m_Context.transform.parent = null;
        }

        void Update()
        {
            if (!m_Context.activeSelf)
            {
                m_Context.SetActive(true);
                m_Completed = true;
            }

            if (m_Completed && ++m_FramesSinceCompleted >= k_FrameTimeOut)
            {
                IsTestFinished = true;
                enabled = false;
                throw new TimeoutException("Query failed to re-acquire before timeout");
            }

            switch (m_Context.GetComponent<Proxy>().queryState)
            {
                case QueryState.Unknown:
                    break;
                case QueryState.Unavailable:
                    break;
                case QueryState.Querying:
                    break;
                case QueryState.Tracking:
                    if (m_Completed)
                    {
                        IsTestFinished = true;
                        enabled = false;
                    }
                    else
                    {
                        m_Context.SetActive(false);
                    }

                    break;
                case QueryState.Acquiring:
                    break;
                case QueryState.Resuming:
                    break;
            }
        }

        protected virtual void OnDisable()
        {
            if (m_MARSSession)
                Destroy(m_MARSSession.gameObject);

            if (m_InactiveParent != null)
            {
                Destroy(m_InactiveParent.gameObject);
                m_InactiveParent = null;
            }

            var sceneModule = MARSSceneModule.instance;
            sceneModule.simulateInPlaymode = m_WasSimulatingInPlaymode;
            sceneModule.simulateDiscovery = m_WasSimulatingDiscovery;

            MARSSession.TestMode = false;
        }
    }
}
#endif
