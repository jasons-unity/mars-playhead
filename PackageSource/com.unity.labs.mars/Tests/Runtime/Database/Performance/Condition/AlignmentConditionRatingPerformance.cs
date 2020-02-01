#if UNITY_EDITOR
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests
{
    public class AlignmentConditionRatingPerformance : TimedConditionPerformanceTest<AlignmentCondition, int>
    {
        public void Start()
        {
            for (int i = 0; i < s_DataCount; i++)
            {
                m_DataToCompare[i] = (int) RandomAlignment();
            }
        }

        protected override void Update()
        {
            m_Condition.alignment = RandomAlignment();
            RunTestIteration();
        }

        static MarsPlaneAlignment RandomAlignment()
        {
            var randomRange = Random.Range(0f, 1f);

            if (randomRange > 0.66f)
                return MarsPlaneAlignment.HorizontalUp;

            if (randomRange > 0.33f)
                return MarsPlaneAlignment.Vertical;

            return MarsPlaneAlignment.HorizontalDown;
        }
    }
}
#endif
