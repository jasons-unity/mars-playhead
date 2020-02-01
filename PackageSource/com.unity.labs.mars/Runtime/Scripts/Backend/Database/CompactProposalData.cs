using System;

namespace Unity.Labs.MARS.Data
{
    /// <summary>
    /// Represents a pairing of data id to match rating
    /// </summary>
    public struct CompactProposalData : IComparable<CompactProposalData>
    {
        public int dataId;
        public float score;

        public CompactProposalData(int dataId, float score)
        {
            this.dataId = dataId;
            this.score = score;
        }

        // default sorting behavior is by descending score
        public int CompareTo(CompactProposalData other)
        {
            return -score.CompareTo(other.score);
        }
    }
}
