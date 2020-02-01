using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.Labs.MARS
{
    public class PointCloudRecording : DataRecording
    {
        const string k_ProviderName = "Recorded Point Cloud Provider";

        [SerializeField]
        SignalTrack m_SignalTrack;

        public SignalTrack SignalTrack { set { m_SignalTrack = value; } }

        public override void SetupDataProviders(PlayableDirector director, List<IFunctionalityProvider> providers)
        {
            var providerObj = GameObjectUtils.Create(k_ProviderName);
            var pointCloudProvider = providerObj.AddComponent<RecordedPointCloudProvider>();
            providers.Add(pointCloudProvider);
            director.SetGenericBinding(m_SignalTrack, providerObj);
        }
    }
}
