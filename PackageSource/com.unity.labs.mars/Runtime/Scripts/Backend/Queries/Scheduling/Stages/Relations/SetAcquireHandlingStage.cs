using System;
using System.Collections.Generic;

namespace Unity.Labs.MARS.Query
{
    // technically this doesn't transform data, but it's easier / more consistent to put it in this format
    class SetAcquireHandlingTransform : DataTransform<SetQueryResult[], Action<SetQueryResult>[]>
    {
        public SetAcquireHandlingTransform()
        {
            Process = CallHandlers;
        }

        /// <summary>
        /// Call every acquire handler with its corresponding query result
        /// </summary>
        /// <param name="indices">The indices to operate on</param>
        /// <param name="results">The collection of all query results</param>
        /// <param name="handlers">The collection of all query acquire handlers</param>
        static void CallHandlers(List<int> indices, SetQueryResult[] results,
            ref Action<SetQueryResult>[] handlers)
        {
            foreach (var i in indices)
            {
                handlers[i](results[i]);
            }
        }
    }


    class ManageSetIndicesTransform : DataTransform<int[][], HashSet<int>, List<int>>
    {
        public ManageSetIndicesTransform()
        {
            Process = UpdateIndices;
        }

        static void UpdateIndices(List<int> indices, int[][] allMemberIndices, HashSet<int> updatingIndices,
            ref List<int> acquiringIndices)
        {
            foreach (var i in indices)
            {
                var memberIndices = allMemberIndices[i];
                foreach (var mi in memberIndices)
                {
                    updatingIndices.Add(mi);
                    acquiringIndices.Remove(mi);
                }
            }
        }
    }

    class SetAcquireHandlingStage : QueryStage<SetAcquireHandlingTransform, ManageIndicesTransform, ManageSetIndicesTransform>
    {
        public SetAcquireHandlingStage(SetAcquireHandlingTransform acquireTransform,
            ManageIndicesTransform indicesTransform, ManageSetIndicesTransform setIndicesTransform)
            : base("Set Acquire Handling",
                acquireTransform, indicesTransform, setIndicesTransform)
        {
        }
    }
}
