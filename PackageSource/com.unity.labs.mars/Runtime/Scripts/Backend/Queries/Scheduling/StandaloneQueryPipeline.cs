using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Labs.MARS.Query
{
    internal class StandaloneQueryPipeline : QueryPipelineBase
    {
        internal QueryStage[] Stages;

        int m_CurrentRunGroupIndex;
        int m_LastCompletedFence;

        internal CacheTraitReferencesStage CacheTraitReferencesStage;
        internal ConditionMatchRatingStage ConditionRatingStage;
        internal FindMatchProposalsStage FindMatchProposalsStage;
        internal DataAvailabilityCheckStage DataAvailabilityStage;
        internal TraitRequirementFilterStage TraitFilterStage;
        internal MatchReductionStage MatchReductionStage;
        internal FindBestStandaloneMatchStage BestStandaloneMatchStage;
        internal ResultFillStage ResultFillStage;
        internal MarkUsedStage MarkUsedStage;
        internal AcquireHandlingStage AcquireHandlingStage;

        /// <summary>
        /// Stores all data that exists for each standalone context / query
        /// </summary>
        internal ParallelQueryData Data;

        internal event Action<HashSet<QueryMatchID>> onTimeout;
        internal event Action onStageGroupCompletion;

        /// <summary>
        /// The active run configuration for the pipeline
        /// </summary>
        public QueryPipelineConfiguration configuration;

        public StandaloneQueryPipeline(MARSDatabase db)
        {
            m_Database = db;
            SetupData();
            SetupStageArray();
        }

        void SetupStageArray()
        {
            Stages = new QueryStage[]
            {
                // the null stage represents the idle part of the cycle
                null,
                CacheTraitReferencesStage,
                ConditionRatingStage,
                FindMatchProposalsStage,
                DataAvailabilityStage,
                TraitFilterStage,
                MatchReductionStage,
                BestStandaloneMatchStage,
                ResultFillStage,
                MarkUsedStage,
                AcquireHandlingStage
            };
        }

        internal void SetupData()
        {
            configuration = QueryPipelineConfiguration.instance;
            Data = new ParallelQueryData(MARSMemoryOptions.instance.QueryDataCapacity);

            CacheTraitReferencesStage = SetupTraitCacheStage();
            ConditionRatingStage = SetupMatchRating();
            FindMatchProposalsStage = SetupMatchIntersection();
            DataAvailabilityStage = SetupAvailabilityCheckStage();
            TraitFilterStage = SetupTraitFilterStage();
            MatchReductionStage = SetupMatchReduction();
            BestStandaloneMatchStage = SetupBestStandaloneMatchStage();
            ResultFillStage = SetupQueryResultFill();
            MarkUsedStage = SetupMarkUsedStage();
            AcquireHandlingStage = SetupAcquireHandlingStage();
        }

        internal void ClearData()
        {
            Data.Clear();
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

        void CycleStart()
        {
            Data.ClearCycleIndices();
            BestStandaloneMatchStage.CycleStart();
            CacheTraitReferencesStage.CycleStart();
            ConditionRatingStage.CycleStart();
            FindMatchProposalsStage.CycleStart();
            DataAvailabilityStage.CycleStart();
            TraitFilterStage.CycleStart();
            MatchReductionStage.CycleStart();
            BestStandaloneMatchStage.CycleStart();
            ResultFillStage.CycleStart();
            MarkUsedStage.CycleStart();
            AcquireHandlingStage.CycleStart();
        }

        internal bool ForceEvaluation()
        {
            Profiler.BeginSample("ForceEvaluation");

            CycleStart();
            AcquireHandlingStage.OnCycleStart();
            BestStandaloneMatchStage.CycleStart();

            CacheTraitReferencesStage.Complete();
            ConditionRatingStage.Complete();

            FindMatchProposalsStage.Complete();
            DataAvailabilityStage.Complete();
            TraitFilterStage.Complete();
            MatchReductionStage.Complete();
            BestStandaloneMatchStage.Complete();

            ResultFillStage.Transformation.applyOffsetToPose = m_Database.ApplyOffsetToPose;
            ResultFillStage.Complete();
            MarkUsedStage.Complete();

            Profiler.EndSample();
            // acquire handling usually takes longer than any other stage by far and we already have the answers
            AcquireHandlingStage.Complete();

            if(onStageGroupCompletion != null)
                onStageGroupCompletion();

            CurrentlyActive = false;
            // Return whether we had any matches this iteration
            return Data.definiteMatchAcquireIndices.Count > 0;
        }

        internal CacheTraitReferencesStage SetupTraitCacheStage()
        {
            var dataTransform = new TraitRefTransform(m_Database.CacheTraitReferences)
            {
                WorkingIndices = Data.acquiringIndices,
                Input1 = Data.filteredAcquiringIndices,
                Input2 = Data.conditions,
                Output =  Data.cachedTraits
            };

            return new CacheTraitReferencesStage(dataTransform);
        }

        internal ConditionMatchRatingStage SetupMatchRating()
        {
            var dataTransform = new MatchRatingDataTransform
            {
                WorkingIndices = Data.filteredAcquiringIndices,
                Input1 = Data.conditions,
                Input2 = Data.cachedTraits,
                Output = Data.conditionRatings
            };

            return new ConditionMatchRatingStage(dataTransform);
        }

        internal FindMatchProposalsStage SetupMatchIntersection()
        {
            var dataTransform = new FindMatchProposalsTransform
            {
                WorkingIndices = Data.filteredAcquiringIndices,
                Input1 = Data.potentialMatchAcquiringIndices,
                Input2 = Data.conditionRatings,
                Output = Data.conditionMatchSets
            };

            return new FindMatchProposalsStage(dataTransform);
        }

        internal DataAvailabilityCheckStage SetupAvailabilityCheckStage()
        {
            var dataTransform = new DataAvailabilityTransform()
            {
                WorkingIndices = Data.potentialMatchAcquiringIndices,
                Input1 = m_Database.DataUsedByQueries,
                Input2 = m_Database.ReservedData,
                Input3 = m_Database.SharedDataUsersCounter,
                Input4 = Data.queryMatchIds,
                Input5 = Data.exclusivities,
                Output = Data.conditionMatchSets
            };

            return new DataAvailabilityCheckStage(dataTransform);
        }

        internal TraitRequirementFilterStage SetupTraitFilterStage()
        {
            var dataTransform = new TraitRequirementFilterTransform()
            {
                WorkingIndices = Data.potentialMatchAcquiringIndices,
                Input1 = m_Database.TypeToFilterAction,
                Input2 = Data.traitRequirements,
                Output = Data.conditionMatchSets,
            };

            return new TraitRequirementFilterStage(dataTransform);
        }

        internal MatchReductionStage SetupMatchReduction()
        {
            var dataTransform = new MatchReductionTransform
            {
                WorkingIndices = Data.potentialMatchAcquiringIndices,
                Input1 = Data.conditionRatings,
                Input2 = Data.conditionMatchSets,
                Output = Data.reducedConditionRatings
            };

            return new MatchReductionStage(dataTransform);
        }

        internal FindBestStandaloneMatchStage SetupBestStandaloneMatchStage()
        {
            var dataIdCollection = Data.bestMatchDataIds;
            // assign all match Ids to an invalid value when we initialize the pipeline,
            // so that we do not get queries matching against ID 0 by mistake.
            for (var i = 0; i < dataIdCollection.Count; i++)
            {
                dataIdCollection[i] = (int)ReservedDataIDs.Invalid;
            }

            var transformation = new FindBestMatchTransform(MARSMemoryOptions.instance.QueryDataCapacity)
            {
                WorkingIndices = Data.potentialMatchAcquiringIndices,
                Input1 = Data.definiteMatchAcquireIndices,
                Input2 = Data.reducedConditionRatings,
                Input3 = Data.exclusivities,
                Output = Data.bestMatchDataIds
            };

            return new FindBestStandaloneMatchStage(transformation);
        }

        internal ResultFillStage SetupQueryResultFill()
        {
            var dataTransform = new ResultFillTransform
            {
                WorkingIndices = Data.definiteMatchAcquireIndices,
                Input1 = Data.bestMatchDataIds,
                Input2 = Data.conditions,
                Input3 = Data.cachedTraits,
                Input4 = Data.traitRequirements,
                Input5 = Data.queryMatchIds,
                Output = Data.queryResults,
                FillRequiredTraitsAction = m_Database.FillQueryResultRequirements
            };

            return new ResultFillStage(dataTransform);
        }

        internal MarkUsedStage SetupMarkUsedStage()
        {
            var transform = new MarkDataUsedTransform
            {
                Process = m_Database.MarkDataUsedAction,
                WorkingIndices = Data.definiteMatchAcquireIndices,
                Input1 = Data.bestMatchDataIds,
                Input2 = Data.queryMatchIds,
                Output = Data.exclusivities
            };

            return new MarkUsedStage(transform);
        }

        internal AcquireHandlingStage SetupAcquireHandlingStage()
        {
            var handlerTransform = new AcquireHandlingTransform()
            {
                WorkingIndices = Data.definiteMatchAcquireIndices,
                Input1 = Data.queryResults,
                Output = Data.acquireHandlers
            };

            var indicesTransform = new ManageIndicesTransform()
            {
                WorkingIndices = Data.definiteMatchAcquireIndices,
                Input1 = Data.updatingIndices,
                Output = Data.acquiringIndices
            };

            return new AcquireHandlingStage(handlerTransform, indicesTransform);
        }

        // handling timeouts is documented as query stage just like the rest, but since it
        // removes query matches, it is handled specially.
        internal void HandleTimeouts()
        {
            CheckTimeouts(Data.acquiringIndices, Data.timeOuts, CycleDeltaTime, k_TimeoutIndexes);
            foreach (var index in k_TimeoutIndexes)
            {
                k_TimeoutIds.Add(Data.queryMatchIds[index]);
            }

            onTimeout?.Invoke(k_TimeoutIds);
            k_TimeoutIds.Clear();
        }

        internal static void CheckTimeouts(List<int> workingIndexes, List<float> timeoutsRemaining, float deltaTime,
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

        public void OnUpdate()
        {
            var budgets = configuration.FrameBudgets;
            if (budgets.Length == 0)
                return;

            if(budgets.Length != Stages.Length)
                Debug.LogErrorFormat("standalone frame budgets length {0}, but we have {1} stages",
                    budgets.Length, Stages.Length);

            var workingStage = Stages[m_LastCompletedFence];
            var stageBudget = budgets[m_LastCompletedFence];

            if (workingStage == null)
            {
                m_LastCompletedFence++;
                workingStage = Stages[m_LastCompletedFence];
                stageBudget = QueryPipelineConfiguration.instance.FrameBudgets[m_LastCompletedFence];
            }

            workingStage.FrameBudget = stageBudget;
            while (stageBudget == 0)
            {
                workingStage.Complete();
                onStageGroupCompletion?.Invoke();
                m_LastCompletedFence++;
                if (m_LastCompletedFence == Stages.Length)
                {
                    m_LastCompletedFence = 1;
                    CurrentlyActive = false;
                    return;
                }

                workingStage = Stages[m_LastCompletedFence];
                stageBudget = QueryPipelineConfiguration.instance.FrameBudgets[m_LastCompletedFence];
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
                // if we've just finished the last stage, mark the pipeline inactive and reset state
                if (m_LastCompletedFence == Stages.Length)
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
