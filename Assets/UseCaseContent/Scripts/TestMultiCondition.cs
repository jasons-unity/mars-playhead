using System;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS.UseCaseContent
{
    [DisallowMultipleComponent]
    [MonoBehaviourComponentMenu(typeof(FlatFloorCondition), "Condition/Test/MultiCondition")]
    public class TestMultiCondition : MultiCondition<TestMultiCondition.TestTagSubCondition, TestMultiCondition.InRadiusSubCondition>
    {
        const string k_TestTag = "test_tag";

        [Serializable]
        public class TestTagSubCondition : SubCondition, ISemanticTagCondition
        {
            static readonly TraitRequirement[] k_RequiredTraits = { new TraitRequirement(k_TestTag, typeof(bool)) };

            public string traitName { get { return k_RequiredTraits[0].TraitName; } }

            public SemanticTagMatchRule matchRule
            {
                get { return SemanticTagMatchRule.Match; }
            }

            public TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

            public float RateDataMatch(ref bool data)
            {
                return data ? 1f : 0f;
            }
        }

        [Serializable]
        public class InRadiusSubCondition : SubCondition, ICondition<Pose>, ISpatialCondition
        {
            static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Pose };

            [SerializeField]
            [Range(0.01f, 10.0f)]
            float m_Radius = 1.0f;

            [SerializeField]
            Vector3 m_Center = Vector3.zero;

            public string traitName { get { return k_RequiredTraits[0].TraitName; } }

            public TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

            public float RateDataMatch(ref Pose data)
            {
                var offset = (data.position - m_Center).magnitude;
                return Mathf.Clamp01(Mathf.InverseLerp(m_Radius, 0.0f, offset));
            }

            public void ScaleParameters(float scale)
            {
                m_Radius *= scale;
                m_Center *= scale;
            }
        }
    }
}
