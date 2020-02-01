using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.PrefabHandles;
using UnityEngine.PrefabHandles.Picking;

namespace Unity.Labs.MARS
{
    public class RuntimeHandleContextModule : IModuleBehaviorCallbacks, IUsesFunctionalityInjection
    {
        sealed class MouseInputRuntimeContext : RuntimeHandleContext
        {
            const float k_PickingDistance = 15.0f;
            readonly InteractionState m_State;
            public InteractionState.State interactionState => m_State.state;

            public MouseInputRuntimeContext(Camera camera)
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

        static MouseInputRuntimeContext s_Context;

        static RuntimeHandleContextModule s_Instance;

        /// <summary>
        /// Whether there is currently a handle hover or drag happening
        /// </summary>
        public static bool IsInteracting => s_Context != null && s_Context.interactionState != HandleContext.InteractionState.State.Idle;

        public static RuntimeHandleContextModule Instance => s_Instance;
        public IProvidesFunctionalityInjection provider { get; set; }

        public GameObject CreateHandle(GameObject prefab)
        {
            if (s_Context == null)
                s_Context = new MouseInputRuntimeContext(MARSSession.Instance.cameraReference.GetComponent<Camera>());

            var handle = s_Context.CreateHandle(prefab);
            this.InjectFunctionalitySingle(handle);
            return handle;
        }

        public bool DestroyHandle(GameObject handle)
        {
            return s_Context.DestroyHandle(handle);
        }

        public void LoadModule()
        {
            s_Instance = this;
        }

        public void UnloadModule()
        {
            if (s_Context != null)
                s_Context.Dispose();

            s_Instance = null;
        }

        public void OnBehaviorAwake() { }
        public void OnBehaviorEnable() { }
        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
        {
            if (s_Context != null)
                s_Context.Update();
        }

        public void OnBehaviorDisable() { }
        public void OnBehaviorDestroy() { }
    }
}
