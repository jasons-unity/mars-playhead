using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
#endif
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#else
using UnityObject = UnityEngine.Object;
#endif

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Holds environment scene settings
    /// </summary>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlaneExtractionSettings))]
    class MARSEnvironmentSettings : MonoBehaviour
    {
        static MARSEnvironmentSettings s_SimulationEnvironmentSettings;

        [SerializeField]
        MARSEnvironmentInfo m_EnvironmentInfo = new MARSEnvironmentInfo();

#pragma warning disable 649
        [SerializeField]
        SimulationRenderSettings m_RenderSettings;

        [SerializeField]
#if UNITY_POST_PROCESSING_STACK_V2
        PostProcessProfile m_PostProcessProfile;
#else
        [HideInInspector]
        UnityObject m_PostProcessProfile;
#endif
#pragma warning restore 649

#if UNITY_EDITOR
        readonly HashSet<SimulationViewCameraLighting> m_CameraLightingSettings = new HashSet<SimulationViewCameraLighting>();
#endif

#if UNITY_POST_PROCESSING_STACK_V2
        PostProcessVolume m_PostProcessVolume;
#endif

        public MARSEnvironmentInfo EnvironmentInfo { get { return m_EnvironmentInfo; } }

        public SimulationRenderSettings RenderSettings { get { return m_RenderSettings; } }

        /// <summary>
        /// Get the MARSEnvironmentSettings from the GameObject and create settings if none was found.
        /// </summary>
        /// <param name="gameObject">Root game object we are checking for settings</param>
        /// <param name="environmentSettings">Environment Settings that is associated with the game object</param>
        /// <returns>True if settings was found.</returns>
        public static bool GetOrCreateSettings(GameObject gameObject, out MARSEnvironmentSettings environmentSettings)
        {
            // Check for existing object
            environmentSettings = gameObject.GetComponentInChildren<MARSEnvironmentSettings>();

            if (environmentSettings == null)
            {
                Debug.Log("MARS Environment Settings not present in the gameObject - creating one now.");
                environmentSettings = gameObject.AddComponent<MARSEnvironmentSettings>();
                return false;
            }

            return true;
        }

        void Awake()
        {
            if (!Application.isPlaying)
                return;

            var fiModule = ModuleLoaderCore.instance.GetModule<FunctionalityInjectionModule>();

            if (fiModule == null)
                return;

            fiModule.activeIsland.InjectFunctionality(gameObject);
        }

#if UNITY_EDITOR
        void OnEnable()
        {
            // Cannot check prefab stages on enable of a prefab game object
            EditorApplication.delayCall += OnEnableDelay;
        }

        void OnDisable()
        {
            EditorApplication.delayCall -= OnEnableDelay;

            if (s_SimulationEnvironmentSettings == this)
            {
                EditorOnlyDelegates.AddToSimulationViewCameraLighting = null;
                s_SimulationEnvironmentSettings = null;
            }

            foreach (var cameraLighting in m_CameraLightingSettings)
            {
                UnityObjectUtils.Destroy(cameraLighting);
            }

            m_CameraLightingSettings.Clear();
        }

        void OnEnableDelay()
        {
            // New scene will not remove `OnEnableDelay` from `EditorApplication.delayCall` correctly
            if (this == null)
                return;

            // Need camera compositing in for render settings to be using in play mode
            if (Application.isPlaying)
                return;

#if UNITY_POST_PROCESSING_STACK_V2
            // MARSEnvironmentManager needs to set the game object layer first in play mode
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                SetupPostProcessingVolume();
#endif

            // If this is part of a prefab scene we only need to use these settings for the prefab stage camera
            var prefabStage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (prefabStage == null)
                return;

            foreach (var sceneCamera in SceneView.GetAllSceneCameras())
            {
                if (sceneCamera.scene == prefabStage.scene)
                    AddToSimulationViewCameraLighting(sceneCamera, prefabStage.scene);
            }
        }

#if UNITY_POST_PROCESSING_STACK_V2
        public void SetupPostProcessingVolume()
        {
            if (m_PostProcessProfile == null)
                return;

            if (m_PostProcessVolume == null)
            {
                m_PostProcessVolume = gameObject.AddComponent<PostProcessVolume>();
                m_PostProcessVolume.hideFlags = HideFlags.HideAndDontSave;
            }

            m_PostProcessVolume.isGlobal = true;
            m_PostProcessVolume.profile = m_PostProcessProfile;
        }
#endif // UNITY_POST_PROCESSING_STACK_V2
#endif // UNITY_EDITOR

        public void UpdatePrefabInfo()
        {
            var bounds = BoundsUtils.GetBounds(gameObject.transform);
            var startingPose = m_EnvironmentInfo.SimulationStartingPose;
            if (!bounds.Contains(startingPose.position))
            {
                Debug.LogWarningFormat(
                    "Simulation starting pose for environment '{0}' is outside the total bounds of the environment. " +
                    "The starting pose will be moved to the closest point on the bounds.", gameObject.name);
                startingPose.position = bounds.ClosestPoint(startingPose.position);
                m_EnvironmentInfo.SimulationStartingPose = startingPose;
            }

            m_EnvironmentInfo.EnvironmentBounds = bounds;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Sets the default camera settings based on the current scene view camera
        /// </summary>
        /// <param name="sceneView">Scene view that we are getting the camera from</param>
        /// <param name="isSimView">Is the scene view a simulation view</param>
        public void SetDefaultEnvironmentCamera(SceneView sceneView, bool isSimView)
        {
            MARSSession.EnsureRuntimeState();
            var simCamera = sceneView.camera;
            var cameraTransform = simCamera.transform;
            if (isSimView)
            {
                var session = MARSSession.Instance;
                var cameraScale = session.transform.localScale.x;
                var camPose = new Pose(cameraTransform.position / cameraScale, cameraTransform.rotation);

                m_EnvironmentInfo.DefaultCameraWorldPose = camPose;
                m_EnvironmentInfo.DefaultCameraPivot = sceneView.pivot / cameraScale;
                m_EnvironmentInfo.DefaultCameraSize = sceneView.size / cameraScale;
            }
            else
            {
                m_EnvironmentInfo.DefaultCameraWorldPose = cameraTransform.GetWorldPose();
                m_EnvironmentInfo.DefaultCameraPivot = sceneView.pivot;
                m_EnvironmentInfo.DefaultCameraSize = sceneView.size;
            }
        }

        /// <summary>
        /// Sets the simulated device starting pose based on the current scene view camera
        /// </summary>
        /// <param name="cameraPose">Camera pose we using</param>
        /// <param name="isSimView">Is this camera from a simulation view</param>
        public void SetSimulationStartingPose(Pose cameraPose, bool isSimView)
        {
            MARSSession.EnsureRuntimeState();
            var session = MARSSession.Instance;
            if (isSimView)
            {
                var cameraScale = session.transform.localScale.x;
                cameraPose.position /= cameraScale;
            }

            m_EnvironmentInfo.SimulationStartingPose = cameraPose;
        }

        void CameraPreRenderEnvironment()
        {
            if (!Application.isPlaying)
                m_RenderSettings.ApplyTempRenderSettings();
        }

        void AddToSimulationViewCameraLighting(Camera simCamera, Scene targetScene)
        {
            var cameraLighting = simCamera.GetComponent<SimulationViewCameraLighting>();
            if (cameraLighting == null)
                cameraLighting = simCamera.gameObject.AddComponent<SimulationViewCameraLighting>();

            cameraLighting.EnvironmentScene = targetScene;
            cameraLighting.OnCameraPreRenderEnvironment += CameraPreRenderEnvironment;

            m_CameraLightingSettings.Add(cameraLighting);
        }
#endif
    }
}
