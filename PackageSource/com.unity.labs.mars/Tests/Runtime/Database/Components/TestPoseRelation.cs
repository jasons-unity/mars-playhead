using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests
{
    public class TestPoseRelation : TestRelation<Pose, Pose>
    {
        public override float RateDataMatch(ref Pose child1Data, ref Pose child2Data)
        {
            var averagePos = (child1Data.position + child2Data.position) * 0.1f;
            return Mathf.Clamp01(Vector3.Magnitude(averagePos));
        }
    }
}
