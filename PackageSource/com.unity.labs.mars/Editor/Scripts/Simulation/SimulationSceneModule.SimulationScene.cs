using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    public partial class SimulationSceneModule
    {
        //Adapted from PreviewRenderUtility.cs PreviewScene
        class SimulationScene : IDisposable
        {
            readonly HashSet<Camera> m_Cameras = new HashSet<Camera>();
            readonly bool m_IsPreview;

            internal Scene contentScene { get; private set; }
            internal Scene environmentScene { get; private set; }

            internal SimulationScene()
            {
                // When not in play mode, use a preview scene for simulation
                m_IsPreview = !(EditorApplication.isPlayingOrWillChangePlaymode);

                contentScene = m_IsPreview
                    ? EditorSceneManager.NewPreviewScene()
                    : SceneManager.CreateScene("Simulated Content Scene");

                if (!contentScene.IsValid())
                    throw new InvalidOperationException("Preview scene could not be created");

                environmentScene = m_IsPreview
                    ? EditorSceneManager.NewPreviewScene()
                    : SceneManager.CreateScene("Simulated Environment Scene");

                if (!environmentScene.IsValid())
                    throw new InvalidOperationException("Preview scene could not be created");

                EditorOnlyDelegates.GetAllSimulationSceneCameras = GetAllSimulationSceneCameras;
            }

            internal void AddSimulatedGameObject(GameObject go, bool isContent)
            {
                SceneManager.MoveGameObjectToScene(go, isContent ? contentScene : environmentScene);
            }

            internal bool IsCameraAssignedToSimulationScene(Camera camera)
            {
                var cameraScene = camera.scene;
                return m_Cameras.Contains(camera) && (cameraScene == contentScene || cameraScene == environmentScene);
            }

            internal void AssignCameraToSimulation(Camera camera)
            {
                if (IsCameraAssignedToSimulationScene(camera))
                {
                    Debug.LogWarning($"{camera.name} is already assigned to Sim View.");
                    return;
                }

                camera.enabled = false;

                // Explicitly use forward rendering for all previews
                // (deferred fails when generating some static previews at editor launch; and we never want
                // vertex lit previews if that is chosen in the player settings)
                camera.renderingPath = RenderingPath.Forward;
                camera.useOcclusionCulling = false;
                camera.scene = contentScene;
                m_Cameras.Add(camera);

                if (EditorOnlyDelegates.AddToSimulationViewCameraLighting != null)
                    EditorOnlyDelegates.AddToSimulationViewCameraLighting(camera, environmentScene);
            }

            internal void RemoveCameraFromSimulationScene(Camera camera, bool dispose = false)
            {
                if (dispose || !m_Cameras.Remove(camera))
                    return;

                camera.scene = SceneManager.GetActiveScene();
            }

            public void Dispose()
            {
                if (m_Cameras != null && m_Cameras.Count > 0)
                {
                    foreach (var camera in m_Cameras)
                    {
                        if (camera != null)
                            RemoveCameraFromSimulationScene(camera, true);
                    }
                    m_Cameras.Clear();
                }

                CloseScene(contentScene);
                CloseScene(environmentScene);

                EditorOnlyDelegates.GetAllSimulationSceneCameras = null;
                contentScene = new Scene();
                environmentScene = new Scene();
            }

            void CloseScene(Scene scene)
            {
                if (!scene.IsValid())
                    return;

                if (m_IsPreview)
                    EditorSceneManager.ClosePreviewScene(scene);
                else
                    SceneManager.UnloadSceneAsync(scene);
            }

            void GetAllSimulationSceneCameras(List<Camera> cameras)
            {
                cameras.AddRange(m_Cameras);
            }
        }
    }
}
