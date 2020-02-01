using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ModuleBehaviorCallbackOrder(ModuleOrders.BackendBehaviorOrder)]
    [ModuleUnloadOrder(ModuleOrders.BackendUnloadOrder)]
    [ModuleOrder(ModuleOrders.BackendLoadOrder)]
    public class MARSQueryBackend : ScriptableSettings<MARSQueryBackend>, IModuleBehaviorCallbacks,
        IModuleMarsUpdate, IModuleSceneCallbacks, IModuleDependency<MARSDatabase>,
        IModuleDependency<ReasoningModule>, IModuleDependency<QueryPipelinesModule>, IUsesDatabaseQuerying
    {
        static readonly List<int> k_UpdateCheckKeys = new List<int>();

        ReasoningModule m_ReasoningModule;
        MARSDatabase m_Database;

        internal StandaloneQueryPipeline Pipeline;     // internal for tests

        readonly List<QueryMatchID> m_QueryRemovalList = new List<QueryMatchID>();

        readonly Dictionary<QueryMatchID, QueryArgs> m_QueryAddList =
            new Dictionary<QueryMatchID, QueryArgs>();

        readonly Dictionary<QueryMatchID, SetQueryArgs> m_SetQueryPipelineAddList =
            new Dictionary<QueryMatchID, SetQueryArgs>();

        readonly List<QueryMatchID> m_SetQueryRemovalList = new List<QueryMatchID>();
        readonly Dictionary<int, Queue<int>> m_ReacquireQueues = new Dictionary<int, Queue<int>>();
        readonly Dictionary<int, float> m_LastUpdateCheckTimes = new Dictionary<int, float>();

        readonly List<QueryMatchID> m_QueryTimeoutList = new List<QueryMatchID>();
        readonly List<QueryMatchID> m_SetTimeoutList = new List<QueryMatchID>();

        QueryPipelinesModule m_PipelinesModule;

#pragma warning disable 67
        public event Action<QueryMatchID, int> onQueryMatchFound;
        public event Action<QueryMatchID, Dictionary<int, float>> onQueryMatchesFound;
        public event Action<QueryMatchID, Dictionary<IMRObject, int>> onSetQueryMatchFound;
#pragma warning restore 67

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly HashSet<int> k_NeedsReAcquireSet = new HashSet<int>();
        static readonly HashSet<int> k_SetsThatNeedsReAcquire = new HashSet<int>();

        public void ConnectDependency(QueryPipelinesModule dependency)
        {
            m_PipelinesModule = dependency;
        }

        public void ConnectDependency(MARSDatabase dependency)
        {
            m_Database = dependency;
        }

        public void LoadModule()
        {
            SetupQueryManagement();
            IUsesQueryResultsMethods.RegisterQuery = RegisterQuery;
            IUsesDevQueryResultsMethods.RegisterOverrideQuery = RegisterQuery;
            IUsesQueryResultsMethods.UnregisterQuery = UnregisterQuery;
            ISetQueryResultsMethods.RegisterSetQuery = RegisterSetQuery;
            ISetQueryResultsMethods.RegisterSetOverrideQuery = RegisterSetQuery;
            ISetQueryResultsMethods.UnregisterSetQuery = UnregisterSetQuery;
        }

        public void UnloadModule()
        {
            TearDownQueryManagement();
            Pipeline = null;
            IUsesQueryResultsMethods.RegisterQuery = null;
            IUsesQueryResultsMethods.UnregisterQuery = UnregisterQueryNoop;
            IUsesDevQueryResultsMethods.RegisterOverrideQuery = null;
            ISetQueryResultsMethods.RegisterSetQuery = null;
            ISetQueryResultsMethods.RegisterSetOverrideQuery = null;
            ISetQueryResultsMethods.UnregisterSetQuery = UnregisterQueryNoop;
        }

        public void ConnectDependency(ReasoningModule dependency)
        {
            m_ReasoningModule = dependency;
        }

        void SetupQueryManagement()
        {
            Pipeline = m_PipelinesModule.StandalonePipeline;
            Pipeline.onTimeout += OnStandaloneQueryTimeouts;
            m_PipelinesModule.SetPipeline.OnTimeout += OnSetTimeouts;

            QueryMatchID.ResetCounter();
        }

        internal void ResetQueryManagement()
        {
            SetupQueryManagement();
            TearDownQueryManagement();
        }

        void TearDownQueryManagement()
        {
            if (Pipeline != null)
            {
                Pipeline.onTimeout -= OnStandaloneQueryTimeouts;
            }

            m_PipelinesModule.SetPipeline.OnTimeout -= OnSetTimeouts;
            m_PipelinesModule.Clear();

            m_QueryRemovalList.Clear();
            m_QueryAddList.Clear();
            m_SetQueryRemovalList.Clear();
            m_SetQueryPipelineAddList.Clear();
            m_ReacquireQueues.Clear();

            m_Database.StopUpdateBuffering();
        }

        internal QueryMatchID RegisterQuery(QueryArgs queryArgs)
        {
            var queryMatchID = QueryMatchID.Generate();
            RegisterQuery(queryMatchID, queryArgs);
            return queryMatchID;
        }

        void RegisterQuery(QueryMatchID queryMatchID, QueryArgs queryArgs)
        {
            AddQuery(queryMatchID, queryArgs);
        }

        internal bool UnregisterQuery(QueryMatchID queryMatchID, bool allMatches)
        {
            int index;
            if(!Pipeline.Data.matchIdToIndex.TryGetValue(queryMatchID, out index))
                return false;

            var matchFound = Pipeline.Data.updatingIndices.Contains(index);
            if (allMatches)
            {
                foreach (var activeQueryID in Pipeline.Data.queryMatchIds)
                {
                    if (!activeQueryID.SameQuery(queryMatchID))
                        continue;

                    if (matchFound)
                        this.UnmarkDataUsedForUpdates(activeQueryID);

                    RemoveQuery(activeQueryID);
                }
            }
            else
            {
                if (matchFound)
                    this.UnmarkDataUsedForUpdates(queryMatchID);

                RemoveQuery(queryMatchID);
            }

            return matchFound;
        }

        internal bool UnregisterSetQuery(QueryMatchID queryMatchID, bool allMatches)
        {
            var setPipeline = m_PipelinesModule.SetPipeline;
            if(!setPipeline.Data.MatchIdToIndex.TryGetValue(queryMatchID, out var index))
                return false;

            var matchFound = Pipeline.Data.updatingIndices.Contains(index);
            if (allMatches)
            {
                foreach (var activeQueryID in setPipeline.Data.QueryMatchIds)
                {
                    if (!activeQueryID.SameQuery(queryMatchID))
                        continue;

                    if (matchFound)
                        this.UnmarkSetDataUsedForUpdates(activeQueryID);

                    RemoveQuery(activeQueryID);
                }
            }
            else
            {
                if (matchFound)
                    this.UnmarkSetDataUsedForUpdates(queryMatchID);

                RemoveQuery(queryMatchID);
            }

            return matchFound;
        }

        void AddQuery(QueryMatchID queryMatchID, QueryArgs args)
        {
            m_QueryAddList.Add(queryMatchID, args);
        }

        void RemoveQuery(QueryMatchID queryMatchID)
        {
            m_QueryRemovalList.Add(queryMatchID);
        }

        public QueryMatchID RegisterSetQuery(SetQueryArgs queryArgs)
        {
            var queryMatchID = QueryMatchID.Generate();
            m_SetQueryPipelineAddList.Add(queryMatchID, queryArgs);
            return queryMatchID;
        }

        void RegisterSetQuery(QueryMatchID queryMatchID, SetQueryArgs queryArgs)
        {
            m_SetQueryPipelineAddList.Add(queryMatchID, queryArgs);
        }

        // This dummy function is used to prevent errors from happening if a query tries to unregister after unload
        static bool UnregisterQueryNoop(QueryMatchID queryMatchID, bool allMatches) { return false; }

        void RemoveSetQuery(QueryMatchID queryMatchID)
        {
            m_SetQueryRemovalList.Add(queryMatchID);
        }

        void OnStandaloneQueryTimeouts(HashSet<QueryMatchID> timeoutMatchIds)
        {
            foreach (var id in timeoutMatchIds)
            {
                m_QueryTimeoutList.Add(id);
            }
        }

        void OnSetTimeouts(HashSet<QueryMatchID> timeoutMatchIds)
        {
            foreach (var id in timeoutMatchIds)
            {
                m_SetTimeoutList.Add(id);
            }
        }

        public void OnBehaviorAwake() {}

        public void OnBehaviorEnable()
        {
            SetAllLastUpdateTimes(MarsTime.Time);
        }

        public void OnBehaviorStart() {}

        public void OnBehaviorUpdate() { }

        public void OnBehaviorDisable() { }

        public void OnBehaviorDestroy() {}

        public void OnMarsUpdate()
        {
            if (MARSCore.instance.paused)
                return;

            SyncQueryBuffers();
        }

        void ClearRemovedQueries()
        {
            foreach (var matchId in m_QueryTimeoutList)
            {
                Pipeline.Data.RemoveAndTimeout(matchId);
            }

            foreach (var matchId in m_QueryRemovalList)
            {
                Pipeline.Data.Remove(matchId);
            }

            m_QueryTimeoutList.Clear();
            m_QueryRemovalList.Clear();

            foreach (var matchId in m_SetTimeoutList)
            {
                m_PipelinesModule.SetPipeline.RemoveAndTimeout(matchId);
            }

            foreach (var matchId in m_SetQueryRemovalList)
            {
                m_PipelinesModule.SetPipeline.Remove(matchId);
            }

            m_SetTimeoutList.Clear();
            m_SetQueryRemovalList.Clear();
        }

        void AddPendingQueries()
        {
            foreach (var kvp in m_QueryAddList)
            {
                // add the pending query to the active query data
                Pipeline.Data.Register(kvp.Key, kvp.Value);
            }

            m_QueryAddList.Clear();

            foreach (var kvp in m_SetQueryPipelineAddList)
            {
                m_PipelinesModule.SetPipeline.Register(kvp.Key, kvp.Value);
            }

            m_SetQueryPipelineAddList.Clear();
        }

        internal void SyncQueryBuffers()
        {
            ClearRemovedQueries();
            AddPendingQueries();
        }

        internal bool RunAllQueries()
        {
            SyncQueryBuffers();
            // make sure we get all the traits we require from reasoning APIs first
            m_ReasoningModule.UpdateReasoningAPIData();
            m_ReasoningModule.ProcessReasoningAPIScenes();

            bool anyNewSetMatches;
            var setPipe = m_PipelinesModule.SetPipeline;
            anyNewSetMatches = setPipe.ForceEvaluation();

            // force-run all queries in the standalone pipeline
            var pipe = m_PipelinesModule.StandalonePipeline;
            var anyNewMatches = pipe.ForceEvaluation();
            if (anyNewMatches)
            {
                if (onQueryMatchFound != null)
                {
                    var answeredIndices = pipe.Data.definiteMatchAcquireIndices;
                    var queryMatchIds = pipe.Data.queryMatchIds;
                    var dataMatchIds = pipe.Data.bestMatchDataIds;
                    foreach (var i in answeredIndices)
                    {
                        onQueryMatchFound(queryMatchIds[i], dataMatchIds[i]);
                    }
                }
                if (onQueryMatchesFound != null)
                {
                    var answeredIndices = pipe.Data.definiteMatchAcquireIndices;
                    var queryMatchIds = pipe.Data.queryMatchIds;
                    var reducedRatings = pipe.Data.reducedConditionRatings;
                    foreach (var i in answeredIndices)
                    {
                        onQueryMatchesFound(queryMatchIds[i], reducedRatings[i]);
                    }
                }
            }

            return anyNewMatches || anyNewSetMatches;
        }

        internal void RunMatchUpdates(ParallelQueryData data)
        {
            k_NeedsReAcquireSet.Clear();

            var time = MarsTime.Time;
            foreach (var i in data.updatingIndices)
            {
                if (m_LastUpdateCheckTimes.TryGetValue(i, out var lastTime))
                {
                    var interval = data.updateMatchInterval[i];
                    if (time - lastTime < interval)
                        continue;
                }

                PipelineMatchUpdate(i,
                    data.queryMatchIds,
                    data.bestMatchDataIds,
                    data.conditions,
                    data.traitRequirements,
                    data.queryResults,
                    data.reAcquireOnLoss,
                    data.updateHandlers,
                    data.lossHandlers);

                m_LastUpdateCheckTimes[i] = time;
            }

            SyncNeedsAcquireSet();
        }

        void SetAllLastUpdateTimes(float time)
        {
            k_UpdateCheckKeys.Clear();
            foreach (var key in m_LastUpdateCheckTimes.Keys)
            {
                k_UpdateCheckKeys.Add(key);
            }

            foreach (var key in k_UpdateCheckKeys)
            {
                m_LastUpdateCheckTimes[key] = time;
            }

            var setData = m_PipelinesModule.SetPipeline.Data;
            var lastSetUpdates = setData.LastUpdateCheckTime;
            foreach (var setIndex in setData.UpdatingIndices)
            {
                lastSetUpdates[setIndex] = time;
            }
        }

        internal void RunSetMatchUpdates(ParallelSetData setData)
        {
            var time = MarsTime.Time;
            foreach (var i in setData.UpdatingIndices)
            {
                var lastTime = setData.LastUpdateCheckTime[i];
                var interval = setData.UpdateMatchInterval[i];
                if (time - lastTime < interval)
                    continue;

                SetPipelineMatchUpdate(i,
                    setData.QueryMatchIds,
                    setData.SetMatchData,
                    setData.Relations,
                    setData.QueryResults,
                    setData.ReAcquireOnLoss,
                    setData.LastUpdateCheckTime,
                    setData.SetMatchData,
                    setData.UpdateHandlers,
                    setData.LossHandlers);

                setData.LastUpdateCheckTime[i] = time;
            }
        }

        internal void PipelineMatchUpdate(int index,
            List<QueryMatchID> queryMatchIds,
            List<int> dataIds,
            List<Conditions> conditions,
            List<ContextTraitRequirements> traitRequirements,
            List<QueryResult> results,
            List<bool> reAcquireSettings,
            List<Action<QueryResult>> onMatchUpdateHandlers,
            List<Action<QueryResult>> onMatchLossHandlers)
        {
            var queryMatchId = queryMatchIds[index];
            // Check if any of the object's queried data is dirty.  If so, try to get the updated data
            if (this.QueryDataDirty(queryMatchId))
            {
                var result = results[index];
                var dataId = dataIds[index];
                var queryConditions = conditions[index];
                result.queryMatchId = queryMatchId;

                if (this.TryUpdateQueryMatchData(dataId, queryConditions, traitRequirements[index], result))
                {
                    var onMatchUpdate = onMatchUpdateHandlers[index];
                    if (onMatchUpdate != null)
                        onMatchUpdate(result);
                }
                else
                {
                    // If looking for all matches for a query, we should not try to reacquire for this query match
                    // since we are already looking for another match.
                    if (!reAcquireSettings[index])
                        RemoveQuery(queryMatchId);
                    else
                    {
                        m_LastUpdateCheckTimes.Remove(index);
                        k_NeedsReAcquireSet.Add(index);
                    }

                    this.UnmarkDataUsedForUpdates(queryMatchId);
                    var onLoss = onMatchLossHandlers[index];
                    if (onLoss!= null)
                        onLoss(result);
                }
            }

            m_LastUpdateCheckTimes[index] = MarsTime.Time;
        }

        internal void SetPipelineMatchUpdate(int index,
            QueryMatchID[] queryMatchIds,
            SetMatchData[] matchData,
            Relations[] relations,
            SetQueryResult[] results,
            bool[] reAcquireSettings,
            float[] lastUpdateCheckTimes,
            SetMatchData[] allSetMatchData,
            Action<SetQueryResult>[] onMatchUpdateHandlers,
            Action<SetQueryResult>[] onMatchLossHandlers)
        {
            var queryMatchId = queryMatchIds[index];
            // Check if any of the object's queried data is dirty.  If so, try to get the updated data
            if (this.IsSetQueryDataDirty(queryMatchId))
            {
                var result = results[index];
                result.queryMatchId = queryMatchId;

                if (this.TryUpdateSetQueryMatchData(matchData[index], relations[index], results[index]))
                {
                    // If there are any non-required children that have been lost, we should free up their data.
                    var nonRequiredChildrenLost = result.nonRequiredChildrenLost;
                    if (nonRequiredChildrenLost.Count > 0)
                        this.UnmarkPartialSetDataUsedForUpdates(queryMatchId, nonRequiredChildrenLost);

                    onMatchUpdateHandlers[index]?.Invoke(result);
                }
                else
                {
                    // If looking for all matches for a query, we should not try to reacquire for this query match
                    // since we are already looking for another match.
                    if (!reAcquireSettings[index])
                        RemoveSetQuery(queryMatchId);
                    else
                    {
                        result.RestoreResults();
                        k_SetsThatNeedsReAcquire.Add(index);
                    }

                    this.UnmarkSetDataUsedForUpdates(queryMatchId);

                    var setMatchData = allSetMatchData[index];
                    setMatchData?.dataAssignments.Clear();

                    onMatchLossHandlers[index]?.Invoke(result);
                }
            }

            lastUpdateCheckTimes[index] = MarsTime.Time;
        }

        internal void SyncNeedsAcquireSet()
        {
            var pipelineData = Pipeline.Data;
            foreach (var index in k_NeedsReAcquireSet)
            {
                pipelineData.updatingIndices.Remove(index);

                // If the query is reserved then it's OK to look for multiple matches at a time,
                // since the pipeline handles reserved data conflicts
                if (pipelineData.exclusivities[index] == Exclusivity.Reserved)
                {
                    pipelineData.acquiringIndices.Add(index);
                    continue;
                }

                // The pipeline does not support finding multiple matches at a time for a shared or read-only query.
                // If there is already an acquiring query with a certain query ID, we queue up queries with that same ID
                // that want to reacquire.
                var matchIDs = pipelineData.queryMatchIds;
                var reacquireQueryId = matchIDs[index].queryID;
                if (m_ReacquireQueues.TryGetValue(reacquireQueryId, out var reacquireQueue))
                {
                    reacquireQueue.Enqueue(index);
                    continue;
                }

                var canReacquire = true;
                var acquiringIndices = pipelineData.acquiringIndices;
                foreach (var acquiring in acquiringIndices)
                {
                    if (matchIDs[acquiring].queryID == reacquireQueryId)
                    {
                        canReacquire = false;
                        reacquireQueue = new Queue<int>();
                        reacquireQueue.Enqueue(index);
                        m_ReacquireQueues[reacquireQueryId] = reacquireQueue;
                        pipelineData.acquireHandlers[acquiring] += OnBlockingQueryAcquired;
                        break;
                    }
                }

                if (canReacquire)
                    acquiringIndices.Add(index);
            }

            var setData = m_PipelinesModule.SetPipeline.Data;
            foreach (var index in k_SetsThatNeedsReAcquire)
            {
                setData.UpdatingIndices.Remove(index);

                var matchIDs = setData.QueryMatchIds;
                var reacquireQueryId = matchIDs[index].queryID;
                if (m_ReacquireQueues.TryGetValue(reacquireQueryId, out var reacquireQueue))
                {
                    reacquireQueue.Enqueue(index);
                    continue;
                }

                var canReacquire = true;
                var acquiringIndices = setData.AcquiringIndices;
                foreach (var acquiring in acquiringIndices)
                {
                    if (matchIDs[acquiring].queryID == reacquireQueryId)
                    {
                        canReacquire = false;
                        reacquireQueue = new Queue<int>();
                        reacquireQueue.Enqueue(index);
                        m_ReacquireQueues[reacquireQueryId] = reacquireQueue;
                        setData.AcquireHandlers[acquiring] += OnBlockingSetQueryAcquired;
                        break;
                    }
                }

                if (canReacquire)
                    acquiringIndices.Add(index);
            }

            k_SetsThatNeedsReAcquire.Clear();
        }

        void OnBlockingQueryAcquired(QueryResult queryData)
        {
            var pipelineData = Pipeline.Data;
            var blockingQueryMatchID = queryData.queryMatchId;
            var blockingQueryIndex = pipelineData.matchIdToIndex[blockingQueryMatchID];
            pipelineData.acquireHandlers[blockingQueryIndex] -= OnBlockingQueryAcquired;

            var queryID = blockingQueryMatchID.queryID;
            var reacquireQueue = m_ReacquireQueues[queryID];
            var reacquireIndex = reacquireQueue.Dequeue();
            Pipeline.Data.acquiringIndices.Add(reacquireIndex);

            // If there are still more queries that want to reacquire, then we need to know when this query has acquired a match
            if (reacquireQueue.Count > 0)
                pipelineData.acquireHandlers[reacquireIndex] += OnBlockingQueryAcquired;
            else
                m_ReacquireQueues.Remove(queryID);
        }

        void OnBlockingSetQueryAcquired(SetQueryResult queryData)
        {
            var setData = m_PipelinesModule.SetPipeline.Data;
            var blockingQueryMatchId = queryData.queryMatchId;
            var blockingQueryIndex = setData.MatchIdToIndex[blockingQueryMatchId];
            setData.AcquireHandlers[blockingQueryIndex] -= OnBlockingSetQueryAcquired;

            var queryId = blockingQueryMatchId.queryID;
            var reacquireQueue = m_ReacquireQueues[queryId];
            var reacquireIndex = reacquireQueue.Dequeue();
            setData.AcquiringIndices.Add(reacquireIndex);

            // If there are still more queries that want to reacquire, then we need to know when this query has acquired a match
            if (reacquireQueue.Count > 0)
                setData.AcquireHandlers[reacquireIndex] += OnBlockingSetQueryAcquired;
            else
                m_ReacquireQueues.Remove(queryId);
        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetAllLastUpdateTimes(MarsTime.Time);
        }

        public void OnSceneUnloaded(Scene scene) { }

        public void OnActiveSceneChanged(Scene oldScene, Scene newScene) { }

#if UNITY_EDITOR
        public void OnNewSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode) { }

        public void OnSceneOpening(string path, OpenSceneMode mode) { }

        public void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            SetAllLastUpdateTimes(0f);
        }
#endif
    }
}
