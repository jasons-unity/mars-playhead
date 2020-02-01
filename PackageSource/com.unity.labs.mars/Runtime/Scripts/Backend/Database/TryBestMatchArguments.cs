using System.Collections.Generic;
using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS.Data
{
    /// <summary>
    /// Container for all arguments required to find the best match for a set of Conditions
    /// </summary>
    public class TryBestMatchArguments
    {
        /// <summary>
        /// The set of Conditions to find data matches for
        /// </summary>
        public Conditions conditions;

        /// <summary>
        /// The query's data exclusivity setting
        /// </summary>
        public Exclusivity exclusivity;

        /// <summary>
        /// Pre-allocated collections used during the calculation of match ratings
        /// </summary>
        public ConditionRatingsData ratings;

        /// <summary>
        /// A list of traits required for this query to function
        /// </summary>
        public ContextTraitRequirements traitRequirements;

        /// <summary>
        /// A mapping of data ID to match rating, for IDs matching this condition
        /// </summary>
        public Dictionary<int, float> output;

        public TryBestMatchArguments(Conditions conditions, Exclusivity exclusivity,
            ContextTraitRequirements traitRequirements = null)
        {
            this.conditions = conditions;
            this.exclusivity = exclusivity;
            ratings = new ConditionRatingsData(conditions);
            output = new Dictionary<int, float>();
            this.traitRequirements = traitRequirements;
        }

        /// <summary>
        /// Use this overload to get arguments for the next instance of Replicator
        /// </summary>
        /// <param name="original">The original / previous spawn's arguments</param>
        public TryBestMatchArguments(TryBestMatchArguments original)
        {
            conditions = original.conditions;
            exclusivity = original.exclusivity;
            // we can re-use the internal ratings structure for all instances of a spawn
            ratings = original.ratings;
            // we could also re-use the output dictionary, but that's more useful for debugging and inspecting.
            output = new Dictionary<int, float>(original.output.Count);
        }
    }
}
