using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a situation that depends on the existence of a pose
    /// </summary>
    [DisallowMultipleComponent]
    [ComponentTooltip("Requires the object to have a pose (position).")]
    [MonoBehaviourComponentMenu(typeof(HasPoseCondition), "Condition/Trait/Pose")]
    public class HasPoseCondition : Condition<Pose>
    {
        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Pose };

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

        public override float RateDataMatch(ref Pose data)
        {
            return 1.0f;
        }
    }
}
