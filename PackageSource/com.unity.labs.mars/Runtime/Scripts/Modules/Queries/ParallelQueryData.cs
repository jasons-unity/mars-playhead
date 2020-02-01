using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Stores all data for each standalone query match in a number of parallel lists
    /// </summary>
    class ParallelQueryData : ParallelSparseLists
    {
        internal readonly List<int> acquiringIndices = new List<int>();
        internal readonly List<int> filteredAcquiringIndices = new List<int>();
        internal readonly List<int> potentialMatchAcquiringIndices = new List<int>();
        internal readonly List<int> definiteMatchAcquireIndices = new List<int>();
        internal readonly HashSet<int> updatingIndices = new HashSet<int>();

        public int Count { get { return m_Count; } }

        /// <summary>
        /// Maps a query match to an index into the parallel lists
        /// </summary>
        public readonly Dictionary<QueryMatchID, int> matchIdToIndex;

        // All per query match data is stored in these lists
        public List<QueryMatchID> queryMatchIds;
        public List<QueryArgs> queryArgs;
        // input data - this data comes from QueryArgs
        public List<Exclusivity> exclusivities;
        public List<float> updateMatchInterval;
        public List<float> timeOuts;
        public List<bool> reAcquireOnLoss;
        public List<Conditions> conditions;
        public List<ContextTraitRequirements> traitRequirements;

        // internal data - traits, match sets, ratings
        public List<CachedTraitCollection> cachedTraits;
        public List<ConditionRatingsData> conditionRatings;
        public List<HashSet<int>> conditionMatchSets;
        public List<Dictionary<int, float>> reducedConditionRatings;
        // final per-query results - match id and query result
        public List<int> bestMatchDataIds;
        public List<QueryResult> queryResults;
        // event handlers
        public List<Action<QueryResult>> acquireHandlers;
        public List<Action<QueryResult>> updateHandlers;
        public List<Action<QueryResult>> lossHandlers;
        public List<Action<QueryArgs>> timeoutHandlers;

        public void ClearCycleIndices()
        {
            filteredAcquiringIndices.Clear();
            potentialMatchAcquiringIndices.Clear();
            definiteMatchAcquireIndices.Clear();
        }

        public bool Remove(QueryMatchID id)
        {
            int index;
            if (!matchIdToIndex.TryGetValue(id, out index))
                return false;

            matchIdToIndex.Remove(id);

            queryMatchIds[index] = QueryMatchID.NullQuery;
            queryArgs[index] = null;
            // the query result can still be in use (by the onLoss event), so we don't pool/recycle it
            queryResults[index] = null;

            Pools.TraitCaches.Recycle(cachedTraits[index]);
            cachedTraits[index] = null;
            Pools.ConditionRatings.Recycle(conditionRatings[index]);
            conditionRatings[index] = null;

            // these collection members are re-usable, but we null them out so it's clear what data is invalid
            Pools.DataIdHashSets.Recycle(conditionMatchSets[index]);
            conditionMatchSets[index] = null;
            Pools.Ratings.Recycle(reducedConditionRatings[index]);
            reducedConditionRatings[index] = null;

            exclusivities[index] = default(Exclusivity);
            bestMatchDataIds[index] = -1;
            timeOuts[index] = 0f;
            conditions[index] = null;
            traitRequirements[index] = null;
            reAcquireOnLoss[index] = false;
            updateMatchInterval[index] = 0f;

            acquireHandlers[index] = null;
            updateHandlers[index] = null;
            lossHandlers[index] = null;
            timeoutHandlers[index] = null;

            FreeIndex(index);
            updatingIndices.Remove(index);
            acquiringIndices.Remove(index);
            m_Count--;
            return true;
        }

        internal bool RemoveAndTimeout(QueryMatchID id)
        {
            if (!matchIdToIndex.TryGetValue(id, out var index))
                return false;

            timeoutHandlers[index]?.Invoke(queryArgs[index]);
            return Remove(id);
        }

        public void Clear()
        {
            m_Count = 0;
            matchIdToIndex.Clear();
            ValidIndices.Clear();
            FreedIndices.Clear();
            filteredAcquiringIndices.Clear();
            acquiringIndices.Clear();
            potentialMatchAcquiringIndices.Clear();
            definiteMatchAcquireIndices.Clear();
            updatingIndices.Clear();

            queryMatchIds.Clear();
            queryArgs.Clear();
            exclusivities.Clear();
            queryResults.Clear();

            cachedTraits.Clear();
            traitRequirements.Clear();
            conditions.Clear();
            conditionRatings.Clear();
            conditionMatchSets.Clear();
            reducedConditionRatings.Clear();

            timeOuts.Clear();
            reAcquireOnLoss.Clear();
            updateMatchInterval.Clear();
            bestMatchDataIds.Clear();

            // event handlers
            acquireHandlers.Clear();
            updateHandlers.Clear();
            lossHandlers.Clear();
            timeoutHandlers.Clear();
        }

        public ParallelQueryData(int initialCapacity)
        {
            matchIdToIndex = new Dictionary<QueryMatchID, int>(initialCapacity);

            queryMatchIds = new List<QueryMatchID>(initialCapacity);
            queryArgs = new List<QueryArgs>(initialCapacity);
            cachedTraits = new List<CachedTraitCollection>(initialCapacity);
            exclusivities = new List<Exclusivity>(initialCapacity);
            bestMatchDataIds = new List<int>(initialCapacity);
            timeOuts = new List<float>(initialCapacity);
            traitRequirements = new List<ContextTraitRequirements>(initialCapacity);
            conditions = new List<Conditions>(initialCapacity);
            conditionRatings = new List<ConditionRatingsData>(initialCapacity);
            reducedConditionRatings = new List<Dictionary<int, float>>(initialCapacity);
            conditionMatchSets = new List<HashSet<int>>(initialCapacity);
            queryResults = new List<QueryResult>(initialCapacity);
            reAcquireOnLoss = new List<bool>(initialCapacity);
            updateMatchInterval = new List<float>(initialCapacity);

            acquireHandlers = new List<Action<QueryResult>>(initialCapacity);
            updateHandlers = new List<Action<QueryResult>>(initialCapacity);
            lossHandlers = new List<Action<QueryResult>>(initialCapacity);
            timeoutHandlers = new List<Action<QueryArgs>>(initialCapacity);
        }

        public QueryMatchID Register(QueryArgs args)
        {
            var matchId = QueryMatchID.Generate();
            Register(matchId, args);
            return matchId;
        }

        public void Register(QueryMatchID id, QueryArgs args)
        {
            var index = GetInsertionIndex();
            // this is an insert at the current end of the lists, so we need to Add
            if (index == queryMatchIds.Count)
            {
                queryMatchIds.Add(id);
                queryArgs.Add(args);
                exclusivities.Add(args.exclusivity);
                bestMatchDataIds.Add(-1);
                timeOuts.Add(args.commonQueryData.timeOut);
                cachedTraits.Add(new CachedTraitCollection(args.conditions));
                traitRequirements.Add(args.traitRequirements);
                conditions.Add(args.conditions);
                conditionRatings.Add(Pools.ConditionRatings.Get().Initialize(args.conditions));
                conditionMatchSets.Add(Pools.DataIdHashSets.Get());
                reducedConditionRatings.Add(Pools.Ratings.Get());
                queryResults.Add(new QueryResult {queryMatchId = id});
                reAcquireOnLoss.Add(args.commonQueryData.reacquireOnLoss);
                updateMatchInterval.Add(args.commonQueryData.updateMatchInterval);
                acquireHandlers.Add(args.onAcquire);
                updateHandlers.Add(args.onMatchUpdate);
                lossHandlers.Add(args.onLoss);
                timeoutHandlers.Add(args.onTimeout);
            }
            // this is an insert into a previously freed index
            else
            {
                queryMatchIds[index] = id;
                queryArgs[index] = args;
                exclusivities[index] = args.exclusivity;
                bestMatchDataIds[index] = -1;
                timeOuts[index] = args.commonQueryData.timeOut;
                cachedTraits[index] = new CachedTraitCollection(args.conditions);
                traitRequirements[index] = args.traitRequirements;
                conditions[index] = args.conditions;
                conditionRatings[index] = Pools.ConditionRatings.Get().Initialize(args.conditions);
                conditionMatchSets[index] = Pools.DataIdHashSets.Get();
                reducedConditionRatings[index] = Pools.Ratings.Get();
                queryResults[index] = new QueryResult {queryMatchId = id};
                reAcquireOnLoss[index] = args.commonQueryData.reacquireOnLoss;
                updateMatchInterval[index] = args.commonQueryData.updateMatchInterval;
                acquireHandlers[index] = args.onAcquire;
                updateHandlers[index] = args.onMatchUpdate;
                lossHandlers[index] = args.onLoss;
                timeoutHandlers[index] = args.onTimeout;
            }

            m_Count++;
            ValidIndices.Add(index);
            matchIdToIndex.Add(id, index);
            acquiringIndices.Add(index);
        }
    }
}
