using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Recording;
using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace Unity.Labs.MARS
{
    public class PointCloudRecorder : DataRecorder, IUsesPointCloud
    {
        internal struct PointCloudEvent
        {
            public float Time;
            public List<SerializedPointCloudData> Data;
        }

        List<PointCloudEvent> m_PointCloudEvents = new List<PointCloudEvent>();

        internal List<PointCloudEvent> PointCloudEvents
        {
            get { return m_PointCloudEvents; }
            set { m_PointCloudEvents = value; }
        }

#if !FI_AUTOFILL
        IProvidesPointCloud IFunctionalitySubscriber<IProvidesPointCloud>.provider { get; set; }
#endif

        public override DataRecording CreateDataRecording(TimelineAsset timeline, List<Object> newAssets)
        {
            var signalTrack = timeline.CreateTrack<SignalTrack>(null, "Point Cloud Events");
            foreach (var pointCloudEvent in m_PointCloudEvents)
            {
                var time = pointCloudEvent.Time;
                var marker = signalTrack.CreateMarker<PointCloudEventMarker>(time);
                marker.Data = pointCloudEvent.Data;
                marker.name = $"Points {time}";
            }

            var recording = ScriptableObject.CreateInstance<PointCloudRecording>();
            recording.SignalTrack = signalTrack;
            recording.hideFlags = HideFlags.NotEditable;
            return recording;
        }

        protected override void Setup()
        {
            this.SubscribePointCloudUpdated(OnPointCloudUpdated);
        }

        protected override void TearDown()
        {
            this.UnsubscribePointCloudUpdated(OnPointCloudUpdated);
        }

        void OnPointCloudUpdated(Dictionary<MarsTrackableId, PointCloudData> data)
        {
            var dataList = new List<SerializedPointCloudData>();
            foreach (var kvp in data)
            {
                var eventData = new SerializedPointCloudData();
                var pointCloudData = kvp.Value;
                var identifiers = pointCloudData.Identifiers;
                if (identifiers.HasValue)
                    eventData.Identifiers = identifiers.Value.ToArray();

                var positions = pointCloudData.Positions;
                if (positions.HasValue)
                    eventData.Positions = positions.Value.ToArray();

                var confidenceValues = pointCloudData.ConfidenceValues;
                if (confidenceValues.HasValue)
                    eventData.ConfidenceValues = confidenceValues.Value.ToArray();

                dataList.Add(eventData);
            }

            m_PointCloudEvents.Add(new PointCloudEvent { Time = TimeFromStart, Data = dataList});
        }
    }
}
