#if UNITY_EDITOR
using Unity.Labs.MARS.Query;
using UnityEngine;

#if !NET_4_6
using Unity.Labs.Utils;
#endif

namespace Unity.Labs.MARS.Data.Tests
{
    public abstract class TimedRelationPerformanceTest<TRelation, TData> : TimedConditionPerformanceTest
        where TRelation : Component, IRelation<TData>
    {
        protected TRelation m_Relation;

        protected readonly TData[] m_DataToCompare = new TData[s_DataCount];

        protected override void Awake()
        {
            base.Awake();
            m_Relation = m_TestObject.AddComponent<TRelation>();
            m_CameraOffsetProvider.ConnectSubscriber(m_Relation);

            // make sure we don't let clients do their normal registration
            var client = m_TestObject.GetComponent<ProxyGroup>();
            if (client != null)
                client.enabled = false;

            m_TestObject.SetActive(true);
        }

        protected void RunTestIteration(TData child1Data)
        {
            s_Stopwatch.Restart();
            for (int i = 0; i < s_DataCount; i++)
            {
                m_Relation.RateDataMatch(ref child1Data, ref m_DataToCompare[i]);
            }
            s_Stopwatch.Stop();

            m_ElapsedTickSamples[m_SampleIndex] = s_Stopwatch.ElapsedTicks;
            m_SampleIndex++;
        }
    }
}
#endif
