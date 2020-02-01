using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Labs.MARS.Query
{
    class QueryPipelineBase
    {
        protected static readonly HashSet<int> k_TimeoutIndexes = new HashSet<int>();
        protected static readonly HashSet<QueryMatchID> k_TimeoutIds = new HashSet<QueryMatchID>();

        protected MARSDatabase m_Database;


        protected int m_ElapsedFramesForCurrentStage;

        /// <summary>
        /// The active run configuration for the pipeline
        /// </summary>
        public QueryPipelineConfiguration Configuration;

        public bool CurrentlyActive { get; protected set; }

        internal float LastCycleStartTime { get; set; }
        internal float CycleDeltaTime { get; set; }
    }

    /// <summary>
    /// Connects data about Sets with the data about their members, as well as
    /// connecting all of that data with all query stages used to solve for Set's answers
    /// </summary>
    partial class SetQueryPipeline : QueryPipelineBase
    {
        static readonly Dictionary<int, RelationMembership[]> k_RelationMemberships =
            new Dictionary<int, RelationMembership[]>();

        static readonly Dictionary<IMRObject, QueryResult> k_TempChildResults = new Dictionary<IMRObject, QueryResult>();

        int m_CurrentRunGroupIndex;
        int m_LastCompletedFence;

        internal List<QueryStage> Stages = new List<QueryStage>();

        internal CacheTraitReferencesStage MemberTraitCacheStage;
        internal CacheRelationTraitReferencesStage RelationTraitCacheStage;
        internal ConditionMatchRatingStage MemberConditionRatingStage;
        internal IncompleteGroupFilterStage IncompleteGroupFilterStage;
        internal FindMatchProposalsStage MemberMatchIntersectionStage;
        internal SetDataAvailabilityCheckStage MemberDataAvailabilityStage;
        internal TraitRequirementFilterStage MemberTraitRequirementStage;
        internal MatchReductionStage MemberMatchReductionStage;
        // relation stages run after reducing member matches
        internal RelationRatingStage SetRelationRatingStage;
        internal FilterRelationMemberMatchesStage FilterRelationMembersStage;
        internal SetMatchSearchStage MatchSearchStage;

        internal MarkSetUsedStage MarkDataUsedStage;
        internal ResultFillStage MemberResultFillStage;
        internal SetResultFillStage SetResultFillStage;
        internal SetAcquireHandlingStage AcquireHandlingStage;

        /// <summary>
        /// Stores all data that exists on a per-Set basis
        /// </summary>
        internal ParallelSetData Data;

        /// <summary>
        /// Stores all data that exists on a per-Member basis
        /// </summary>
        internal ParallelSetMemberData MemberData;

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<int> k_SetMemberIndices = new List<int>();
        static readonly List<RelationDataPair> k_RelationIndexPairs = new List<RelationDataPair>();

        internal event Action onStageGroupCompletion;

        internal event Action<HashSet<QueryMatchID>> OnTimeout;

        public SetQueryPipeline(MARSDatabase db)
        {
            m_Database = db;
            SetupData();
        }

        internal void Register(QueryMatchID id, SetQueryArgs args)
        {
            k_SetMemberIndices.Clear();
            foreach (var kvp in args.relations.children)
            {
                var childArgs = kvp.Value;
                // register each set member in the member data and get its index into those collections
                var memberIndex = MemberData.Register(id, kvp.Key, childArgs);
                k_SetMemberIndices.Add(memberIndex);
            }

            GetIndexPairs(id, args);

            // the index arrays for set & relation members are so small that we create new ones
            // on registration, instead of using pooled Lists
            var memberIndices = k_SetMemberIndices.ToArray();
            var relationIndexPairs = k_RelationIndexPairs.ToArray();
            var setIndex = Data.Register(id, memberIndices, relationIndexPairs, args);

            k_TempChildResults.Clear();
            foreach (var i in memberIndices)
            {
                k_TempChildResults.Add(MemberData.ObjectReferences[i], MemberData.QueryResults[i]);
            }

            Data.QueryResults[setIndex].SetChildren(k_TempChildResults);

            // for each set member, store a representation of which relations it belongs to.
            Data.GetRelationMemberships(setIndex, k_RelationMemberships);
            foreach (var kvp in k_RelationMemberships)
            {
                var memberIndex = kvp.Key;
                MemberData.RelationMemberships[memberIndex] = kvp.Value;
            }

            // calculate the solve order weighting for this set
            var weights = QueryPipelineConfiguration.instance.SolveOrderWeighting;
            var order = GetSetOrderWeight(memberIndices, relationIndexPairs.Length, MemberData.Exclusivities, weights);
            Data.OrderWeights[setIndex] = order;

            Data.InitializeSearchData(setIndex, MemberData.ReducedConditionRatings);
        }

        internal bool Remove(QueryMatchID id)
        {
            if (!Data.MatchIdToIndex.ContainsKey(id))
                return false;

            Data.Remove(id);
            MemberData.Remove(id);
            return true;
        }

        internal bool RemoveAndTimeout(QueryMatchID id)
        {
            if (!Data.MatchIdToIndex.TryGetValue(id, out var index))
                return false;

            Data.TimeoutHandlers[index]?.Invoke(Data.SetQueryArgs[index]);
            Data.Remove(id);
            MemberData.Remove(id);
            return true;
        }

        void GetIndexPairs(QueryMatchID id, SetQueryArgs args)
        {
            k_RelationIndexPairs.Clear();
            var relations = args.relations;

            GetAllIndexPairs(id, relations);
        }

        void GetAllIndexPairs(object queryMatchId, object relations) { }

        void GetIndexPairs<T>(QueryMatchID id, IRelation<T>[] relations)
        {
            foreach (var relation in relations)
            {
                int one, two;
                if (MemberData.TryGetRelationMemberIndices(id, relation.child1, relation.child2, out one, out two))
                    k_RelationIndexPairs.Add(new RelationDataPair(one, two));
            }
        }

        /// <summary>
        /// Calculate the weight that determines which of the active Sets get solved first, for a single Set.
        /// </summary>
        /// <param name="memberIndices">The member indices for the Set</param>
        /// <param name="relationCount">The number of relations in the Set</param>
        /// <param name="memberExclusivities">The global container for all set members' Exclusivity value</param>
        /// <param name="weights">The configured weights that determine how important each contribution is</param>
        /// <returns>The order weighting for the Set - higher numbers go before lower ones</returns>
        internal static float GetSetOrderWeight(int[] memberIndices, int relationCount,
            List<Exclusivity> memberExclusivities, SetOrderWeights weights)
        {
            var score = relationCount * weights.RelationWeight;
            foreach (var memberIndex in memberIndices)
            {
                switch (memberExclusivities[memberIndex])
                {
                    case Exclusivity.Reserved:
                        score += weights.ReservedMemberWeight;
                        break;
                    case Exclusivity.Shared:
                        score += weights.SharedMemberWeight;
                        break;
                }
            }

            return score;
        }

        static readonly HashSet<int> k_TimeoutIndices = new HashSet<int>();

        internal void HandleTimeouts()
        {
            CheckTimeouts(Data.AcquiringIndices, Data.TimeOuts, CycleDeltaTime, k_TimeoutIndices);
            foreach (var index in k_TimeoutIndices)
            {
                k_TimeoutIds.Add(Data.QueryMatchIds[index]);
            }

            if (OnTimeout != null)
                OnTimeout(k_TimeoutIds);

            k_TimeoutIds.Clear();
        }

        internal static void CheckTimeouts(List<int> workingIndexes, float[] timeoutsRemaining, float deltaTime,
            HashSet<int> indexesToRemove)
        {
            indexesToRemove.Clear();
            foreach (var i in workingIndexes)
            {
                var remaining = timeoutsRemaining[i];
                // no timeout - ignore this one
                if (remaining <= 0f)
                    continue;

                remaining -= deltaTime;
                if(remaining <= 0f)
                    indexesToRemove.Add(i);
                else
                    timeoutsRemaining[i] = remaining;
            }
        }

        internal void CycleStart()
        {
            Data.ClearCycleIndices();

            // make sure that set members' acquiring indices are always synced to sets' acquiring indices
            MemberData.AcquiringIndices.Clear();
            foreach (var setAcquiringIndex in Data.AcquiringIndices)
            {
                foreach (var mi in Data.MemberIndices[setAcquiringIndex])
                {
                    MemberData.AcquiringIndices.Add(mi);
                }
            }

            MemberData.ClearCycleIndices();

            MemberTraitCacheStage.CycleStart();
            RelationTraitCacheStage.CycleStart();
            MemberConditionRatingStage.CycleStart();
            MemberMatchIntersectionStage.CycleStart();
            MemberTraitRequirementStage.CycleStart();
            MemberDataAvailabilityStage.CycleStart();
            MemberMatchReductionStage.CycleStart();
            SetRelationRatingStage.CycleStart();
            MemberResultFillStage.CycleStart();
            SetResultFillStage.CycleStart();
            FilterRelationMembersStage.CycleStart();
            MatchSearchStage.CycleStart();
            AcquireHandlingStage.CycleStart();
        }

        public void StartCycle()
        {
            var time = MarsTime.Time;
            CycleDeltaTime = time - LastCycleStartTime;
            LastCycleStartTime = time;
            HandleTimeouts();

            m_Database.StartUpdateBuffering();
            m_LastCompletedFence = 1;
            CurrentlyActive = true;

            CycleStart();
        }

        internal void SetupData()
        {
            Configuration = QueryPipelineConfiguration.instance;

            var memoryOptions = MARSMemoryOptions.instance;
            Data = new ParallelSetData(memoryOptions.QueryDataCapacity);
            // make sure that we refresh references to arrays when they have been resized behind the scenes
            Data.OnResize += WireStages;

            var memberCapacityMultiplier = memoryOptions.SetMemberCapacityMultiplier;
            MemberData = new ParallelSetMemberData(memoryOptions.QueryDataCapacity * memberCapacityMultiplier);

            // insert a blank stage to represent the idle part of the cycle
            Stages.Add(null);

            MemberTraitCacheStage = SetupMemberTraitCacheStage();
            RelationTraitCacheStage = SetupRelationTraitCacheStage();
            MemberConditionRatingStage = SetupMemberMatchRating();
            IncompleteGroupFilterStage = SetupIncompleteGroupFilter();
            MemberMatchIntersectionStage = SetupMatchIntersection();
            MemberTraitRequirementStage = SetupTraitFilterStage();
            MemberDataAvailabilityStage = SetupAvailabilityCheckStage();
            MemberMatchReductionStage = SetupMatchReduction();
            SetRelationRatingStage = SetupRelationRatingStage();
            FilterRelationMembersStage = SetupFilterRelationMembersStage();
            MatchSearchStage = SetupMatchSearchStage();
            MarkDataUsedStage = SetupMarkUsedStage();
            MemberResultFillStage = SetupQueryResultFill();
            SetResultFillStage = SetupSetQueryResultFill();

            AcquireHandlingStage = SetupAcquireHandlingStage();
        }

        internal void WireStages()
        {
            WireRelationTraitCacheStage();
            WireRelationRatingStage();
            WireFilterRelationMembersStage();
            WireIncompleteGroupFilterStage();
            WireMatchSearchStage();
            WireMarkUsedStage();
            WireSetQueryResultFillStage();
            WireAcquireHandlingStage();
            WireDataAvailabilityStage();
        }

        internal CacheTraitReferencesStage SetupMemberTraitCacheStage()
        {
            var dataTransform = new TraitRefTransform(m_Database.CacheTraitReferences)
            {
                WorkingIndices = MemberData.AcquiringIndices,
                Input1 = MemberData.FilteredAcquiringIndices,
                Input2 = MemberData.Conditions,
                Output =  MemberData.CachedTraits
            };

            var stage = new CacheTraitReferencesStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        internal CacheRelationTraitReferencesStage SetupRelationTraitCacheStage()
        {
            var dataTransform = new RelationTraitRefTransform(m_Database.CacheTraitReferences)
            {
                WorkingIndices = Data.AcquiringIndices,
                Input1 = Data.FilteredAcquiringIndices,
                Input2 = Data.Relations,
                Output =  Data.CachedTraits
            };

            var stage = new CacheRelationTraitReferencesStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        void WireRelationTraitCacheStage()
        {
            RelationTraitCacheStage.Transformation.Input2 = Data.Relations;
            RelationTraitCacheStage.Transformation.Output = Data.CachedTraits;
        }

        internal ConditionMatchRatingStage SetupMemberMatchRating()
        {
            var dataTransform = new MatchRatingDataTransform
            {
                WorkingIndices = MemberData.FilteredAcquiringIndices,
                Input1 = MemberData.Conditions,
                Input2 = MemberData.CachedTraits,
                Output = MemberData.ConditionRatings,
            };

            var stage = new ConditionMatchRatingStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        internal IncompleteGroupFilterStage SetupIncompleteGroupFilter()
        {
            var dataTransform = new IncompleteGroupFilterTransform()
            {
                WorkingIndices = MemberData.FilteredAcquiringIndices,
                Input1 = Data.AcquiringIndices,
                Output = Data.MemberIndices,
            };

            var stage = new IncompleteGroupFilterStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        void WireIncompleteGroupFilterStage()
        {
            IncompleteGroupFilterStage.Transformation.Output = Data.MemberIndices;
        }

        internal FindMatchProposalsStage SetupMatchIntersection()
        {
            var dataTransform = new FindMatchProposalsTransform
            {
                WorkingIndices = MemberData.FilteredAcquiringIndices,
                Input1 = MemberData.PotentialMatchAcquiringIndices,
                Input2 = MemberData.ConditionRatings,
                Output = MemberData.ConditionMatchSets
            };

            var stage = new FindMatchProposalsStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        internal SetDataAvailabilityCheckStage SetupAvailabilityCheckStage()
        {
            var dataTransform = new CheckSetDataAvailabilityTransform()
            {
                WorkingIndices = Data.AcquiringIndices,
                Input1 = Data.MemberIndices,
                Input2 = m_Database.DataUsedByQueries,
                Input3 = m_Database.ReservedData,
                Input4 = m_Database.SharedDataUsersCounter,
                Input5 = MemberData.QueryMatchIds,
                Input6 = MemberData.Exclusivities,
                Output = MemberData.ConditionMatchSets
            };

            var stage = new SetDataAvailabilityCheckStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        void WireDataAvailabilityStage()
        {
            MemberDataAvailabilityStage.Transformation.Input1 = Data.MemberIndices;
        }

        internal TraitRequirementFilterStage SetupTraitFilterStage()
        {
            var dataTransform = new TraitRequirementFilterTransform()
            {
                WorkingIndices = MemberData.PotentialMatchAcquiringIndices,
                Input1 = m_Database.TypeToFilterAction,
                Input2 = MemberData.TraitRequirements,
                Output = MemberData.ConditionMatchSets,
            };

            var stage = new TraitRequirementFilterStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        internal MatchReductionStage SetupMatchReduction()
        {
            var dataTransform = new MatchReductionTransform
            {
                WorkingIndices = MemberData.PotentialMatchAcquiringIndices,
                Input1 = MemberData.ConditionRatings,
                Input2 = MemberData.ConditionMatchSets,
                Output = MemberData.ReducedConditionRatings
            };

            var stage = new MatchReductionStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        internal RelationRatingStage SetupRelationRatingStage()
        {
            var dataTransform = new RelationRatingTransform()
            {
                WorkingIndices = Data.FilteredAcquiringIndices,
                Input1 = Data.PotentialMatchAcquiringIndices,
                Input2 = Data.RelationIndexPairs,
                Input3 = Data.Relations,
                Input4 = Data.CachedTraits,
                Input5 = MemberData.ReducedConditionRatings,
                Output = Data.RelationRatings,
            };

            var stage = new RelationRatingStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        void WireRelationRatingStage()
        {
            SetRelationRatingStage.Transformation.Input2 = Data.RelationIndexPairs;
            SetRelationRatingStage.Transformation.Input3 = Data.Relations;
            SetRelationRatingStage.Transformation.Input4 = Data.CachedTraits;
            SetRelationRatingStage.Transformation.Output = Data.RelationRatings;
        }

        internal FilterRelationMemberMatchesStage SetupFilterRelationMembersStage()
        {
            var dataTransform = new FilterRelationMemberMatchesTransform()
            {
                WorkingIndices = Data.PotentialMatchAcquiringIndices,
                Input1 = Data.MemberIndices,
                Input2 = Data.RelationRatings,
                Input3 = MemberData.RelationMemberships,
                Output = MemberData.ReducedConditionRatings,
            };

            var stage = new FilterRelationMemberMatchesStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        void WireFilterRelationMembersStage()
        {
            FilterRelationMembersStage.Transformation.Input2 = Data.RelationRatings;
            FilterRelationMembersStage.Transformation.Input1 = Data.MemberIndices;
        }

        internal SetMatchSearchStage SetupMatchSearchStage()
        {
            var transform = new SetMatchSearchTransform()
            {
                SearchSpacePortionCurve = QueryPipelineConfiguration.instance.SearchSpacePortionCurve,
                SetMatchIds = Data.QueryMatchIds,
                PreviousMatches = m_Database.SetDataUsedByQueries,
                WorkingIndices = Data.PotentialMatchAcquiringIndices,
                Input1 = Data.DefiniteMatchAcquireIndices,
                Input2 = Data.MemberIndices,
                Input3 = Data.LocalRelationIndexPairs,
                Input4 = Data.RelationRatings,
                Input5 = Data.OrderWeights,
                Input6 = Data.SearchData,
                Input7 = MemberData.RelationMemberships,
                Input8 = MemberData.Exclusivities,
                Input9 = MemberData.ReducedConditionRatings,
                Output = MemberData.BestMatchDataIds
            };

            var stage = new SetMatchSearchStage(transform);
            Stages.Add(stage);
            return stage;
        }

        void WireMatchSearchStage()
        {
            MatchSearchStage.Transformation.SetMatchIds = Data.QueryMatchIds;
            MatchSearchStage.Transformation.Input3 = Data.LocalRelationIndexPairs;
            MatchSearchStage.Transformation.Input4 = Data.RelationRatings;
            MatchSearchStage.Transformation.Input5 = Data.OrderWeights;
            MatchSearchStage.Transformation.Input6 = Data.SearchData;
            MatchSearchStage.Transformation.Input2 = Data.MemberIndices;
        }

        internal MarkSetUsedStage SetupMarkUsedStage()
        {
            var markUsed = new MarkSetDataUsedTransform
            {
                Process = m_Database.MarkSetDataUsedAction,
                WorkingIndices = Data.DefiniteMatchAcquireIndices,
                Input1 = Data.MemberIndices,
                Input2 = Data.QueryMatchIds,
                Input3 = Data.UsedByMatch,
                Input4 = Data.SetMatchData,
                Input5 = MemberData.BestMatchDataIds,
                Input6 = MemberData.ObjectReferences,
                Output = MemberData.Exclusivities
            };

            var updateWorkingMembers = new SetWorkingMembersTransform()
            {
                WorkingIndices = Data.DefiniteMatchAcquireIndices,
                Input1 = Data.MemberIndices,
                Output = MemberData.DefiniteMatchAcquireIndices
            };

            var stage = new MarkSetUsedStage(markUsed, updateWorkingMembers);
            Stages.Add(stage);
            return stage;
        }

        void WireMarkUsedStage()
        {
            MarkDataUsedStage.Transformation1.Input1 = Data.MemberIndices;
            MarkDataUsedStage.Transformation1.Input2 = Data.QueryMatchIds;
            MarkDataUsedStage.Transformation1.Input3 = Data.UsedByMatch;
            MarkDataUsedStage.Transformation1.Input4 = Data.SetMatchData;
            MarkDataUsedStage.Transformation2.Input1 = Data.MemberIndices;
        }

        internal ResultFillStage SetupQueryResultFill()
        {
            var dataTransform = new ResultFillTransform
            {
                WorkingIndices = MemberData.DefiniteMatchAcquireIndices,
                Input1 = MemberData.BestMatchDataIds,
                Input2 = MemberData.Conditions,
                Input3 = MemberData.CachedTraits,
                Input4 = MemberData.TraitRequirements,
                Input5 = MemberData.QueryMatchIds,
                Output = MemberData.QueryResults,
                FillRequiredTraitsAction = m_Database.FillQueryResultRequirements
            };

            var stage = new ResultFillStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        internal SetResultFillStage SetupSetQueryResultFill()
        {
            var dataTransform = new SetResultFillTransform()
            {
                WorkingIndices = Data.DefiniteMatchAcquireIndices,
                Input1 = Data.MemberIndices,
                Input2 = Data.QueryMatchIds,
                Input3 = MemberData.QueryResults,
                Input4 = MemberData.ObjectReferences,
                Output = Data.QueryResults
            };

            var stage = new SetResultFillStage(dataTransform);
            Stages.Add(stage);
            return stage;
        }

        void WireSetQueryResultFillStage()
        {
            SetResultFillStage.Transformation.Input2 = Data.QueryMatchIds;
            SetResultFillStage.Transformation.Input1 = Data.MemberIndices;
            SetResultFillStage.Transformation.Output = Data.QueryResults;
        }

        SetAcquireHandlingStage SetupAcquireHandlingStage()
        {
            var dataTransform = new SetAcquireHandlingTransform()
            {
                WorkingIndices = Data.DefiniteMatchAcquireIndices,
                Input1 = Data.QueryResults,
                Output = Data.AcquireHandlers
            };

            var indicesTransform = new ManageIndicesTransform()
            {
                WorkingIndices = Data.DefiniteMatchAcquireIndices,
                Input1 = Data.UpdatingIndices,
                Output = Data.AcquiringIndices
            };

            var memberIndicesTransform = new ManageSetIndicesTransform()
            {
                WorkingIndices = Data.DefiniteMatchAcquireIndices,
                Input1 = Data.MemberIndices,
                Input2 = MemberData.UpdatingIndices,
                Output = MemberData.AcquiringIndices
            };

            var stage = new SetAcquireHandlingStage(dataTransform, indicesTransform, memberIndicesTransform);
            Stages.Add(stage);
            return stage;
        }

        void WireAcquireHandlingStage()
        {
            AcquireHandlingStage.Transformation1.Input1 = Data.QueryResults;
            AcquireHandlingStage.Transformation1.Output = Data.AcquireHandlers;
            AcquireHandlingStage.Transformation3.Input1 = Data.MemberIndices;
        }

        internal void Clear()
        {
            Data.Clear();
            MemberData.Clear();
        }

        internal bool ForceEvaluation()
        {
            Profiler.BeginSample("ForceSetEvaluation");
            CycleStart();

            MemberTraitCacheStage.Complete();
            RelationTraitCacheStage.Complete();
            MemberConditionRatingStage.Complete();
            IncompleteGroupFilterStage.Complete();
            MemberMatchIntersectionStage.Complete();
            MemberDataAvailabilityStage.Complete();
            MemberTraitRequirementStage.Complete();
            MemberMatchReductionStage.Complete();
            SetRelationRatingStage.Complete();
            FilterRelationMembersStage.Complete();
            MatchSearchStage.Complete();
            MarkDataUsedStage.Complete();
            MemberResultFillStage.Complete();
            SetResultFillStage.Complete();

            Profiler.EndSample();
            // acquire handling comes after the profiler label because we've already found the answer
            AcquireHandlingStage.Complete();

            onStageGroupCompletion?.Invoke();
            // Return whether we had any matches this iteration
            return Data.DefiniteMatchAcquireIndices.Count > 0;
        }

        public void OnUpdate()
        {
            var budgets = Configuration.SetFrameBudgets;
            if (budgets.Length == 0)
                return;

            if(budgets.Length != Stages.Count)
                Debug.LogErrorFormat("set frame budgets length {0}, but we have {1} stages",
                    budgets.Length, Stages.Count);

            var workingStage = Stages[m_LastCompletedFence];
            if (workingStage == null)
                return;

            var stageBudget = budgets[m_LastCompletedFence];
            workingStage.FrameBudget = stageBudget;

            // adjacent stages with a budget of 0 get run on the same frame as each other
            while (stageBudget == 0)
            {
                workingStage.Complete();

                onStageGroupCompletion?.Invoke();
                m_LastCompletedFence++;
                if (m_LastCompletedFence == Stages.Count)
                {
                    m_LastCompletedFence = 1;
                    CurrentlyActive = false;
                    return;
                }

                workingStage = Stages[m_LastCompletedFence];
                stageBudget = budgets[m_LastCompletedFence];
                workingStage.FrameBudget = stageBudget;
            }

            if (m_ElapsedFramesForCurrentStage >= stageBudget)
                workingStage.Complete();
            else
                workingStage.Tick();

            if (workingStage.IsComplete)
            {
                m_ElapsedFramesForCurrentStage = 0;
                onStageGroupCompletion?.Invoke();
                m_LastCompletedFence++;

                if (m_LastCompletedFence == Stages.Count)
                {
                    m_LastCompletedFence = 1;
                    CurrentlyActive = false;
                }
            }
            else
            {
                m_ElapsedFramesForCurrentStage++;
            }
        }
    }
}
