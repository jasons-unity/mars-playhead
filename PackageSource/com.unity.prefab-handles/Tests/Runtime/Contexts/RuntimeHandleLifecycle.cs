namespace UnityEngine.PrefabHandles.Tests.Contexts
{
    public sealed class RuntimeDummyContext : RuntimeHandleContext, ITestContext
    {
        public int handleCount
        {
            get { return handles.Count; }
        }
    }

    public sealed class RuntimeHandleLifecycleTests : HandleLifecycleTests<RuntimeDummyContext>
    {
    }
}