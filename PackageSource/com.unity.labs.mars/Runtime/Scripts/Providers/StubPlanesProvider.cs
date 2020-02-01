using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [AddComponentMenu("")]
    public class StubPlanesProvider : MonoBehaviour, IProvidesPlaneFinding
    {
#pragma warning disable 67
        public event Action<MRPlane> planeAdded;
        public event Action<MRPlane> planeUpdated;
        public event Action<MRPlane> planeRemoved;
#pragma warning restore 67

        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            if (obj is IFunctionalitySubscriber<IProvidesPlaneFinding> planeFindingSubscriber)
                planeFindingSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        public void GetPlanes(List<MRPlane> planes) { }

        public void StopDetectingPlanes() { }

        public void StartDetectingPlanes() { }
    }
}
