using System.Collections.Generic;

namespace Unity.Labs.MARS.Query
{
    class MarkDataUsedTransform : DataTransform<List<int>, List<QueryMatchID>, List<Exclusivity>> {}

    class MarkUsedStage : QueryStage<MarkDataUsedTransform>
    {
        public MarkUsedStage(MarkDataUsedTransform transformation)
            : base("Mark Data Used", transformation)
        {
        }
    }
}
