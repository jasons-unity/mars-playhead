using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a situation that depends on the existence of a face
    /// </summary>
    [DisallowMultipleComponent]
    [ComponentTooltip("Requires the object to be a human face.")]
    [MonoBehaviourComponentMenu(typeof(IsFaceCondition), "Condition/Trait/Face")]
    public class IsFaceCondition : SimpleTagCondition
    {
        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Face };

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }
    }
}
