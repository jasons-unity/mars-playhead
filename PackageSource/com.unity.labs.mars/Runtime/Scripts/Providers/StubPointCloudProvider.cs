using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [AddComponentMenu("")]
    public class StubPointCloudProvider : MonoBehaviour, IProvidesPointCloud
    {
#pragma warning disable 67
        public event Action<Dictionary<MarsTrackableId, PointCloudData>> PointCloudUpdated;
#pragma warning restore 67

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var pointCloudSubscriber = obj as IFunctionalitySubscriber<IProvidesPointCloud>;
            if (pointCloudSubscriber != null)
                pointCloudSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        public Dictionary<MarsTrackableId, PointCloudData> GetPoints()
        {
            return default(Dictionary<MarsTrackableId, PointCloudData>);
        }

        public void StopDetectingPoints() { }

        public void StartDetectingPoints() { }
    }
}
