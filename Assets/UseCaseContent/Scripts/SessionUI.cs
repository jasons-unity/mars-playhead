using System;
using TMPro;
using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.Labs.MARS
{
    public class SessionUI : MonoBehaviour, IUsesFunctionalityInjection
    {
        class Subscriber : IUsesSessionControl, IUsesPointCloud, IUsesPlaneFinding, IUsesMarkerTracking
        {
#if !FI_AUTOFILL
            IProvidesSessionControl IFunctionalitySubscriber<IProvidesSessionControl>.provider { get; set; }
            IProvidesPointCloud IFunctionalitySubscriber<IProvidesPointCloud>.provider { get; set; }
            IProvidesPlaneFinding IFunctionalitySubscriber<IProvidesPlaneFinding>.provider { get; set; }
            IProvidesMarkerTracking IFunctionalitySubscriber<IProvidesMarkerTracking>.provider { get; set; }
#endif
        }

        [Serializable]
        class PauseEvent : UnityEvent<bool> { }

        const string k_PauseText = "Pause";
        const string k_ResumeText = "Resume";

#pragma warning disable 649
        [SerializeField]
        TextMeshProUGUI m_Text;
#pragma warning restore 649

        readonly Subscriber m_Subscriber = new Subscriber();

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
#endif

        void Start()
        {
            m_Text.text = MARSCore.instance.paused ? k_ResumeText : k_PauseText;
            this.InjectFunctionalitySingle(m_Subscriber);
        }

        public void TogglePaused()
        {
            var paused = MARSCore.instance.paused;
            if (paused)
            {
                m_Text.text = k_PauseText;
                paused = false;
            }
            else
            {
                m_Text.text = k_ResumeText;
                paused = true;
            }

            MARSCore.instance.paused = paused;
            if (paused)
            {
                if (m_Subscriber.HasProvider<IProvidesPointCloud>())
                    m_Subscriber.StopDetectingPoints();

                if (m_Subscriber.HasProvider<IProvidesPlaneFinding>())
                    m_Subscriber.StopDetectingPlanes();

                if (m_Subscriber.HasProvider<IProvidesMarkerTracking>())
                    m_Subscriber.StopTrackingMarkers();
            }
            else
            {
                if (m_Subscriber.HasProvider<IProvidesPointCloud>())
                    m_Subscriber.StartDetectingPoints();

                if (m_Subscriber.HasProvider<IProvidesPlaneFinding>())
                    m_Subscriber.StartDetectingPlanes();

                if (m_Subscriber.HasProvider<IProvidesMarkerTracking>())
                    m_Subscriber.StartTrackingMarkers();
            }
        }

        public void TriggerSessionReset()
        {
            if (m_Subscriber.HasProvider<IProvidesSessionControl>())
                m_Subscriber.ResetSession();
        }
    }
}
