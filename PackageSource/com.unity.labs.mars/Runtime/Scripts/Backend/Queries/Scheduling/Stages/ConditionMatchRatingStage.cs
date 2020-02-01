using System.Collections.Generic;
using Unity.Labs.MARS.Data;

namespace Unity.Labs.MARS.Query
{
    partial class MatchRatingDataTransform : DataTransform<
        List<Conditions>,
        List<CachedTraitCollection>,
        List<ConditionRatingsData>>
    {
        readonly List<int> m_IndicesToFilter = new List<int>();

        public MatchRatingDataTransform()
        {
            Process = Processor;
        }

        void Processor(List<int> indices, List<Conditions> conditions,
            List<CachedTraitCollection> references, ref List<ConditionRatingsData> ratings)
        {
            m_IndicesToFilter.Clear();
            foreach (var i in indices)
            {
                if(!RateConditionMatches(conditions[i], references[i], ratings[i]))
                    m_IndicesToFilter.Add(i);
            }

            foreach (var filteredIndex in m_IndicesToFilter)
            {
                indices.Remove(filteredIndex);
            }
        }

        internal static bool RateConditionMatches(Conditions conditions, CachedTraitCollection traitCache,
            ConditionRatingsData resultData)
        {
            conditions.TryGetType(out ISemanticTagCondition[] semanticTagConditions);
            traitCache.TryGetType(out List<Dictionary<int, bool>> semanticTagTraits);

            var semanticTagsMatched = RateSemanticTagConditionMatches(semanticTagConditions,
                semanticTagTraits, resultData[typeof(bool)], resultData.MatchRuleIndexes);

            return semanticTagsMatched && RateConditionMatchesInternal(conditions, traitCache, resultData);
        }

        internal static bool RateConditionMatches<T>(ICondition<T>[] typeConditions, List<Dictionary<int, T>> traitCollections,
            List<Dictionary<int, float>> preAllocatedRatingStorage)
        {
            for (var i = 0; i < typeConditions.Length; i++)
            {
                var condition = typeConditions[i];
                var traits = traitCollections[i];
                var ratings = preAllocatedRatingStorage[i];
                ratings.Clear();
                foreach (var kvp in traits)
                {
                    var value = kvp.Value;
                    var rating = condition.RateDataMatch(ref value);
                    // exclude all failing pieces of data from the results list
                    if (rating <= 0f)
                        continue;

                    ratings.Add(kvp.Key, rating);
                }

                if (ratings.Count == 0)        // trait found, but no matches
                    return false;
            }

            return true;
        }

        internal static bool RateSemanticTagConditionMatches(ISemanticTagCondition[] conditions, List<Dictionary<int, bool>> traitCollections,
            List<Dictionary<int, float>> preAllocatedRatingStorage, List<SemanticTagMatchRule> matchRuleIndexes)
        {
            if (conditions == null)
                return true;

            matchRuleIndexes.Clear();
            for (var i = 0; i < conditions.Length; i++)
            {
                var condition = conditions[i];
                matchRuleIndexes.Add(condition.matchRule);
                var isExcluding = condition.matchRule == SemanticTagMatchRule.Exclude;

                var traits = traitCollections[i];
                // If this condition is excluding a trait, and that trait isn't found, it passes.
                if (traits == null)
                {
                    if (isExcluding)
                        continue;

                    return false;
                }

                var ratings = preAllocatedRatingStorage[i];
                ratings.Clear();
                foreach (var kvp in traits)
                {
                    var id = kvp.Key;
                    var value = kvp.Value;
                    var rating = condition.RateDataMatch(ref value);
                    if(rating > 0f)
                        ratings.Add(id, rating);
                }

                // trait found, but no matches
                if (ratings.Count == 0 && !isExcluding)
                    return false;
            }

            return true;
        }

        internal static bool RateMatches<T1, T2>(ICondition<T1, T2> condition,
            Dictionary<int, T1> traitValues1,
            Dictionary<int, T2> traitValues2,
            IEnumerable<int> intersection,
            ref NativeConditionRatings ratingStorage,
            ref int conditionIndex)
            where T1 : struct
            where T2 : struct
        {
            var count = 0;
            ratingStorage.StartCondition(conditionIndex);
            foreach (var id in intersection)
            {
                var t1Value = traitValues1[id];
                var t2Value = traitValues2[id];
                var rating = condition.RateDataMatch(ref t1Value, ref t2Value);
                if(ratingStorage.Add(rating))
                    count++;
            }

            conditionIndex++;
            return count != 0;
        }

        internal static bool RateMatches<T1, T2, T3>(ICondition<T1, T2, T3> condition,
            Dictionary<int, T1> traitValues1,
            Dictionary<int, T2> traitValues2,
            Dictionary<int, T3> traitValues3,
            IEnumerable<int> intersection,
            ref NativeConditionRatings ratingStorage,
            ref int conditionIndex)
            where T1 : struct
            where T2 : struct
            where T3 : struct
        {
            var count = 0;
            ratingStorage.StartCondition(conditionIndex);
            foreach (var id in intersection)
            {
                var t1Value = traitValues1[id];
                var t2Value = traitValues2[id];
                var t3Value = traitValues3[id];
                var rating = condition.RateDataMatch(ref t1Value, ref t2Value, ref t3Value);
                if(ratingStorage.Add(rating))
                    count++;
            }

            conditionIndex++;
            return count != 0;
        }

        // ReSharper disable once UnusedMember.Local
        static bool RateConditionMatchesInternal(object conditions, object traits, object results) { return false; }
    }

    class ConditionMatchRatingStage : QueryStage<MatchRatingDataTransform>
    {
        public ConditionMatchRatingStage(MatchRatingDataTransform transformation, int frameBudget = 1)
            : base("Condition Match Rating", transformation)
        {
        }
    }
}
