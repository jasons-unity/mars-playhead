#if INCLUDE_AR_FOUNDATION
using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace Unity.Labs.MARS.Providers
{
    class XRSDKReferencePointProvider : IProvidesReferencePoints, IProvidesTraits<Pose>, IUsesMARSTrackableData<MRReferencePoint>
    {
        static readonly TraitDefinition[] k_ProvidedTraits = { TraitDefinitions.Pose };

        readonly Dictionary<MarsTrackableId, ARReferencePoint> k_TrackableIds = new Dictionary<MarsTrackableId, ARReferencePoint>();

        ARReferencePointManager m_ARReferencePointManager;
        ARReferencePointManager m_NewARReferencePointManager;

        public event Action<MRReferencePoint> pointAdded;
        public event Action<MRReferencePoint> pointUpdated;
        public event Action<MRReferencePoint> pointRemoved;

        public TraitDefinition[] GetProvidedTraits() { return k_ProvidedTraits; }

        public void LoadProvider()
        {
            ARFoundationSessionProvider.RequireARSession();

            var currentSession = ARFoundationSessionProvider.currentSession;
            if (currentSession)
            {
                var currentSessionGameObject = currentSession.gameObject;
                m_ARReferencePointManager = currentSessionGameObject.GetComponent<ARReferencePointManager>();
                if (!m_ARReferencePointManager)
                {
                    m_ARReferencePointManager = currentSessionGameObject.AddComponent<ARReferencePointManager>();
                    m_ARReferencePointManager.hideFlags = HideFlags.DontSave;
                    m_NewARReferencePointManager = m_ARReferencePointManager;
                }

                m_ARReferencePointManager.referencePointsChanged += ARReferencePointManagerOnReferencePointsChanged;
            }
        }

        void ARReferencePointManagerOnReferencePointsChanged(ARReferencePointsChangedEventArgs changedEvent)
        {
            foreach (var point in changedEvent.removed)
            {
                var mrPoint = point.ToMRReferencePoint();
                k_TrackableIds.Remove(mrPoint.id);
                var dataId = this.RemoveData(mrPoint);
                this.RemoveTrait(dataId, TraitNames.Pose);
                if (pointRemoved != null)
                    pointRemoved(mrPoint);
            }

            foreach (var point in changedEvent.updated)
            {
                var mrPoint = point.ToMRReferencePoint();
                var dataId = this.AddOrUpdateData(mrPoint);
                this.AddOrUpdateTrait(dataId, TraitNames.Pose, mrPoint.pose);
                if (pointUpdated != null)
                    pointUpdated(mrPoint);
            }

            foreach (var point in changedEvent.added)
            {
                var mrPoint = point.ToMRReferencePoint();
                k_TrackableIds[mrPoint.id] = point;
                var dataId = this.AddOrUpdateData(mrPoint);
                this.AddOrUpdateTrait(dataId, TraitNames.Pose, mrPoint.pose);
                if (pointAdded != null)
                    pointAdded(mrPoint);
            }
        }

        public void UnloadProvider()
        {
            m_ARReferencePointManager.referencePointsChanged -= ARReferencePointManagerOnReferencePointsChanged;
            if (m_NewARReferencePointManager)
                UnityObjectUtils.Destroy(m_NewARReferencePointManager);

            ARFoundationSessionProvider.TearDownARSession();
        }

        public void GetAllReferencePoints(List<MRReferencePoint> referencePoints)
        {
            foreach (var point in m_ARReferencePointManager.trackables)
            {
                referencePoints.Add(point.ToMRReferencePoint());
            }
        }

        public bool TryAddReferencePoint(Pose pose, out MarsTrackableId referencePointId)
        {
            if (m_ARReferencePointManager)
            {
                var pointAdded = m_ARReferencePointManager.AddReferencePoint(pose);
                referencePointId = pointAdded.ToMRReferencePoint().id;
                return true;
            }

            referencePointId = default(MarsTrackableId);
            return false;
        }

        public bool TryGetReferencePoint(MarsTrackableId id, out MRReferencePoint referencePoint)
        {
            ARReferencePoint arReferencePoint;
            if (!k_TrackableIds.TryGetValue(id, out arReferencePoint) || m_ARReferencePointManager == null)
            {
                referencePoint = default(MRReferencePoint);
                return false;
            }

            var result = m_ARReferencePointManager.GetReferencePoint(arReferencePoint.trackableId);
            referencePoint = result.ToMRReferencePoint();
            return true;
        }

        public bool TryRemoveReferencePoint(MarsTrackableId id)
        {
            ARReferencePoint referencePoint;
            if (!k_TrackableIds.TryGetValue(id, out referencePoint))
                return false;

            if (m_ARReferencePointManager)
                return m_ARReferencePointManager.RemoveReferencePoint(referencePoint);

            return false;
        }

        public void StopTrackingReferencePoints()
        {
            if (m_ARReferencePointManager && m_ARReferencePointManager.subsystem != null)
                m_ARReferencePointManager.subsystem.Stop();
        }

        public void StartTrackingReferencePoints()
        {
            if (m_ARReferencePointManager && m_ARReferencePointManager.subsystem != null)
                m_ARReferencePointManager.subsystem.Start();
        }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var referencePointSubscriber = obj as IFunctionalitySubscriber<IProvidesReferencePoints>;
            if (referencePointSubscriber != null)
                referencePointSubscriber.provider = this;
#endif
        }
    }
}
#endif
