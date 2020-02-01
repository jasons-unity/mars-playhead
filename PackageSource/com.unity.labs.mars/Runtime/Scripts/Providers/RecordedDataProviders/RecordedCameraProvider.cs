using System;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public class RecordedCameraProvider : MonoBehaviour, IProvidesCameraPose, IProvidesCameraProjectionMatrix
    {
#pragma warning disable 67
        public event Action<Pose> poseUpdated;
        public event Action<MRCameraTrackingState> trackingStateChanged;
#pragma warning restore 67

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

        public Pose GetCameraPose() { return transform.GetLocalPose(); }

        public Matrix4x4? GetProjectionMatrix() { return null; }

        void Update()
        {
            if (poseUpdated != null)
                poseUpdated(GetCameraPose());
        }
    }
}
