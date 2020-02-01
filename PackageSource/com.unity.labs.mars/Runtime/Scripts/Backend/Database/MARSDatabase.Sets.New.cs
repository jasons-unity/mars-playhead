using System.Collections.Generic;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    public partial class MARSDatabase
    {
        internal Dictionary<int, PreviousSetMatches> SetDataUsedByQueries { get; private set;} =
            new Dictionary<int, PreviousSetMatches>(12);

        internal Dictionary<QueryMatchID, HashSet<int>> SetDataUsedByQueryMatches { get; private set;} =
            new Dictionary<QueryMatchID,  HashSet<int>>(24);

        public void MarkSetDataUsedForUpdates(QueryMatchID queryMatchId, HashSet<int> data)
        {
            var setPipe = QueryPipelinesModule.instance.SetPipeline;
            if (!setPipe.Data.MatchIdToIndex.TryGetValue(queryMatchId, out var setIndex))
                return;

            if (SetDataUsedByQueryMatches.ContainsKey(queryMatchId))
            {
                Debug.LogErrorFormat(
                    "Query '{0}' is already using set data. If you wish to mark new data as used, " +
                    "first call UnmarkSetDataUsedForUpdates with this query ID.", queryMatchId);
                return;
            }

            SetDataUsedByQueryMatches[queryMatchId] = data;
            var queryId = queryMatchId.queryID;
            if (SetDataUsedByQueries.TryGetValue(queryId, out var dataUsedByQuery))
                dataUsedByQuery.Add(data);
            else
            {
                dataUsedByQuery = new PreviousSetMatches(data.Count);
                dataUsedByQuery.Add(data);
                SetDataUsedByQueries[queryId] = dataUsedByQuery;
            }

            var memberIndices = setPipe.Data.MemberIndices[setIndex];
            var memberData = setPipe.MemberData;
            foreach (var mi in memberIndices)
            {
                var assignedId = memberData.BestMatchDataIds[mi];
                var exclusivity = memberData.Exclusivities[mi];
                ReserveDataForQueryMatch(assignedId, queryMatchId, exclusivity);
            }
        }

        public void UnmarkSetDataUsedForUpdates(QueryMatchID queryMatchId)
        {
            if (!SetDataUsedByQueryMatches.TryGetValue(queryMatchId, out var dataUsedByQueryMatch))
                return;

            SetDataUsedByQueryMatches.Remove(queryMatchId);
            var queryId = queryMatchId.queryID;
            if (!SetDataUsedByQueries.TryGetValue(queryId, out var dataUsedByQuery))
                return;

            dataUsedByQuery.Remove(dataUsedByQueryMatch);
            if (dataUsedByQuery.Count == 0)
                SetDataUsedByQueries.Remove(queryId);

            foreach (var id in dataUsedByQueryMatch)
            {
                UnreserveDataForQueryMatch(id, queryMatchId);
            }

            dataUsedByQueryMatch.Clear();
        }

        public void UnmarkPartialSetDataUsedForUpdates(QueryMatchID queryMatchId, ICollection<IMRObject> childrenToUnmark)
        {
            if (!SetDataUsedByQueryMatches.TryGetValue(queryMatchId, out var dataUsedByQueryMatch))
                return;

            var setPipe = QueryPipelinesModule.instance.SetPipeline;
            if (!setPipe.Data.MatchIdToIndex.TryGetValue(queryMatchId, out var setIndex))
                return;

            var memberIndices = setPipe.Data.MemberIndices[setIndex];
            var memberData = setPipe.MemberData;
            foreach (var mi in memberIndices)
            {
                var objectRef = setPipe.MemberData.ObjectReferences[mi];
                if (!childrenToUnmark.Contains(objectRef))
                    continue;

                var assignedId = memberData.BestMatchDataIds[mi];
                UnreserveDataForQueryMatch(assignedId, queryMatchId);
                dataUsedByQueryMatch.Remove(assignedId);
            }

            var setMatchData = setPipe.Data.SetMatchData[setIndex];
            foreach (var child in childrenToUnmark)
            {
                setMatchData.dataAssignments.Remove(child);
            }
        }

        public bool IsSetQueryDataDirty(QueryMatchID queryMatchId)
        {
            if (!SetDataUsedByQueryMatches.TryGetValue(queryMatchId, out var setData))
                return false;

            foreach (var id in setData)
            {
                if (QueryDataDirty(id, queryMatchId))
                    return true;
            }

            return false;
        }

        internal void MarkSetDataUsedAction(List<int> workingSetIndices, int[][] allMemberIndices,
            QueryMatchID[] setMatchIds,
            HashSet<int>[] allSetDataUsed,
            SetMatchData[] allSetMatchData,
            List<int> allMemberDataIds,
            List<IMRObject> allMemberObjects,
            ref List<Exclusivity> allMemberExclusivities)
        {
            foreach (var i in workingSetIndices)
            {
                var usedSet = allSetDataUsed[i];
                usedSet.Clear();

                var matchData = allSetMatchData[i];
                foreach (var mi in allMemberIndices[i])
                {
                    var id = allMemberDataIds[mi];
                    usedSet.Add(id);
                    matchData.dataAssignments.Add(allMemberObjects[mi], id);
                }

                MarkSetDataUsedForUpdates(setMatchIds[i], usedSet);
            }
        }
    }
}
