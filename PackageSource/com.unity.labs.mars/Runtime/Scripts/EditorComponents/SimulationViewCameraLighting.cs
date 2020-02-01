using System;
#if UNITY_EDITOR
using Unity.Labs.Utils;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class SimulationViewCameraLighting : MonoBehaviour
    {
#if UNITY_EDITOR
        Camera m_Camera;

        public event Action OnCameraPreRenderEnvironment;
        public event Action OnCameraPreRenderContent;

        [NonSerialized]
        public Scene EnvironmentScene = new Scene();

        [NonSerialized]
        public Scene ContentScene = new Scene();

        void OnEnable()
        {
            m_Camera = GetComponent<Camera>();

            if (m_Camera == null)
                UnityObjectUtils.Destroy(this);
        }

        void OnPreRender()
        {
            if (!m_Camera.scene.IsValid())
                return;

            if (!(m_Camera.scene == EnvironmentScene || m_Camera.scene == ContentScene))
                return;

#if UNITY_2018_4_OR_NEWER || UNITY_2019_1_OR_NEWER
            Unsupported.SetOverrideLightingSettings(m_Camera.scene);
#else
            Unsupported.SetOverrideRenderSettings(m_Camera.scene);
#endif
            if (m_Camera.scene == EnvironmentScene && OnCameraPreRenderEnvironment != null)
                OnCameraPreRenderEnvironment.Invoke();
            else if (m_Camera.scene == ContentScene && OnCameraPreRenderContent != null)
                OnCameraPreRenderContent.Invoke();
        }

        void OnPostRender()
        {
            if (!m_Camera.scene.IsValid())
                return;

            if (!(m_Camera.scene == EnvironmentScene || m_Camera.scene == ContentScene))
                return;

#if UNITY_2018_4_OR_NEWER || UNITY_2019_1_OR_NEWER
            Unsupported.RestoreOverrideLightingSettings();
#else
            Unsupported.RestoreOverrideRenderSettings();
#endif
        }
#endif // UNITY_EDITOR
    }
}
