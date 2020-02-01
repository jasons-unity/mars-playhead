using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Condition that ensures an entity has both a floor tag and a horizontal alignment
    /// </summary>
    [DisallowMultipleComponent]
    [MonoBehaviourComponentMenu(typeof(FlatFloorCondition), "Condition/FlatFloorCondition")]
    public class FlatFloorCondition : MultiCondition<FlatFloorCondition.FloorTagSubCondition, FlatFloorCondition.HorizontalAlignmentSubCondition>
    {
        [System.Serializable]
        public class FloorTagSubCondition : SubCondition, ISemanticTagCondition
        {
            static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Floor };

            public float RateDataMatch(ref bool data)
            {
                return data ? 1f : 0f;
            }

            public string traitName { get { return k_RequiredTraits[0].TraitName; } }

            public SemanticTagMatchRule matchRule
            {
                get { return SemanticTagMatchRule.Match; }
            }

            public TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }
        }

        [System.Serializable]
        public class HorizontalAlignmentSubCondition : SubCondition, ICondition<int>
        {
            static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Alignment };

            public string traitName { get { return k_RequiredTraits[0].TraitName; } }

            public float RateDataMatch(ref int data)
            {
                return (data & (int)MarsPlaneAlignment.HorizontalUp) != 0 ? 1f : 0f;
            }

            public TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }
        }
    }
}
