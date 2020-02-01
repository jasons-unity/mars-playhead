using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a situation that depends on the existence or lack of a certain trait
    /// </summary>
    [ComponentTooltip("Requires the object to have or lack the specified trait.")]
    [MonoBehaviourComponentMenu(typeof(SemanticTagCondition), "Condition/Semantic Tag")]
    public class SemanticTagCondition : Condition<bool>, ISemanticTagCondition
    {
        [Delayed]
        [SerializeField]
        [Tooltip("Sets the name of the trait that must be present or not")]
        string m_TraitName;

        [SerializeField]
        [Tooltip("Whether to require a semantic tag to be present or be excluded")]
        SemanticTagMatchRule m_MatchRule;

        readonly TraitRequirement[] m_RequiredTraits = new TraitRequirement[1];

        public SemanticTagMatchRule matchRule
        {
            get { return m_MatchRule; }
            set { m_MatchRule = value; }
        }

        public void SetTraitName(string newName)
        {
            m_TraitName = newName;
            SetTraitRequirement();
        }

        void SetTraitRequirement()
        {
            m_RequiredTraits[0] = new TraitRequirement(m_TraitName, typeof(bool));
        }

        public override TraitRequirement[] GetRequiredTraits()
        {
            var requirement = m_RequiredTraits[0];
            if (requirement == null || !string.Equals(requirement.TraitName, m_TraitName))
                SetTraitRequirement();

            return m_RequiredTraits;
        }

        // tag conditions have binary pass / fail answers
        public override float RateDataMatch(ref bool data)
        {
            // if this is an exclusive tag, we want to add only failures to the ratings dict
            return data ? 1f : 0f;
        }

        public override bool CheckTraitPasses(SynthesizedTrait trait)
        {
            if (matchRule == SemanticTagMatchRule.Exclude)
                return !base.CheckTraitPasses(trait);

            return base.CheckTraitPasses(trait);
        }
    }
}
