using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Container for a single trait value of an relation child
    /// </summary>
    /// <typeparam name="TValue">The trait's type</typeparam>
    public struct RelationChildValues<TValue> : IRelationChildValues<TValue> where TValue : struct
    {
        public TValue Trait1 { get; set; }
    }

    /// <summary>
    /// Container for 2 trait values of a relation child
    /// </summary>
    /// <typeparam name="TValue1">The 1st trait's type</typeparam>
    /// <typeparam name="TValue2">The 2nd trait's type</typeparam>
    public struct RelationChildValues<TValue1, TValue2> : IRelationChildValues<TValue1, TValue2>
        where TValue1 : struct
        where TValue2 : struct
    {
        public TValue1 Trait1 { get; set; }
        public TValue2 Trait2 { get; set; }
    }

    /// <summary>
    /// Container for 3 trait values of a relation child
    /// </summary>
    /// <typeparam name="TValue1">The 1st trait's type</typeparam>
    /// <typeparam name="TValue2">The 2nd trait's type</typeparam>
    /// <typeparam name="TValue3">The 3rd trait's type</typeparam>
    public struct RelationChildValues<TValue1, TValue2, TValue3> : IRelationChildValues<TValue1, TValue2, TValue3>
        where TValue1 : struct
        where TValue2 : struct
        where TValue3 : struct
    {
        public TValue1 Trait1 { get; set; }
        public TValue2 Trait2 { get; set; }
        public TValue3 Trait3 { get; set; }
    }
}
