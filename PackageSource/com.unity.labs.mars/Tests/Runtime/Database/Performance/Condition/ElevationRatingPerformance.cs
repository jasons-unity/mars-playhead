#if UNITY_EDITOR
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests
{
    public class ElevationRatingPerformance : TimedRelationPerformanceTest<ElevationRelation, Pose>
    {
        public void Start()
        {
            m_Relation.maximum = 4f;
            m_Relation.minimum = 2f;

            for (int i = 0; i < s_DataCount; i++)
            {
                var position = Random.insideUnitSphere * 5f;
                m_DataToCompare[i] = new Pose(position, Quaternion.identity);
            }
        }

        protected override void Update()
        {
            var newPosition = Random.insideUnitSphere * 4f;
            RunTestIteration(new Pose(newPosition, Quaternion.identity));
        }
    }
}
#endif
