using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Labs.MARS.Data
{
    struct NativeConditionRatings : IDisposable
    {
        const int k_DefaultCapacity = 128;

        public NativeArray<int> DataIds;

        /*
         * The way memory is laid out within this buffer is as follows:
         * The buffer is at least of length ConditionCount * DataIdCapacity.
         * The chunks are per-Condition:
         * If a condition has conditionIndex = 2, then its ratings start at index = 2 * DataIds.Length
         * The ratings for all data ids for a condition are contiguous in memory.
         * You will find the rating for a given condition + data id combo at
         * (conditionIndex * DataIds.Length) + the index of the data id in DataIds.
         */
        public NativeArray<float> Buffer;

        Allocator m_Allocator;

        int m_CurrentConditionStartingIndex;

        // internal for testing
        internal int CurrentDataIdOffset { get; private set; }

        public int ConditionCount { get; }

        public int DataIdCapacity { get; }

        public NativeConditionRatings(int conditionCount, int dataIdCapacity = k_DefaultCapacity,
            Allocator allocator = Allocator.Persistent)
        {
            m_Allocator = allocator;
            m_CurrentConditionStartingIndex = 0;
            CurrentDataIdOffset = 0;
            ConditionCount = conditionCount;
            DataIdCapacity = dataIdCapacity;
            DataIds = new NativeArray<int>(dataIdCapacity, allocator);
            Buffer = new NativeArray<float>(dataIdCapacity * conditionCount, allocator);
        }

        /// <summary>
        /// This must be called when we begin rating a new Condition within a query.
        /// </summary>
        /// <param name="conditionIndex">The index of the condition within the query</param>
        public void StartCondition(int conditionIndex)
        {
            m_CurrentConditionStartingIndex = conditionIndex * ConditionCount;
            CurrentDataIdOffset = 0;
        }

        public void SetDataIds<T>(T ids) where T : IEnumerable<int>
        {
            var i = 0;
            foreach (var id in ids)
            {
                DataIds[i] = id;
                i++;
            }
        }

        /// <summary>
        /// Call this even if the rating would be a failure, so we can know to move to the next data id
        /// </summary>
        /// <param name="rating">The rating for the current data id</param>
        public bool Add(float rating)
        {
            if (rating <= 0f)
            {
                CurrentDataIdOffset++;
                return false;
            }

            Buffer[m_CurrentConditionStartingIndex + CurrentDataIdOffset] = rating;
            CurrentDataIdOffset++;
            return true;
        }

        public void Resize(int newSize)
        {
            if (newSize <= Buffer.Length)
                return;

            var newBuffer = new NativeArray<float>(newSize, m_Allocator);
            for (var i = 0; i < Buffer.Length; i++)
            {
                newBuffer[i] = Buffer[i];
            }
            Buffer.Dispose();
            Buffer = newBuffer;
        }

        public void Reset()
        {
            m_CurrentConditionStartingIndex = 0;
            CurrentDataIdOffset = 0;
        }

        public void Dispose()
        {
            DataIds.Dispose();
            Buffer.Dispose();
        }
    }
}
