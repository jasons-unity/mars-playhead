namespace UnityEngine.PrefabHandles
{
    public interface IHandleContext
    {
        GameObject CreateHandle(DefaultHandle handle);
        GameObject CreateHandle(GameObject prefab);
        bool DestroyHandle(GameObject handle);
    }
}