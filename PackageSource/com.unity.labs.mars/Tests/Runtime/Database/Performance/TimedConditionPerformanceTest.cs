#if UNITY_EDITOR
using Unity.Labs.MARS.Providers;
using Unity.Labs.MARS.Query;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests
{
    public abstract class TimedConditionPerformanceTest : TimedPerformanceTest
    {
        protected MARSDatabase m_Db;
        protected QueryPipelinesModule m_PipelinesModule;
        protected MARSQueryBackend m_QueryBackend;

        protected GameObject m_TestObject;
        protected CameraOffsetProvider m_CameraOffsetProvider;

        protected override void Awake()
        {
            base.Awake();

            m_Db = new MARSDatabase();
            m_Db.LoadModule();

            m_PipelinesModule = QueryPipelinesModule.instance;
            m_PipelinesModule.ConnectDependency(m_Db);
            m_PipelinesModule.LoadModule();

            m_QueryBackend = MARSQueryBackend.instance;
            m_QueryBackend.ConnectDependency(m_PipelinesModule);
            m_QueryBackend.LoadModule();

            m_TestObject = new GameObject();
            m_TestObject.SetActive(false);
            m_TestObject.AddComponent<Camera>();
            m_CameraOffsetProvider = m_TestObject.AddComponent<CameraOffsetProvider>();
        }

        protected override void OnDestroy()
        {
            UnityObjectUtils.Destroy(m_TestObject);
        }
    }

    public abstract class TimedConditionPerformanceTest<TCondition, TData> : TimedConditionPerformanceTest
        where TCondition : Component, ICondition<TData>
    {
        protected TCondition m_Condition;

        protected TData[] m_DataToCompare = new TData[s_DataCount];

        protected override void Awake()
        {
            base.Awake();
            m_Condition = m_TestObject.AddComponent<TCondition>();
            var realWorldObject = m_TestObject.GetComponent<Proxy>();
            if (realWorldObject != null)
            {
                realWorldObject.enabled = false;
            }

            m_CameraOffsetProvider.ConnectSubscriber(m_Condition);
            m_TestObject.SetActive(true);
        }

        protected override void Update()
        {
            RunTestIteration();
        }

        protected void RunTestIteration()
        {
            s_Stopwatch.Restart();
            for (var i = 0; i < s_DataCount; i++)
            {
                m_Condition.RateDataMatch(ref m_DataToCompare[i]);
            }
            s_Stopwatch.Stop();

            m_ElapsedTickSamples[m_SampleIndex] = s_Stopwatch.ElapsedTicks;
            m_SampleIndex++;
        }
    }
}
#endif
