using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;

// Ensures this is always defined for providers to optionally depend on MARS--non-standard usage of CCU
[assembly: OptionalDependency("Unity.Labs.MARS.MARSCore", "INCLUDE_MARS")]
#endif

namespace Unity.Labs.MARS
{
    public class MARSCore : ScriptableSettings<MARSCore>, IModuleSceneCallbacks
    {
        public const string UserSettingsFolder = "MARSUserSettings";
        public const string SettingsFolder = "MARSSettings";

#pragma warning disable 649
        [SerializeField]
        [Tooltip("Sets the default length of time a condition should be active before canceling due to lack of data. " +
            "-1 waits forever.")]
        float m_DefaultEntityTimeout = -1.0f;

        [SerializeField]
        bool m_BlockEnsureSession;

        [SerializeField]
        FunctionalityIsland m_DefaultFaceIsland;

#if UNITY_EDITOR
        [SerializeField]
        BuildPreset m_DefaultFaceBuildSettings;
#endif
#pragma warning restore 649

        public bool BlockEnsureSession
        {
            get { return m_BlockEnsureSession; }
            set { m_BlockEnsureSession = value; }
        }

        public float defaultEntityTimeout { get { return m_DefaultEntityTimeout; } }

        public FunctionalityIsland defaultFaceIsland { get { return m_DefaultFaceIsland; } }

#if UNITY_EDITOR
        public BuildPreset defaultFaceBuildSettings { get { return m_DefaultFaceBuildSettings; } }
#endif

        public bool paused { get; set; }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<MonoBehaviour> k_Behaviors = new List<MonoBehaviour>();

        public void LoadModule() { }

        public void UnloadModule() { paused = false; }

#if UNITY_EDITOR
        public void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode) { }
        public void OnSceneOpening(string path, OpenSceneMode mode) { }

        public void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            CheckMARSBehaviors(scene);
        }
#endif

        public void OnSceneUnloaded(Scene scene) { }

        public void OnActiveSceneChanged(Scene oldScene, Scene newScene) { }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckMARSBehaviors(scene);
        }

        static void CheckMARSBehaviors(Scene scene)
        {
            var hasMarsBehaviors = false;

            k_Behaviors.Clear();
            GameObjectUtils.GetComponentsInScene(scene, k_Behaviors, true);
            foreach (var behavior in k_Behaviors)
            {
                if (behavior == null)
                    continue;

                if ((behavior.gameObject.hideFlags & HideFlags.DontSave) != 0)
                    continue;

                if (behavior is MARSEntity || behavior is IFunctionalitySubscriber)
                {
                    hasMarsBehaviors = true;
                    break;
                }
            }

            // TODO: shut down MARS entirely if there are no MARS behaviors--the issue is starting it back up when adding them
            if (hasMarsBehaviors)
                MARSSession.EnsureRuntimeState();
        }
    }
}
