namespace UnityEngine.PrefabHandles
{
    public abstract class InteractiveHandle : HandleBehaviour
    {
        public abstract Plane GetProjectionPlane(Vector3 camPosition);
    }
}