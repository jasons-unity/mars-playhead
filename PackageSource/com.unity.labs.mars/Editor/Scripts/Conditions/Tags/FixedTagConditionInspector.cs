namespace Unity.Labs.MARS
{
    /// <summary>
    /// Base class for blank semantic tag condition inspectors
    /// </summary>
    public abstract class FixedTagConditionInspector : ComponentInspector
    {
        public override bool HasDisplayProperties()
        {
            return false;
        }
    }
}
