using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    ///  Defines a region of space that can be cut into dynamically to view the contents
    /// </summary>
    [DisallowMultipleComponent]
    public class XRayRegion : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The floor in local coordinates")]
        float m_FloorHeight = 0.0f;

        [SerializeField]
        [Tooltip("The ceiling in local coordinates")]
        float m_CeilingHeight = 2.5f;

        [SerializeField]
        [Tooltip("How much the camera clipping plane moves forward from the center of this region")]
        float m_ClipOffset = 0.5f;

        [SerializeField]
        [Tooltip("The active size of the clipping region")]
        Vector3 m_ViewBounds = new Vector3(3.0f, 3.0f, 3.0f);

        /// <summary>
        /// The floor  in local coordinates
        /// </summary>
        public float FloorHeight { get { return m_FloorHeight; } }
        public float CeilingHeight { get { return m_CeilingHeight; } }
        public float ClipOffset { get { return m_ClipOffset; } }
        public Vector3 ViewBounds { get { return m_ViewBounds; } }

        void OnDrawGizmosSelected()
        {
            var cubePosition = transform.position;

            var drawPosition = cubePosition;
            drawPosition.y += (m_CeilingHeight + m_FloorHeight) * 0.5f;

            var interiorHeight = m_CeilingHeight - m_FloorHeight;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(drawPosition, new Vector3(m_ViewBounds.x, interiorHeight, m_ViewBounds.z));

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(cubePosition, m_ViewBounds);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(drawPosition, new Vector3(m_ClipOffset*2.0f, interiorHeight, m_ClipOffset*2.0f));
        }
    }
}
