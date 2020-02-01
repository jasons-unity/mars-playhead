#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    public static class EditorOnlyDelegates
    {
        internal static Func<GameObject, bool> IsEnvironmentPrefab { get; set; }
        internal static Func<Scene, MARSSession> GetMARSSession { get; set; }
        internal static Func<GameObject> GetSimulatedEnvironmentRoot { get; set; }
        internal static Func<Scene> GetSimulatedContentScene { get; set; }
        internal static Func<Scene> GetSimulatedEnvironmentScene { get; set; }
        public static Func<Camera> TryGetSimulatedCamera { get; set; }
        internal static Action<Scene> CullEnvironmentFromSceneLights { get; set; }
        public static Action<List<Camera>> GetAllSimulationSceneCameras { get; set; }
        public static Func<int> GetEnvironmentLayer { get; set; }
        public static Func<bool> IsEnvironmentSetup { get; set; }
        public static Action<bool> SwitchToNextEnvironment { get; set; }
        public static Action OpenSimulationScene { get; set; }
        public static Action<Camera, Scene> AddToSimulationViewCameraLighting { get; set; }
        public static Action<Transform, Transform> AddSpawnedTransformToSimulationManager { get; set; }
        public static Action<ISimulatable, ISimulatable> AddSpawnedSimulatableToSimulationManager { get; set; }
        public static Func<Camera, bool> IsGizmosCamera { get; set; }
        public static Action DirtySimulatableScene { get; set; }

#if INCLUDE_XR_MOCK
        public static Func<bool> IsRemoteActive { get; set; }
#endif
    }
}
#endif
