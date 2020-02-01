#if INCLUDE_AR_FOUNDATION
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Random = UnityEngine.Random;

namespace Unity.Labs.MARS.Providers
{
    class XRSDKPointCloudProvider : IProvidesPointCloud
    {
#if UNITY_EDITOR
        const int k_NumPoints = 100;
        const float k_PointCloudSize = 5;
#if INCLUDE_XR_MOCK
        bool m_IsRemoteActive;
#endif
#endif

        ARPointCloudManager m_ARPointCloudManager;
        ARPointCloudManager m_NewARPointCloudManager;

        readonly Dictionary<MarsTrackableId, PointCloudData> m_Data = new Dictionary<MarsTrackableId, PointCloudData>();

        NativeArray<ulong> m_Identifiers;
        NativeArray<Vector3> m_Positions;
        NativeArray<float> m_ConfidenceValues;

        public event Action<Dictionary<MarsTrackableId, PointCloudData>> PointCloudUpdated;

        public void StopDetectingPoints()
        {
            if (m_ARPointCloudManager && m_ARPointCloudManager.subsystem != null)
                m_ARPointCloudManager.subsystem.Stop();
        }

        public void StartDetectingPoints()
        {
            if (m_ARPointCloudManager && m_ARPointCloudManager.subsystem != null)
                m_ARPointCloudManager.subsystem.Start();
        }

        public void LoadProvider()
        {
            ARFoundationSessionProvider.RequireARSession();
            var currentSession = ARFoundationSessionProvider.currentSession;
            if (currentSession)
            {
                var currentSessionGameObject = currentSession.gameObject;
                m_ARPointCloudManager = currentSessionGameObject.GetComponent<ARPointCloudManager>();
                if (!m_ARPointCloudManager)
                {
                    m_ARPointCloudManager = currentSessionGameObject.AddComponent<ARPointCloudManager>();
                    m_ARPointCloudManager.hideFlags = HideFlags.DontSave;
                    m_NewARPointCloudManager = m_ARPointCloudManager;
                }

                m_ARPointCloudManager.pointCloudsChanged += ARPointCloudManagerOnPointCloudsChanged;
            }

#if UNITY_EDITOR
#if INCLUDE_XR_MOCK
            // Cache value on load because call to FindObjectsOfTypeAll<> is expensive
            m_IsRemoteActive = EditorOnlyDelegates.IsRemoteActive();

            if (m_IsRemoteActive)
                return;
#endif

            var data = new PointCloudData();
            m_Identifiers = new NativeArray<ulong>(k_NumPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_Positions = new NativeArray<Vector3>(k_NumPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            m_ConfidenceValues = new NativeArray<float>(k_NumPoints, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < k_NumPoints; i++)
            {
                m_Identifiers[i] = (ulong)i;
                m_Positions[i] = Random.insideUnitSphere * k_PointCloudSize;
                m_ConfidenceValues[i] = Random.Range(0, 1);
            }

            data.Identifiers = new NativeSlice<ulong>(m_Identifiers);
            data.Positions = new NativeSlice<Vector3>(m_Positions);
            data.ConfidenceValues = new NativeSlice<float>(m_ConfidenceValues);
            m_Data[MarsTrackableId.Create()] = data;

            EditorApplication.delayCall += () =>
            {
                if (PointCloudUpdated != null)
                    PointCloudUpdated(GetPoints());
            };
#endif
        }

        void ARPointCloudManagerOnPointCloudsChanged(ARPointCloudChangedEventArgs pointCloudEvent)
        {
            UpdatePoints();
            if (PointCloudUpdated != null)
                PointCloudUpdated(GetPoints());
        }

        public void UnloadProvider()
        {
            m_ARPointCloudManager.pointCloudsChanged -= ARPointCloudManagerOnPointCloudsChanged;
            if (m_NewARPointCloudManager)
                UnityObjectUtils.Destroy(m_NewARPointCloudManager);

            ARFoundationSessionProvider.TearDownARSession();

            if (m_Identifiers.IsCreated)
                m_Identifiers.Dispose();

            if (m_Positions.IsCreated)
                m_Positions.Dispose();

            if(m_ConfidenceValues.IsCreated)
                m_ConfidenceValues.Dispose();
        }

        public Dictionary<MarsTrackableId, PointCloudData> GetPoints()
        {
            return m_Data;
        }

        void UpdatePoints()
        {
#if INCLUDE_XR_MOCK
            if (!m_IsRemoteActive)
                return;
#endif
            if (m_ARPointCloudManager == null)
                return;

            var trackables = m_ARPointCloudManager.trackables;
            m_Data.Clear();
            foreach (var pointCloud in trackables)
            {
                var data = new PointCloudData
                {
                    Identifiers = pointCloud.identifiers,
                    Positions = pointCloud.positions,
                    ConfidenceValues = pointCloud.confidenceValues
                };

                var id = pointCloud.trackableId;
                var marsTrackableId = new MarsTrackableId(id.subId1, id.subId2);
                m_Data[marsTrackableId] = data;
            }
        }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var pointCloudSubscriber = obj as IFunctionalitySubscriber<IProvidesPointCloud>;
            if (pointCloudSubscriber != null)
                pointCloudSubscriber.provider = this;
#endif
        }
    }
}
#endif
