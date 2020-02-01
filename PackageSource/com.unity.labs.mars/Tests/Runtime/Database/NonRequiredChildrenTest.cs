#if UNITY_EDITOR
using System;
using NUnit.Framework;
using Unity.Labs.MARS.Tests;
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests
{
    public class NonRequiredChildrenTest : RuntimeQueryTest, IProvidesTraits<bool>, IProvidesTraits<Pose>
    {
        const int k_FloorId = 1000;
        const int k_NotFloorId = 1001;

        public GameObject TestObject;

        GameObject m_Instance;

        GameObject m_RequiredPlane;
        GameObject m_NonRequiredChild;

        public void Start()
        {
            m_FrameCount = 11;
            TestObject = QueryTestObjectSettings.instance.NonRequiredChildrenSet;
        }

        protected override void OnMarsUpdate()
        {
            var frameCount = MarsTime.FrameCount - m_StartFrame;
            switch (frameCount)
            {
                case 3:
                    m_Instance = InstantiateReferenceObject(TestObject);
                    FindSetChildren(m_Instance);
                    break;
                case 4:
                    Assert.False(m_RequiredPlane.activeInHierarchy);
                    Assert.False(m_NonRequiredChild.activeInHierarchy);
                    break;
                case 5:
                {
                    // this data is what the contexts in the prefab need to match
                    this.AddOrUpdateTrait(k_FloorId, TraitNames.Floor, true);
                    this.AddOrUpdateTrait(k_FloorId, TraitNames.Pose, new Pose());
                    this.AddOrUpdateTrait(k_NotFloorId, TraitNames.Plane, true);
                    this.AddOrUpdateTrait(k_NotFloorId, TraitNames.Pose, new Pose());
                    break;
                }
                case 6:
                    ForceUpdateQueries();
                    break;
                case 7:
                    Assert.True(m_RequiredPlane.activeInHierarchy);
                    Assert.True(m_NonRequiredChild.activeInHierarchy);
                    break;
                case 8:
                    // this should cause our non-required floor to be lost
                    this.RemoveTrait<bool>(k_FloorId, "floor");
                    ForceUpdateQueries();
                    break;
                case 9:
                    Assert.True(m_RequiredPlane.activeInHierarchy);
                    Assert.False(m_NonRequiredChild.activeInHierarchy);
                    break;
            }
        }

        void FindSetChildren(GameObject setRoot)
        {
            m_RequiredPlane = setRoot.transform.GetChild(0).GetChild(0).gameObject;
            m_NonRequiredChild = setRoot.transform.GetChild(1).GetChild(0).gameObject;
            Assert.NotNull(m_RequiredPlane);
            Assert.NotNull(m_NonRequiredChild);
        }
    }
}
#endif
