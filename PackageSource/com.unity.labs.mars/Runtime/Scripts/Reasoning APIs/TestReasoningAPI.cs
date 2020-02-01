using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    [CreateAssetMenu(menuName = "MARS/Test ReasoningAPI")]
    public class TestReasoningAPI : ScriptableObject, IReasoningAPI
    {
        static readonly TraitDefinition[] k_ProvidedTraits = null;

#pragma warning disable 649
        [SerializeField]
        float m_ProcessSceneInterval;
#pragma warning restore 649

        [SerializeField]
        string m_ProcessMessage = "Processing Scene!";

        [SerializeField]
        string m_UpdateMessage = "Updating data!";

        public float processSceneInterval { get { return m_ProcessSceneInterval; } }

        void IReasoningAPI.Setup() { }

        void IReasoningAPI.TearDown() { }

        void IReasoningAPI.ProcessScene()
        {
            if (string.IsNullOrEmpty(m_ProcessMessage) != true)
            {
                Debug.Log(m_ProcessMessage);
            }
        }

        void IReasoningAPI.UpdateData()
        {
            if (string.IsNullOrEmpty(m_UpdateMessage) != true)
            {
                Debug.Log(m_UpdateMessage);
            }
        }

        public TraitDefinition[] GetProvidedTraits() { return k_ProvidedTraits; }
    }
}
