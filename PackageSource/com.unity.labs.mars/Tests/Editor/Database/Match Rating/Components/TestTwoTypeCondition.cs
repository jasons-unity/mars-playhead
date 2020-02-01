using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS.Tests
{
    class TestTwoTypeCondition : ICondition<int, float>
    {
        public float Multiplier = 0.01f;

        public bool enabled { get; set; }

        public string TraitName1 => "testInt";
        public string TraitName2 => "testFloat";

        public float RateDataMatch(ref int trait1, ref float trait2)
        {
            return Mathf.Clamp01(trait1 * trait2 * Multiplier);
        }
    }
}
