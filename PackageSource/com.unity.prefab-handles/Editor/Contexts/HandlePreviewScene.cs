using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if HDRP_ENABLED
using UnityEngine.Rendering.HighDefinition;
#endif


namespace UnityEditor.PrefabHandles
{
    [DefaultExecutionOrder(1000)] 
    internal sealed class HandlePreviewScene : ScriptableSingleton<HandlePreviewScene> 
    {
        const string k_SceneName = "handle-rendering-scene";

        Scene m_Scene;
        Camera m_Camera;

        static readonly Predicate<GameObject> s_IsNull = (go) => go == null;
        readonly List<GameObject> m_Handles = new List<GameObject>(16);

        public static Camera camera
        {
            get { return instance.m_Camera; }
        }

        public static IReadOnlyList<GameObject> handles
        {
            get
            {
                var handle = instance.m_Handles;
                handle.RemoveAll(s_IsNull);
                return handle;
            }
        }

        void OnEnable()
        {
            EnsureSceneExists();
        }

        void OnDisable()
        {
            EditorSceneManager.ClosePreviewScene(m_Scene);
        }

        public static void CopyCamera(Camera cameraToCopy)
        {
            camera.CopyFrom(cameraToCopy);
            SetupCamera(camera, instance.m_Scene);
        }

        public static void AddHandle(GameObject handle)
        {
            instance.AddHandle_Impl(handle);
        }

        void AddHandle_Impl(GameObject handle)
        {
            EnsureSceneExists();
            SceneManager.MoveGameObjectToScene(handle, m_Scene);
        }

        void EnsureSceneExists()
        {
            if (m_Scene.IsValid())
                return;

            CreateScene(out m_Scene, out m_Camera);
        }

        static void CreateScene(out Scene scene, out Camera camera)
        {
            scene = SceneManager.GetSceneByName(k_SceneName);
            if (!scene.IsValid())
            {
                scene = EditorSceneManager.NewPreviewScene();
            }

            //Cleanup
            var roots = scene.GetRootGameObjects();
            for (int i = roots.Length - 1; i >= 0; --i)
            {
                Object.DestroyImmediate(roots[i]);
            }

            camera = new GameObject("Camera").AddComponent<Camera>();
            camera.hideFlags = HideFlags.HideAndDontSave;
            camera.enabled = false;
            SetupCamera(camera, scene);
            SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
        }

        static void SetupCamera(Camera camera, Scene scene)
        {
            camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = LayerMask.GetMask("Default");
            camera.cameraType = CameraType.SceneView;
            camera.overrideSceneCullingMask = 0;
            camera.scene = scene;

#if HDRP_ENABLED
              HDAdditionalCameraData cameraData;
              if (!camera.gameObject.TryGetComponent<HDAdditionalCameraData>(out cameraData))
                  cameraData = camera.gameObject.AddComponent<HDAdditionalCameraData>();

              cameraData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
              cameraData.backgroundColorHDR = Color.clear;
#endif
        }
    }
}