using System.Collections.Generic;

namespace Unity.Labs.MARS
{
    abstract class ParallelSparseLists
    {
        protected int m_Count;

        /// <summary>
        /// Indices of data that has been removed
        /// </summary>
        internal readonly Queue<int> FreedIndices = new Queue<int>();    // internal for testing

        /// <summary>
        /// All indices which currently contain valid data
        /// </summary>
        public readonly List<int> ValidIndices  = new List<int>();

        protected int GetInsertionIndex()
        {
            return FreedIndices.Count > 0 ? FreedIndices.Dequeue() : m_Count;
        }

        protected void FreeIndex(int index)
        {
            FreedIndices.Enqueue(index);
            ValidIndices.Remove(index);
        }
    }
}
