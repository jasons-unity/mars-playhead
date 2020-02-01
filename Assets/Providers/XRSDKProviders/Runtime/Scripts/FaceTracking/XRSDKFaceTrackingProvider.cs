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
    class XRSDKFaceTrackingProvider : IProvidesFaceTracking, IUsesMARSTrackableData<IMRFace>, IProvidesFacialExpressions,
        IProvidesTraits<bool>, IProvidesTraits<Pose>, ITrackableProvider
    {
        static readonly TraitDefinition[] k_ProvidedTraits =
        {
            TraitDefinitions.Face,
            TraitDefinitions.Pose
        };

        ARFaceManager m_ARFaceManager;
        ARFaceManager m_NewARFaceManager;

        readonly Dictionary<TrackableId, XRSDKFace> m_TrackedFaces = new Dictionary<TrackableId, XRSDKFace>();

        public event Action<IMRFace> FaceAdded;

        public event Action<IMRFace> FaceUpdated;

        public event Action<IMRFace> FaceRemoved;

        // ReSharper disable once UnusedMember.Local
        static TraitDefinition[] GetStaticProvidedTraits() { return k_ProvidedTraits; }

        public TraitDefinition[] GetProvidedTraits() { return k_ProvidedTraits; }

        void ARFaceManagerOnFacesChanged(ARFacesChangedEventArgs changedEvent)
        {
            foreach (var arFace in changedEvent.removed)
            {
                var trackableId = arFace.trackableId;
                m_TrackedFaces.TryGetValue(trackableId, out var xrSdkFace);
                arFace.ToXRSDKFace(m_ARFaceManager.subsystem, ref xrSdkFace);
                m_TrackedFaces.Remove(trackableId);
                RemoveFaceData(xrSdkFace);
            }

            foreach (var arFace in changedEvent.updated)
            {
                UpdateFaceData(GetOrAddFace(arFace));
            }

            foreach (var arFace in changedEvent.added)
            {
                AddFaceData(GetOrAddFace(arFace));
            }
        }

        XRSDKFace GetOrAddFace(ARFace arFace)
        {
            var trackableId = arFace.trackableId;
            m_TrackedFaces.TryGetValue(trackableId, out var xrSdkFace);
            arFace.ToXRSDKFace(m_ARFaceManager.subsystem, ref xrSdkFace);
            m_TrackedFaces[trackableId] = xrSdkFace;
            return xrSdkFace;
        }

        void AddFaceData(XRSDKFace xrsdkFace)
        {
            var id = this.AddOrUpdateData(xrsdkFace);
            this.AddOrUpdateTrait(id, TraitNames.Face, true);
            this.AddOrUpdateTrait(id, TraitNames.Pose, xrsdkFace.pose);

            if (FaceAdded != null)
                FaceAdded(xrsdkFace);

        }

        void UpdateFaceData(XRSDKFace xrsdkFace)
        {
            var id = this.AddOrUpdateData(xrsdkFace);
            this.AddOrUpdateTrait(id, TraitNames.Pose, xrsdkFace.pose);
            if (FaceUpdated != null)
                FaceUpdated(xrsdkFace);
        }

        void RemoveFaceData(XRSDKFace xrsdkFace)
        {
            var id = this.RemoveData(xrsdkFace);
            this.RemoveTrait<bool>(id, TraitNames.Face);
            this.RemoveTrait<Pose>(id, TraitNames.Pose);
            if (FaceRemoved != null)
                FaceRemoved(xrsdkFace);
        }

        public void AddExistingTrackables()
        {
#if !UNITY_EDITOR
            if (m_ARFaceManager == null)
                return;

            foreach (var arFace in m_ARFaceManager.trackables)
            {
                AddFaceData(GetOrAddFace(arFace));
            }
#endif
        }

        public void ClearTrackables()
        {
            if (m_ARFaceManager == null)
                return;

            foreach (var kvp in m_TrackedFaces)
            {
                RemoveFaceData(kvp.Value);
            }

            m_TrackedFaces.Clear();
        }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            if (obj is IFunctionalitySubscriber<IProvidesFaceTracking> faceTrackingSubscriber)
                faceTrackingSubscriber.provider = this;

            if (obj is IFunctionalitySubscriber<IProvidesFacialExpressions> faceExpressionSubscriber)
                faceExpressionSubscriber.provider = this;
#endif
        }

        public void LoadProvider()
        {
            ARFoundationSessionProvider.RequireARSession();

            var currentSession = ARFoundationSessionProvider.currentSession;
            if (currentSession)
            {
                var currentSessionGameObject = currentSession.gameObject;
                m_ARFaceManager = currentSessionGameObject.GetComponent<ARFaceManager>();
                if (!m_ARFaceManager)
                {
                    m_ARFaceManager = currentSessionGameObject.AddComponent<ARFaceManager>();
                    m_NewARFaceManager = m_ARFaceManager;
                }

                m_ARFaceManager.facesChanged += ARFaceManagerOnFacesChanged;
            }

            AddExistingTrackables();
        }

        public void UnloadProvider()
        {
            m_ARFaceManager.facesChanged -= ARFaceManagerOnFacesChanged;

            if (m_NewARFaceManager)
                UnityObjectUtils.Destroy(m_NewARFaceManager);

            ARFoundationSessionProvider.TearDownARSession();
        }

        public int GetMaxFaceCount()
        {
            return m_ARFaceManager == null ? 0 : m_ARFaceManager.maximumFaceCount;
        }

        public void GetFaces(List<IMRFace> faces)
        {
            if (m_ARFaceManager == null)
                return;

            foreach (var arFace in m_ARFaceManager.trackables)
            {
                faces.Add(GetOrAddFace(arFace));
            }
        }

        public void SubscribeToExpression(MRFaceExpression expression, Action<float> engaged, Action<float> disengaged)
        {
#if !UNITY_EDITOR
#if UNITY_IOS
            ARKitFaceExpressionsExtensions.SubscribeToExpression(expression, engaged, disengaged);
#elif UNITY_ANDROID
            ARCoreFaceExpressionsExtensions.SubscribeToExpression(expression, engaged, disengaged);
#endif
#endif
        }

        public void UnsubscribeToExpression(MRFaceExpression expression, Action<float> engaged, Action<float> disengaged)
        {
#if !UNITY_EDITOR
#if UNITY_IOS
            ARKitFaceExpressionsExtensions.UnsubscribeToExpression(expression, engaged, disengaged);
#elif UNITY_ANDROID
            ARCoreFaceExpressionsExtensions.UnsubscribeToExpression(expression, engaged, disengaged);
#endif
#endif
        }
    }
}
#endif
