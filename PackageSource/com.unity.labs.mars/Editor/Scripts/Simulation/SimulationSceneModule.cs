using System;
using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Maintains a preview scene that the MARS Environment Manager uses to hold the environment objects
    /// and simulation query objects.
    /// </summary>
    [ModuleOrder(ModuleOrders.SimSceneLoadOrder)]
    public partial class SimulationSceneModule : IModuleDependency<MARSSceneModule>
    {
        SimulationScene m_SimulationScene;
        SimulationSceneUsers m_SimulationSceneUsers;
        MARSSceneModule m_SceneModule;

        public bool IsSimulationReady
        {
            get
            {
                return m_SimulationScene != null && m_SimulationScene.contentScene.IsValid() && m_SimulationScene.contentScene.isLoaded;
            }
        }

        public static bool UsingSimulation
        {
            get
            {
                return SimulationSceneUsers.instance && SimulationSceneUsers.instance.simulationSceneUserCount > 0;
            }
        }

        public static bool isAssemblyReloading { get; private set; }
        public static SimulationSceneModule instance { get; private set; }

        /// <summary>
        /// The preview scene being used by the Simulation View to render simulated content objects.
        /// </summary>
        public Scene ContentScene { get { return m_SimulationScene != null ? m_SimulationScene.contentScene : new Scene(); } }

        /// <summary>
        /// The preview scene being used by the Simulation View to render the simulated environment.
        /// </summary>
        public Scene EnvironmentScene { get { return m_SimulationScene != null ? m_SimulationScene.environmentScene : new Scene(); } }

        public static event Action SimulationSceneCreated;
        public static event Action SimulationSceneDestroyed;

        public bool IsCameraAssignedToSimulationScene(Camera camera)
        {
            return camera != null && IsSimulationReady && m_SimulationScene.IsCameraAssignedToSimulationScene(camera);
        }

        /// <summary>
        /// Moves a GameObject to the simulated content scene
        /// </summary>
        /// <param name="go"> The GameObject to move</param>
        public void AddContentGameObject(GameObject go)
        {
            m_SimulationScene.AddSimulatedGameObject(go, true);
        }

        /// <summary>
        /// Moves a GameObject to the simulated Environment scene
        /// </summary>
        /// <param name="go"> The GameObject to move</param>
        public void AddEnvironmentGameObject(GameObject go)
        {
            m_SimulationScene.AddSimulatedGameObject(go, false);
        }

        /// <summary>
        /// Assigns a camera to a simulation with proper settings to render that view.
        /// </summary>
        /// <param name="camera">Camera to render a simulation scene</param>
        public void AssignCameraToSimulation(Camera camera)
        {
            m_SimulationScene.AssignCameraToSimulation(camera);
        }

        /// <summary>
        /// Camera to be removed from rendering a simulation.
        /// </summary>
        /// <param name="camera">Camera to stop rendering the simulation scene</param>
        public void RemoveCameraFromSimulation(Camera camera)
        {
            if (m_SimulationScene != null)
                m_SimulationScene.RemoveCameraFromSimulationScene(camera);
        }

        /// <summary>
        /// Is the Scriptable Object assigned as a user of the Simulation Scene
        /// </summary>
        /// <param name="user">The object to check if using</param>
        /// <returns>True if the object is using the simulation scene</returns>
        public static bool ContainsSimulationUser(ScriptableObject user)
        {
            return SimulationSceneUsers.instance &&
                SimulationSceneUsers.instance.ContainsSimulationUser(user);
        }

        /// <summary>
        /// Adds an object to the simulation scene users and opens the simulation scene if it is not already open
        /// </summary>
        /// <param name="user">The object using the simulation scene</param>
        public void RegisterSimulationUser(ScriptableObject user)
        {
            m_SimulationSceneUsers.AddSimulationUser(user);
            OpenSimulation();
        }

        /// <summary>
        /// Removes an object from the simulation scene users.
        /// If there are no users after this removal then the simulation scene is closed.
        /// </summary>
        /// <param name="user">The object no longer using the simulation scene</param>
        public void UnregisterSimulationUser(ScriptableObject user)
        {
            if (m_SimulationSceneUsers)
            {
                m_SimulationSceneUsers.RemoveSimulationUser(user);

                if (m_SimulationSceneUsers.simulationSceneUserCount < 1)
                    CloseSimulation();
            }
            else
                CloseSimulation();
        }

        /// <summary>
        /// Opens a new simulation scene if it is not already open and if any objects are using the simulation scene
        /// </summary>
        /// <returns>True if any objects are using the simulation scene</returns>
        void OpenSimulation()
        {
            if (m_SimulationScene == null && UsingSimulation)
            {
                m_SimulationScene = new SimulationScene();
                EditorOnlyDelegates.GetSimulatedContentScene = () => m_SimulationScene.contentScene;
                EditorOnlyDelegates.GetSimulatedEnvironmentScene = () => m_SimulationScene.environmentScene;

                if (SimulationSceneCreated != null)
                    SimulationSceneCreated();
            }
        }

        /// <summary>
        /// Closes the current simulation scene without changing if any objects are using the simulation scene
        /// </summary>
        public void CloseSimulation()
        {
            if (m_SimulationScene != null)
            {
                if (SimulationSceneDestroyed != null)
                    SimulationSceneDestroyed();
                
                m_SimulationScene.Dispose();
                m_SimulationScene = null;
            }

            EditorOnlyDelegates.GetSimulatedContentScene = null;
            EditorOnlyDelegates.GetSimulatedEnvironmentScene = null;
        }

        public void ConnectDependency(MARSSceneModule dependency) { m_SceneModule = dependency; }

        public void LoadModule()
        {
            if (!m_SceneModule.simulateInPlaymode && Application.isPlaying)
                return;

            // TODO: Move environment manager functionality to runtime assembly
            EditorOnlyDelegates.OpenSimulationScene = OpenSimulation;
            instance = this;

            if (!SimulationSceneUsers.instance)
                m_SimulationSceneUsers = SimulationSceneUsers.CreateSimulationSceneSubscribers();
            else
                m_SimulationSceneUsers = SimulationSceneUsers.instance;

            if (m_SceneModule.simulateInPlaymode && EditorApplication.isPlayingOrWillChangePlaymode)
                m_SimulationSceneUsers.AddSimulationUser(MARSSceneModule.instance);

            if (!Application.isPlaying)
                OpenSimulation();

            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        public void UnloadModule()
        {
            if (m_SimulationSceneUsers && m_SceneModule.simulateInPlaymode && EditorApplication.isPlayingOrWillChangePlaymode)
                m_SimulationSceneUsers.RemoveSimulationUser(MARSSceneModule.instance);

            EditorOnlyDelegates.OpenSimulationScene = null;
            CloseSimulation();

            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;

            instance = null;
        }

        void OnAfterAssemblyReload()
        {
            isAssemblyReloading = false;
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (!SimulationSceneUsers.instance)
                m_SimulationSceneUsers = SimulationSceneUsers.CreateSimulationSceneSubscribers();
            else
                m_SimulationSceneUsers = SimulationSceneUsers.instance;

            OpenSimulation();
        }

        void OnBeforeAssemblyReload()
        {
            isAssemblyReloading = true;
            CloseSimulation();
        }
    }
}
