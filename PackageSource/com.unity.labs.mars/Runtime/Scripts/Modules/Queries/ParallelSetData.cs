using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Stores all data for each set query, in a number of parallel arrays
    /// </summary>
    class ParallelSetData : ParallelSparseArrays
    {
        const int k_RelationsDefaultCapacity = 12;

        internal readonly List<int> AcquiringIndices = new List<int>();
        internal readonly List<int> FilteredAcquiringIndices = new List<int>();
        internal readonly List<int> PotentialMatchAcquiringIndices = new List<int>();
        internal readonly List<int> DefiniteMatchAcquireIndices = new List<int>();
        internal readonly HashSet<int> UpdatingIndices = new HashSet<int>();

        /// <summary> Maps a query match to an index into the parallel arrays</summary>
        internal readonly Dictionary<QueryMatchID, int> MatchIdToIndex;

        /// <summary>The QueryMatchID of the Set</summary>
        internal QueryMatchID[] QueryMatchIds;
        /// <summary>The input arguments for each set query - used for timeout handling</summary>
        internal SetQueryArgs[] SetQueryArgs;
        /// <summary> How often to update each Set </summary>
        internal float[] UpdateMatchInterval;
        /// <summary> MarsTime.Time's value when this query's Update was last called</summary>
        internal float[] LastUpdateCheckTime;
        /// <summary>How long until the Set times out</summary>
        internal float[] TimeOuts;
        /// <summary>Whether to attempt to acquire another match when the match is lost</summary>
        internal bool[] ReAcquireOnLoss;
        /// <summary>All Relations between set members </summary>
        internal Relations[] Relations;
        /// <summary>References to the trait values required by Relations</summary>
        internal RelationTraitCache[] CachedTraits;
        /// <summary>The weights used to determine the final rating for a given set</summary>
        internal SetRatingConfiguration[] RatingConfigurations;
        /// <summary>Where all results for Relation evaluation are stored</summary>
        internal RelationRatingsData[] RelationRatings;
        /// <summary>The final results that will be passed to user code on acquisition</summary>
        internal SetQueryResult[] QueryResults;

        // event handlers
        internal Action<SetQueryResult>[] AcquireHandlers;
        internal Action<SetQueryResult>[] UpdateHandlers;
        internal Action<SetQueryResult>[] LossHandlers;
        internal Action<SetQueryArgs>[] TimeoutHandlers;

        // All collections below this point are not directly from user input or result output,
        // but are internal record keeping.

        /// <summary>
        /// For each Relation in the Set, an index pair for the Relation members,
        /// where the indices refer to the parallel collections that store all Sets' members
        /// </summary>
        internal RelationDataPair[][] RelationIndexPairs;
        /// <summary>
        /// For each Relation in the Set, an index pair for the Relation members,
        /// where the indices refer to a collection with only that Set's members
        /// </summary>
        internal RelationDataPair[][] LocalRelationIndexPairs;
        /// <summary>For each member of the Set, an index for that member</summary>
        internal int[][] MemberIndices;
        /// <summary>The weighting number used to determine the order Sets are solved</summary>
        internal float[] OrderWeights;
        /// <summary>Intermediate data used in searching set matches</summary>
        internal SetSearchData[] SearchData;
        /// <summary>All data ids used by each acquired instance of the Set query</summary>
        internal HashSet<int>[] UsedByMatch;
        /// <summary>
        /// A record of match data in the format needed to mark set data as used
        /// TODO - obsolete this in a future PR, by moving data use marking entirely to new model
        /// </summary>
        internal SetMatchData[] SetMatchData;

        internal Action OnResize { get; set; }

        // local method use only - not part of the parallel collections that store query data
        static readonly List<RelationMembership> k_CurrentMemberRelations = new List<RelationMembership>(k_RelationsDefaultCapacity);
        static readonly HashSet<int> k_AllRelationMemberIndicesInSet = new HashSet<int>();
        static readonly Dictionary<int, int> k_IndexTranslations = new Dictionary<int, int>();

        public void ClearCycleIndices()
        {
            FilteredAcquiringIndices.Clear();
            PotentialMatchAcquiringIndices.Clear();
            DefiniteMatchAcquireIndices.Clear();
        }

        internal void GetRelationMemberships(int setIndex, Dictionary<int, RelationMembership[]> memberships)
        {
            memberships.Clear();
            var pairs = RelationIndexPairs[setIndex];
            // find the set of unique members that belong to any Relation in this Set
            k_AllRelationMemberIndicesInSet.Clear();
            foreach (var pair in pairs)
            {
                k_AllRelationMemberIndicesInSet.Add(pair.Child1);
                k_AllRelationMemberIndicesInSet.Add(pair.Child2);
            }

            foreach (var relationMemberIndex in k_AllRelationMemberIndicesInSet)
            {
                // find which Relations this Set member belongs to, and which child it is in that Relation
                k_CurrentMemberRelations.Clear();
                for (var p = 0; p < pairs.Length; p++)
                {
                    var pair = pairs[p];
                    if (pair.Child1 == relationMemberIndex)
                        k_CurrentMemberRelations.Add(new RelationMembership(p, true));
                    else if (pair.Child2 == relationMemberIndex)
                        k_CurrentMemberRelations.Add(new RelationMembership(p, false));
                }

                memberships.Add(relationMemberIndex, k_CurrentMemberRelations.ToArray());
            }
        }

        public bool Remove(QueryMatchID id)
        {
            int index;
            if (!MatchIdToIndex.TryGetValue(id, out index))
                return false;

            MatchIdToIndex.Remove(id);

            QueryMatchIds[index] = QueryMatchID.NullQuery;
            // the query result can still be in use (by the onLoss event), so we don't pool/recycle it
            QueryResults[index] = null;

            Pools.DataIdHashSets.Recycle(UsedByMatch[index]);
            UsedByMatch[index] = null;

            Pools.RelationTraitCaches.Recycle(CachedTraits[index]);
            CachedTraits[index] = null;
            Pools.RelationRatings.Recycle(RelationRatings[index]);
            RelationRatings[index] = null;

            RelationIndexPairs[index] = null;
            LocalRelationIndexPairs[index] = null;
            RatingConfigurations[index] = default(SetRatingConfiguration);
            SetQueryArgs[index] = null;
            SearchData[index] = null;
            SetMatchData[index] = default;

            TimeOuts[index] = 0f;
            OrderWeights[index] = 0f;
            Relations[index] = null;
            ReAcquireOnLoss[index] = false;
            UpdateMatchInterval[index] = 0f;
            LastUpdateCheckTime[index] = 0f;

            AcquireHandlers[index] = null;
            UpdateHandlers[index] = null;
            LossHandlers[index] = null;
            TimeoutHandlers[index] = null;

            FreeIndex(index);
            if (UpdatingIndices.Contains(index))
                UpdatingIndices.Remove(index);

            if (AcquiringIndices.Contains(index))
                AcquiringIndices.Remove(index);

            m_Count--;
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

            Array.Clear(QueryMatchIds, 0, QueryMatchIds.Length);
            Array.Clear(SetQueryArgs, 0, SetQueryArgs.Length);
            Array.Clear(QueryResults, 0, QueryResults.Length);

            foreach (var hashSet in UsedByMatch)
            {
                if(hashSet != null)
                    Pools.DataIdHashSets.Recycle(hashSet);
            }
            Array.Clear(UsedByMatch, 0,  UsedByMatch.Length);

            Array.Clear(CachedTraits, 0, CachedTraits.Length);
            Array.Clear(Relations, 0, Relations.Length);
            Array.Clear(RelationRatings, 0, RelationRatings.Length);
            Array.Clear(SearchData, 0, SearchData.Length);
            Array.Clear(SetMatchData, 0, SetMatchData.Length);
            Array.Clear(RatingConfigurations, 0, RatingConfigurations.Length);
            Array.Clear(RelationIndexPairs, 0, RelationIndexPairs.Length);
            Array.Clear(LocalRelationIndexPairs, 0, LocalRelationIndexPairs.Length);
            Array.Clear(ReAcquireOnLoss, 0, ReAcquireOnLoss.Length);
            Array.Clear(UpdateMatchInterval, 0, UpdateMatchInterval.Length);
            Array.Clear(LastUpdateCheckTime, 0, LastUpdateCheckTime.Length);
            Array.Clear(OrderWeights, 0, OrderWeights.Length);
            Array.Clear(TimeOuts, 0, UpdateMatchInterval.Length);
            Array.Clear(AcquireHandlers, 0, AcquireHandlers.Length);
            Array.Clear(UpdateHandlers, 0, UpdateHandlers.Length);
            Array.Clear(LossHandlers, 0, LossHandlers.Length);
            Array.Clear(TimeoutHandlers, 0, TimeoutHandlers.Length);
        }

        public ParallelSetData(int initialCapacity) : base(initialCapacity)
        {
            MatchIdToIndex = new Dictionary<QueryMatchID, int>(initialCapacity);

            QueryMatchIds = new QueryMatchID[initialCapacity];
            SetQueryArgs = new SetQueryArgs[initialCapacity];
            MemberIndices = new int[initialCapacity][];

            CachedTraits = new RelationTraitCache[initialCapacity];
            TimeOuts = new float[initialCapacity];
            Relations = new Relations[initialCapacity];
            RelationRatings = new RelationRatingsData[initialCapacity];
            RatingConfigurations = new SetRatingConfiguration[initialCapacity];
            SearchData = new SetSearchData[initialCapacity];
            SetMatchData = new SetMatchData[initialCapacity];
            UsedByMatch = new HashSet<int>[initialCapacity];

            OrderWeights = new float[initialCapacity];
            RelationIndexPairs = new RelationDataPair[initialCapacity][];
            LocalRelationIndexPairs = new RelationDataPair[initialCapacity][];

            QueryResults = new SetQueryResult[initialCapacity];
            ReAcquireOnLoss = new bool[initialCapacity];
            UpdateMatchInterval = new float[initialCapacity];
            LastUpdateCheckTime = new float[initialCapacity];

            AcquireHandlers = new Action<SetQueryResult>[initialCapacity];
            UpdateHandlers = new Action<SetQueryResult>[initialCapacity];
            LossHandlers = new Action<SetQueryResult>[initialCapacity];
            TimeoutHandlers = new Action<SetQueryArgs>[initialCapacity];
        }

        public int Register(QueryMatchID id, int[] members, RelationDataPair[] pairs, SetQueryArgs args)
        {
            var index = Add(id,
                members,
                args.commonQueryData.timeOut,
                new RelationTraitCache(args.relations),
                args.relations,
                Pools.RelationRatings.Get().Initialize(args.relations),
                pairs,
                new SetQueryResult(id),
                args.commonQueryData.reacquireOnLoss,
                args.commonQueryData.updateMatchInterval,
                args.onAcquire,
                args.onMatchUpdate,
                args.onLoss,
                args.onTimeout);

            SetQueryArgs[index] = args;

            MatchIdToIndex.Add(id, index);
            AcquiringIndices.Add(index);
            return index;
        }

        /// <summary>
        /// Translate the global relation member indices into "local" indices.
        /// A global index refers to the member's index in the parallel collections that hold all data.
        /// A local index refers to the index when all set members are represented
        /// in a collection that has length equal to count of set members.
        /// </summary>
        /// <param name="setMemberIndices">All member indices in the set</param>
        /// <param name="globalPairs">The relation index pairs for the global collections</param>
        /// <param name="localPairs">The output local relation pairs</param>
        internal static void MapGlobalToLocalRelationPairs(int[] setMemberIndices,
            RelationDataPair[] globalPairs, RelationDataPair[] localPairs)
        {
            k_IndexTranslations.Clear();
            for (var i = 0; i < setMemberIndices.Length; i++)
                k_IndexTranslations.Add(setMemberIndices[i], i);

            for (var i = 0; i < globalPairs.Length; i++)
            {
                var inputPair = globalPairs[i];
                var translated1 = k_IndexTranslations[inputPair.Child1];
                var translated2 = k_IndexTranslations[inputPair.Child2];
                localPairs[i] = new RelationDataPair(translated1, translated2);
            }
        }

        int Add(QueryMatchID matchId,
            int[] members,
            float timeOut,
            RelationTraitCache traitCache,
            Relations relationConditions,
            RelationRatingsData rating,
            RelationDataPair[] indexPairs,
            SetQueryResult result,
            bool reAcquireQueryOnLoss,
            float matchUpdateInterval,
            Action<SetQueryResult> acquireHandler,
            Action<SetQueryResult> updateHandler,
            Action<SetQueryResult> lossHandler,
            Action<SetQueryArgs> timeoutHandler)
        {
            var index = GetInsertionIndex();

            QueryMatchIds[index] = matchId;
            MemberIndices[index] = members;

            UsedByMatch[index] = Pools.DataIdHashSets.Get();
            SetMatchData[index] = new SetMatchData()
            {
                dataAssignments = new Dictionary<IMRObject, int>(),
                exclusivities = new Dictionary<IMRObject, Exclusivity>()
            };

            CachedTraits[index] = traitCache;
            Relations[index] = relationConditions;
            RelationRatings[index] = rating;

            RelationIndexPairs[index] = indexPairs;
            var localPairs = new RelationDataPair[indexPairs.Length];
            MapGlobalToLocalRelationPairs(members, indexPairs, localPairs);
            LocalRelationIndexPairs[index] = localPairs;

            QueryResults[index] = result;
            TimeOuts[index] = timeOut;
            ReAcquireOnLoss[index] = reAcquireQueryOnLoss;
            UpdateMatchInterval[index] = matchUpdateInterval;
            LastUpdateCheckTime[index] = 0f;
            AcquireHandlers[index] = acquireHandler;
            UpdateHandlers[index] = updateHandler;
            LossHandlers[index] = lossHandler;
            TimeoutHandlers[index] = timeoutHandler;
            // SearchData will be initialized after the initial add

            m_Count++;
            ValidIndices.Add(index);
            return index;
        }

        internal void InitializeSearchData(int setIndex, List<Dictionary<int, float>> allMemberRatings)
        {
            const int matchBufferCapacity = 64;
            var memberIndices = MemberIndices[setIndex];
            SearchData[setIndex] = new SetSearchData(matchBufferCapacity, memberIndices, allMemberRatings);
        }

        internal override void Resize(int newSize)
        {
            m_Capacity = newSize;
            Array.Resize(ref QueryMatchIds, newSize);
            Array.Resize(ref SetQueryArgs, newSize);
            Array.Resize(ref QueryResults, newSize);
            Array.Resize(ref OrderWeights, newSize);
            Array.Resize(ref UsedByMatch, newSize);
            Array.Resize(ref RelationIndexPairs, newSize);
            Array.Resize(ref LocalRelationIndexPairs, newSize);
            Array.Resize(ref RatingConfigurations, newSize);
            Array.Resize(ref MemberIndices, newSize);
            Array.Resize(ref CachedTraits, newSize);
            Array.Resize(ref Relations, newSize);
            Array.Resize(ref RelationRatings, newSize);
            Array.Resize(ref SearchData, newSize);
            Array.Resize(ref SetMatchData, newSize);
            Array.Resize(ref ReAcquireOnLoss, newSize);
            Array.Resize(ref UpdateMatchInterval, newSize);
            Array.Resize(ref LastUpdateCheckTime, newSize);
            Array.Resize(ref TimeOuts, newSize);
            Array.Resize(ref AcquireHandlers, newSize);
            Array.Resize(ref UpdateHandlers, newSize);
            Array.Resize(ref LossHandlers, newSize);
            Array.Resize(ref TimeoutHandlers, newSize);

            OnResize?.Invoke();
        }
    }
}
