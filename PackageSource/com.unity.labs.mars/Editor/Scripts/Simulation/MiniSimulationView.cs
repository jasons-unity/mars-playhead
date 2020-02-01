using System;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    [Serializable]
    public class MiniSimulationView : IDisposable, ISimulationView
    {
        [NonSerialized]
        static readonly Vector3 k_DefaultPivot = Vector3.zero;

        [NonSerialized]
        static readonly Quaternion k_DefaultRotation = Quaternion.LookRotation(new Vector3(-1, -.7f, -1));

        const string k_CompositeShader = "MARS/CompositeBlit";
        const string k_ShaderKeywordDesaturateOverlay = "DESATURATE_OVERLAY";
        const string k_ShaderKeywordDesaturateBase = "DESATURATE_BASE";
        const float k_PerspectiveFov = 90;
        const float k_DefaultViewSize = 10f;
        const float k_OrthoThresholdAngle = 3f;
        static readonly int k_OverlayTexID = Shader.PropertyToID("_OverlayTex");

        [SerializeField]
        AnimBool m_RotationLocked = new AnimBool();

        [SerializeField]
        AnimBool m_Ortho = new AnimBool(false);

        [SerializeField]
        Camera m_Camera;

        [SerializeField]
        Camera m_ControllingCamera;

        [SerializeField]
        Camera m_CompositeCamera;

        [SerializeField]
        RenderTexture m_RenderTexture;

        [SerializeField]
        RenderTexture m_CompositeTargetTexture;

        [SerializeField]
        AnimQuaternion m_Rotation = new AnimQuaternion(k_DefaultRotation);

        [SerializeField]
        AnimVector3 m_Position = new AnimVector3(k_DefaultPivot);

        [SerializeField]
        AnimFloat m_Size = new AnimFloat(k_DefaultViewSize);

        [SerializeField]
        ScriptableObject m_ViewParent;

        [SerializeField]
        ViewSceneType m_SceneType = ViewSceneType.Simulation;

        bool m_IsSimUser;
        bool m_FramedOnStart;
        Material m_CompositeMaterial;
        readonly SimulationRenderSettings m_CachedRenderSettings;

        public ViewSceneType sceneType { get { return m_SceneType; } set { m_SceneType = value; } }

        public bool isRotationLocked { get { return m_RotationLocked.target; } set { m_RotationLocked.target = value; } }

        /// <summary>
        /// Is the scene view ortho.
        /// </summary>
        public bool orthographic
        {
            get { return m_Ortho.value; }
            set { m_Ortho.value = value; }
        }

        /// <summary>
        /// Center point of the view. Modify it to move the view immediately, or use LookAt to animate it nicely.
        /// </summary>
        public Vector3 pivot { get { return m_Position.value; } set { m_Position.value = value; } }

        /// <summary>
        /// The direction of the scene view. Modify it to rotate the view immediately,
        /// or use LookAt to animate it nicely.
        /// </summary>
        public Quaternion rotation { get { return m_Rotation.value; } set { m_Rotation.value = value; } }

        public float size
        {
            get { return m_Size.value; }
            set
            {
                if (value > 40000f)
                    value = 40000;

                m_Size.value = value;
            }
        }

        /// <summary>
        /// Camera used to render the view.
        /// </summary>
        public Camera camera { get { return m_Camera; } }

        /// <inheritdoc/>
        public Camera ControllingCamera { get { return m_ControllingCamera; } }

        RenderTexture RenderTexture { get { return m_RenderTexture; } }

        public MiniSimulationView(ScriptableObject viewParent)
        {
            m_CachedRenderSettings = new SimulationRenderSettings();
            CreateMiniSimulationView(viewParent, 32, 32);
        }

        public MiniSimulationView(ScriptableObject viewParent, float width, float height)
        {
            m_CachedRenderSettings = new SimulationRenderSettings();
            CreateMiniSimulationView(viewParent, width, height);
        }

        public MiniSimulationView(ScriptableObject viewParent, Rect rect)
        {
            m_CachedRenderSettings = new SimulationRenderSettings();
            CreateMiniSimulationView(viewParent, rect.width, rect.height);
        }

        void CreateMiniSimulationView(ScriptableObject viewParent, float width, float height)
        {
            if (viewParent == null)
                return;

            m_ViewParent = viewParent;

            var camObject = EditorUtility.CreateGameObjectWithHideFlags($"Mini Sim View Camera {viewParent.name}",
                HideFlags.HideAndDontSave,
                typeof(Camera));
            m_Camera = camObject.GetComponent<Camera>();

            var intWidth = width > 1 ? (int)width : 1;
            var intHeight = height > 1 ? (int)height : 1;

            m_Camera.cameraType = CameraType.SceneView;
            m_Camera.clearFlags = CameraClearFlags.SolidColor;

            m_Camera.backgroundColor = Color.clear;

            m_Camera.aspect = intWidth / (float)intHeight;
            m_Camera.transform.position = m_Position.value + m_Camera.transform.rotation
                * new Vector3(0, 0, -CalculateCameraDistance());

            var backgroundCameraGo = new GameObject($"Mini Sim View Background Camera {viewParent.name}", typeof(Camera));
            backgroundCameraGo.hideFlags = HideFlags.HideAndDontSave;
            backgroundCameraGo.transform.SetParent(camera.transform, false);
            m_CompositeCamera = backgroundCameraGo.GetComponent<Camera>();
            m_CompositeCamera.name = backgroundCameraGo.name;
            m_CompositeCamera.backgroundColor = EditorGUIUtils.GetSceneBackgroundColor();

            PrepareRenderTargets(intWidth, intHeight);

            SetupViewAsSimUser();

            m_Rotation.valueChanged.AddListener(Repaint);
            m_Position.valueChanged.AddListener(Repaint);
            m_Size.valueChanged.AddListener(Repaint);
            m_Ortho.valueChanged.AddListener(Repaint);
        }

        /// <inheritdoc/>
        // Based on SceneView.LookAt
        public void LookAt(Vector3 point, Quaternion direction, float newSize, bool ortho, bool instant)
        {
            FixNegativeSize();
            if (instant)
            {
                m_Position.value = point;
                m_Rotation.value = direction;
                m_Size.value = Mathf.Abs(newSize);
                m_Ortho.value = ortho;
            }
            else
            {
                m_Position.target = point;
                m_Rotation.target = direction;
                m_Size.target = Mathf.Abs(newSize);
                m_Ortho.target = ortho;
            }
        }

        /// <inheritdoc/>
        public void Repaint()
        {
            if (Event.current == null || Event.current.type == EventType.Layout)
                return;

            SetupCamera();
            RenderCameras();
        }

        public RenderTexture SingleFrameRepaint()
        {
            SetupViewAsSimUser(true);
            SetupCamera();
            RenderCameras();
            return camera.targetTexture;
        }

        void RenderCameras()
        {
            var moduleLoaderCore = ModuleLoaderCore.instance;
            var simSceneModule = moduleLoaderCore.GetModule<SimulationSceneModule>();
            var envManager = moduleLoaderCore.GetModule<MARSEnvironmentManager>();

            m_Camera.scene = simSceneModule.ContentScene;
            m_CompositeCamera.scene = simSceneModule.EnvironmentScene;

            m_CachedRenderSettings.UseSceneRenderSettings();
            Unsupported.SetOverrideLightingSettings(m_CompositeCamera.scene);

            if (envManager != null && envManager.RenderSettings != null)
                envManager.RenderSettings.ApplyTempRenderSettings();
            else
                m_CachedRenderSettings.ApplyTempRenderSettings();

            m_CompositeCamera.Render();
            Unsupported.RestoreOverrideLightingSettings();

            Unsupported.SetOverrideLightingSettings(camera.scene);
            m_CachedRenderSettings.ApplyTempRenderSettings();
            m_Camera.Render();
            Unsupported.RestoreOverrideLightingSettings();

            var tempRT = RenderTexture.GetTemporary(m_RenderTexture.descriptor);
            m_CompositeMaterial.SetTexture(k_OverlayTexID, m_RenderTexture);
            Graphics.Blit(m_CompositeTargetTexture, tempRT, m_CompositeMaterial);
            Graphics.Blit(tempRT, m_RenderTexture);
            RenderTexture.ReleaseTemporary(tempRT);
        }

        public void Dispose()
        {
            var simSceneModule = ModuleLoaderCore.instance.GetModule<SimulationSceneModule>();

            if (simSceneModule != null && simSceneModule.IsSimulationReady)
            {
                simSceneModule.RemoveCameraFromSimulation(m_Camera);
                simSceneModule.RemoveCameraFromSimulation(m_CompositeCamera);
                simSceneModule.UnregisterSimulationUser(m_ViewParent);
            }

            m_IsSimUser = false;
            if (m_RenderTexture != null)
                m_RenderTexture.Release();

            if (m_CompositeTargetTexture != null)
                m_CompositeTargetTexture.Release();

            if (m_Camera != null)
                m_Camera.targetTexture = null;

            if (m_CompositeCamera != null)
                m_CompositeCamera.targetTexture = null;

            UnityObjectUtils.Destroy(m_RenderTexture);
            m_RenderTexture = null;

            UnityObjectUtils.Destroy(m_CompositeTargetTexture);
            m_RenderTexture = null;

            if (m_Camera != null)
                UnityObjectUtils.Destroy(m_Camera.gameObject);

            if (m_CompositeCamera != null)
                UnityObjectUtils.Destroy(m_CompositeCamera.gameObject);

            m_Camera = null;
            m_CompositeCamera = null;
            m_ControllingCamera = null;
        }

        /// <inheritdoc/>
        public void SetupViewAsSimUser(bool forceFrame = false)
        {
            var moduleLoaderCore = ModuleLoaderCore.instance;
            var simSceneModule = moduleLoaderCore.GetModule<SimulationSceneModule>();

            if (simSceneModule == null)
                return;

            Debug.Assert(sceneType == ViewSceneType.Simulation || sceneType == ViewSceneType.Device);

            if (!m_IsSimUser)
            {
                simSceneModule.RegisterSimulationUser(m_ViewParent);
                m_IsSimUser = SimulationSceneModule.ContainsSimulationUser(m_ViewParent);
            }

            if (!simSceneModule.IsSimulationReady || camera == null)
                return;

            if (Application.isPlaying)
            {
                // Assigning 'new Scene' sets the camera to render all play mode scenes.
                m_Camera.scene = new Scene();
                m_CompositeCamera.scene = new Scene();
            }
            else if (simSceneModule.IsSimulationReady)
            {
                // Assign scene first to avoid camera set up if already tracked in simulation scene
                m_Camera.scene = simSceneModule.ContentScene;
                m_CompositeCamera.scene = simSceneModule.EnvironmentScene;

                if (!simSceneModule.IsCameraAssignedToSimulationScene(camera))
                    simSceneModule.AssignCameraToSimulation(camera);

                if (!simSceneModule.IsCameraAssignedToSimulationScene(m_CompositeCamera))
                    simSceneModule.AssignCameraToSimulation(m_CompositeCamera);
            }

            var envManager = moduleLoaderCore.GetModule<MARSEnvironmentManager>();
            if ((!m_FramedOnStart || forceFrame) && envManager != null &&
                simSceneModule.IsSimulationReady && SimulationSettings.environmentMode == EnvironmentMode.Synthetic)
            {
                envManager.FrameSimViewOnSyntheticEnvironment(this, forceFrame, true);
                m_FramedOnStart = true;
            }
        }

        public void PrepareRenderTargets(Rect rect) { PrepareRenderTargets(rect.width, rect.height); }

        void PrepareRenderTargets(float width, float height)
        {
            width = width < 1 ? 1 : width;
            height = height < 1 ? 1 : height;

            var intWidth = (int)width;
            var intHeight = (int)height;
            PrepareRenderTargets(intWidth, intHeight);
        }

        void PrepareRenderTargets(int width, int height)
        {
            width = width < 1 ? 1 : width;
            height = height < 1 ? 1 : height;

            var aspect = width / (float)height;
            m_Camera.aspect = aspect;
            m_CompositeCamera.aspect = aspect;

            var hdr = camera.allowHDR && m_CompositeCamera.allowHDR;

            // Adapted from SceneView.CreateCameraTargetTexture
            // make sure we actually support R16G16B16A16_SFloat
            var format = (hdr && SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render))
                ? GraphicsFormat.R16G16B16A16_SFloat : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);

            PrepareRenderTexture(ref m_CompositeTargetTexture, m_CompositeCamera, $"Mini Sim View Background RT {m_ViewParent.name}", format, width, height);
            PrepareRenderTexture(ref m_RenderTexture, m_Camera, $"Mini Sim View Foreground RT {m_ViewParent.name}", format, width, height);

            if (m_CompositeMaterial == null)
            {
                m_CompositeMaterial = new Material(Shader.Find(k_CompositeShader))
                {
                    hideFlags = HideFlags.HideAndDontSave
                };

                m_CompositeMaterial.DisableKeyword(k_ShaderKeywordDesaturateOverlay);
                m_CompositeMaterial.DisableKeyword(k_ShaderKeywordDesaturateBase);
            }

            m_CompositeMaterial.mainTexture = m_CompositeTargetTexture;
        }

        void PrepareRenderTexture(ref RenderTexture textureTarget, Camera cameraTarget, string name, GraphicsFormat format, int width, int height)
        {
            if (textureTarget != null && textureTarget.graphicsFormat != format)
            {
                UnityObjectUtils.Destroy(textureTarget);
                textureTarget = null;
            }

            if (textureTarget == null)
            {
                textureTarget = new RenderTexture(width, height, 24, format)
                {
                    name = name,
                    antiAliasing = 1,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            if (textureTarget.width != width || textureTarget.height != height)
            {
                textureTarget.Release();
                textureTarget.width = width;
                textureTarget.height = height;
            }
            textureTarget.Create();

            cameraTarget.targetTexture = textureTarget;
        }

        // Based on SceneView.CalculateCameraDistance
        float CalculateCameraDistance()
        {
            var fov = m_Ortho.Fade(k_PerspectiveFov, 0);

            if (!m_Camera.orthographic)
                return size / Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);

            return size * 2f;
        }

        // Based on SceneView.SetupCamera
        void SetupCamera()
        {
            // If you zoom out enough, m_Position would get corrupted with no way to reset it,
            // even after restarting Unity. Crude hack to at least get the scene view working again!
            if (m_Position.value.x.IsUndefined())
                m_Position.value = Vector3.zero;

            if (m_Rotation.value.x.IsUndefined())
                m_Rotation.value = Quaternion.identity;

            m_Camera.transform.rotation = m_Rotation.value;
            m_Camera.backgroundColor = Color.clear;

            var fov = m_Ortho.Fade(k_PerspectiveFov, 0);
            if (fov > k_OrthoThresholdAngle)
            {
                m_Camera.orthographic = false;
                m_Camera.fieldOfView = camera.GetVerticalFOV(fov);
            }
            else
            {
                m_Camera.orthographic = true;
                m_Camera.orthographicSize = camera.GetVerticalOrthoSize(size);
            }

            m_Camera.transform.position = m_Position.value + m_Camera.transform.rotation
                * new Vector3(0, 0, -CalculateCameraDistance());

            var farClip = Mathf.Max(1000f, 2000f * size);
            m_Camera.nearClipPlane = farClip * 0.000005f;
            m_Camera.farClipPlane = farClip;

            m_CompositeCamera.fieldOfView = m_Camera.fieldOfView;
            m_CompositeCamera.aspect = m_Camera.aspect;
            m_CompositeCamera.nearClipPlane = m_Camera.nearClipPlane;
            m_CompositeCamera.farClipPlane = m_Camera.farClipPlane;
            m_CompositeCamera.orthographic = m_Camera.orthographic;
            m_CompositeCamera.orthographicSize = m_Camera.orthographicSize;
            m_CompositeCamera.allowHDR = m_Camera.allowHDR;
            m_CompositeCamera.usePhysicalProperties = m_Camera.usePhysicalProperties;
            m_CompositeCamera.backgroundColor = EditorGUIUtils.GetSceneBackgroundColor();
        }

        // Based on SceneView.FixNegativeSize
        void FixNegativeSize()
        {
            if (size >= 0)
                return;

            var distance = size / Mathf.Tan(k_PerspectiveFov * 0.5f * Mathf.Deg2Rad);
            var p = m_Position.value + rotation * new Vector3(0, 0, -distance);
            size = -size;
            distance = size / Mathf.Tan(k_PerspectiveFov * 0.5f * Mathf.Deg2Rad);
            m_Position.value = p + rotation * new Vector3(0, 0, distance);
        }

        /// <summary>
        /// Draw as a dynamic view in the given rect.
        /// </summary>
        /// <param name="rect">Rect to draw the View.</param>
        public void DrawMarsRenderView(Rect rect)
        {
            if (Event.current.type == EventType.Repaint)
            {
                SetupViewAsSimUser();

                PrepareRenderTargets(rect);

                if (camera.cullingMask != Tools.visibleLayers)
                    camera.cullingMask = Tools.visibleLayers;

                Repaint();
            }

            EditorGUI.DrawPreviewTexture(rect, RenderTexture);
        }

        /// <summary>
        /// Assign a camera that will drive the values of the Mini Simulation View's camera.
        /// </summary>
        /// <param name="controllingCamera">Camera that is used to drive the values of the render camera for the view.</param>
        /// <param name="useCameraImmediately">Should this camera start driving the values immediately.</param>
        public void AssignControllingCamera(Camera controllingCamera, bool useCameraImmediately)
        {
            m_ControllingCamera = controllingCamera;
            if (!useCameraImmediately)
            {
                Repaint();
                return;
            }

            camera.transform.SetWorldPose(ControllingCamera.transform.GetWorldPose());

            // With very small world scales, it's possible to make the near clip plane
            // obstruct the view of the environment, so we scale it
            var worldScale = MARSWorldScaleModule.GetWorldScale();
            camera.nearClipPlane = controllingCamera.nearClipPlane * worldScale;
            // A similar issue occurs for the far clip plane at very large world scales
            camera.farClipPlane = controllingCamera.farClipPlane * worldScale;

            Repaint();
        }
    }
}
