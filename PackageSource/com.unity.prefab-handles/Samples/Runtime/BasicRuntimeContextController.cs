using UnityEngine.PrefabHandles.Picking;

namespace UnityEngine.PrefabHandles.RuntimeSample
{
    [DefaultExecutionOrder(-1)]
    public sealed class BasicRuntimeContextController : MonoBehaviour
    {
        sealed class BasicRuntimeContext : RuntimeHandleContext
        {
            const float k_PickingDistance = 15.0f;

            readonly InteractionState m_State;

            public BasicRuntimeContext(Camera camera)
            {
                m_State = new InteractionState(this); 
                this.camera = camera;
            }

            public void Update()
            {
                if (!m_State.isDragging)
                {
                    PickingHit hit;
                    ScreenPickingUtility.GetHovered(
                        handles,
                        Input.mousePosition, 
                        camera,
                        k_PickingDistance,
                        out hit);

                    m_State.SetHovered(GetHandle(hit.target));
                    m_State.Update(HandleUtility.ProjectScreenPositionOnHandle(
                        m_State.activeHandle,
                        Input.mousePosition,
                        camera));
                }
                else
                {
                    m_State.Update(HandleUtility.ProjectScreenPositionOnHandle(
                        m_State.activeHandle,
                        Input.mousePosition,
                        camera));
                }

                if (Input.GetMouseButtonDown(0))
                {
                    m_State.StartDrag(HandleUtility.ProjectScreenPositionOnHandle(
                        m_State.activeHandle,
                        Input.mousePosition,
                        camera));
                }
                else if (Input.GetMouseButtonUp(0))
                {
                    m_State.StopDrag();
                }
            }
        }

        [SerializeField] Camera m_Camera = null;

        static BasicRuntimeContext s_Context;

        void Awake()
        {
            s_Context = new BasicRuntimeContext(m_Camera);
        }

        void Update()
        {
            s_Context.Update();
        }
        
        public static IHandleContext context
        {
            get { return s_Context; }
        }
    }
}