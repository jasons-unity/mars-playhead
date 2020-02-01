using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a situation that depends on the existence of a plane
    /// </summary>
    [DisallowMultipleComponent]
    [ComponentTooltip("Requires the object to be a plane.")]
    [MonoBehaviourComponentMenu(typeof(IsPlaneCondition), "Condition/Trait/Plane")]
    public class IsPlaneCondition : SimpleTagCondition
    {
        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Plane };

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }
    }
}
