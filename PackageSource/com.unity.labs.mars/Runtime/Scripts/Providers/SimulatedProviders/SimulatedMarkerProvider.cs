using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using UnityEngine;

namespace Unity.Labs.MARS.Providers
{
#if UNITY_EDITOR
    [AddComponentMenu("")]
    public class SimulatedMarkerProvider : SimulatedTrackablesProvider<MRMarker>, IProvidesMarkerTracking, IProvidesTraits<bool>,
        IProvidesTraits<Pose>, IProvidesTraits<Vector2>, IProvidesTraits<string>, IUsesMARSTrackableData<MRMarker>
    {
        static readonly TraitDefinition[] k_ProvidedTraits =
        {
            TraitDefinitions.Marker,
            TraitDefinitions.Pose,
            TraitDefinitions.Bounds2D,
            TraitDefinitions.MarkerId
        };

// Suppresses the warning "The event 'event' is never used", because it is not an issue if the marker provider events are not used
#pragma warning disable 67
        public event Action<MRMarker> markerAdded;
        public event Action<MRMarker> markerUpdated;
        public event Action<MRMarker> markerRemoved;
#pragma warning restore 67

        public TraitDefinition[] GetProvidedTraits() { return k_ProvidedTraits; }

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
            if (obj is IUsesMarkerTracking markerTrackingSubscriber)
                markerTrackingSubscriber.provider = this;
        }

        public void UnloadProvider() { }

        public bool SetActiveMarkerLibrary(IMRMarkerLibrary activeLibrary) { return true; }
        public void StopTrackingMarkers() { }
        public void StartTrackingMarkers() { }

        public void GetMarkers(List<MRMarker> markers)
        {
            foreach (var pair in m_SimulatedTrackables)
            {
                markers.AddRange(pair.Value);
            }
        }

        protected override void AddObjectTrackables(SimulatedObject simulatedObject)
        {
            var objectMarkers = new List<MRMarker>();
            foreach (var synthesizedMarker in simulatedObject.GetComponentsInChildren<SynthesizedMarker>())
            {
                if (!synthesizedMarker.isActiveAndEnabled)
                    continue;

                synthesizedMarker.Initialize();
                var marker = synthesizedMarker.GetData();

                if (marker.id == MarsTrackableId.InvalidId)
                    continue;

                objectMarkers.Add(marker);
                var dataId = this.AddOrUpdateData(marker);
                synthesizedMarker.dataID = dataId;
                this.AddOrUpdateTrait(dataId, TraitNames.Marker, true);
                this.AddOrUpdateTrait(dataId, TraitNames.MarkerId, marker.markerId.ToString());
                this.AddOrUpdateTrait(dataId, TraitNames.Pose, marker.pose);
                this.AddOrUpdateTrait(dataId, TraitNames.Bounds2D, marker.extents);

                foreach (var synthTag in synthesizedMarker.GetComponents<SynthesizedSemanticTag>())
                {
                    if (!synthTag.isActiveAndEnabled)
                        continue;

                    this.AddOrUpdateTrait(dataId, synthTag.TraitName, synthTag.GetTraitData());
                }

                markerAdded?.Invoke(marker);
            }

            if (objectMarkers.Count > 0)
                m_SimulatedTrackables[simulatedObject] = objectMarkers;
        }

        protected override void UpdateObjectTrackables(SimulatedObject simulatedObject)
        {
            m_SimulatedTrackables[simulatedObject].Clear();

            foreach (var synthesizedMarker in simulatedObject.GetComponentsInChildren<SynthesizedMarker>())
            {
                var marker = synthesizedMarker.GetData();
                var dataId = this.AddOrUpdateData(marker);
                this.AddOrUpdateTrait(dataId, TraitNames.Pose, marker.pose);
                this.AddOrUpdateTrait(dataId, TraitNames.Bounds2D, marker.extents);
                markerUpdated?.Invoke(marker);
                m_SimulatedTrackables[simulatedObject].Add(marker);
            }
        }

        protected override void RemoveTrackable(MRMarker trackable)
        {
            var dataId = this.RemoveData(trackable);
            this.RemoveTrait<bool>(dataId, TraitNames.Marker);
            this.RemoveTrait<string>(dataId, TraitNames.MarkerId);
            this.RemoveTrait<Vector2>(dataId, TraitNames.Bounds2D);
            this.RemoveTrait<Pose>(dataId, TraitNames.Pose);
            markerRemoved?.Invoke(trackable);
        }
    }
#else
    public class SimulatedMarkerProvider : MonoBehaviour { }
#endif
}
