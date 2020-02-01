using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Helper class to make glow act more like a point light
    /// by making the projector always orient away from the camera
    /// </summary>
    public class ProjectedGlow : MonoBehaviour
    {
        [SerializeField]
        Transform m_ViewSource;

        // Use this for initialization
        void Start ()
        {
            if (m_ViewSource == null)
            {
                m_ViewSource = Camera.main.transform;
            }
        }

        void Update ()
        {
            // We want to face a point that is opposite of the camera position, so we just mirror the camera point relative to this glow source
            var targetPoint = 2*transform.position - m_ViewSource.position;
            transform.LookAt(targetPoint);
        }
    }
}
