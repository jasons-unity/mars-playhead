using System.Collections.Generic;

namespace Unity.Labs.MARS
{
    abstract class ParallelSparseArrays
    {
        protected int m_Count;
        protected int m_Capacity;

        /// <summary>
        /// Indices of data that has been removed
        /// </summary>
        internal readonly Queue<int> FreedIndices = new Queue<int>();    // internal for testing

        /// <summary>
        /// All indices which currently contain valid data
        /// </summary>
        public readonly List<int> ValidIndices  = new List<int>();

        public int Count => m_Count;
        public int Capacity => m_Capacity;
        
        public int ResizeMultiplier = 2;
        
        internal abstract void Resize(int newSize);

        public ParallelSparseArrays(int startingCapacity)
        {
            m_Capacity = startingCapacity;
        }

        protected int GetInsertionIndex()
        {
            if(m_Count >= m_Capacity)
                Resize(m_Capacity * ResizeMultiplier);

            return FreedIndices.Count > 0 ? FreedIndices.Dequeue() : m_Count;
        }

        protected void FreeIndex(int index)
        {
            FreedIndices.Enqueue(index);
            ValidIndices.Remove(index);
        }
    }
}
