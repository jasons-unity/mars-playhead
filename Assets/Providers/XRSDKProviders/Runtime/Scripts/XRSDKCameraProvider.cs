#if INCLUDE_AR_FOUNDATION
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS.Providers
{
    public class XRSDKCameraProvider : IProvidesCameraTexture, IProvidesCameraProjectionMatrix
    {
        ARCameraManager m_NewARCameraManager;
        ARCameraManager m_ARCameraManager;
        ARCameraBackground m_NewARCameraBackground;

        Texture2D m_CurrentTexture;
        Matrix4x4? m_CurrentProjectionMatrix;

        public void LoadProvider()
        {
            ARFoundationSessionProvider.RequireARSession();
            InitializeCameraProvider();
        }

        void InitializeCameraProvider()
        {
            var camera = UnityObject.FindObjectOfType<Camera>();

            if (camera)
            {
                if (!camera.GetComponent<ARCameraBackground>())
                {
                    m_NewARCameraBackground = camera.gameObject.AddComponent<ARCameraBackground>();
                    m_NewARCameraBackground.hideFlags = HideFlags.DontSave;
                }

                m_ARCameraManager = camera.GetComponent<ARCameraManager>();

                if (!m_ARCameraManager)
                {
                    m_ARCameraManager = camera.gameObject.AddComponent<ARCameraManager>();
                    m_NewARCameraManager = m_ARCameraManager;
                    m_ARCameraManager.hideFlags = HideFlags.DontSave;
                }

                m_ARCameraManager.frameReceived += ARCameraManagerOnFrameReceived;
            }

            m_CurrentTexture = null;
            m_CurrentProjectionMatrix = null;
        }

        void ARCameraManagerOnFrameReceived(ARCameraFrameEventArgs cameraFrameEvent)
        {
            m_CurrentProjectionMatrix = cameraFrameEvent.projectionMatrix;

            m_CurrentTexture = cameraFrameEvent.textures.Count > 0 ? cameraFrameEvent.textures[0] : null;
        }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var cameraSubscriber = obj as IFunctionalitySubscriber<IProvidesCameraTexture>;
            if (cameraSubscriber != null)
                cameraSubscriber.provider = this;

            var projectionMatrixSubscriber = obj as IFunctionalitySubscriber<IProvidesCameraProjectionMatrix>;
            if (projectionMatrixSubscriber != null)
                projectionMatrixSubscriber.provider = this;
#endif
        }

        public void UnloadProvider()
        {
            ShutdownCameraProvider();
            ARFoundationSessionProvider.TearDownARSession();
        }

        void ShutdownCameraProvider()
        {
            m_ARCameraManager.frameReceived -= ARCameraManagerOnFrameReceived;

            if (m_NewARCameraBackground)
                UnityObjectUtils.Destroy(m_NewARCameraBackground);

            if (m_NewARCameraManager)
                UnityObjectUtils.Destroy(m_NewARCameraManager);
        }

        public Matrix4x4? GetProjectionMatrix()
        {
            return m_CurrentProjectionMatrix;
        }

        public Texture2D GetCameraTexture()
        {
            return m_CurrentTexture;
        }
    }
}
#endif
