using UnityEngine;

namespace Unity.Labs.MARS
{
    public class AdjustConditionOnTap : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        LayerMask m_LayerMask;

        [SerializeField]
        ConditionBase m_Condition;
#pragma warning restore 649

        void OnDisable()
        {
            m_Condition.adjusting = false;
        }

        void Update()
        {
            var mainCamera = Camera.main;
            if (Input.GetMouseButtonUp(0) && mainCamera != null)
            {
                if (RuntimeHandleContextModule.IsInteracting)
                    return;

                var ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, mainCamera.farClipPlane, m_LayerMask))
                {
                    if (hit.collider.gameObject == gameObject)
                    {
                        m_Condition.adjusting = !m_Condition.adjusting;
                    }
                }
            }
        }
    }
}
