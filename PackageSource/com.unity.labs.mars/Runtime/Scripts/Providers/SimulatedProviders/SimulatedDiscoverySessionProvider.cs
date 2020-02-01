using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
#if UNITY_EDITOR
    public class SimulatedDiscoverySessionProvider : MonoBehaviour, IProvidesSessionControl
    {
#pragma warning disable 649
        [SerializeField]
        SimulatedDiscoveryPlanesProvider m_PlanesProvider;

        [SerializeField]
        SimulatedDiscoveryPointCloudProvider m_PointCloudProvider;
#pragma warning restore 649

        public bool Destroyed { get; private set; }
        public bool Paused { get; private set; }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var subscriber = obj as IFunctionalitySubscriber<IProvidesSessionControl>;
            if (subscriber != null)
                subscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        public bool SessionExists() { return !Destroyed; }

        public bool SessionRunning() { return !Destroyed && !Paused; }

        public bool SessionReady() { return true; }

        public void CreateSession() { Destroyed = false; }

        public void DestroySession() { Destroyed = true; }

        public void ResetSession()
        {
            m_PlanesProvider.ClearPlanes();
            m_PointCloudProvider.ClearPoints();
        }

        public void PauseSession()
        {
            Paused = true;
        }

        public void ResumeSession()
        {
            Paused = false;
        }
    }
#else
    public class SimulatedDiscoverySessionProvider : MonoBehaviour
    {
        [SerializeField]
        SimulatedDiscoveryPlanesProvider m_PlanesProvider;

        [SerializeField]
        SimulatedDiscoveryPointCloudProvider m_PointCloudProvider;
    }
#endif
}
