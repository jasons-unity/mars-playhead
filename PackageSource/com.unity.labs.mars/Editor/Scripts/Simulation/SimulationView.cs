using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;
#if UNITY_POST_PROCESSING_STACK_V2
using UnityEngine.Rendering.PostProcessing;
#endif

namespace Unity.Labs.MARS
{
    /// <summary>
    /// MARS Simulation View displays a separate 3D scene by extending SceneView.
    /// </summary>
    //[EditorWindowTitle(useTypeNameAsIconName = false)]  // TODO update when this is not an Internal attribute
    [Serializable]
    public class SimulationView : SceneView, ISimulationView
    {
        class SimulationViewData
        {
            public float size;
            public Vector3 pivot;
            public Quaternion rotation;
            public bool usesWorldScale;

            bool m_DrawGizmos;
            bool m_SceneLighting;
            SceneViewState m_SceneViewState;
            bool m_In2DMode;
            bool m_IsRotationLocked;
            bool m_AudioPlay;
            CameraSettings m_CameraSettings;
            bool m_Orthographic;

            public void CopySimulationViewData(SimulationView view)
            {
                m_DrawGizmos = view.drawGizmos;
                m_SceneLighting = view.sceneLighting;
                m_SceneViewState = view.sceneViewState;
                m_In2DMode = view.in2DMode;
                m_IsRotationLocked = view.isRotationLocked;
                m_AudioPlay = view.audioPlay;
                m_CameraSettings = view.cameraSettings;
                size = view.size;
                m_Orthographic = view.orthographic;
                pivot = view.pivot;
                rotation = view.rotation;
            }

            public void CopySimulationViewData(SimulationViewData data)
            {
                m_DrawGizmos = data.m_DrawGizmos;
                m_SceneLighting = data.m_SceneLighting;
                m_SceneViewState = data.m_SceneViewState;
                m_In2DMode = data.m_In2DMode;
                m_IsRotationLocked = data.m_IsRotationLocked;
                m_AudioPlay = data.m_AudioPlay;
                m_CameraSettings = data.m_CameraSettings;
                size = data.size;
                m_Orthographic = data.m_Orthographic;
                pivot = data.pivot;
                rotation = data.rotation;
                usesWorldScale = data.usesWorldScale;
            }

            public void SetSimulationViewFromData(SimulationView view, bool useViewLocation = true)
            {
                view.drawGizmos = m_DrawGizmos;
                view.sceneLighting = m_SceneLighting;
                view.sceneViewState = m_SceneViewState;
                view.in2DMode = m_In2DMode;
                view.isRotationLocked = m_IsRotationLocked;
                view.audioPlay = m_AudioPlay;
                view.cameraSettings = m_CameraSettings;
                view.orthographic = m_Orthographic;

                if (useViewLocation)
                {
                    view.size = size;
                    view.pivot = pivot;
                    view.rotation = rotation;
                }
            }
        }

        class Styles
        {
            public readonly GUIContent SimulationViewTitleContent;
            public readonly GUIContent DeviceViewTitleContent;
            public readonly GUIContent CustomViewTitleContent;

            public Styles()
            {
                SimulationViewTitleContent = new GUIContent(SimulationViewWindowTitle, MARSUIResources.instance.SimulationViewIcon);
                DeviceViewTitleContent = new GUIContent(DeviceViewWindowTitle, MARSUIResources.instance.SimulationViewIcon);
                CustomViewTitleContent = new GUIContent(k_CustomMARSViewWindowTitle, MARSUIResources.instance.SimulationViewIcon);
            }
        }

        public const string SimulationViewWindowTitle = "Simulation View";
        public const string DeviceViewWindowTitle = "Device View";

        const string k_CustomMARSViewWindowTitle = "Custom MARS View";
        const int k_SceneViewToolbarHeight = 17; // EditorGUI.kWindowToolbarHeight
        const string k_CompositeShader = "MARS/CompositeBlit";
        const string k_ShaderKeywordDesaturateOverlay = "DESATURATE_OVERLAY";
        const string k_ShaderKeywordDesaturateBase = "DESATURATE_BASE";
        const string k_SceneBackgroundPrefsKey = "Scene/Background";
        static readonly List<SimulationView> k_SimulationViews = new List<SimulationView>();
        static readonly int k_OverlayTexID = Shader.PropertyToID("_OverlayTex");

        static SimulationView s_ActiveSimulationView;
        static Styles s_Styles;
        static FieldInfo s_SceneTargetTextureFieldInfo;

        static FieldInfo SceneTargetTextureFieldInfo
        {
            get
            {
                if (s_SceneTargetTextureFieldInfo == null)
                    s_SceneTargetTextureFieldInfo = typeof(SceneView).GetField("m_SceneTargetTexture",
                        BindingFlags.Instance | BindingFlags.NonPublic);

                return s_SceneTargetTextureFieldInfo;
            }
        }

        [SerializeField]
        Camera m_ControllingCamera;

        [SerializeField]
        ViewSceneType m_SceneType = ViewSceneType.None;

        [SerializeField]
        SimulationViewData m_DefaultViewData;

        [SerializeField]
        Camera m_CompositeCamera;

        [SerializeField]
        bool m_BackgroundSceneActive;

        [SerializeField]
        bool m_DesaturateInactive;

        bool m_IsSimUser;
        bool m_FramedOnStart;
        Bounds m_MovementBounds;
        Material m_CompositeMaterial;
        SimulationRenderSettings m_CachedRenderSettings;
        RenderTexture m_CompositeTargetTexture;
        CompositeCameraRenderer m_CompositeCameraRenderer;
        CameraFPSModeHandler m_FPSModeHandler;
        SimulatedObjectsManager m_SimulatedObjectsManager;
        SimulationSceneModule m_SimulationSceneModule;
        MARSEnvironmentManager m_EnvironmentManager;
#if UNITY_POST_PROCESSING_STACK_V2
        LayerMask m_EnvironmentLayerMask;
        PostProcessLayer m_CompositePostProcess;
        PostProcessLayer m_CameraPostProcess;
        Scene m_CameraScene;
#endif

        readonly Dictionary<ViewSceneType, SimulationViewData> m_DataForViewType = new Dictionary<ViewSceneType, SimulationViewData>();

        Color EditorBackgroundColor => EditorMaterialUtils.PrefToColor(EditorPrefs.GetString(k_SceneBackgroundPrefsKey));

        /// <summary>
        /// Camera used to render the scene composited with scene being rendered by the <c>camera</c>.
        /// </summary>
        Camera compositeCamera
        {
            get
            {
                if (m_CompositeCamera != null)
                    return m_CompositeCamera;

                var compositeCameraGo = new GameObject($"{SimulationViewWindowTitle} Composite {GetInstanceID()}", typeof(Camera))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                if (m_SimulationSceneModule != null && m_SimulationSceneModule.IsSimulationReady)
                {
                    if (m_BackgroundSceneActive)
                        m_SimulationSceneModule.AddContentGameObject(compositeCameraGo);
                    else
                        m_SimulationSceneModule.AddEnvironmentGameObject(compositeCameraGo);
                }

                m_CompositeCamera = compositeCameraGo.GetComponent<Camera>();
                m_CompositeCamera.name = compositeCameraGo.name;

                if (camera != null)
                {
                    m_CompositeCameraRenderer = camera.GetComponent<CompositeCameraRenderer>();
                    if (m_CompositeCameraRenderer == null)
                        m_CompositeCameraRenderer = camera.gameObject.AddComponent<CompositeCameraRenderer>();
                }

#if UNITY_POST_PROCESSING_STACK_V2
                m_CompositePostProcess = compositeCameraGo.AddComponent<PostProcessLayer>();
                m_CompositePostProcess.volumeLayer = m_EnvironmentLayerMask;
#endif

                m_CompositeCameraRenderer.PostRenderCamera = PostRenderCamera;
                m_CompositeCameraRenderer.PreRenderCamera = PreRenderCamera;
                m_CompositeCameraRenderer.RenderImage = RenderImage;

                PrepareCompositeTargetTexture();

                compositeCamera.targetTexture = m_CompositeTargetTexture;

                return m_CompositeCamera;
            }
        }

        GUIContent CurrentTitleContent
        {
            get
            {
                switch (sceneType)
                {
                    case ViewSceneType.Simulation:
                        return styles.SimulationViewTitleContent;
                    case ViewSceneType.Device:
                        return styles.DeviceViewTitleContent;
                    default:
                        return styles.CustomViewTitleContent;
                }
            }
        }

        bool UseMovementBounds => MARSUserPreferences.instance.RestrictCameraToEnvironmentBounds && m_MovementBounds != default;

        /// <summary>
        /// Camera that can be used to drive the values of the rendering camera.
        /// </summary>
        public Camera ControllingCamera => m_ControllingCamera;

        /// <summary>
        /// Whether this view is in Sim or Device mode
        /// </summary>
        public ViewSceneType sceneType
        {
            get => m_SceneType;
            set => SetupSceneTypeData(value);
        }

        /// <summary>
        /// The primary scene view.  Not a Simulation View.
        /// </summary>
        public static SceneView NormalSceneView { get; private set; }

        /// <summary>
        /// List of all Simulation Views
        /// </summary>
        public static List<SimulationView> SimulationViews => k_SimulationViews;

        /// <summary>
        /// The last Simulation View the user focused
        /// </summary>
        public static SceneView ActiveSimulationView
        {
            get
            {
                if (!s_ActiveSimulationView && k_SimulationViews.Count > 0)
                    s_ActiveSimulationView = k_SimulationViews[0];

                return s_ActiveSimulationView;
            }
        }

        public bool backgroundSceneActive
        {
            get => m_BackgroundSceneActive;
            set
            {
                m_BackgroundSceneActive = value;
                SetupCompositingRender();
            }
        }

        public bool DesaturateInactive
        {
            get => m_DesaturateInactive;
            set => m_DesaturateInactive = value;
        }


        // Delay creation of Styles till first access
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        [MenuItem(MenuConstants.MenuPrefix + SimulationViewWindowTitle, priority = MenuConstants.SimulationViewPriority)]
        public static void InitWindowInSimulationView()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = SimulationViewWindowTitle, active = true});
            if (!FindNormalSceneView())
            {
                NormalSceneView = GetWindow<SceneView>();
                NormalSceneView.Show();
            }

            var window = GetWindow<SimulationView>();
            window.sceneType = ViewSceneType.Simulation;
            s_ActiveSimulationView = window;
            window.Show();
            window.ShowTab();
        }

        internal static void NewTabSimulationView(object userData)
        {
            if (!FindNormalSceneView())
            {
                NormalSceneView = MARSUtils.CustomAddTabToHere(typeof(SceneView)) as SceneView;
                NormalSceneView.Show();
            }

            if (MARSUtils.CustomAddTabToHere(userData) is SimulationView window)
            {
                window.sceneType = ViewSceneType.Simulation;
                s_ActiveSimulationView = window;
            }

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = SimulationViewWindowTitle, active = true});
        }

        [MenuItem(MenuConstants.MenuPrefix + DeviceViewWindowTitle, priority = MenuConstants.DeviceViewPriority)]
        public static void InitWindowInDeviceView()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = DeviceViewWindowTitle, active = true});
            if (!FindNormalSceneView())
            {
                NormalSceneView = GetWindow<SceneView>();
                NormalSceneView.Show();
            }

            var window = GetWindow<SimulationView>();
            window.sceneType = ViewSceneType.Device;
            window.Show();
            window.ShowTab();
        }

        internal static void NewTabDeviceView(object userData)
        {
            if (!FindNormalSceneView())
            {
                NormalSceneView = MARSUtils.CustomAddTabToHere(typeof(SceneView)) as SceneView;
                NormalSceneView.Show();
            }

            if (MARSUtils.CustomAddTabToHere(userData) is SimulationView window)
                window.sceneType = ViewSceneType.Device;

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = DeviceViewWindowTitle, active = true});
        }

        protected override bool SupportsStageHandling() { return false; }

        /// <inheritdoc/>
        public override void AddItemsToMenu(GenericMenu menu)
        {
            this.MARSCustomMenuOptions(menu);
            base.AddItemsToMenu(menu);
        }

        /// <inheritdoc/>
        public override void OnEnable()
        {
            // Suppress the error message about missing scene icon. It is not an exception, but in case any other
            // exceptions happen in base.OnEnable, we use try/catch to log them re-enable logging
            var logEnabled = Debug.unityLogger.logEnabled;
            try
            {
                Debug.unityLogger.logEnabled = false;
                base.OnEnable();
            }
            catch (Exception e)
            {
                Debug.LogFormat("Exception in SimulationView.OnEnable: {0}\n{1}", e.Message, e.StackTrace);
            }
            finally
            {
                Debug.unityLogger.logEnabled = logEnabled;
            }

            titleContent = CurrentTitleContent;
            autoRepaintOnSceneChange = true;

            m_DataForViewType.Clear();
            if (m_DefaultViewData == null)
            {
                m_DefaultViewData = new SimulationViewData();
                m_DefaultViewData.CopySimulationViewData(this);
            }

            var moduleLoaderCore = ModuleLoaderCore.instance;
            // Used for one time module subscribing and setup of values from environment manager
            if (moduleLoaderCore.ModulesAreLoaded)
                EditorApplication.delayCall += OnModulesLoaded;

            moduleLoaderCore.ModulesLoaded += OnModulesLoaded;

            k_SimulationViews.Add(this);

            // Manually move the sim view camera into the preview scene
            // Mandates that it is automatically cleaned up (destroyed) when closing the sim view
            // If this camera's GameObject isn't (manually) deleted when closing a window,
            // it will persist/leak in the hierarchy
            camera.name = $"{SimulationViewWindowTitle} Camera {GetInstanceID()}";
            camera.gameObject.hideFlags = HideFlags.HideAndDontSave;

            m_FPSModeHandler = new CameraFPSModeHandler();
            m_CompositeCameraRenderer = camera.GetComponent<CompositeCameraRenderer>();
            if (m_CompositeCameraRenderer == null)
                m_CompositeCameraRenderer = camera.gameObject.AddComponent<CompositeCameraRenderer>();

            m_CompositeCameraRenderer.PostRenderCamera = PostRenderCamera;
            m_CompositeCameraRenderer.PreRenderCamera = PreRenderCamera;
            m_CompositeCameraRenderer.RenderImage = RenderImage;

            if (sceneType == ViewSceneType.None)
                sceneType = ViewSceneType.Simulation;

            SetupViewAsSimUser();

            m_CachedRenderSettings = new SimulationRenderSettings();
        }

        void OnModulesLoaded()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            m_EnvironmentLayerMask = 1 << MARSEnvironmentManager.EnvironmentObjectsLayer;
#endif

            var moduleLoaderCore = ModuleLoaderCore.instance;
            m_SimulatedObjectsManager = moduleLoaderCore.GetModule<SimulatedObjectsManager>();
            m_SimulationSceneModule = moduleLoaderCore.GetModule<SimulationSceneModule>();
            m_EnvironmentManager = moduleLoaderCore.GetModule<MARSEnvironmentManager>();
            if (m_SimulatedObjectsManager == null || m_SimulationSceneModule == null || m_EnvironmentManager == null)
                return;

            m_SimulatedObjectsManager.OnSimulatedCameraCreated += AssignCamera;

            // Scene Type is not set till after OnEnable for new windows.
            // We don't want to start setting the camera values till this is set.
            if (sceneType != ViewSceneType.None && m_SimulatedObjectsManager.SimulatedCamera != null)
                AssignCamera(m_SimulatedObjectsManager.SimulatedCamera);
            else
                AssignCamera(Camera.main);

            SetupCompositingRender();

            MARSEnvironmentManager.onEnvironmentSetup += OnEnvironmentSetup;
            OnEnvironmentSetup();
        }

        void OnEnvironmentSetup()
        {
            if (m_EnvironmentManager != null)
                m_MovementBounds = m_EnvironmentManager.EnvironmentBounds;
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            var moduleLoaderCore = ModuleLoaderCore.instance;
            moduleLoaderCore.ModulesLoaded -= OnModulesLoaded;

            if (m_SimulatedObjectsManager != null)
                m_SimulatedObjectsManager.OnSimulatedCameraCreated -= AssignCamera;

            MARSEnvironmentManager.onEnvironmentSetup -= OnEnvironmentSetup;

            if (m_SimulationSceneModule != null && m_SimulationSceneModule.IsSimulationReady)
            {
                if (camera != null)
                    m_SimulationSceneModule.RemoveCameraFromSimulation(camera);

                if (compositeCamera != null)
                    m_SimulationSceneModule.RemoveCameraFromSimulation(compositeCamera);

                m_SimulationSceneModule.UnregisterSimulationUser(this);
            }

            UnityObjectUtils.Destroy(m_CompositeCameraRenderer);
            m_CompositeCameraRenderer = null;

            UnityObjectUtils.Destroy(compositeCamera.gameObject);
            m_CompositeCamera = null;

            m_IsSimUser = false;
            k_SimulationViews.Remove(this);
            m_FPSModeHandler.StopMoveInput(Vector2.zero);
            m_FPSModeHandler = null;

            CheckActiveSimulationView();

            base.OnDisable();
        }

        protected new virtual void OnDestroy()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = "Simulation View", active = false});
            base.OnDestroy();
        }

        void OnFocus()
        {
            if (sceneType != ViewSceneType.Simulation)
                return;

            s_ActiveSimulationView = this;
        }

        void Update()
        {
            // Check if normal scene view is closed and close simulation because otherwise Window -> Scene will focus
            // simulation view instead of reopening normal scene view
            if (NormalSceneView == null && !FindNormalSceneView())
            {
                Close();
                return;
            }

            // Need to make sure camera is assigned to the sim scene if scene has changed.
            SetupViewAsSimUser();
        }

        static bool FindNormalSceneView()
        {
            var allSceneViews = Resources.FindObjectsOfTypeAll(typeof (SceneView)) as SceneView[];
            if (allSceneViews == null)
                return false;

            foreach (var view in allSceneViews)
            {
                if (view is SimulationView)
                    continue;

                NormalSceneView = view;
                return true;
            }

            return false;
        }

        protected override void OnGUI()
        {
            var currentEvent = Event.current;
            var type = currentEvent.type;

#if UNITY_POST_PROCESSING_STACK_V2
            var needsPostProcessingSetup = sceneType == ViewSceneType.Simulation && m_SimulationSceneModule != null
                && m_SimulationSceneModule.IsSimulationReady && currentEvent.type == EventType.Repaint;

            if (needsPostProcessingSetup)
            {
                var cameraGameObject = camera.gameObject;
                m_CameraScene = cameraGameObject.scene;

                if (m_CameraScene.IsValid())
                {
                    if (m_BackgroundSceneActive)
                    {
                        m_SimulationSceneModule.AddEnvironmentGameObject(cameraGameObject);
                        camera.scene = m_SimulationSceneModule.EnvironmentScene;
                    }
                    else
                    {
                        m_SimulationSceneModule.AddContentGameObject(cameraGameObject);
                        camera.scene = m_SimulationSceneModule.ContentScene;
                    }

                    if (m_CameraPostProcess == null)
                        m_CameraPostProcess = camera.GetComponent<PostProcessLayer>();

                    if (m_CameraPostProcess != null)
                    {
                        if (m_BackgroundSceneActive)
                            m_CameraPostProcess.volumeLayer = m_EnvironmentLayerMask;

                        m_CameraPostProcess.finalBlitToCameraTarget = false;
                    }
                }
                else
                {
                    needsPostProcessingSetup = false;
                }
            }
#endif

            // Setting in OnGUI prevents single frame flash of skybox
            // when interacting with the scene view state in the GUI.
            if (sceneType == ViewSceneType.Simulation)
                sceneViewState.showSkybox = false;

            var toolbarHeightOffset = MARSEditorGUI.Styles.ToolbarHeight + k_SceneViewToolbarHeight;
            var rect = new Rect(0, toolbarHeightOffset, position.width,
                position.height - toolbarHeightOffset);

            // Called before base.OnGUI to consume input
            if (Event.current.type != EventType.Repaint)
                this.DrawSimulationViewToolbar();

            base.OnGUI();

            if (Event.current.type == EventType.Repaint)
                this.DrawSimulationViewToolbar();

#if UNITY_POST_PROCESSING_STACK_V2
            if (needsPostProcessingSetup)
            {
                SceneManager.MoveGameObjectToScene(camera.gameObject, m_CameraScene);
            }
#endif

            if (sceneType == ViewSceneType.Device)
            {
                var querySimulationModule = ModuleLoaderCore.instance.GetModule<QuerySimulationModule>();

                if (focusedWindow == this && SimulationSettings.environmentMode == EnvironmentMode.Synthetic &&
                    // User has pressed "Play" on device mode to move.
                    querySimulationModule != null && querySimulationModule.simulatingTemporal)
                {
                    m_FPSModeHandler.MovementBounds = m_MovementBounds;
                    m_FPSModeHandler.UseMovementBounds = UseMovementBounds;
                    m_FPSModeHandler.HandleGUIInput(rect, currentEvent, type);
                }
                else
                {
                    m_FPSModeHandler.StopMoveInput(currentEvent.mousePosition);
                }
            }

            SimulationControlsGUI.DrawHelpArea(sceneType);

            UpdateCamera();
        }

        /// <summary>
        /// Cache the LookAt information for the Simulation scene type when it is not the active view type
        /// </summary>
        /// <param name="point">The position in world space to frame.</param>
        /// <param name="direction">The direction that the Scene view should view the target point from.</param>
        /// <param name="newSize">The amount of camera zoom. Sets <c>size</c>.</param>
        public void CacheLookAt(Vector3 point, Quaternion direction, float newSize)
        {
            if (sceneType == ViewSceneType.Simulation)
                return;

            if (m_DefaultViewData == null)
            {
                m_DefaultViewData = new SimulationViewData();
                m_DefaultViewData.CopySimulationViewData(this);
            }

            if (!m_DataForViewType.TryGetValue(ViewSceneType.Simulation, out var data))
            {
                data = new SimulationViewData();
                data.CopySimulationViewData(m_DefaultViewData);
            }

            data.pivot = point;
            data.rotation = direction;
            data.size = Mathf.Abs(newSize);
            m_DataForViewType[ViewSceneType.Simulation] = data;
        }

        /// <inheritdoc/>
        public void SetupViewAsSimUser(bool forceFrame = false)
        {
            if (m_SimulationSceneModule == null)
                return;

            switch (sceneType)
            {
                case ViewSceneType.Simulation: case ViewSceneType.Device:
                {
                    if (!m_IsSimUser)
                    {
                        m_SimulationSceneModule.RegisterSimulationUser(this);
                        m_IsSimUser = SimulationSceneModule.ContainsSimulationUser(this);
                    }

                    if (!Application.isPlaying && m_SimulationSceneModule.IsSimulationReady && camera != null)
                    {
                        if (!m_SimulationSceneModule.IsCameraAssignedToSimulationScene(camera))
                            m_SimulationSceneModule.AssignCameraToSimulation(camera);

                        if (!m_SimulationSceneModule.IsCameraAssignedToSimulationScene(compositeCamera))
                            m_SimulationSceneModule.AssignCameraToSimulation(compositeCamera);

                        // Pointing to empty scene struct to use scene.IsValid logic for null equivalent.
                        camera.scene = new Scene();
                        SetupCompositingRender();

                        // Only Frame Synthetic Simulation View
                        if (sceneType != ViewSceneType.Device && (!m_FramedOnStart || forceFrame) && m_SimulationSceneModule.IsSimulationReady
                            && SimulationSettings.environmentMode == EnvironmentMode.Synthetic)
                        {
                            MARSEnvironmentManager.instance.FrameSimViewOnSyntheticEnvironment(this, forceFrame, true);
                            m_FramedOnStart = true;
                        }
                    }

                    break;
                }
                default:
                {
                    Debug.Log($"Scene type {sceneType} not supported in Simulation View.");
                    sceneType = ViewSceneType.Simulation;
                    break;
                }
            }
        }

        void SetupCompositingRender()
        {
            if (m_SimulationSceneModule == null || !m_SimulationSceneModule.IsSimulationReady)
                return;

            if (camera == null)
                return;

            if (m_BackgroundSceneActive)
            {
                // Setup composite camera to render the content scene on top of camera rendering env scene
                m_SimulationSceneModule.AddContentGameObject(compositeCamera.gameObject);
                compositeCamera.scene = m_SimulationSceneModule.ContentScene;
                compositeCamera.clearFlags = CameraClearFlags.SolidColor;
                compositeCamera.backgroundColor = EditorBackgroundColor;

                customScene = m_SimulationSceneModule.EnvironmentScene;
                camera.clearFlags = CameraClearFlags.Skybox;

                m_CompositeCameraRenderer.enabled = true;

#if UNITY_POST_PROCESSING_STACK_V2
                m_CompositePostProcess.enabled = false;
#endif
            }
            else
            {
                // Setup composite camera to render the env scene under the camera rendering content scene
                m_SimulationSceneModule.AddEnvironmentGameObject(compositeCamera.gameObject);
                compositeCamera.scene = m_SimulationSceneModule.EnvironmentScene;
                compositeCamera.clearFlags = CameraClearFlags.Skybox;
                compositeCamera.backgroundColor = EditorBackgroundColor;

                customScene = m_SimulationSceneModule.ContentScene;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;

                m_CompositeCameraRenderer.enabled = true;
#if UNITY_POST_PROCESSING_STACK_V2
                m_CompositePostProcess.enabled = true;
#endif
            }
        }

        void UpdateCompositeCamera()
        {
            if (m_SimulationSceneModule == null || Event.current.type != EventType.Repaint)
                return;

            var compositeCameraGo = compositeCamera.gameObject;
            if (m_SimulationSceneModule.IsSimulationReady
                && compositeCameraGo.scene != m_SimulationSceneModule.EnvironmentScene)
            {
                m_SimulationSceneModule.AddEnvironmentGameObject(compositeCameraGo);
            }

            compositeCamera.transform.SetWorldPose(camera.transform.GetWorldPose());

            compositeCamera.fieldOfView = camera.fieldOfView;
            compositeCamera.aspect = camera.aspect;
            compositeCamera.nearClipPlane = camera.nearClipPlane;
            compositeCamera.farClipPlane = camera.farClipPlane;
            compositeCamera.orthographic = camera.orthographic;
            compositeCamera.orthographicSize = camera.orthographicSize;
        }

        void PreRenderCamera()
        {
            if (m_EnvironmentManager == null || m_SimulationSceneModule == null || !m_SimulationSceneModule.IsSimulationReady)
                return;

            UpdateCompositeCamera();

            // Need to control the rendering of the background camera here.
            // This is to make sure the render setting are applied correctly
            // and the skybox renders to the correct view.
            PrepareCompositeTargetTexture();

            sceneViewState.showSkybox = false;
            if (m_BackgroundSceneActive)
            {
                RenderForeground();
#if UNITY_POST_PROCESSING_STACK_V2
                if (m_CameraPostProcess == null)
                    m_CameraPostProcess = camera.GetComponent<PostProcessLayer>();

                if (m_CameraPostProcess != null)
                {
                    if (m_BackgroundSceneActive)
                        m_CameraPostProcess.volumeLayer = m_EnvironmentLayerMask;

                    m_CameraPostProcess.finalBlitToCameraTarget = false;
                }
#endif
            }
            else
            {
                RenderBackground();
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
        }

        void RenderBackground()
        {
            m_CachedRenderSettings.UseSceneRenderSettings();

            var targetSceneValid = compositeCamera.scene.IsValid();
            if (targetSceneValid)
            {
                Unsupported.SetOverrideLightingSettings(compositeCamera.scene);

                if (m_EnvironmentManager != null && m_EnvironmentManager.RenderSettings != null)
                    m_EnvironmentManager.RenderSettings.ApplyTempRenderSettings();
                else
                    m_CachedRenderSettings.ApplyTempRenderSettings();
            }

            compositeCamera.Render();
            if (targetSceneValid)
                Unsupported.RestoreOverrideLightingSettings();

            GL.Clear(true, false, Color.black);
        }

        void RenderForeground()
        {
            if (m_SimulationSceneModule == null || m_EnvironmentManager == null)
                return;

            var targetSceneValid = m_SimulationSceneModule.EnvironmentScene.IsValid();

            if (targetSceneValid)
                Unsupported.RestoreOverrideLightingSettings();

            compositeCamera.clearFlags = CameraClearFlags.SolidColor;
            compositeCamera.backgroundColor = Color.clear;
            compositeCamera.Render();

            GL.Clear(true, false, Color.black);

            if (targetSceneValid)
            {
                m_CachedRenderSettings.UseSceneRenderSettings();

                Unsupported.SetOverrideLightingSettings(m_SimulationSceneModule.EnvironmentScene);

                if (m_EnvironmentManager.RenderSettings != null)
                    m_EnvironmentManager.RenderSettings.ApplyTempRenderSettings();
                else
                    m_CachedRenderSettings.ApplyTempRenderSettings();
            }
        }

        void PostRenderCamera()
        {
#if UNITY_POST_PROCESSING_STACK_V2
            if (m_CameraPostProcess == null)
                m_CameraPostProcess = camera.GetComponent<PostProcessLayer>();

            if (m_CameraPostProcess != null)
            {
                if (m_BackgroundSceneActive)
                    m_CameraPostProcess.volumeLayer = m_EnvironmentLayerMask;

                m_CameraPostProcess.finalBlitToCameraTarget = false;
            }
#endif
        }

        void RenderImage(RenderTexture src, RenderTexture dest)
        {
            if (m_SimulationSceneModule == null || !m_SimulationSceneModule.IsSimulationReady)
            {
                Graphics.Blit(src, dest);
                return;
            }

            var tempRT = RenderTexture.GetTemporary(m_CompositeTargetTexture.descriptor);

            m_CompositeMaterial.SetTexture(k_OverlayTexID, m_CompositeTargetTexture);

            if (m_BackgroundSceneActive)
            {
                if (m_DesaturateInactive)
                {
                    m_CompositeMaterial.EnableKeyword(k_ShaderKeywordDesaturateOverlay);
                    m_CompositeMaterial.DisableKeyword(k_ShaderKeywordDesaturateBase);
                }
                else
                {
                    m_CompositeMaterial.DisableKeyword(k_ShaderKeywordDesaturateOverlay);
                    m_CompositeMaterial.DisableKeyword(k_ShaderKeywordDesaturateBase);
                }

                Graphics.Blit(src, tempRT, m_CompositeMaterial);
                Graphics.Blit(tempRT, dest);
            }
            else
            {
                m_CompositeMaterial.SetTexture(k_OverlayTexID, src);
                if (m_DesaturateInactive)
                {
                    m_CompositeMaterial.DisableKeyword(k_ShaderKeywordDesaturateOverlay);
                    m_CompositeMaterial.EnableKeyword(k_ShaderKeywordDesaturateBase);
                }
                else
                {
                    m_CompositeMaterial.DisableKeyword(k_ShaderKeywordDesaturateOverlay);
                    m_CompositeMaterial.DisableKeyword(k_ShaderKeywordDesaturateBase);
                }

                Graphics.Blit(m_CompositeTargetTexture, tempRT, m_CompositeMaterial);
                Graphics.Blit(tempRT, dest);
            }

            RenderTexture.ReleaseTemporary(tempRT);
        }

        void PrepareCompositeTargetTexture()
        {
            var sceneTargetTexture = SceneTargetTextureFieldInfo.GetValue(this) as RenderTexture;
            if (sceneTargetTexture == null)
                return;

            if (camera == null)
                return;

            var hdr = camera.allowHDR && compositeCamera.allowHDR;
            var width = sceneTargetTexture.width;
            var height = sceneTargetTexture.height;

            // Adapted from SceneView.CreateCameraTargetTexture
            // make sure we actually support R16G16B16A16_SFloat
            var format = (hdr && SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render))
                ? GraphicsFormat.R16G16B16A16_SFloat : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

            if (m_CompositeTargetTexture != null)
            {
                if (m_CompositeTargetTexture.graphicsFormat != format)
                {
                    compositeCamera.targetTexture = null;
                    m_CompositeTargetTexture.Release();
                    UnityObjectUtils.Destroy(m_CompositeTargetTexture);
                    m_CompositeTargetTexture = null;
                }
            }

            if (m_CompositeTargetTexture == null)
            {
                m_CompositeTargetTexture = new RenderTexture(width, height, 24, format)
                {
                    name = $"{SimulationViewWindowTitle} Background RT {GetInstanceID()}",
                    antiAliasing = 1,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else if (m_CompositeTargetTexture.width != width || m_CompositeTargetTexture.height != height)
            {
                m_CompositeTargetTexture.Release();
                m_CompositeTargetTexture.width = width;
                m_CompositeTargetTexture.height = height;
            }

            m_CompositeTargetTexture.Create();

            if (m_CompositeMaterial == null)
            {
                m_CompositeMaterial = new Material(Shader.Find(k_CompositeShader))
                {
                    hideFlags = HideFlags.DontSave
                };
            }

            compositeCamera.targetTexture = m_CompositeTargetTexture;
        }

        void AssignCamera(Camera newCamera)
        {
            AssignControllingCamera(newCamera, sceneType == ViewSceneType.Device);
        }

        /// <inheritdoc/>
        public void AssignControllingCamera(Camera controllingCamera, bool useImmediately)
        {
            m_ControllingCamera = controllingCamera;
            UpdateCamera(true);

            if (!useImmediately)
                return;

            RepaintAll();
        }

        void UpdateCamera(bool firstFrame = false)
        {
            if (!firstFrame && (Event.current == null || Event.current.type == EventType.Layout))
                return;

            if (sceneType == ViewSceneType.Device)
            {
                // Do not let Device View enter orthographic mode
                orthographic = false;
                in2DMode = false;
                isRotationLocked = true;

                if (ControllingCamera == null)
                    return;

                camera.fieldOfView = ControllingCamera.fieldOfView;

                if (ControllingCamera.usePhysicalProperties)
                {
                    camera.usePhysicalProperties = true;
                    camera.focalLength = ControllingCamera.focalLength;
                }
                else
                {
                    camera.usePhysicalProperties = false;
                }

                var transform = ControllingCamera.transform;
                rotation = transform.rotation;
                pivot = transform.position + transform.forward * cameraDistance;
            }

            Repaint();
        }

        void SetupSceneTypeData(ViewSceneType newType)
        {
            if (m_SceneType == newType)
                return;

            var oldType = m_SceneType;
            if (oldType != ViewSceneType.None)
            {
                if (!m_DataForViewType.TryGetValue(oldType, out var oldTypeData))
                    oldTypeData = new SimulationViewData();

                oldTypeData.CopySimulationViewData(this);
                m_DataForViewType[oldType] = oldTypeData;
            }

            if (m_DefaultViewData == null)
            {
                m_DefaultViewData = new SimulationViewData();
                m_DefaultViewData.CopySimulationViewData(this);
                m_DefaultViewData.usesWorldScale = false;
            }

            if (m_DataForViewType.TryGetValue(newType, out var newTypeData))
                newTypeData.SetSimulationViewFromData(this);
            else
                m_DefaultViewData.SetSimulationViewFromData(this, newType != ViewSceneType.Device);

            m_SceneType = newType;
            titleContent = CurrentTitleContent;

            switch (newType)
            {
                case ViewSceneType.None:
                    CheckActiveSimulationView();
                    break;
                case ViewSceneType.Simulation:
                    s_ActiveSimulationView = this;
                    break;
                case ViewSceneType.Device:
                    CheckActiveSimulationView();
                    drawGizmos = false;
                    in2DMode = false;
                    isRotationLocked = true;
                    sceneLighting = true;
                    orthographic = false;
                    break;
            }

            if (oldType == ViewSceneType.None && newType == ViewSceneType.Device)
            {
                // Scene Type is not set till after OnEnable for new windows.
                // We don't want to start setting the camera values till this is set.
                if (m_SimulatedObjectsManager != null && m_SimulatedObjectsManager.SimulatedCamera != null)
                    AssignCamera(m_SimulatedObjectsManager.SimulatedCamera);
            }
        }

        void CheckActiveSimulationView()
        {
            if (s_ActiveSimulationView != this)
                return;

            for (var i = k_SimulationViews.Count - 1; i >= 0; i--)
            {
                if (k_SimulationViews[i].sceneType != ViewSceneType.Simulation)
                    continue;

                s_ActiveSimulationView = k_SimulationViews[i];
                return;
            }
        }

        internal void OffsetViewCachedData(Transform envTransform, Matrix4x4 inversePreviousOffset,
            Quaternion differenceRotation, float differenceScale)
        {
            foreach (var viewTypeData in m_DataForViewType)
            {
                var data = viewTypeData.Value;
                if (!data.usesWorldScale)
                    continue;

                var scaledSize = data.size * differenceScale;

                var previousLocalPivot = inversePreviousOffset.MultiplyPoint3x4(data.pivot);
                data.pivot = envTransform.TransformPoint(previousLocalPivot);
                data.rotation = differenceRotation * data.rotation;
                data.size = scaledSize;
            }
        }
    }
}
