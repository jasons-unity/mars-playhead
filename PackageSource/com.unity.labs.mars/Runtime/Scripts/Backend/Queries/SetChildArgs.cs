using System.Collections.Generic;
using Unity.Labs.MARS.Data;

namespace Unity.Labs.MARS.Query
{
    public struct SetChildArgs
    {
        /// <summary>Is this child necessary to maintain the set after it has been initially matched?</summary>
        public bool required;

        /// <summary>Collection of this child's potential data assignments</summary>
        public Dictionary<int, SetChildDataCandidate> dataAssignmentCandidates;

        /// <summary>
        /// The pre-constructed arguments for matching this child's conditions
        /// </summary>
        public TryBestMatchArguments tryBestMatchArgs;

        /// <summary>A list of traits required for this query to function</summary>
        public ContextTraitRequirements TraitRequirements;

        public SetChildArgs(Conditions conditions, ContextTraitRequirements requirements = null) :
            this(conditions, Exclusivity.ReadOnly, true, requirements) { }

        public SetChildArgs(Conditions conditions, Exclusivity exclusivity, bool required,
            ContextTraitRequirements requirements = null)
        {
            this.required = required;
            dataAssignmentCandidates = new Dictionary<int, SetChildDataCandidate>();
            tryBestMatchArgs = new TryBestMatchArguments(conditions, exclusivity);
            TraitRequirements = requirements;
        }
    }
}
