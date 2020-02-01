using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Module that handles cutting out a region of the simulation environment so the interiors of rooms and
    /// buildings can be viewed and interacted with while lighting stays the same
    /// </summary>
    public class XRayModule : IModuleDependency<ScenePlacementModule>, IModuleDependency<SimulationSceneModule>
    {
        // Default values for the XRay shader if no region is set
        const float k_DefaultFloorHeight = 0.0f;
        const float k_DefaultCeilingHeight = 2.5f;
        const float k_DefaultThickness = 0.5f;
        const float k_DefaultRoomWidth = 3.0f;
        const float k_DefaultScale = 1.0f;

        // Shader IDs for the same properties
        static readonly int k_RoomCenterShaderID = Shader.PropertyToID("_RoomCenter");
        static readonly int k_FloorHeightShaderID = Shader.PropertyToID("_FloorHeight");
        static readonly int k_CeilingHeightShaderID = Shader.PropertyToID("_CeilingHeight");
        static readonly int k_ClipOffsetShaderID = Shader.PropertyToID("_RoomClipOffset");
        static readonly int k_FadeThicknessShaderID = Shader.PropertyToID("_FadeThickness");
        static readonly int k_XRayScaleID = Shader.PropertyToID("_XRayScale");

        // Clipping data for the collider disabling
        const float k_VerticalClipOffset = 1.0f;
        const float k_VerticalClipRange = 0.95f;

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly Collider[] k_Colliders = new Collider[128];

        /// <summary>
        /// Stores the status of a given scene's active XRay data
        /// </summary>
        class XRaySceneState
        {
            /// <summary>
            /// Whether or not the scene's XRayRegion has been searched for yet
            /// </summary>
            public bool Initialized = false;

            /// <summary>
            /// A scene's XRayRegion, if it exists
            /// </summary>
            public XRayRegion XRayRegion;

            /// <summary>
            /// Resets the data so a new search can occur
            /// </summary>
            public void Clear()
            {
                Initialized = false;
                XRayRegion = null;
            }
        }

        Scene m_PrefabStageScene;

        XRaySceneState m_SceneViewXRay = new XRaySceneState();
        XRaySceneState m_SimViewXRay = new XRaySceneState();
        XRaySceneState m_IsolationXRay = new XRaySceneState();

        XRaySceneState m_LastXRay;

        Vector3 m_XRayCenter = Vector3.zero;
        float m_XRayFloorHeight = k_DefaultFloorHeight;
        float m_XRayCeilingHeight = k_DefaultCeilingHeight;
        float m_XRayThickness = k_DefaultThickness;
        float m_XRayRoomWidth = k_DefaultRoomWidth;
        float m_XRayScale = k_DefaultScale;

        bool m_PhysicsInitialized = false;
        List<Collider> m_DisabledColliders = new List<Collider>();

        ScenePlacementModule m_ScenePlacementModule;
        SimulationSceneModule m_SimulationSceneModule;

        public void ConnectDependency(ScenePlacementModule dependency) { m_ScenePlacementModule = dependency; }
        public void ConnectDependency(SimulationSceneModule dependency) { m_SimulationSceneModule = dependency; }

        /// <summary>
        /// Ensures the module gets all events that change potential XRay regions or objects that interact with them
        /// </summary>
        public void LoadModule()
        {
            SceneView.beforeSceneGui += OnSceneGUI;
            MARSEnvironmentManager.onEnvironmentSetup += OnEnvironmentSetup;
            EditorSceneManager.sceneSaving += OnSceneSaving;
            PrefabStage.prefabSaving += OnPrefabSaving;
            Selection.selectionChanged += OnSelectionChanged;
        }

        /// <summary>
        /// Undoes any temporary collider changes and unhooks from any events
        /// </summary>
        public void UnloadModule()
        {
            m_PrefabStageScene = default(Scene);
            m_SceneViewXRay.Clear();
            m_SimViewXRay.Clear();
            m_IsolationXRay.Clear();
            ClearColliderChanges();

            SceneView.beforeSceneGui -= OnSceneGUI;
            MARSEnvironmentManager.onEnvironmentSetup -= OnEnvironmentSetup;
            Selection.selectionChanged -= OnSelectionChanged;
            EditorSceneManager.sceneSaving -= OnSceneSaving;
            PrefabStage.prefabSaving -= OnPrefabSaving;
        }

        void OnSelectionChanged()
        {
            // If the active object has an XRay region, we clear all the environments so the XRay set up gets refreshed
            var selectedObject = Selection.activeGameObject;
            if (selectedObject == null)
                return;

            var selectedXRay = selectedObject.GetComponentInChildren<XRayRegion>();
            if (selectedXRay == null)
                return;

            m_SceneViewXRay.Clear();
            m_SimViewXRay.Clear();
            m_IsolationXRay.Clear();
            m_LastXRay = null;
            ClearColliderChanges();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            // Determine which type of scene is being operated on, regular scene view, simulation view, or prefab isolation
            var camera = sceneView.camera;
            var cameraScene = camera.scene;

            var activeXRay = m_SceneViewXRay;
            var updateXRayData = false;

            if (sceneView is SimulationView)
            {
                // In the simulation view, we only care about the environment being X-Ray'd out'
                activeXRay = m_SimViewXRay;
                cameraScene = m_SimulationSceneModule.EnvironmentScene;
            }
            else
            {
                // The regular scene view can operate in regular or isolation mode - or potentially switch between
                // one prefab and another
                if (!m_PrefabStageScene.IsValid())
                {
                    m_IsolationXRay.Clear();

                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                    if (prefabStage != null && prefabStage.scene.IsValid())
                    {
                        m_PrefabStageScene = prefabStage.scene;
                    }
                }

                if (m_PrefabStageScene.IsValid())
                    activeXRay = m_IsolationXRay;
            }
            var targetScene = cameraScene.IsValid() ? cameraScene : SceneManager.GetActiveScene();

            // If a given scene view has not been searched for an XRayRegion, attempt that now
            if (!activeXRay.Initialized)
            {
                updateXRayData = true;
                foreach (var root in targetScene.GetRootGameObjects())
                {
                    activeXRay.XRayRegion = root.GetComponentInChildren<XRayRegion>();

                    if (activeXRay.XRayRegion != null)
                    {
                        activeXRay.Initialized = true;
                        break;
                    }
                }
            }

            // If the selected XRay data has changed, update the active X-Ray values for physics and shader
            if (m_LastXRay != activeXRay)
            {
                updateXRayData = true;
                m_LastXRay = activeXRay;

                if (activeXRay != null && activeXRay.XRayRegion != null)
                {
                    // Scale can vary between views and prefabs, so we grab 'ground truth' from the active XRay Region
                    m_XRayScale = activeXRay.XRayRegion.transform.lossyScale.x;

                    m_XRayCenter = activeXRay.XRayRegion.transform.position;
                    m_XRayFloorHeight = activeXRay.XRayRegion.FloorHeight;
                    m_XRayCeilingHeight = activeXRay.XRayRegion.CeilingHeight;
                    m_XRayThickness = activeXRay.XRayRegion.ClipOffset;
                    m_XRayRoomWidth = Mathf.Max( activeXRay.XRayRegion.ViewBounds.x,  activeXRay.XRayRegion.ViewBounds.z);
                }
                else
                {
                    m_XRayScale = k_DefaultScale;
                    m_XRayCenter = Vector3.zero;
                    m_XRayFloorHeight = k_DefaultFloorHeight;
                    m_XRayCeilingHeight = k_DefaultCeilingHeight;
                    m_XRayThickness = k_DefaultThickness;
                    m_XRayRoomWidth = k_DefaultRoomWidth;
                }
            }

            if (updateXRayData)
            {
                Shader.SetGlobalVector(k_RoomCenterShaderID, m_XRayCenter);
                Shader.SetGlobalFloat(k_FloorHeightShaderID, m_XRayFloorHeight);
                Shader.SetGlobalFloat(k_CeilingHeightShaderID, m_XRayCeilingHeight);
                Shader.SetGlobalFloat(k_ClipOffsetShaderID, m_XRayThickness);
                Shader.SetGlobalFloat(k_XRayScaleID, m_XRayScale);
            }

            // Is there an object being dragged?
            // If so, do collision logic
            // Otherwise, cancel it
            if (!m_ScenePlacementModule.isDragging)
            {
                ClearColliderChanges();
            }
            else
            {
                if (m_PhysicsInitialized)
                    return;

                m_PhysicsInitialized = true;

                // Ensure we are operating in isolation physics scene if in prefab mode
                var physicsScene = cameraScene.IsValid() ? cameraScene.GetPhysicsScene() : Physics.defaultPhysicsScene;

                var scaledFloor = m_XRayScale * m_XRayFloorHeight;
                var scaledCeiling = m_XRayScale * m_XRayCeilingHeight;
                var scaledThickness = m_XRayScale * m_XRayThickness;
                var scaledWidth = m_XRayScale * m_XRayRoomWidth;

                // Create a physics box that lines up with the clipping plane the XRay Shader uses
                // Get all the colliders inside of that and temporarily disable them
                var roomHeight = scaledCeiling - scaledFloor;
                var roomOffset = (scaledFloor + scaledCeiling)*0.5f;

                var cameraToCenter = camera.transform.position - m_XRayCenter;
                if (cameraToCenter.y > scaledCeiling)
                {
                    roomHeight += k_VerticalClipRange;
                    roomOffset += k_VerticalClipOffset;
                }

                if (cameraToCenter.y < scaledFloor)
                {
                    roomHeight += k_VerticalClipRange;
                    roomOffset -= k_VerticalClipOffset;
                }
                cameraToCenter.y = 0.0f;
                var cameraDistance = cameraToCenter.magnitude;
                cameraToCenter.Normalize();

                // Offset towards the camera half the camera distance as the box is centered
                var boxCenter = m_XRayCenter + ((cameraDistance*0.5f + scaledThickness) * cameraToCenter);
                boxCenter.y = m_XRayCenter.y + roomOffset;

                var boxExtents = new Vector3(scaledWidth, roomHeight, cameraDistance)*0.5f;
                var boxOrientation = Quaternion.LookRotation(cameraToCenter.normalized, Vector3.up);
                var collisionCount = physicsScene.OverlapBox(boxCenter, boxExtents, k_Colliders, boxOrientation);

                // Go through colliders and disable - only if they have the XRay collider component
                for (var i = 0; i < collisionCount; i++)
                {
                    var currentCollider = k_Colliders[i];

                    if (!currentCollider.enabled || !currentCollider.TryGetComponent(out XRayCollider _))
                        continue;

                    currentCollider.enabled = false;
                    m_DisabledColliders.Add(currentCollider);
                }
            }
        }

        void ClearColliderChanges()
        {
            if (m_PhysicsInitialized)
            {
                foreach (var collider in m_DisabledColliders)
                {
                    if (collider != null)
                        collider.enabled = true;
                }
                m_PhysicsInitialized = false;
                m_DisabledColliders.Clear();
            }
        }

        void OnEnvironmentSetup()
        {
            m_SceneViewXRay.Clear();
            m_SimViewXRay.Clear();
            m_IsolationXRay.Clear();
            ClearColliderChanges();
        }

        void OnSceneSaving(Scene scene, string path)
        {
            ClearColliderChanges();
        }
        void OnPrefabSaving(GameObject prefab)
        {
            ClearColliderChanges();
        }
    }
}
