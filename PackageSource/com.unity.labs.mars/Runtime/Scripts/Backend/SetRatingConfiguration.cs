using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Defines how a set's match rating components will be reduced to a single rating
    /// </summary>
    public struct SetRatingConfiguration
    {
        /*
            We want to prevent extreme weights for child matches.
            If the weight of child matches is too small, the match will optimize only for the
            relations, even if the matches for all children are close to failing.
            The opposite happens if the weight of child matches is too large.
        */
        const float k_MinimumWeight = 0.1f;
        const float k_DefaultWeight = 0.5f;
        const float k_MaximumWeight = 0.9f;

        /// <summary>
        /// The percentage of the set's match rating that is determined by the ratings of child matches
        /// </summary>
        public readonly float MemberMatchWeight;

        public SetRatingConfiguration(float membersWeight = k_DefaultWeight)
        {
            MemberMatchWeight = Mathf.Clamp(membersWeight, k_MinimumWeight, k_MaximumWeight);
        }
    }
}
