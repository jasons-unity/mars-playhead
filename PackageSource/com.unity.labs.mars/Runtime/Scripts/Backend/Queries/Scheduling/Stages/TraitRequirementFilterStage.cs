using System;
using System.Collections.Generic;

namespace Unity.Labs.MARS.Query
{
    class TraitRequirementFilterTransform : DataTransform<
        Dictionary<Type, Action<string, HashSet<int>>>,
        List<ContextTraitRequirements>,
        List<HashSet<int>>>
    {
        public TraitRequirementFilterTransform()
        {
            Process = ProcessStage;
        }

        public static void ProcessStage(List<int> workingIndices,
            Dictionary<Type, Action<string, HashSet<int>>> typeToFilterAction,
            List<ContextTraitRequirements> traitRequirements,
            ref List<HashSet<int>> matchSets)
        {
            foreach (var i in workingIndices)
            {
                var requirements = traitRequirements[i];
                var matchSet = matchSets[i];

                foreach (var requirement in requirements)
                {
                    // TODO - if this requirement's trait has already been provided by conditions, skip it.
                    // it works just fine without doing this, but we could save some computing time.

                    if (!requirement.Required)    // don't filter based on optional traits.
                        continue;

                    if (typeToFilterAction.TryGetValue(requirement.Type, out var filterAction))
                        filterAction.Invoke(requirement.TraitName, matchSet);
                }
            }
        }
    }

    class TraitRequirementFilterStage : QueryStage<TraitRequirementFilterTransform>
    {
        public TraitRequirementFilterStage(TraitRequirementFilterTransform transformation)
            : base("Trait Presence Filter", transformation)
        {
        }
    }
 }
