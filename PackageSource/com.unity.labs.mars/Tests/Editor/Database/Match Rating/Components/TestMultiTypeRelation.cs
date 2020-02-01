using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS.Tests
{
    struct TestIntFloatChildValues: IRelationChildValues<int, float>
    {
        public int Trait1 { get; set; }
        public float Trait2 { get; set; }

        public TestIntFloatChildValues(int trait1, float trait2)
        {
            Trait1 = trait1;
            Trait2 = trait2;
        }
    }

    struct TestIntChildValues : IRelationChildValues<int>
    {
        public int Trait1 { get; set; }

        public TestIntChildValues(int trait1)
        {
            Trait1 = trait1;
        }
    }

    class TestMultiTypeRelation : IRelation<TestIntFloatChildValues, TestIntChildValues>
    {
        public bool enabled { get; set; }
        public IMRObject child1 { get; }
        public IMRObject child2 { get; }

        static readonly string[] k_Child1TraitNames = { "testInt", "testFloat" };
        static readonly string[] k_Child2TraitNames = { "testInt" };

        public string[] Child1TraitNames => k_Child1TraitNames;
        public string[] Child2TraitNames => k_Child2TraitNames;

        public float RateDataMatch(ref TestIntFloatChildValues child1Data, ref TestIntChildValues child2Data)
        {
            // multiplying all inputs together makes it so that any combination of inputs that includes a 0 fails
            return Mathf.Clamp01(child1Data.Trait1 * child1Data.Trait2 * child2Data.Trait1);
        }
    }
}
