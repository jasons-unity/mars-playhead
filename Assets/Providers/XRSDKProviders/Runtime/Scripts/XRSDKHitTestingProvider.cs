#if INCLUDE_AR_FOUNDATION
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace Unity.Labs.MARS.Providers
{
    public class XRSDKHitTestingProvider : IProvidesMRHitTesting
    {
        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<ARRaycastHit> k_Hits = new List<ARRaycastHit>();

#if UNITY_EDITOR && INCLUDE_XR_MOCK
        bool m_IsRemoteActive;
#endif

        ARRaycastManager m_ARRaycastManager;
        ARRaycastManager m_NewARRaycastManager;

        public bool ScreenHitTest(Vector2 screenPosition, out MRHitTestResult result, MRHitTestResultTypes types = MRHitTestResultTypes.Any)
        {
#if UNITY_EDITOR && INCLUDE_XR_MOCK
            if (!m_IsRemoteActive)
            {
                // Hit testing is not currently supported by XRSDK remote workflow
                result = default(MRHitTestResult);
                return false;
            }
#endif

            if (m_ARRaycastManager == null)
            {
                result = default(MRHitTestResult);
                return false;
            }

            k_Hits.Clear();
            if (m_ARRaycastManager.Raycast(screenPosition, k_Hits, HitTestResultTypeToTrackableType(types)))
            {
                foreach (var hit in k_Hits)
                {
                    result = hit.ToMRHitTestResult();
                    return true;
                }
            }

            result = new MRHitTestResult();
            return false;
        }

        public bool WorldHitTest(Ray ray, out MRHitTestResult result, MRHitTestResultTypes types = MRHitTestResultTypes.Any)
        {
#if UNITY_EDITOR && INCLUDE_XR_MOCK
            if (!m_IsRemoteActive)
            {
                // Hit testing is not currently supported by XRSDK remote workflow
                result = default(MRHitTestResult);
                return false;
            }
#endif

            if (m_ARRaycastManager == null)
            {
                result = default(MRHitTestResult);
                return false;
            }

            k_Hits.Clear();
            if (m_ARRaycastManager.Raycast(ray, k_Hits, HitTestResultTypeToTrackableType(types)))
            {
                foreach (var hit in k_Hits)
                {
                    result = hit.ToMRHitTestResult();
                    return true;
                }
            }

            result = new MRHitTestResult();
            return false;
        }

        public void StopHitTesting()
        {
            if (m_ARRaycastManager && m_ARRaycastManager.subsystem != null)
                m_ARRaycastManager.subsystem.Stop();
        }

        public void StartHitTesting()
        {
            if (m_ARRaycastManager && m_ARRaycastManager.subsystem != null)
                m_ARRaycastManager.subsystem.Start();
        }

        static TrackableType HitTestResultTypeToTrackableType(MRHitTestResultTypes types)
        {
            switch (types)
            {
                case MRHitTestResultTypes.FeaturePoint:
                    return TrackableType.FeaturePoint;
                case MRHitTestResultTypes.HorizontalPlane:
                case MRHitTestResultTypes.VerticalPlane:
                case MRHitTestResultTypes.Plane:
                    return TrackableType.PlaneWithinPolygon;
                default:
                    return TrackableType.All;
            }
        }

        public void LoadProvider()
        {
            ARFoundationSessionProvider.RequireARSession();
            var currentSession = ARFoundationSessionProvider.currentSession;
            if (currentSession)
            {
                var currentSessionGameObject = currentSession.gameObject;
                m_ARRaycastManager = currentSessionGameObject.GetComponent<ARRaycastManager>();
                if (!m_ARRaycastManager)
                {
                    m_ARRaycastManager = currentSessionGameObject.AddComponent<ARRaycastManager>();
                    m_ARRaycastManager.hideFlags = HideFlags.DontSave;
                    m_NewARRaycastManager = m_ARRaycastManager;
                }
            }


#if UNITY_EDITOR && INCLUDE_XR_MOCK
            // Cache value on load because call to FindObjectsOfTypeAll<> is expensive
            m_IsRemoteActive = EditorOnlyDelegates.IsRemoteActive();
#endif
        }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var subscriber = obj as IFunctionalitySubscriber<IProvidesMRHitTesting>;
            if (subscriber != null)
                subscriber.provider = this;
#endif
        }

        public void UnloadProvider()
        {
            if (m_NewARRaycastManager)
                UnityObjectUtils.Destroy(m_NewARRaycastManager);

            ARFoundationSessionProvider.TearDownARSession();
        }
    }
}
#endif
