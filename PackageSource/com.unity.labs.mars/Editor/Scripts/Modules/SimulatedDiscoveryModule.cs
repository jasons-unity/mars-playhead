using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Fakes discovery of scene data for simulations
    /// </summary>
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    [ModuleOrder(ModuleOrders.SimDiscoveryLoadOrder)]
    public class SimulatedDiscoveryModule : EditorScriptableSettings<SimulatedDiscoveryModule>,
        IModuleDependency<MARSEnvironmentManager>
    {
        MARSEnvironmentManager m_EnvironmentManager;

        readonly List<GeneratedPlanesRoot> m_PlanesRoots = new List<GeneratedPlanesRoot>();
        bool m_RunningDiscovery;
        bool m_EnvironmentPrepared;

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        // Reference type collections must also be cleared after use
        static readonly List<MeshRenderer> k_EnvironmentMeshes = new List<MeshRenderer>();

        public void ConnectDependency(MARSEnvironmentManager dependency) { m_EnvironmentManager = dependency; }

        public void LoadModule()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode && !MARSSceneModule.simulatedDiscoveryInPlayMode)
                return;

            MARSEnvironmentManager.onEnvironmentSetup += OnEnvironmentSetup;
            QuerySimulationModule.onTemporalSimulationStart += OnTemporalSimulationStart;
            QuerySimulationModule.onTemporalSimulationStop += StopDiscovery;

            if (m_EnvironmentManager.EnvironmentSetup)
                OnEnvironmentSetup();
        }

        public void UnloadModule()
        {
            MARSEnvironmentManager.onEnvironmentSetup -= OnEnvironmentSetup;
            QuerySimulationModule.onTemporalSimulationStart -= OnTemporalSimulationStart;
            QuerySimulationModule.onTemporalSimulationStop -= StopDiscovery;

            StopDiscovery();
            m_EnvironmentPrepared = false;
            m_PlanesRoots.Clear();
        }

        void OnTemporalSimulationStart()
        {
            if (SimulationSettings.environmentMode != EnvironmentMode.Synthetic)
                return;

            PrepareEnvironment();
            StartDiscovery();
        }

        void OnEnvironmentSetup()
        {
            m_EnvironmentPrepared = false;
            var playing = EditorApplication.isPlayingOrWillChangePlaymode;
            if (SimulationSettings.environmentMode != EnvironmentMode.Synthetic ||
                !playing && !QuerySimulationModule.instance.simulatingTemporal)
            {
                return;
            }

            PrepareEnvironment();
            if (playing)
                StartDiscovery();
        }

        void PrepareEnvironment()
        {
            // if this environment has already been prepared, when opened or on a previous start, we're good
            if (m_EnvironmentPrepared)
                return;

            m_EnvironmentManager.EnvironmentParent.GetComponentsInChildren(false, m_PlanesRoots);
            EnsureMeshColliders(m_EnvironmentManager.EnvironmentParent);
            m_EnvironmentPrepared = true;
        }

        static void EnsureMeshColliders(GameObject root)
        {
            // k_EnvironmentMeshes is cleared by GetComponentsInChildren
            root.GetComponentsInChildren(k_EnvironmentMeshes);
            foreach (var mesh in k_EnvironmentMeshes)
            {
                var meshObject = mesh.gameObject;

                if (mesh.GetComponent<MeshCollider>() != null)
                    continue;

                meshObject.AddComponent<MeshCollider>();
            }

            k_EnvironmentMeshes.Clear();
        }

        void StartDiscovery()
        {
            m_RunningDiscovery = true;

            // Deactivate generated planes - we don't want them to interfere with voxel-based plane discovery
            foreach (var planesRoot in m_PlanesRoots)
            {
                planesRoot.gameObject.SetActive(false);
            }
        }

        void StopDiscovery()
        {
            if (!m_RunningDiscovery)
                return;

            m_RunningDiscovery = false;
            foreach (var planesRoot in m_PlanesRoots)
            {
                if (planesRoot != null)
                    planesRoot.gameObject.SetActive(true);
            }
        }
    }
}
