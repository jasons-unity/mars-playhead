using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Base class for any condition that just wants to check the existence for a specific semantic tag
    /// </summary>
    [ComponentTooltip("Requires the object to have a specific trait")]
    public abstract class SimpleTagCondition : Condition<bool>, ISemanticTagCondition
    {
        // tag conditions have binary pass / fail answers
        public override float RateDataMatch(ref bool data)
        {
            return 1.0f;
        }

        public SemanticTagMatchRule matchRule => SemanticTagMatchRule.Match;
    }
}
