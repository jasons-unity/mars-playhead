namespace Unity.Labs.MARS
{
    public interface ISpatialCondition
    {
#if UNITY_EDITOR
        void ScaleParameters(float scale);
#endif
    }
}
