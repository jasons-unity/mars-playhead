using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS.Tests
{
    class TestThreeTypeCondition : ICondition<int, float, int>
    {
        public float Multiplier = 0.25f;

        public bool enabled { get; set; }

        public string TraitName1 => "testInt1";
        public string TraitName2 => "testFloat";
        public string TraitName3 => "testInt2";

        public float RateDataMatch(ref int trait1, ref float trait2, ref int trait3)
        {
            return Mathf.Clamp01(trait1 * trait2 * trait3 * Multiplier);
        }
    }
}
