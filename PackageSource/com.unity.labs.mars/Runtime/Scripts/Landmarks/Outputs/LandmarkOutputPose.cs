using System;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Component that contains pose data (position and rotation) for a landmark.
    /// </summary>
    public class LandmarkOutputPose : MonoBehaviour, ILandmarkOutput
    {
        [SerializeField]
        bool m_ShowPoseAxes = true;

        [SerializeField]
        Color m_PoseAxesColor = Color.cyan;

        [SerializeField]
        [Range(0.01f, 1f)]
        float m_PoseAxesSize = 0.1f;

        Pose m_CurrentPose;

#pragma warning disable 649
        [SerializeField]
        [Tooltip("If enabled, the landmark rotation will be ignored when setting the transform. " +
            "Enable this if another component is setting the rotation.")]
        bool m_IgnoreRotation;
#pragma warning restore 649

        public Pose currentPose { get { return m_CurrentPose; } set { m_CurrentPose = value; } }

        public void UpdateOutput()
        {
            if (m_IgnoreRotation)
                transform.position = currentPose.position;
            else
                transform.SetWorldPose(currentPose);
        }

        void OnDrawGizmosSelected()
        {
            if (!m_ShowPoseAxes)
                return;

            Gizmos.color = m_PoseAxesColor;
            Gizmos.DrawWireSphere(currentPose.position, m_PoseAxesSize * 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawLine(currentPose.position, currentPose.position + currentPose.rotation * Vector3.right * m_PoseAxesSize);

            Gizmos.color = Color.green;
            Gizmos.DrawLine(currentPose.position, currentPose.position + currentPose.rotation * Vector3.up * m_PoseAxesSize);

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(currentPose.position, currentPose.position + currentPose.rotation * Vector3.forward * m_PoseAxesSize);
        }
    }
}
