using Unity.Labs.Utils.GUI;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a situation where a given plane must match a given set of alignments
    /// </summary>
    [DisallowMultipleComponent]
    [ComponentTooltip("Requires the object (a surface) to have the specified alignment (horizontal, vertical, or other).")]
    [MonoBehaviourComponentMenu(typeof(AlignmentCondition), "Condition/Alignment")]
    public class AlignmentCondition : Condition<int>
    {
        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Alignment };

        [FlagsProperty]
        [SerializeField]
        MarsPlaneAlignment m_Alignment = MarsPlaneAlignment.HorizontalUp;

        public MarsPlaneAlignment alignment
        {
            get { return m_Alignment; }
            set { m_Alignment = value; }
        }

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

        public override float RateDataMatch(ref int data)
        {
            return (data & (int) m_Alignment) != 0 ? 1f : 0f;
        }
    }
}
