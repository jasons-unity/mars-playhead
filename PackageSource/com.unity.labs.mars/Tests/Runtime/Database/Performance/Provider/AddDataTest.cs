#if UNITY_EDITOR
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests
{
    [AddComponentMenu("")]
    public class AddDataTest : DatabasePerformanceTest
    {
        public void OnEnable()
        {
            ConnectDb();
            m_StartFrame = Time.frameCount;
        }

        public void Update()
        {
            for (var i = 0; i < s_DataCount; i++)
                m_Db.planeData.AddOrUpdateData(MakeNewPlane());
        }
    }
}
#endif
