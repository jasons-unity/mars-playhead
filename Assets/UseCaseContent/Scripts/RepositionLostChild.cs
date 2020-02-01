using System.Collections.Generic;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS.UseCaseContent
{
    public class RepositionLostChild : MonoBehaviour, ISetMatchAcquireHandler, ISetMatchUpdateHandler,
        ISetMatchLossHandler, ISetMatchTimeoutHandler
    {
#pragma warning disable 649
        [SerializeField]
        Proxy m_Child;

        [SerializeField]
        Transform m_TargetUponLoss;
#pragma warning restore 649

        readonly List<Transform> m_ChildContent = new List<Transform>();

        void Awake()
        {
            m_ChildContent.Clear();
            foreach (Transform content in m_Child.transform)
            {
                m_ChildContent.Add(content);
            }
        }

        void OnDisable()
        {
            var childTransform = m_Child.transform;
            foreach (var content in m_ChildContent)
            {
                content.SetParent(childTransform, false);
            }
        }

        public void OnSetMatchAcquire(SetQueryResult queryResult) { }

        public void OnSetMatchUpdate(SetQueryResult queryResult)
        {
            if (queryResult.nonRequiredChildrenLost.Contains(m_Child))
            {
                foreach (var content in m_ChildContent)
                {
                    content.SetParent(m_TargetUponLoss, false);
                    content.gameObject.SetActive(true);
                }
            }
        }

        public void OnSetMatchLoss(SetQueryResult queryResult) { }

        public void OnSetMatchTimeout(SetQueryArgs queryArgs) { }
    }
}
