using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS.Query
{
    partial class SetResultFillTransform : DataTransform<
        int[][],
        QueryMatchID[],
        List<QueryResult>,
        List<IMRObject>,
        SetQueryResult[]>
    {
        public SetResultFillTransform()
        {
            Process = ProcessStage;
        }

        internal void ProcessStage(List<int> indices,
            int[][] allMemberIndices,
            QueryMatchID[] matchIds,
            List<QueryResult> allMemberResults,
            List<IMRObject> allMemberObjects,
            ref SetQueryResult[] results)
        {
            foreach (var i in indices)
            {
                var memberIndices = allMemberIndices[i];
                var result = results[i];

                result.childResults.Clear();
                foreach (var mi in memberIndices)
                {
                    var objectRef = allMemberObjects[mi];
                    result.childResults.Add(objectRef, allMemberResults[mi]);
                }
            }
        }
    }

    class SetResultFillStage : QueryStage<SetResultFillTransform>
    {
        public SetResultFillStage(SetResultFillTransform transformation)
             : base("Set Result Fill Stage", transformation)
        {
        }
    }
}
