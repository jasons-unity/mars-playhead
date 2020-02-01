using UnityEngine;
using UnityEngine.PrefabHandles;

namespace Unity.Labs.MARS
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ScaleWorldCanvasWithCameraDistance : HandleBehaviour
    {
        Vector3? m_InitialScale;
        RectTransform m_RectTransform;

        protected override void PreRender(Camera camera)
        {
            base.PreRender(camera);
            if (m_RectTransform == null)
                m_RectTransform = GetComponent<RectTransform>();

            if (m_InitialScale == null)
                m_InitialScale = m_RectTransform.localScale;

            m_RectTransform.localScale = HandleUtility.GetHandleSize(m_RectTransform.position) * m_InitialScale.Value;
        }
    }
}
