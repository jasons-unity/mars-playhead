using System;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.SpatialTracking;

namespace Unity.Labs.MARS.Providers
{
    public class CameraPoseProvider : TrackedPoseDriver, IProvidesCameraPose, IUsesCameraOffset
    {
        const float k_MoveSpeed = 0.1f;
        const float k_SpeedBoost = 10f;
        const float k_TurnSpeed = 10f;

        public event Action<Pose> poseUpdated;
#pragma warning disable 67 // TODO: See about getting tracking state from TrackedPoseDriver
        public event Action<MRCameraTrackingState> trackingStateChanged;
#pragma warning restore 67

#if !FI_AUTOFILL
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }
#endif

        bool m_WasMouseDownLastFrame;
        Vector3 m_LastMousePosition;
        Vector3 m_EulerAngles;

#if UNITY_EDITOR && INCLUDE_XR_MOCK
        bool m_IsRemoteActive;
#endif

        void Start()
        {
            SetPoseSource(DeviceType.GenericXRDevice, TrackedPose.ColorCamera);
            UseRelativeTransform = false;

            var camera = Camera.main;
            if (camera)
            {
                var cameraTransform = camera.transform;
                var cameraParent = cameraTransform.parent;
                if (!cameraParent)
                {
                    cameraParent = new GameObject("XRCameraRig").transform;
                    cameraTransform.parent = cameraParent;
                }

                transform.SetParent(cameraParent, false);
            }

#if UNITY_EDITOR && INCLUDE_XR_MOCK
            // Cache value at start because call to FindObjectsOfTypeAll<> is expensive
            if (EditorOnlyDelegates.IsRemoteActive != null)
                m_IsRemoteActive = EditorOnlyDelegates.IsRemoteActive();
#endif
        }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var cameraSubscriber = obj as IFunctionalitySubscriber<IProvidesCameraPose>;
            if (cameraSubscriber != null)
                cameraSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        public Pose GetCameraPose()
        {
            return transform.GetLocalPose();
        }

        protected override void PerformUpdate()
        {
#if UNITY_EDITOR
#if INCLUDE_XR_MOCK
            if (m_IsRemoteActive)
            {
                base.PerformUpdate();
                if (poseUpdated != null)
                    poseUpdated(GetCameraPose());

                return;
            }
#endif

            var moveSpeed = this.GetCameraScale() * k_MoveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
                moveSpeed *= k_SpeedBoost;

            var cameraPose = GetCameraPose();
            var forward = cameraPose.rotation * Vector3.forward;
            var right = cameraPose.rotation * Vector3.right;
            var up = cameraPose.rotation * Vector3.up;

            if (Input.GetKey(KeyCode.W))
                cameraPose.position += forward * Time.deltaTime * moveSpeed;

            if (Input.GetKey(KeyCode.S))
                cameraPose.position -= forward * Time.deltaTime * moveSpeed;

            if (Input.GetKey(KeyCode.A))
                cameraPose.position -= right * Time.deltaTime * moveSpeed;

            if (Input.GetKey(KeyCode.D))
                cameraPose.position += right * Time.deltaTime * moveSpeed;

            if (Input.GetKey(KeyCode.Q))
                cameraPose.position += up * Time.deltaTime * moveSpeed;

            if (Input.GetKey(KeyCode.Z))
                cameraPose.position -= up * Time.deltaTime * moveSpeed;

            if (Input.GetMouseButton(1))
            {
                if (!m_WasMouseDownLastFrame)
                    m_LastMousePosition = Input.mousePosition;

                var deltaPosition = Input.mousePosition - m_LastMousePosition;
                var turnSpeed = Time.deltaTime * k_TurnSpeed;
                m_EulerAngles.y += turnSpeed * deltaPosition.x;
                m_EulerAngles.x -= turnSpeed * deltaPosition.y;
                cameraPose.rotation = Quaternion.Euler(m_EulerAngles);
                m_LastMousePosition = Input.mousePosition;
                m_WasMouseDownLastFrame = true;
            }
            else
            {
                m_WasMouseDownLastFrame = false;
            }

            transform.SetLocalPose(cameraPose);
#else
            base.PerformUpdate();
#endif

            if (poseUpdated != null)
                poseUpdated(GetCameraPose());

            //TODO: Tracking state
        }
    }
}
