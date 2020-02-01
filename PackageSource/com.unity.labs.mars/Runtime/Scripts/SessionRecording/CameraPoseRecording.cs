using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Unity.Labs.MARS
{
    public class CameraPoseRecording : DataRecording
    {
        const string k_ProviderName = "Recorded Camera Provider";

        [SerializeField]
        AnimationTrack m_AnimationTrack;

        public AnimationTrack AnimationTrack
        {
            set { m_AnimationTrack = value; }
        }

        public override void SetupDataProviders(PlayableDirector director, List<IFunctionalityProvider> providers)
        {
            var providerObj = GameObjectUtils.Create(k_ProviderName);
            var animator = providerObj.AddComponent<Animator>();
            director.SetGenericBinding(m_AnimationTrack, animator);
            providers.Add(providerObj.AddComponent<RecordedCameraProvider>());
        }
    }
}
