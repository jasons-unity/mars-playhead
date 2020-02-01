namespace Unity.Labs.MARS
{
    public abstract class SpatialConditionInspector : ConditionBaseInspector
    {
        protected Condition condition { get; private set; }

        public override void OnEnable()
        {
            condition = (Condition)target;
            base.OnEnable();
        }
    }
}
