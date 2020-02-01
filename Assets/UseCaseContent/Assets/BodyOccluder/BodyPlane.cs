using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// All-in-one script for managing a body occlusion plane
    /// </summary>
    public class BodyPlane : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Transform m_ViewSource;

        [SerializeField]
        GameObject m_Border;
#pragma warning restore 649

        void Start ()
        {
            if (m_ViewSource == null)
            {
                m_ViewSource = Camera.main.transform;
            }

            // Hide border object
            m_Border.SetActive(false);
	    }

        // Update is called once per frame
        void Update ()
        {
            // Face the camera on XZ
            var targetPoint = m_ViewSource.position;
            targetPoint.y = transform.position.y;
            transform.LookAt(targetPoint);
        }
    }
}

