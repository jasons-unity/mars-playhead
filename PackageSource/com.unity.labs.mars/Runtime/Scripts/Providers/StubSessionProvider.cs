using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [AddComponentMenu("")]
    public class StubSessionProvider : MonoBehaviour, IProvidesSessionControl
    {
        bool m_Destroyed;
        bool m_Paused;

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {

#if !FI_AUTOFILL
            var sessionSubscriber = obj as IFunctionalitySubscriber<IProvidesSessionControl>;
            if (sessionSubscriber != null)
                sessionSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        public bool SessionExists() { return !m_Destroyed; }

        public bool SessionRunning() { return !m_Destroyed && !m_Paused; }

        public bool SessionReady() { return true; }

        public void CreateSession() { m_Destroyed = false; }

        public void DestroySession() { m_Destroyed = true; }

        public void ResetSession() { }

        public void PauseSession() { m_Paused = true; }

        public void ResumeSession() { m_Paused = false; }
    }
}
