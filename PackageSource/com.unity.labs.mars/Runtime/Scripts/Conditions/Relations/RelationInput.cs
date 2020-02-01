using System;
using Unity.Collections;
using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    struct RelationInput<T1, T2> : IDisposable
        where T1 : struct, IRelationChildValues
        where T2 : struct, IRelationChildValues
    {
        public int Count;

        public NativeArray<int> DataIds;
        public NativeArray<T1> Child1Values;
        public NativeArray<T2> Child2Values;

        public RelationInput(int capacity, Allocator allocator = Allocator.Persistent)
        {
            Count = 0;
            DataIds = new NativeArray<int>(capacity, allocator);
            Child1Values = new NativeArray<T1>(capacity, allocator);
            Child2Values = new NativeArray<T2>(capacity, allocator);
        }

        public void Add(int dataId, T1 child1Value, T2 child2Value)
        {
            DataIds[Count] = dataId;
            Child1Values[Count] = child1Value;
            Child2Values[Count] = child2Value;
            Count++;
        }

        public void SetIndex(int i, int dataId, T1 child1Value, T2 child2Value)
        {
            DataIds[i] = dataId;
            Child1Values[i] = child1Value;
            Child2Values[i] = child2Value;
        }

        public void Dispose()
        {
            DataIds.Dispose();
            Child1Values.Dispose();
            Child2Values.Dispose();
        }
    }
}
