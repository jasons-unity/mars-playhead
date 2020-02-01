using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS.Behaviors
{
    [AddComponentMenu("")]
    public class MARSCamera : MonoBehaviour, IUsesCameraPose, IUsesCameraProjectionMatrix, ISimulatable
    {
#pragma warning disable 649
        [SerializeField]
        GameObject m_TrackingWarning;
#pragma warning restore 649

        Camera m_Camera;

#if !FI_AUTOFILL
        IProvidesCameraPose IFunctionalitySubscriber<IProvidesCameraPose>.provider { get; set; }
        IProvidesCameraProjectionMatrix IFunctionalitySubscriber<IProvidesCameraProjectionMatrix>.provider { get; set; }
#endif

        void OnEnable()
        {
            this.SubscribePoseUpdated(OnPoseUpdated);
            m_Camera = GetComponent<Camera>();
            m_Camera.clearFlags = CameraClearFlags.SolidColor;
            m_Camera.backgroundColor = Color.black;
            if (m_TrackingWarning)
                this.SubscribeTrackingTypeChanged(OnTrackingStateChanged);

            var projectionMatrix = this.GetProjectionMatrix();
            if (projectionMatrix.HasValue)
                m_Camera.projectionMatrix = projectionMatrix.Value;

            transform.SetLocalPose(this.GetPose());
        }

        void OnDisable()
        {
            this.UnsubscribePoseUpdated(OnPoseUpdated);
            this.UnsubscribeTrackingTypeChanged(OnTrackingStateChanged);
        }

        void OnTrackingStateChanged(MRCameraTrackingState state)
        {
            switch (state)
            {
                case MRCameraTrackingState.Normal:
                    m_TrackingWarning.SetActive(false);
                    break;
                default:
                    m_TrackingWarning.SetActive(true);
                    break;
            }
        }

        void OnPoseUpdated(Pose pose)
        {
            var projectionMatrix = this.GetProjectionMatrix();
            if (projectionMatrix.HasValue)
                m_Camera.projectionMatrix = projectionMatrix.Value;

            transform.SetLocalPose(pose);
        }
    }
}
