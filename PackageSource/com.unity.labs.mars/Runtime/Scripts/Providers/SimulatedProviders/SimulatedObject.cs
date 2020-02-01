using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Identifies an object as a simulated replacement for real world data.
    /// It injects functionality into all synthesized traits and trackables on this object.
    /// </summary>
    public class SimulatedObject : MonoBehaviour, IUsesFunctionalityInjection
    {
        public event Action<SimulatedObject> onDisabled;

        readonly List<SynthesizedTrait> m_Traits = new List<SynthesizedTrait>();
        readonly List<SynthesizedTrackable> m_Trackables = new List<SynthesizedTrackable>();

        public List<SynthesizedTrait> traits { get { return m_Traits; } }

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
#endif

        void OnDisable()
        {
            if (onDisabled != null)
                onDisabled(this);
        }

        void OnEnable()
        {
            GetComponentsInChildren(m_Traits);
            GetComponentsInChildren(m_Trackables);

            this.InjectFunctionalitySingle(this);
            foreach (var currentTrait in m_Traits)
            {
                this.InjectFunctionalitySingle(currentTrait);
            }

            foreach (var currentTrackable in m_Trackables)
            {
                this.InjectFunctionalitySingle(currentTrackable);
            }
        }
    }
}
