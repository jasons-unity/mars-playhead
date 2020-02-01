using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Module that handles placing objects in the scene onto interaction targets. It handles parenting
    /// when dragging objects from the project window and translating objects while holding Shift
    /// </summary>
    public class ScenePlacementModule :
        IModuleDependency<MARSEnvironmentManager>, IModuleDependency<SimulationSceneModule>, IModuleDependency<EntityVisualsModule>
    {
        public class PlacementOverrideData
        {
            public bool useSnapToPivotOverride { get; private set; }
            public bool snapToPivotOverride { get; private set; }
            public bool useOrientToSurfaceOverride { get; private set; }
            public bool orientToSurfaceOverride { get; private set; }
            public bool useAxisOverride { get; private set; }
            public AxisEnum axisOverride { get; private set; }
            public Vector3 axisOverrideVector { get; private set; }

            public void ResetData()
            {
                useSnapToPivotOverride = false;
                useOrientToSurfaceOverride = false;
                useAxisOverride = false;
                axisOverride = AxisEnum.None;
            }

            void SetSnapToPivot(bool use, bool value)
            {
                if (useSnapToPivotOverride || !use)
                    return;

                useSnapToPivotOverride = true;
                snapToPivotOverride = value;
            }

            void SetOrientToSurface(bool use, bool value)
            {
                if (useOrientToSurfaceOverride || !use)
                    return;

                useOrientToSurfaceOverride = true;
                orientToSurfaceOverride = value;
            }

            void SetAxisOverride(bool use, AxisEnum value, Vector3 vector)
            {
                if (useAxisOverride || !use)
                    return;

                useAxisOverride = true;
                axisOverride = value;
                axisOverrideVector = vector;
            }

            public void SetOverrideData(PlacementOverride overrides)
            {
                SetSnapToPivot(overrides.useSnapToPivotOverride, overrides.snapToPivotOverride);
                SetOrientToSurface(overrides.useOrientToSurfaceOverride, overrides.orientToSurfaceOverride);
                SetAxisOverride(overrides.useAxisOverride, overrides.axisOverride, overrides.axisOverrideVector);
            }
        }

        static readonly int k_DropPosShaderID = Shader.PropertyToID("_DropPos");
        const int k_RaycastHitCount = 32;
        static readonly RaycastHit[] k_RaycastHits = new RaycastHit[k_RaycastHitCount];

        static readonly string k_TypeName = typeof(ScenePlacementModule).FullName;

        /// <summary>
        /// Action when a gameobject is dropped onto another gameobject and attached to it.
        /// </summary>
        public event Action<GameObject, GameObject> objectDropped;

        MARSEnvironmentManager m_EnvironmentManager;
        GameObject m_NewlyAddedObject;
        InteractionTarget m_HoveringInteractionTarget;
        GameObject m_CurrentDraggedObject;
        GameObject m_PlacementPreviewObject;
        PivotMode m_PivotModeCache;
        Vector3 m_OrientAxisDirection;
        SimulationSceneModule m_SimulationSceneModule;
        EntityVisualsModule m_EntityVisualsModule;

        public PlacementOverrideData PlacementOverrides { get; } = new PlacementOverrideData();

        public bool isDragging => m_CurrentDraggedObject != null;

        /// <summary>
        /// If enabled, objects being placed in the editor should orient to the surface
        /// </summary>
        public bool orientToSurface
        {
            get
            {
                if (PlacementOverrides.useOrientToSurfaceOverride)
                    return PlacementOverrides.orientToSurfaceOverride;

                var toSurface = EditorPrefsUtils.GetBool(k_TypeName);
                return Event.current.control ? !toSurface : toSurface;
            }
            set => EditorPrefsUtils.SetBool(k_TypeName, value);
        }

        /// <summary>
        /// If enabled, objects being placed in the editor should snap to the pivot of the target
        /// </summary>
        public bool snapToPivot
        {
            get
            {
                if (PlacementOverrides.useSnapToPivotOverride)
                    return PlacementOverrides.snapToPivotOverride;

                var toPivot = EditorPrefsUtils.GetBool(k_TypeName);
                return Event.current.alt ? !toPivot : toPivot;
            }
            set => EditorPrefsUtils.SetBool(k_TypeName, value);
        }

        /// <summary>
        /// Axis for the up direction of an object being oriented to a surface
        /// </summary>
        public AxisEnum orientAxis
        {
            get
            {
                return PlacementOverrides.useAxisOverride
                    ? PlacementOverrides.axisOverride
                    : (AxisEnum) EditorPrefsUtils.GetInt(k_TypeName);
            }
            set
            {
                if (m_CurrentDraggedObject != null)
                    return;

                SetOrientAxisDirection(value);
                EditorPrefsUtils.SetInt(k_TypeName, (int)value);

            }
        }

        /// <summary>
        /// The up axis of the object that will be aligned with the normal when orienting to surface
        /// </summary>
        Vector3 OrientAxisDirection
        {
            get
            {
                return PlacementOverrides.useAxisOverride
                    ? PlacementOverrides.axisOverrideVector
                    : m_OrientAxisDirection;
            }
        }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<PhysicsScene> k_PhysicsScenes = new List<PhysicsScene>();

        public void ConnectDependency(MARSEnvironmentManager dependency) { m_EnvironmentManager = dependency; }

        public void ConnectDependency(SimulationSceneModule dependency) { m_SimulationSceneModule = dependency; }

        public void ConnectDependency(EntityVisualsModule dependency) { m_EntityVisualsModule = dependency; }

        public void LoadModule()
        {
            Selection.selectionChanged += OnSelectionChanged;
            SceneView.duringSceneGui += OnSceneGUI;
            SetOrientAxisDirection(orientAxis);
            ScenePlacementGUI.AddPlacementSettingsToToolbar();
        }

        public void UnloadModule()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;

            if (m_HoveringInteractionTarget != null)
                EndHover(m_HoveringInteractionTarget);

            if (m_PlacementPreviewObject != null)
                UnityObjectUtils.Destroy(m_PlacementPreviewObject);

            if (m_CurrentDraggedObject != null)
                UnityObjectUtils.Destroy(m_CurrentDraggedObject);

#if UNITY_2019_3 // 19.3 specific.
            ScenePlacementGUI.RemovePlacementSettingsFromToolbar();
#endif
        }

        static void OnSelectionChanged()
        {
            // Set the InteractionTarget to selected when the its attach target is in the selection
            var selectedObjects = Selection.GetTransforms(SelectionMode.Deep);
            var selectedTransformSet = new HashSet<Transform>(selectedObjects);

            foreach (var interactionTarget in InteractionTarget.AllTargets)
            {
                if (interactionTarget.AttachTarget != null)
                    interactionTarget.SetSelected(selectedTransformSet.Contains(interactionTarget.AttachTarget));
            }
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (EditorWindow.mouseOverWindow == sceneView) // Avoid running the dragging placement logic on scene views that the mouse is not over
            {
                CheckNewlyAddedObject(sceneView);

                var currentEvent = Event.current;

                if (m_NewlyAddedObject != null)
                    m_CurrentDraggedObject = m_NewlyAddedObject;
                else if (IsMovingSelection() && m_CurrentDraggedObject == null && currentEvent.shift)
                    m_CurrentDraggedObject = Selection.activeGameObject;
            }

            MoveObjectBetweenSceneViews(sceneView);
            InteractionTargetsCheck(sceneView);
            DisableEntityVisualCollision();
        }

        void InteractionTargetsCheck(SceneView sceneView)
        {
            var currentEvent = Event.current;

            if (m_CurrentDraggedObject != null &&
                (currentEvent.type == EventType.DragUpdated || currentEvent.type == EventType.MouseDrag))
            {
                k_PhysicsScenes.Clear();
                InteractionTarget closestInteractionTarget = null;
                var closestHit = new RaycastHit();
                var currentSceneCamera = sceneView.camera;
                var farClip = currentSceneCamera.farClipPlane;

                // If the view is a simulation view we need to check the simulation scenes.
                if (sceneView is SimulationView)
                {
                    var contentScene = m_SimulationSceneModule.ContentScene;
                    k_PhysicsScenes.Add(contentScene.IsValid()
                        ? contentScene.GetPhysicsScene()
                        : Physics.defaultPhysicsScene);

                    var environmentScene = m_SimulationSceneModule.EnvironmentScene;
                    if (environmentScene != contentScene)
                    {
                        k_PhysicsScenes.Add(environmentScene.IsValid()
                            ? environmentScene.GetPhysicsScene()
                            : Physics.defaultPhysicsScene);
                    }
                }
                else // This will use prefab isolation if in use.  Otherwise, it will be the default scene.
                {
                    k_PhysicsScenes.Add(currentSceneCamera.scene.IsValid()
                        ? currentSceneCamera.scene.GetPhysicsScene()
                        : Physics.defaultPhysicsScene);
                }

                var ray = HandleUtility.GUIPointToWorldRay(currentEvent.mousePosition);

                var targetFound = FindClosestInteractionTarget(k_PhysicsScenes, ray, farClip, ref closestInteractionTarget, ref closestHit);

                if (closestInteractionTarget != m_HoveringInteractionTarget)
                {
                    if (m_HoveringInteractionTarget != null)
                        EndHover(m_HoveringInteractionTarget);

                    m_HoveringInteractionTarget = closestInteractionTarget;

                    if (m_HoveringInteractionTarget != null)
                        StartHover(m_HoveringInteractionTarget);
                }

                if (targetFound && m_HoveringInteractionTarget != null)
                {
                    UpdateHover(m_HoveringInteractionTarget, closestHit, m_CurrentDraggedObject);
                }
            }

            if (m_CurrentDraggedObject == null && m_HoveringInteractionTarget != null)
            {
                EndHover(m_HoveringInteractionTarget);
                m_HoveringInteractionTarget = null;
            }

            if ((currentEvent.type == EventType.DragPerform || currentEvent.rawType == EventType.MouseUp) &&
                m_CurrentDraggedObject != null)
            {
                if (m_HoveringInteractionTarget != null)
                {
                    OnDrop(m_HoveringInteractionTarget, m_CurrentDraggedObject);
                }
                else if (m_CurrentDraggedObject.scene == m_SimulationSceneModule.ContentScene)
                {
                    const string message = "Assets can only be dropped onto simulated data (surfaces, etc.) in the Simulation View.";
                    EditorUtility.DisplayDialog("Drop assets on simulated data", message, "OK");

                    // Destroy the object and consume the GUI drop event so the editor doesn't try to use the destroyed object
                    UnityObjectUtils.Destroy(m_CurrentDraggedObject);
                    Event.current.Use();
                }

                m_NewlyAddedObject = null;
                m_CurrentDraggedObject = null;
            }
        }

        bool FindClosestInteractionTarget(IEnumerable<PhysicsScene> physicsScenes, Ray ray, float distance,
            ref InteractionTarget closestInteractionTarget, ref RaycastHit closestHit)
        {
            var closestDistance = float.MaxValue;

            var interactionTargetFound = false;
            foreach (var physicsScene in physicsScenes)
            {
                var hitCount = physicsScene.Raycast(ray.origin, ray.direction, k_RaycastHits, distance);

                for (var i = 0; i < hitCount; i++)
                {
                    var hit = k_RaycastHits[i];
                    if (hit.transform.root.gameObject == m_CurrentDraggedObject)
                        continue;

                    var interactionTarget = hit.collider.GetComponentInParent<InteractionTarget>();
                    if (interactionTarget == null || !interactionTarget.UseInteractionTarget)
                        continue;

                    interactionTargetFound = true;

                    if (hit.distance >= closestDistance)
                        continue;

                    closestInteractionTarget = interactionTarget;
                    closestHit = hit;
                    closestDistance = hit.distance;
                }
            }

            return interactionTargetFound;
        }

        void MoveObjectBetweenSceneViews(SceneView sceneView)
        {
            var currentEvent = Event.current;
            if (m_CurrentDraggedObject == null || currentEvent.type != EventType.DragUpdated &&
                currentEvent.type != EventType.MouseDrag)
            {
                return;
            }

            var mousePosition = Event.current.mousePosition;
            if (mousePosition.x < 0 || mousePosition.y < 0 || mousePosition.x > sceneView.camera.pixelWidth
                || mousePosition.y > sceneView.camera.pixelHeight)
            {
                return;
            }

            var scene = sceneView.camera.scene;
            var targetScene = scene.IsValid() ? scene : SceneManager.GetActiveScene();
            if (m_CurrentDraggedObject.scene == targetScene)
                return;

            m_CurrentDraggedObject.transform.SetParent(null);
            SceneManager.MoveGameObjectToScene(m_CurrentDraggedObject, targetScene);

            if (!(sceneView is SimulationView))
                return;

            if (scene == m_SimulationSceneModule.ContentScene)
            {
                m_SimulationSceneModule.AddContentGameObject(m_CurrentDraggedObject);
            }
            else
            {
                m_SimulationSceneModule.AddEnvironmentGameObject(m_CurrentDraggedObject);
                m_CurrentDraggedObject.transform.SetParent(m_EnvironmentManager.EnvironmentParent.transform);
            }
        }

        void StartHover(InteractionTarget target)
        {
            target.SetHovered(true);
            m_PivotModeCache = Tools.pivotMode;
            Tools.pivotMode = PivotMode.Pivot;
            UpdatePlacementOverrides();
            MouseLabelModule.AddMouseLabel(target, target.name);
        }

        void UpdatePlacementOverrides()
        {
            // Interaction target placement overrides will be given priority
            PlacementOverrides.ResetData();
            var targetPlacementOverride = m_HoveringInteractionTarget.GetComponent<PlacementOverride>();
            if (targetPlacementOverride != null)
                PlacementOverrides.SetOverrideData(targetPlacementOverride);

            var objectPlacementOverride = m_CurrentDraggedObject.GetComponent<PlacementOverride>();
            if (objectPlacementOverride != null)
                PlacementOverrides.SetOverrideData(objectPlacementOverride);
        }

        void UpdateHover(InteractionTarget target, RaycastHit hit, GameObject dropObject)
        {
            Shader.SetGlobalVector(k_DropPosShaderID, hit.point);

            if (m_PlacementPreviewObject == null)
                CreatePlacementPreviewObject(dropObject);

            dropObject.SetActive(false);
            m_PlacementPreviewObject.SetActive(true);

            var targetRotation = Quaternion.identity;
            if (orientToSurface)
                targetRotation = GetRotationForSurfaceAndAxis(hit.normal, OrientAxisDirection);

            m_PlacementPreviewObject.transform.rotation = targetRotation;
            m_PlacementPreviewObject.transform.position = snapToPivot ? target.transform.position : hit.point;
        }

        static Quaternion GetRotationForSurfaceAndAxis(Vector3 surfaceNormal, Vector3 axis)
        {
            return Quaternion.LookRotation(surfaceNormal, Vector3.up)
                * Quaternion.Inverse(Quaternion.FromToRotation(Vector3.forward, axis));
        }

        void CreatePlacementPreviewObject(GameObject dropObject)
        {
            Undo.RegisterCompleteObjectUndo(dropObject, "Drop onto target.");
            dropObject.SetActive(false);
            m_PlacementPreviewObject = UnityObject.Instantiate(dropObject, dropObject.transform.parent);
            if (dropObject.scene.IsValid() && dropObject.scene != m_PlacementPreviewObject.scene)
                SceneManager.MoveGameObjectToScene(m_PlacementPreviewObject, dropObject.scene);

            m_PlacementPreviewObject.SetHideFlagsRecursively(HideFlags.HideAndDontSave);
            foreach (var collider in m_PlacementPreviewObject.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
        }

        void EndHover(InteractionTarget target)
        {
            Shader.SetGlobalVector(k_DropPosShaderID, Vector3.zero);

            if (m_PlacementPreviewObject != null)
                UnityObjectUtils.Destroy(m_PlacementPreviewObject);

            if(m_CurrentDraggedObject != null)
                m_CurrentDraggedObject.SetActive(true);

            PlacementOverrides.ResetData();
            Tools.pivotMode = m_PivotModeCache;
            target.SetHovered(false);
            MouseLabelModule.RemoveMouseLabel(target);
        }

        void OnDrop(InteractionTarget target, GameObject droppedObj)
        {
            droppedObj.SetActive(true);
            if (m_PlacementPreviewObject != null)
            {
                droppedObj.transform.position = m_PlacementPreviewObject.transform.position;
                droppedObj.transform.rotation = m_PlacementPreviewObject.transform.rotation;
            }

            if (droppedObj.transform.parent != target.AttachTarget)
            {
                if (m_NewlyAddedObject == droppedObj)
                    droppedObj.transform.SetParent(target.AttachTarget);
                else
                    Undo.SetTransformParent(droppedObj.transform, target.AttachTarget, "Change parent.");


                if (objectDropped != null)
                {
                    objectDropped(droppedObj,
                        target.AttachTarget == null ? null : target.AttachTarget.gameObject);
                }

                EditorApplication.DirtyHierarchyWindowSorting();
                EditorApplication.RepaintHierarchyWindow();
                EditorGUIUtility.PingObject(droppedObj);
            }

            var targetType = "unknown";
            var targetLabel = "unknown";
            var faceInteractionTarget = target as FaceLandmarkInteractionTarget;
            if (faceInteractionTarget != null)
            {
                targetType = "face landmark";
                targetLabel = faceInteractionTarget.landmark.ToString();
            }
            EditorEvents.DropTargetUsed.Send(new DropTargetUsedArgs
            {
                type = targetType,
                label = targetLabel
            });
        }

        static bool IsMovingSelection()
        {
            var selectedTransform = Selection.activeTransform;
            if (Event.current.type != EventType.MouseDrag || selectedTransform == null || Tools.current != Tool.Move
                || !selectedTransform.hasChanged)
            {
                return false;
            }

            selectedTransform.hasChanged = false;
            return true;

        }

        void CheckNewlyAddedObject(SceneView sceneView)
        {
            if (Event.current.type != EventType.DragUpdated)
                return;

            var draggedObjects = DragAndDrop.objectReferences;
            if (draggedObjects.Length == 0)
            {
                m_NewlyAddedObject = null;
            }
            else if (m_NewlyAddedObject == null) // Adding something, but the object has not been determined
            {
                var scene = sceneView.camera.scene;
                if (!scene.IsValid())
                    scene = SceneManager.GetActiveScene();

                var gameObjs = scene.GetRootGameObjects();
                foreach (var go in gameObjs)
                {
                    if (go.hideFlags == HideFlags.HideInHierarchy && go.name == draggedObjects[0].name)
                    {
                        go.transform.SetParent(null, true);
                        m_NewlyAddedObject = go;
                        break;
                    }
                }
            }
        }

        void DisableEntityVisualCollision()
        {
            if (m_CurrentDraggedObject != null)
            {
                var entities = m_CurrentDraggedObject.GetComponentsInChildren<MARSEntity>();
                foreach (var entity in entities)
                {
                    m_EntityVisualsModule.DisableVisualCollision(entity);
                }
            }
            else
            {
                m_EntityVisualsModule.ResetVisualCollision();
            }
        }

        void SetOrientAxisDirection(AxisEnum axis)
        {
            switch (axis)
            {
                case AxisEnum.XUp:
                    m_OrientAxisDirection = Vector3.right;
                    break;
                case AxisEnum.XDown:
                    m_OrientAxisDirection = Vector3.left;
                    break;
                case AxisEnum.YUp:
                    m_OrientAxisDirection = Vector3.up;
                    break;
                case AxisEnum.YDown:
                    m_OrientAxisDirection = Vector3.down;
                    break;
                case AxisEnum.ZUp:
                    m_OrientAxisDirection = Vector3.forward;
                    break;
                case AxisEnum.ZDown:
                    m_OrientAxisDirection = Vector3.back;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
