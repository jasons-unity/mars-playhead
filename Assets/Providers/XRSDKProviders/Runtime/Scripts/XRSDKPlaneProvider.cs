#if INCLUDE_AR_FOUNDATION
using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Unity.Labs.MARS.Providers
{
    class XRSDKPlaneProvider : IProvidesPlaneFinding, IProvidesTraits<bool>, IProvidesTraits<Pose>,
        IProvidesTraits<Vector2>, IProvidesTraits<int>, IUsesMARSTrackableData<MRPlane>, ITrackableProvider
    {
        static readonly TraitDefinition[] k_ProvidedTraits =
        {
            TraitDefinitions.Plane,
            TraitDefinitions.Pose,
            TraitDefinitions.Bounds2D,
            TraitDefinitions.Alignment
        };

        ARPlaneManager m_ARPlaneManager;
        ARPlaneManager m_NewARPlaneManager;

        public event Action<MRPlane> planeAdded;
        public event Action<MRPlane> planeUpdated;
        public event Action<MRPlane> planeRemoved;

        public TraitDefinition[] GetProvidedTraits() { return k_ProvidedTraits; }

        public void LoadProvider()
        {
            ARFoundationSessionProvider.RequireARSession();

            var currentSession = ARFoundationSessionProvider.currentSession;
            if (currentSession)
            {
                var currentSessionGameObject = currentSession.gameObject;
                m_ARPlaneManager = currentSessionGameObject.GetComponent<ARPlaneManager>();
                if (!m_ARPlaneManager)
                {
                    m_ARPlaneManager = currentSessionGameObject.AddComponent<ARPlaneManager>();
                    m_ARPlaneManager.hideFlags = HideFlags.DontSave;
                    m_NewARPlaneManager = m_ARPlaneManager;
                }

                m_ARPlaneManager.planesChanged += ARPlaneManagerOnPlanesChanged;
            }

            AddExistingTrackables();
        }

        void ARPlaneManagerOnPlanesChanged(ARPlanesChangedEventArgs changedEvent)
        {
            foreach (var plane in changedEvent.removed)
            {
                RemovePlane(plane);
            }

            foreach (var plane in changedEvent.updated)
            {
                TryUpdatePlane(plane);
            }

            foreach (var plane in changedEvent.added)
            {
                TryAddPlane(plane);
            }
        }

        public void AddExistingTrackables()
        {
#if !UNITY_EDITOR
            if (m_ARPlaneManager == null)
                return;

            foreach (var plane in m_ARPlaneManager.trackables)
            {
                TryAddPlane(plane);
            }
#endif
        }

        public void UnloadProvider()
        {
            m_ARPlaneManager.planesChanged -= ARPlaneManagerOnPlanesChanged;

            if (m_NewARPlaneManager)
                UnityObjectUtils.Destroy(m_NewARPlaneManager);

            ARFoundationSessionProvider.TearDownARSession();
        }

        public void GetPlanes(List<MRPlane> planes)
        {
            if (m_ARPlaneManager == null)
                return;

            foreach (var plane in m_ARPlaneManager.trackables)
            {
                if(plane.subsumedBy == null)
                    planes.Add(plane.ToMRPlane());
            }
        }

        public void StopDetectingPlanes()
        {
            if (m_ARPlaneManager != null && m_ARPlaneManager.subsystem != null)
                m_ARPlaneManager.subsystem.Stop();
        }

        public void StartDetectingPlanes()
        {
            if (m_ARPlaneManager && m_ARPlaneManager.subsystem != null)
                m_ARPlaneManager.subsystem.Start();
        }

        void TryAddPlane(ARPlane arPlane)
        {
            if(arPlane.subsumedBy != null)
                return;

            AddPlane(arPlane);
        }

        void TryUpdatePlane(ARPlane arPlane)
        {
            if (arPlane.subsumedBy == null)
            {
                UpdatePlane(arPlane);
            }
            else
            {
                RemovePlane(arPlane);
            }
        }

        void AddPlane(ARPlane arPlane)
        {
            var plane = arPlane.ToMRPlane();
            var id = this.AddOrUpdateData(plane);
            var worldCenter = plane.pose.position + ( plane.pose.rotation * plane.center );
            var centerPose = new Pose(worldCenter, plane.pose.rotation);
            this.AddOrUpdateTrait(id, TraitNames.Plane, true);
            this.AddOrUpdateTrait(id, TraitNames.Pose, centerPose);
            this.AddOrUpdateTrait(id, TraitNames.Bounds2D, plane.extents);
            this.AddOrUpdateTrait(id, TraitNames.Alignment, (int)plane.alignment);
            if (planeAdded != null)
                planeAdded(plane);
        }

        void UpdatePlane(ARPlane arPlane)
        {
            var plane = arPlane.ToMRPlane();
            var id = this.AddOrUpdateData(plane);
            this.AddOrUpdateTrait(id, TraitNames.Pose, plane.pose);
            this.AddOrUpdateTrait(id, TraitNames.Bounds2D, plane.extents);
            if (planeUpdated != null)
                planeUpdated(plane);
        }

        void RemovePlane(ARPlane arPlane)
        {
            var plane = arPlane.ToMRPlane();
            var id = this.RemoveData(plane);
            this.RemoveTrait<bool>(id, TraitNames.Plane);
            this.RemoveTrait<Pose>(id, TraitNames.Pose);
            this.RemoveTrait<Vector2>(id, TraitNames.Bounds2D);
            this.RemoveTrait<int>(id, TraitNames.Alignment);
            if (planeRemoved != null)
                planeRemoved(plane);
        }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var planeSubscriber = obj as IFunctionalitySubscriber<IProvidesPlaneFinding>;
            if (planeSubscriber != null)
                planeSubscriber.provider = this;
#endif
        }

        public void ClearTrackables()
        {
#if !UNITY_EDITOR
            if (m_ARPlaneManager == null)
                return;

            foreach (var plane in m_ARPlaneManager.trackables)
            {
                RemovePlane(plane);
            }
#endif
        }
    }
}
#endif
