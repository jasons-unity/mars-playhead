using System;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS.Providers
{
    public class SimulatedCameraProvider : MonoBehaviour, IProvidesCameraPose, IProvidesCameraProjectionMatrix,
        IUsesDeviceSimulationSettings
    {
        [SerializeField]
        bool m_UseMovementBoundsInGame = true;

        CameraFPSModeHandler m_FPSModeHandler;

        public event Action<Pose> poseUpdated;
#pragma warning disable 67
        public event Action<MRCameraTrackingState> trackingStateChanged;
#pragma warning restore 67

#if !FI_AUTOFILL
        IProvidesDeviceSimulationSettings IFunctionalitySubscriber<IProvidesDeviceSimulationSettings>.provider { get; set; }
#endif

        public Pose GetCameraPose() { return transform.GetLocalPose(); }

        public Matrix4x4? GetProjectionMatrix() { return null; }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var cameraPoseSubscriber = obj as IUsesCameraPose;
            if (cameraPoseSubscriber != null)
                cameraPoseSubscriber.provider = this;

            var cameraProjectionMatrixSubscriber = obj as IUsesCameraProjectionMatrix;
            if (cameraProjectionMatrixSubscriber != null)
                cameraProjectionMatrixSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                m_FPSModeHandler = new CameraFPSModeHandler();
                this.SubscribeEnvironmentChanged(OnEnvironmentChanged);
                return;
            }

            transform.SetWorldPose(this.GetDeviceStartingPose());
        }

        void OnDisable()
        {
            if (Application.isPlaying)
                this.UnsubscribeEnvironmentChanged(OnEnvironmentChanged);
        }

        void OnEnvironmentChanged()
        {
            if (Application.isPlaying)
            {
                transform.SetWorldPose(this.GetDeviceStartingPose());
                poseUpdated?.Invoke(GetCameraPose());
            }
        }

        void Update()
        {
            var playing = Application.isPlaying;
            var fpsModeContext = playing ? m_FPSModeHandler : CameraFPSModeHandler.activeHandler;

            if (fpsModeContext == null)
                return;

            if (playing)
            {
                fpsModeContext.UseMovementBounds = m_UseMovementBoundsInGame;
                fpsModeContext.MovementBounds = this.GetEnvironmentBounds();
                fpsModeContext.HandleGameInput();
            }

            if (!fpsModeContext.MoveActive)
                return;

            var pose = fpsModeContext.CalculateMovement(transform.GetWorldPose(), playing);
            transform.SetWorldPose(pose);
            if (poseUpdated != null)
                poseUpdated(GetCameraPose());
        }
    }
}
