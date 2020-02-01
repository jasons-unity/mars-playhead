using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Manages world scale adjustment for each scene
    /// </summary>
    public class MARSWorldScaleModule : EditorScriptableSettings<MARSWorldScaleModule>, IModule
    {
        [Serializable]
        public struct ScaleVisual
        {
#pragma warning disable 649
            [SerializeField]
            Texture2D m_ScaleVisual;

            [SerializeField]
            Texture2D m_ScaleIcon;
#pragma warning restore 649

            public Texture2D scaleVisual { get { return m_ScaleVisual; } }
            public Texture2D scaleIcon { get { return m_ScaleIcon; } }
        }

        const float k_BaseLog = 10f;
        static readonly Vector3 k_ScaleReferenceOffset = new Vector3(1f, 0.5f, 0);

        static MARSSession s_CachedSession;
        static SerializedObject s_SerializedTransform;

#pragma warning disable 649
        [SerializeField]
        int m_MinScaleExponent = -2;

        [SerializeField]
        int m_MaxScaleExponent = 3;

        [SerializeField]
        ScaleVisual[] m_ScaleVisuals;

        [SerializeField]
        Material m_ScaleReferenceMaterial;
#pragma warning restore 649

        Transform m_ScaleReference;
        Renderer m_ScaleReferenceRenderer;
        Vector3 m_ScaleReferenceOrigin;
        int m_CurrentScaleReferenceIcon;
        Material m_ScaleReferenceMaterialClone;
        float m_MinScale;
        float m_MaxScale;
        int m_VisualsZeroIndex;

        readonly List<AudioSource> m_EnvironmentAudioSources = new List<AudioSource>();
        readonly List<AudioReverbZone> m_EnvironmentReverbZones = new List<AudioReverbZone>();
        readonly List<Light> m_EnvironmentLights = new List<Light>();

        internal static bool worldScaleControlsShown { get; set; }

        public int visualsZeroIndex { get { return m_VisualsZeroIndex; } }
        public int minScaleExponent { get { return m_MinScaleExponent; } }
        public int maxScaleExponent { get { return m_MaxScaleExponent; } }
        public float minScale { get { return m_MinScale; } }
        public float maxScale { get { return m_MaxScale; } }
        public ScaleVisual smallScaleIcon { get { return m_ScaleVisuals[0]; } }
        public ScaleVisual scaleOneIcon { get { return m_ScaleVisuals[visualsZeroIndex]; } }
        public ScaleVisual largeScaleIcon { get { return m_ScaleVisuals[m_ScaleVisuals.Length - 1]; } }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<GameObject> k_RootGameObjects = new List<GameObject>();
        static readonly List<MARSEntity> k_EntityList = new List<MARSEntity>();
        static readonly List<Transform> k_EntityTransformList = new List<Transform>();
        static readonly List<Transform> k_EntityChildTransformList = new List<Transform>();
        static readonly List<ISpatialCondition> k_SpatialConditionList = new List<ISpatialCondition>();
        static readonly List<MARSEntity> k_Entities = new List<MARSEntity>();
        static readonly List<ISpatialCondition> k_SpatialConditions = new List<ISpatialCondition>();
        static readonly List<AudioSource> k_AudioSources = new List<AudioSource>();
        static readonly List<AudioReverbZone> k_ReverbZones = new List<AudioReverbZone>();
        static readonly List<Light> k_SceneLights = new List<Light>();

        public void LoadModule()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            UpdateScaleValues();
        }

        public void UnloadModule()
        {
            SceneView.duringSceneGui -= OnSceneGUI;

            s_CachedSession = null;
            s_SerializedTransform = null;

            ClearEnvironmentRangeScaledComponents();

            if (m_ScaleReferenceMaterialClone != null)
                DestroyImmediate(m_ScaleReferenceMaterialClone);

            if (m_ScaleReference != null)
                DestroyImmediate(m_ScaleReference.gameObject);

            m_ScaleReferenceRenderer = null;
            m_ScaleReferenceOrigin = default(Vector3);
            m_CurrentScaleReferenceIcon = 0;
            m_MinScale = 0;
            m_MaxScale = 0;
            m_VisualsZeroIndex = 0;
        }

        public static float GetWorldScale()
        {
            var scaleProperty = GetWorldScaleProperty();
            return  scaleProperty != null ? GetWorldScaleProperty().vector3Value.x : 1f;
        }

        static SerializedProperty GetWorldScaleProperty()
        {
            var session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            if (session == null || session.transform == null)
                return null;

            if (s_CachedSession != session || s_SerializedTransform == null)
            {
                s_CachedSession = session;
                s_SerializedTransform = new SerializedObject(session.transform);
            }

            s_SerializedTransform.UpdateIfRequiredOrScript();
            return s_SerializedTransform.FindProperty("m_LocalScale");
        }

        void OnSceneGUI(SceneView sceneView)
        {
            var session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            if (session && worldScaleControlsShown && !EditorApplication.isPlayingOrWillChangePlaymode)
                UpdateScaleReference();
            else if (m_ScaleReference != null)
                DestroyImmediate(m_ScaleReference.gameObject);
        }

        public void UpdateScaleReference()
        {
            // Current scene view is null after script compile till the user interacts with the scene view.
            var sceneView = SceneView.currentDrawingSceneView;
            if (sceneView == null || sceneView is SimulationView)
                return;

            var worldScaleProperty = GetWorldScaleProperty();
            var worldScale = worldScaleProperty.vector3Value.x;

            if (m_ScaleReference == null)
            {
                var scaleReferenceObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
                scaleReferenceObj.name = "World Scale Reference";
                scaleReferenceObj.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
                m_ScaleReference = scaleReferenceObj.transform;
                m_ScaleReferenceRenderer = scaleReferenceObj.GetComponent<Renderer>();
                m_ScaleReferenceRenderer.material = m_ScaleReferenceMaterialClone;
            }

            if (m_ScaleReferenceMaterialClone == null)
                m_ScaleReferenceMaterialClone = Instantiate(m_ScaleReferenceMaterial);

            if (m_ScaleReferenceRenderer.sharedMaterial == null)
                m_ScaleReferenceRenderer.material = m_ScaleReferenceMaterialClone;

            // Leave the small version around for half of the new order of magnitude
            // or the icon will take up too much screen space
            const float sizeExponentOffset = 0.7f;
            var sizeExponent = Mathf.Log10(sceneView.size / worldScale) - sizeExponentOffset;
            var previousIconIndex = m_CurrentScaleReferenceIcon;
            var closestIntExponent = Mathf.RoundToInt(sizeExponent);
            m_CurrentScaleReferenceIcon = closestIntExponent - m_MinScaleExponent;
            if (m_CurrentScaleReferenceIcon < 0)
                m_CurrentScaleReferenceIcon = 0;

            if (m_CurrentScaleReferenceIcon >= m_ScaleVisuals.Length)
                m_CurrentScaleReferenceIcon = m_ScaleVisuals.Length - 1;

            if (m_CurrentScaleReferenceIcon != previousIconIndex || m_ScaleReferenceMaterialClone.mainTexture == null)
                m_ScaleReferenceMaterialClone.mainTexture = m_ScaleVisuals[m_CurrentScaleReferenceIcon].scaleVisual;

            var selectedGameObject = Selection.activeGameObject;
            if (selectedGameObject != null)
            {
                var bounds = BoundsUtils.GetBounds(selectedGameObject.transform);
                m_ScaleReferenceOrigin = new Vector3(bounds.max.x, bounds.min.y, bounds.center.z);
            }

            var iconScale = Mathf.Pow(10, m_CurrentScaleReferenceIcon - 2) * worldScale;
            var scaleReferenceTransform = m_ScaleReference.transform;
            scaleReferenceTransform.position = m_ScaleReferenceOrigin + k_ScaleReferenceOffset * iconScale;

            var cameraTransform = sceneView.camera.transform;
            scaleReferenceTransform.LookAt(cameraTransform.position, cameraTransform.up);
            scaleReferenceTransform.localScale = Vector3.one * iconScale;
        }

        public float AdjustWorldScale(float scale)
        {
            k_Entities.Clear();
            k_SpatialConditions.Clear();
            k_AudioSources.Clear();
            k_ReverbZones.Clear();
            k_SceneLights.Clear();

            // Static collections used below are cleared by the methods that use them
            SceneManager.GetActiveScene().GetRootGameObjects(k_RootGameObjects);
            foreach (var gameObject in k_RootGameObjects)
            {
                gameObject.GetComponentsInChildren(k_EntityList);
                k_Entities.AddRange(k_EntityList);

                gameObject.GetComponentsInChildren(k_SpatialConditionList);
                k_SpatialConditions.AddRange(k_SpatialConditionList);

                gameObject.GetComponentsInChildren(k_AudioSources);
                gameObject.GetComponentsInChildren(k_ReverbZones);
                gameObject.GetComponentsInChildren(k_SceneLights);
            }

            k_EntityTransformList.Clear();
            k_EntityChildTransformList.Clear();
            foreach (var entity in k_Entities)
            {
                var entityTransform = entity.transform;
                k_EntityTransformList.Add(entityTransform);
                foreach (Transform child in entity.transform)
                {
                    k_EntityChildTransformList.Add(child);
                }
            }

            var previousScale = GetWorldScale();
            if (previousScale < 0)
                previousScale = Mathf.Epsilon;

            if (scale < 0)
                scale = Mathf.Epsilon;

            var scaleProperty = GetWorldScaleProperty();
            if (scaleProperty == null)
                return previousScale;

            scaleProperty.vector3Value = Vector3.one * scale;
            scaleProperty.serializedObject.ApplyModifiedProperties();

            var userPrefs = MARSUserPreferences.instance;
            var scalePositions = userPrefs.scaleEntityPositions;
            if (scalePositions)
            {
                var entityTransforms = k_EntityTransformList.Cast<UnityObject>().ToArray();
                Undo.RecordObjects(entityTransforms, "WorldScale");
            }
            else
            {
                var spatialConditionObjects = k_SpatialConditions.Cast<UnityObject>().ToArray();
                Undo.RecordObjects(spatialConditionObjects, "WorldScale");
                foreach (var spatialCondition in k_SpatialConditions)
                {
                    // We inverse scale condition parameters to account for the fact that entity positions stay the same.
                    spatialCondition.ScaleParameters(previousScale / scale);
                }
            }

            var scaleChildren = userPrefs.scaleEntityChildren;
            if (scaleChildren)
            {
                var entityChildTransforms = k_EntityChildTransformList.Cast<UnityObject>().ToArray();
                Undo.RecordObjects(entityChildTransforms, "WorldScale");
            }

            var scaleFactor = scale / previousScale;
            foreach (var entity in k_Entities)
            {
                if (scalePositions)
                    entity.transform.position *= scaleFactor;

                if (scaleChildren)
                {
                    foreach (Transform child in entity.transform)
                    {
                        child.localScale *= scaleFactor;
                        child.localPosition *= scaleFactor;
                        var rectTransform = child.GetComponent<RectTransform>();
                        if (rectTransform)
                            rectTransform.anchoredPosition3D = child.localPosition;
                    }
                }
            }

            HandleAudioScaling(m_EnvironmentAudioSources, m_EnvironmentReverbZones, scaleFactor);
            HandleLightScaling(m_EnvironmentLights, scaleFactor);

            if (userPrefs.scaleSceneAudio)
                HandleAudioScaling(k_AudioSources, k_ReverbZones, scaleFactor);

            if (userPrefs.scaleSceneLighting)
                HandleLightScaling(k_SceneLights, scaleFactor);

            if (userPrefs.scaleClippingPlanes)
                ScaleClippingPlanes(scaleFactor);

            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            return scale;
        }

        static void ScaleClippingPlanes(float scaleFactor)
        {
            var session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            if (session == null || session.transform == null)
                return;

            var camera = session.cameraReference.GetComponent<Camera>();
            Undo.RecordObject(camera, "WorldScale");
            camera.nearClipPlane *= scaleFactor;
            camera.farClipPlane *= scaleFactor;
        }

        /// <summary>
        /// Clears the list of Components that have scalar values that need world scale applied to them
        /// </summary>
        public void ClearEnvironmentRangeScaledComponents()
        {
            m_EnvironmentAudioSources.Clear();
            m_EnvironmentReverbZones.Clear();
            m_EnvironmentLights.Clear();
        }

        /// <summary>
        /// Gets Components that have values that need world scale applied to them.
        /// </summary>
        /// <param name="go">game object that we are getting components on</param>
        public void GetEnvironmentRangeScaledComponents(GameObject go)
        {
            go.GetComponentsInChildren(m_EnvironmentAudioSources);
            go.GetComponentsInChildren(m_EnvironmentReverbZones);
            go.GetComponentsInChildren(m_EnvironmentLights);
        }

        /// <summary>
        /// Scales synthetic environment components with scalar values not effect by the lossy scale of an object.
        /// </summary>
        public void ApplyWorldScaleToEnvironment()
        {
            var scaleFactor = GetWorldScale();
            HandleAudioScaling(m_EnvironmentAudioSources, m_EnvironmentReverbZones, scaleFactor);
            HandleLightScaling(m_EnvironmentLights, scaleFactor);
        }

        static void HandleLightScaling(List<Light> lights, float scaleFactor)
        {
            UnityObjectUtils.RemoveDestroyedObjects(lights);
            Undo.RecordObjects(lights.ToArray<UnityObject>(), "WorldScale");
            foreach (var light in lights)
            {
                // Directional lights internally normalize their range
                // so the effect of the scaling only is visible when scaling and produces strange results.
                if (light.type != LightType.Directional)
                    light.range *= scaleFactor;
            }
        }

        static void HandleAudioScaling(List<AudioSource> audioSources, List<AudioReverbZone> reverbZones, float scaleFactor)
        {
            UnityObjectUtils.RemoveDestroyedObjects(audioSources);
            Undo.RecordObjects(audioSources.ToArray<UnityObject>(), "WorldScale");
            foreach (var source in audioSources)
            {
                source.minDistance *= scaleFactor;
                source.maxDistance *= scaleFactor;
            }

            UnityObjectUtils.RemoveDestroyedObjects(reverbZones);
            Undo.RecordObjects(reverbZones.ToArray<UnityObject>(), "WorldScale");
            foreach (var zone in reverbZones)
            {
                zone.minDistance *= scaleFactor;
                zone.maxDistance *= scaleFactor;
            }
        }

        void OnValidate()
        {
            UpdateScaleValues();
        }

        void UpdateScaleValues()
        {
            m_MinScale = Mathf.Pow(k_BaseLog, m_MinScaleExponent);
            m_MaxScale = Mathf.Pow(k_BaseLog, m_MaxScaleExponent);

            if (m_MinScaleExponent == 0 || m_MaxScaleExponent == 0)
                m_VisualsZeroIndex = 0;
            else if (m_MaxScaleExponent > -1 && m_MinScaleExponent < 1)
                m_VisualsZeroIndex = m_ScaleVisuals.Length - m_MaxScaleExponent - 1;
            else
                m_VisualsZeroIndex = -1;
        }
    }
}
