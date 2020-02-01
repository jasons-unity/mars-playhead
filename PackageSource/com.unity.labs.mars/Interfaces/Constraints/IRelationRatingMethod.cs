namespace Unity.Labs.MARS.Query
{
    public interface IRelationRatingMethod<TChild1Values, TChild2Values>
        where TChild1Values : struct, IRelationChildValues
        where TChild2Values : struct, IRelationChildValues
    {
        float RateDataMatch(ref TChild1Values child1, ref TChild2Values child2);
    }
}
