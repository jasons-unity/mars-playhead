#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.TestTools;

namespace Unity.Labs.MARS.Data.Tests
{
    [AddComponentMenu("")]
    public abstract class DatabasePerformanceTest : MonoBehaviour, IMonoBehaviourTest
    {
        protected string[] m_TraitNames = new string[s_DataCount];

        protected int m_StartFrame;

        protected const int k_FrameCount = 60;
        protected static int s_DataCount = 100;
        protected static int s_IdCount;

        protected MARSDatabase m_Db;

        public bool IsTestFinished
        {
            get
            {
                if (Time.frameCount - m_StartFrame >= k_FrameCount)
                {
                    enabled = false;
                    return true;
                }

                return false;
            }
        }

        protected void ConnectDb()
        {
            m_Db = new MARSDatabase();
            m_Db.LoadModule();
        }

        protected MRPlane MakeNewPlane()
        {
            s_IdCount++;
            var plane = new MRPlane();
            plane.id = MarsTrackableId.Create();
            plane.extents = Vector2.one;
            return plane;
        }
    }
}
#endif
