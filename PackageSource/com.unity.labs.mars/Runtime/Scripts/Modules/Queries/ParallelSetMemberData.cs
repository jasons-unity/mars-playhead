using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Stores all data for each set member in a number of parallel lists
    /// </summary>
    class ParallelSetMemberData : ParallelSparseLists
    {
        internal readonly List<int> AcquiringIndices = new List<int>();
        internal readonly List<int> FilteredAcquiringIndices = new List<int>();
        internal readonly List<int> PotentialMatchAcquiringIndices = new List<int>();
        internal readonly List<int> DefiniteMatchAcquireIndices = new List<int>();
        internal readonly HashSet<int> UpdatingIndices = new HashSet<int>();

        public int Count => m_Count;

        /// <summary>
        /// Maps a query match to all indices it is at
        /// </summary>
        public readonly Dictionary<QueryMatchID, List<int>> MatchIdToIndex;

        // All per query match data is stored in these lists
        public readonly List<QueryMatchID> QueryMatchIds;
        // input data - this data comes from QueryArgs
        public List<Exclusivity> Exclusivities;
        public List<bool> Required;
        public List<Conditions> Conditions;
        // internal data - traits, match sets, ratings
        public List<CachedTraitCollection> CachedTraits;
        public List<ContextTraitRequirements> TraitRequirements;
        public List<ConditionRatingsData> ConditionRatings;
        public List<HashSet<int>> ConditionMatchSets;
        public List<Dictionary<int, float>> ReducedConditionRatings;
        // final per-query results - match id and query result
        public List<int> BestMatchDataIds;
        public List<QueryResult> QueryResults;

        public List<IMRObject> ObjectReferences;
        internal List<RelationMembership[]> RelationMemberships;

        public void ClearCycleIndices()
        {
            FilteredAcquiringIndices.Clear();
            PotentialMatchAcquiringIndices.Clear();
            DefiniteMatchAcquireIndices.Clear();
        }

        public bool Remove(QueryMatchID id)
        {
            List<int> indices;
            if (!MatchIdToIndex.TryGetValue(id, out indices))
                return false;

            MatchIdToIndex.Remove(id);
            foreach (var index in indices)
            {
                QueryMatchIds[index] = QueryMatchID.NullQuery;
                // the query result can still be in use (by the onLoss event), so we don't pool/recycle it
                QueryResults[index] = null;
                ObjectReferences[index] = null;

                Pools.TraitCaches.Recycle(CachedTraits[index]);
                CachedTraits[index] = null;
                Pools.ConditionRatings.Recycle(ConditionRatings[index]);
                ConditionRatings[index] = null;

                // these collection members are re-usable, but we null them out so it's clear what data is invalid
                Pools.DataIdHashSets.Recycle(ConditionMatchSets[index]);
                ConditionMatchSets[index] = null;
                Pools.Ratings.Recycle(ReducedConditionRatings[index]);
                ReducedConditionRatings[index] = null;
                TraitRequirements[index] = null;

                Exclusivities[index] = default;
                BestMatchDataIds[index] = -1;
                Conditions[index] = null;
                Required[index] = false;

                FreeIndex(index);
                UpdatingIndices.Remove(index);
                AcquiringIndices.Remove(index);
                m_Count--;
            }

            return true;
        }

        public void Clear()
        {
            m_Count = 0;
            MatchIdToIndex.Clear();

            ValidIndices.Clear();
            FreedIndices.Clear();
            FilteredAcquiringIndices.Clear();
            AcquiringIndices.Clear();
            DefiniteMatchAcquireIndices.Clear();
            UpdatingIndices.Clear();

            QueryMatchIds.Clear();
            Exclusivities.Clear();
            QueryResults.Clear();

            CachedTraits.Clear();
            Conditions.Clear();
            TraitRequirements.Clear();
            ConditionRatings.Clear();
            ConditionMatchSets.Clear();
            ReducedConditionRatings.Clear();

            Required.Clear();
            ObjectReferences.Clear();
            RelationMemberships.Clear();
            BestMatchDataIds.Clear();
        }

        public ParallelSetMemberData(int initialCapacity)
        {
            MatchIdToIndex = new Dictionary<QueryMatchID, List<int>>(initialCapacity);

            QueryMatchIds = new List<QueryMatchID>(initialCapacity);
            CachedTraits = new List<CachedTraitCollection>(initialCapacity);
            Exclusivities = new List<Exclusivity>(initialCapacity);
            BestMatchDataIds = new List<int>(initialCapacity);
            ObjectReferences = new List<IMRObject>(initialCapacity);
            RelationMemberships = new List<RelationMembership[]>(initialCapacity);
            Conditions = new List<Conditions>(initialCapacity);
            TraitRequirements = new List<ContextTraitRequirements>(initialCapacity);
            ConditionRatings = new List<ConditionRatingsData>(initialCapacity);
            ReducedConditionRatings = new List<Dictionary<int, float>>(initialCapacity);
            ConditionMatchSets = new List<HashSet<int>>(initialCapacity);
            QueryResults = new List<QueryResult>(initialCapacity);
            Required = new List<bool>(initialCapacity);
        }

        public void Register(QueryMatchID id, Dictionary<IMRObject, SetChildArgs> childArgs)
        {
            MatchIdToIndex.Add(id, new List<int>());
            foreach (var kvp in childArgs)
            {
                Register(id, kvp.Key, kvp.Value);
            }
        }

        public int Register(QueryMatchID id, IMRObject obj, SetChildArgs args)
        {
            var index = Add(id,
                obj,
                args.tryBestMatchArgs.exclusivity,
                -1,
                args.required,
                new CachedTraitCollection(args.tryBestMatchArgs.conditions),
                args.tryBestMatchArgs.conditions,
                args.TraitRequirements,
                Pools.ConditionRatings.Get().Initialize(args.tryBestMatchArgs.conditions),
                Pools.DataIdHashSets.Get(),
                Pools.Ratings.Get(),
                new QueryResult {queryMatchId = id});

            AcquiringIndices.Add(index);

            List<int> tempIndices;
            if (MatchIdToIndex.TryGetValue(id, out tempIndices))
            {
                tempIndices.Add(index);
            }
            else
            {
                tempIndices = new List<int>{index};
                MatchIdToIndex[id] = tempIndices;
            }

            return index;
        }

        int Add(QueryMatchID matchId,
                IMRObject objectRef,
                Exclusivity exclusivity,
                int bestMatchDataId,
                bool isRequired,
                CachedTraitCollection traitCache,
                Conditions condition,
                ContextTraitRequirements requirements,
                ConditionRatingsData rating,
                HashSet<int> matchSet,
                Dictionary<int, float> flatRatings,
                QueryResult result)
        {
            var index = GetInsertionIndex();

            if (index == QueryMatchIds.Count)
            {
                QueryMatchIds.Add(matchId);
                ObjectReferences.Add(objectRef);
                Exclusivities.Add(exclusivity);
                Required.Add(isRequired);
                BestMatchDataIds.Add(bestMatchDataId);
                CachedTraits.Add(traitCache);
                Conditions.Add(condition);
                TraitRequirements.Add(requirements);
                // this entry will be filled in after all members for a set are registered - just make space for it.
                RelationMemberships.Add(null);
                ConditionRatings.Add(rating);
                ConditionMatchSets.Add(matchSet);
                ReducedConditionRatings.Add(flatRatings);
                QueryResults.Add(result);
            }
            else
            {
                QueryMatchIds[index] = matchId;
                ObjectReferences[index] = objectRef;
                Exclusivities[index] = exclusivity;
                Required[index] = isRequired;
                BestMatchDataIds[index] = bestMatchDataId;
                CachedTraits[index] = traitCache;
                Conditions[index] = condition;
                TraitRequirements[index] = requirements;
                RelationMemberships[index] = null;
                ConditionRatings[index] = rating;
                ReducedConditionRatings[index] = flatRatings;
                ConditionMatchSets[index] = matchSet;
                QueryResults[index] = result;
            }

            m_Count++;
            ValidIndices.Add(index);
            return index;
        }

        /// <summary>
        /// Try to find the indices (into the set member data) of both members of a relation
        /// </summary>
        /// <param name="queryMatchId">ID of the query match</param>
        /// <param name="context2">Object reference to the first member</param>
        /// <param name="context1">Object reference to the second member</param>
        /// <param name="index1">Index of the first member</param>
        /// <param name="index2">Index of the second member</param>
        /// <returns>True if both member's indices are found, false otherwise.</returns>
        public bool TryGetRelationMemberIndices(QueryMatchID queryMatchId, IMRObject context1, IMRObject context2,
            out int index1, out int index2)
        {
            List<int> indices;
            if (!MatchIdToIndex.TryGetValue(queryMatchId, out indices))
            {
                index1 = -1;
                index2 = -1;
                return false;
            }

            int tempIndex1 = -1, tempIndex2 = -1;
            foreach (var storageIndex in indices)
            {
                var storedContext = ObjectReferences[storageIndex];
                if (storedContext == context1)
                    tempIndex1 = storageIndex;
                else if (storedContext == context2)
                    tempIndex2 = storageIndex;
            }

            if (tempIndex1 > -1 && tempIndex2 > -1)
            {
                index1 = tempIndex1;
                index2 = tempIndex2;
                return true;
            }


            index1 = -1;
            index2 = -1;
            return false;
        }
    }
}
