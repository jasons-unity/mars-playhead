using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS.Providers
{
    public class CameraOffsetProvider : MonoBehaviour, IProvidesCameraOffset
    {
        const float k_MinScale = 0.001f;

        Vector3 m_PositionOffset;
        float m_YawOffset;
        float m_Scale = 1;
        float m_InverseScale = 1;
        Quaternion m_RotationOffset = Quaternion.identity;
        Quaternion m_InverseRotationOffset = Quaternion.identity;
        Vector3 m_ScaleOffset = Vector3.one;
        Matrix4x4 m_OffsetMatrix = Matrix4x4.identity;

        Transform m_CameraTransform;
        Transform m_CameraParentTransform;

        public Vector3 cameraPositionOffset
        {
            get { return m_PositionOffset; }
            set
            {
                m_PositionOffset = value;
                if (m_CameraParentTransform == null)
                    GetCameraParent();

                m_CameraParentTransform.position = value;
                CreateOffsetMatrix();
            }
        }

        public float cameraYawOffset
        {
            get { return m_YawOffset; }
            set
            {
                m_YawOffset = value;
                m_RotationOffset = Quaternion.AngleAxis(value, Vector3.up);
                m_InverseRotationOffset = Quaternion.Inverse(m_RotationOffset);
                if (m_CameraParentTransform == null)
                    GetCameraParent();

                m_CameraParentTransform.rotation = m_RotationOffset;
                CreateOffsetMatrix();
            }
        }

        public float cameraScale
        {
            get { return m_Scale; }
            set
            {
                if (value <= 0)
                    value = k_MinScale;

                m_InverseScale = 1 / value;
                m_Scale = value;
                m_ScaleOffset = Vector3.one * value;
                if (m_CameraParentTransform == null)
                    GetCameraParent();

                m_CameraParentTransform.localScale = m_ScaleOffset;
                CreateOffsetMatrix();
            }
        }

        public Matrix4x4 CameraOffsetMatrix { get { return m_OffsetMatrix; } }

        /// <summary>
        /// Apply the camera offset to a pose and return the modified pose
        /// </summary>
        /// <param name="pose">The pose to which the offset will be applied</param>
        /// <returns>The modified pose</returns>
        public Pose ApplyOffsetToPose(Pose pose)
        {
            pose.position = m_RotationOffset * pose.position * m_Scale + m_PositionOffset;
            pose.rotation = m_RotationOffset * pose.rotation;
            return pose;
        }

        /// <summary>
        /// Apply the inverse of the camera offset to a pose and return the modified pose
        /// </summary>
        /// <param name="pose">The pose to which the offset will be applied</param>
        /// <returns>The modified pose</returns>
        public Pose ApplyInverseOffsetToPose(Pose pose)
        {
            pose.position = m_InverseRotationOffset * (pose.position - m_PositionOffset) * m_InverseScale;
            pose.rotation = m_InverseRotationOffset * pose.rotation;
            return pose;
        }

        /// <summary>
        /// Apply the camera offset to a position and return the modified position
        /// </summary>
        /// <param name="position">The position to which the offset will be applied</param>
        /// <returns>The modified position</returns>
        public Vector3 ApplyOffsetToPosition(Vector3 position)
        {
            return m_RotationOffset * position * m_Scale + m_PositionOffset;
        }

        /// <summary>
        /// Apply the inverse of the camera offset to a position and return the modified position
        /// </summary>
        /// <param name="position">The position to which the offset will be applied</param>
        /// <returns>The modified position</returns>
        public Vector3 ApplyInverseOffsetToPosition(Vector3 position)
        {
            return m_InverseRotationOffset * (position - m_PositionOffset) * m_InverseScale;
        }

        public Vector3 ApplyOffsetToDirection(Vector3 direction)
        {
            return m_RotationOffset * direction;
        }

        public Vector3 ApplyInverseOffsetToDirection(Vector3 direction)
        {
            return m_InverseRotationOffset * direction;
        }

        /// <summary>
        /// Apply the camera offset to a rotation and return the modified rotation
        /// </summary>
        /// <param name="rotation">The rotation to which the offset will be applied</param>
        /// <returns>The modified rotation</returns>
        public Quaternion ApplyOffsetToRotation(Quaternion rotation)
        {
            return m_RotationOffset * rotation;
        }

        /// <summary>
        /// Apply the inverse of the camera offset to a rotation and return the modified rotation
        /// </summary>
        /// <param name="rotation">The rotation to which the offset will be applied</param>
        /// <returns>The modified rotation</returns>
        public Quaternion ApplyInverseOffsetToRotation(Quaternion rotation)
        {
            return m_InverseRotationOffset * rotation;
        }

        void OnEnable()
        {
            if (m_CameraParentTransform == null)
                GetCameraParent();

            m_PositionOffset = m_CameraParentTransform.position;
            m_YawOffset = m_CameraParentTransform.rotation.eulerAngles.y;
            m_Scale = m_CameraParentTransform.localScale.x; // Assume uniform  scale

            m_RotationOffset = Quaternion.AngleAxis(m_YawOffset, Vector3.up);
            m_InverseRotationOffset = Quaternion.Inverse(m_RotationOffset);
            m_CameraParentTransform.rotation = m_RotationOffset;

            m_InverseScale = 1 / m_Scale;
            m_ScaleOffset = Vector3.one * m_Scale;
            m_CameraParentTransform.localScale = m_ScaleOffset;

            CreateOffsetMatrix();
        }

        void GetCameraParent()
        {
            var camera = MARSRuntimeUtils.GetMainOrSimulatedCamera();
            if (camera == null)
                camera = FindObjectOfType<Camera>();

            if (camera == null)
            {
                m_CameraParentTransform = transform;
                return;
            }

            m_CameraTransform = camera.transform;
            m_CameraParentTransform = m_CameraTransform.parent;
            if (!m_CameraParentTransform)
            {
                m_CameraParentTransform = new GameObject("XRCameraRig").transform;
                m_CameraTransform.SetParent(m_CameraParentTransform, false);
            }
        }

        void CreateOffsetMatrix()
        {
            m_OffsetMatrix = Matrix4x4.TRS(m_PositionOffset, m_RotationOffset, m_ScaleOffset);
        }

        public void LoadProvider() {}

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var cameraSubscriber = obj as IFunctionalitySubscriber<IProvidesCameraOffset>;
            if (cameraSubscriber != null)
                cameraSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() {}
    }
}
