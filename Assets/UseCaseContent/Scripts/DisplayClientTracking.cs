using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.MARS
{
    [RequireComponent(typeof(Text))]
    public class DisplayClientTracking : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        Proxy m_RealWorldObject;
#pragma warning restore 649

        Text m_Text;
        QueryState m_LastQueryState;

        void Awake()
        {
            m_Text = GetComponent<Text>();
            UpdateTrackingState(m_LastQueryState);
        }

        void Update()
        {
            var queryState = m_RealWorldObject.queryState;
            if (m_LastQueryState != queryState)
            {
                m_LastQueryState = queryState;
                UpdateTrackingState(queryState);
            }
        }

        void UpdateTrackingState(QueryState trackingState)
        {
            m_Text.text = m_RealWorldObject.name;
            switch (trackingState)
            {
                case QueryState.Tracking:
                    m_Text.text += " tracking";
                    break;
                case QueryState.Unavailable:
                    m_Text.text += " lost tracking";
                    break;
                case QueryState.Unknown:
                    m_Text.text += " uninitialized";
                    break;
                case QueryState.Querying:
                    m_Text.text += " querying";
                    break;
                case QueryState.Acquiring:
                    m_Text.text += " acquiring";
                    break;
                case QueryState.Resuming:
                    break;
                default:
                    m_Text.text += " not tracking";
                    break;
            }
        }
    }
}
