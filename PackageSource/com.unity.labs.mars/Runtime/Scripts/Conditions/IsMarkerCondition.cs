using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a situation that depends on the existence of a marker
    /// </summary>
    [DisallowMultipleComponent]
    [ComponentTooltip("Requires the object to be a marker.")]
    [MonoBehaviourComponentMenu(typeof(IsMarkerCondition), "Condition/Trait/Marker")]
    public class IsMarkerCondition : SimpleTagCondition
    {
        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Marker };

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }
    }
}
