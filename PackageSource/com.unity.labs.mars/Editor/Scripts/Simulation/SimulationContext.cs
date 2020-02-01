using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.Video;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// State that determines the setup of a given simulation
    /// </summary>
    public class SimulationContext
    {
        EnvironmentMode m_EnvironmentMode;
        GameObject m_EnvironmentPrefab;
        VideoClip m_RecordedVideo;
        SessionRecordingInfo m_SessionRecording;
        bool m_Temporal;

        public HashSet<Type> SceneSubscriberTypes { get; } = new HashSet<Type>();
        public HashSet<TraitRequirement> SceneRequirements { get; } = new HashSet<TraitRequirement>();

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        // Reference type collections must also be cleared after use
        static readonly HashSet<Type> k_SubscriberTypes = new HashSet<Type>();
        static readonly HashSet<TraitRequirement> k_TraitRequirements = new HashSet<TraitRequirement>();

        internal bool Update(MARSSession marsSession, List<IFunctionalitySubscriber> subscribers, bool temporal, bool videoSimulation)
        {
            k_SubscriberTypes.Clear();
            k_TraitRequirements.Clear();

            k_SubscriberTypes.Add(typeof(QuerySimulationModule));
            foreach (var subscriber in subscribers)
            {
                k_SubscriberTypes.Add(subscriber.GetType());
            }

            k_TraitRequirements.UnionWith(marsSession.requirements.TraitRequirements);
            if (videoSimulation)
            {
                // Pretend the scene requires "Face" if it doesn't already, so that ULS will spin up and start video feed
                k_TraitRequirements.Add(TraitDefinitions.Face);
            }
            else
            {
                // Otherwise ignore "Face" requirement since we're not using a video environment
                k_TraitRequirements.Remove(TraitDefinitions.Face);
            }

            var environmentMode = SimulationSettings.environmentMode;
            var environmentPrefab = SimulationSettings.environmentPrefab;
            var recordedVideo = SimulationSettings.recordedVideo;
            var sessionRecording = SimulationSettings.instance.UseEnvironmentRecording ?
                SimulationSettings.instance.GetRecordingForCurrentEnvironment() : null;

            var changed = !SceneSubscriberTypes.SetEquals(k_SubscriberTypes) ||
                          !SceneRequirements.SetEquals(k_TraitRequirements) ||
                          m_EnvironmentMode != environmentMode ||
                          m_EnvironmentPrefab != environmentPrefab ||
                          m_RecordedVideo != recordedVideo ||
                          m_SessionRecording != sessionRecording ||
                          m_Temporal != temporal;

            SceneSubscriberTypes.Clear();
            SceneSubscriberTypes.UnionWith(k_SubscriberTypes);
            SceneRequirements.Clear();
            SceneRequirements.UnionWith(k_TraitRequirements);
            m_EnvironmentMode = environmentMode;
            m_EnvironmentPrefab = environmentPrefab;
            m_RecordedVideo = recordedVideo;
            m_SessionRecording = sessionRecording;
            m_Temporal = temporal;

            k_SubscriberTypes.Clear();
            k_TraitRequirements.Clear();
            return changed;
        }

        public void Clear()
        {
            SceneSubscriberTypes.Clear();
            SceneRequirements.Clear();
            m_EnvironmentMode = default;
            m_EnvironmentPrefab = null;
            m_RecordedVideo = null;
            m_SessionRecording = null;
            m_Temporal = false;
        }
    }
}
