using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.Timeline;

namespace Unity.Labs.MARS
{
    public class PlaneFindingRecorder : DataRecorder, IUsesPlaneFinding
    {
        struct PlaneEvent
        {
#pragma warning disable 649
            public float time;
            public MRPlane plane;
            public PlaneEventType eventType;
#pragma warning restore 649
        }

        List<PlaneEvent> m_PlaneEvents = new List<PlaneEvent>();

#if !FI_AUTOFILL
        IProvidesPlaneFinding IFunctionalitySubscriber<IProvidesPlaneFinding>.provider { get; set; }
#endif

        public override DataRecording CreateDataRecording(TimelineAsset timeline, List<Object> newAssets)
        {
            var signalTrack = timeline.CreateTrack<SignalTrack>(null, "Plane Events");
            foreach (var planeEvent in m_PlaneEvents)
            {
                var marker = signalTrack.CreateMarker<PlaneEventMarker>(planeEvent.time);
                var plane = planeEvent.plane;
                var eventType = planeEvent.eventType;
                marker.Plane = plane;
                marker.EventType = eventType;
                marker.name = $"{eventType} Plane {plane.id}";
            }

            var recording = ScriptableObject.CreateInstance<PlaneFindingRecording>();
            recording.SignalTrack = signalTrack;
            recording.hideFlags = HideFlags.NotEditable;
            return recording;
        }

        protected override void Setup()
        {
            this.SubscribePlaneAdded(OnPlaneAdded);
            this.SubscribePlaneUpdated(OnPlaneUpdated);
            this.SubscribePlaneRemoved(OnPlaneRemoved);
        }

        protected override void TearDown()
        {
            this.UnsubscribePlaneAdded(OnPlaneAdded);
            this.UnsubscribePlaneUpdated(OnPlaneUpdated);
            this.UnsubscribePlaneRemoved(OnPlaneRemoved);
        }

        void OnPlaneAdded(MRPlane plane)
        {
            m_PlaneEvents.Add(new PlaneEvent
            {
                time = TimeFromStart,
                plane = RecordPlane(plane), // The plane keeps its collection references as it updates, so capture a copy of the plane
                eventType = PlaneEventType.Added
            });
        }

        void OnPlaneUpdated(MRPlane plane)
        {
            m_PlaneEvents.Add(new PlaneEvent
            {
                time = TimeFromStart,
                plane = RecordPlane(plane),
                eventType = PlaneEventType.Updated
            });
        }

        void OnPlaneRemoved(MRPlane plane)
        {
            m_PlaneEvents.Add(new PlaneEvent
            {
                time = TimeFromStart,
                plane = RecordPlane(plane),
                eventType = PlaneEventType.Removed
            });
        }

        static MRPlane RecordPlane(MRPlane plane)
        {
            return new MRPlane
            {
                id = plane.id,
                alignment = plane.alignment,
                pose = plane.pose,
                center = plane.center,
                extents = plane.extents,
                vertices = new List<Vector3>(plane.vertices),
                textureCoordinates = new List<Vector2>(plane.textureCoordinates),
                normals = new List<Vector3>(plane.normals),
                indices = new List<int>(plane.indices)
            };
        }
    }
}
