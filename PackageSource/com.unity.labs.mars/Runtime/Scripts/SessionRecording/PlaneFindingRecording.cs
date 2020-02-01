using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.Labs.MARS
{
    public class PlaneFindingRecording : DataRecording
    {
        const string k_ProviderName = "Recorded Planes Provider";

        [SerializeField]
        SignalTrack m_SignalTrack;

        public SignalTrack SignalTrack { set { m_SignalTrack = value; } }

        public override void SetupDataProviders(PlayableDirector director, List<IFunctionalityProvider> providers)
        {
            var providerObj = GameObjectUtils.Create(k_ProviderName);
            var planesProvider = providerObj.AddComponent<RecordedPlanesProvider>();
            providers.Add(planesProvider);
            director.SetGenericBinding(m_SignalTrack, providerObj);
        }
    }
}
