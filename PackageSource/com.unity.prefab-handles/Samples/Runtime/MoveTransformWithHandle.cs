namespace UnityEngine.PrefabHandles.RuntimeSample
{
    public sealed class MoveTransformWithHandle : MonoBehaviour
    {
        PositionHandle m_PositionHandle;

        void Start()
        {
            m_PositionHandle = BasicRuntimeContextController.context.CreateHandle(DefaultHandle.PositionHandle).GetComponent<PositionHandle>();
            m_PositionHandle.translationUpdated += OnTranslationUpdated;
        }

        void OnTranslationUpdated(TranslationUpdateInfo translation)
        {
            transform.position += translation.world.delta;
        }

        void Update()
        {
            m_PositionHandle.transform.position = transform.position;
            m_PositionHandle.transform.rotation = transform.rotation;
        }
    }
}