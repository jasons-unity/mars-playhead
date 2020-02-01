namespace UnityEngine.PrefabHandles
{
    [ExecuteInEditMode]
    public abstract class HandleBehaviour : MonoBehaviour
    {
        HandleContext m_Context;
        InteractiveHandle m_OwnerHandle;
        bool m_CreatedEventSent;

        /// <summary>
        /// The parent handle that owns this behaviour. Used during a handle event to know if this is the target.
        /// </summary>
        public InteractiveHandle ownerHandle => m_OwnerHandle;

        /// <summary>
        /// The context that owns this behaviour
        /// </summary>
        public HandleContext context
        {
            get => m_Context;
            set
            {
                if (m_Context == value)
                    return;

                var activeAndEnabled = isActiveAndEnabled;
                if (activeAndEnabled)
                    m_Context?.UnregisterHandleBehaviour(this);

                m_Context = value;

                if (activeAndEnabled && m_Context != null)
                {
                    m_Context.RegisterHandleBehaviour(this);
                    SendOnCreatedByContextIfNotAlreadySent();
                }
            }
        }

        void OnEnableINTERNAL()
        {
            m_OwnerHandle = GetParentHandle();

            if (m_Context != null)
            {
                m_Context.RegisterHandleBehaviour(this);
                SendOnCreatedByContextIfNotAlreadySent();
            }
            else
            {
                context = HandleContext.GetContext(gameObject);
            }            
        }

        void SendOnCreatedByContextIfNotAlreadySent()
        {
            if (!m_CreatedEventSent)
            {
                m_CreatedEventSent = true;
                OnCreatedByContext();
            }
        }

        void OnDisableINTERNAL()
        {
            context?.UnregisterHandleBehaviour(this);
        }

        internal virtual void OnTransformParentChanged()
        {
            m_OwnerHandle = GetParentHandle();
        }

        InteractiveHandle GetParentHandle()
        {
            return GetComponentInParent<InteractiveHandle>();
        }

        internal void DoDragBegin(InteractiveHandle target, DragBeginInfo info)
        {
            DragBegin(target, info);
        }

        internal void DoDragUpdate(InteractiveHandle target, DragUpdateInfo info)
        {
            DragUpdate(target, info);
        }

        internal void DoDragEnd(InteractiveHandle target, DragEndInfo info)
        {
            DragEnd(target, info);
        }

        internal void DoHoverBegin(InteractiveHandle target, HoverBeginInfo info)
        {
            HoverBegin(target, info);
        }

        internal void DoHoverUpdate(InteractiveHandle target, HoverUpdateInfo info)
        {
            HoverUpdate(target, info);
        }

        internal void DoHoverEnd(InteractiveHandle target, HoverEndInfo info)
        {
            HoverEnd(target, info);
        }

        internal void DoPreRender(Camera camera)
        {
            PreRender(camera);
        }

        internal void DoPostRender(Camera camera)
        {
            PostRender(camera);
        }
        
        /// <summary>
        /// Called when the handle behaviour was created by a context.
        /// </summary>
        protected virtual void OnCreatedByContext() {}

        /// <summary>
        /// Called when a drag is started on a handle
        /// </summary>
        /// <param name="target">The target handle which is currently dragged</param>
        /// <param name="info">The event arguments of the drag begin</param>
        protected virtual void DragBegin(InteractiveHandle target, DragBeginInfo info) { }
        /// <summary>
        /// Called when a drag is updated on a handle
        /// </summary>
        /// <param name="target">The target handle which is currently dragged</param>
        /// <param name="info">The event arguments of the drag update</param>
        protected virtual void DragUpdate(InteractiveHandle target, DragUpdateInfo info) { }
        /// <summary>
        /// Called when a drag is stopped on a handle
        /// </summary>
        /// <param name="target">The target handle which is currently dragged</param>
        /// <param name="info">The event arguments of the drag end</param>
        protected virtual void DragEnd(InteractiveHandle target, DragEndInfo info) { }
        /// <summary>
        /// Called when a hover is started on a handle
        /// </summary>
        /// <param name="target">The target handle which is currently hovered</param>
        /// <param name="info">The event arguments of the hover begin</param>
        protected virtual void HoverBegin(InteractiveHandle target, HoverBeginInfo info) { }
        /// <summary>
        /// Called when a hover is updated on a handle
        /// </summary>
        /// <param name="target">The target handle which is currently hovered</param>
        /// <param name="info">The event arguments of the hover update</param>
        protected virtual void HoverUpdate(InteractiveHandle target, HoverUpdateInfo info) { }
        /// <summary>
        /// Called when a hover is stopped on a handle
        /// </summary>
        /// <param name="target">The target handle which is currently hovered</param>
        /// <param name="info">The event arguments of the hover end</param>
        protected virtual void HoverEnd(InteractiveHandle target, HoverEndInfo info) { }
        /// <summary>
        /// Called before the handle is renderer
        /// </summary>
        /// <param name="camera">The camera used to render the handle</param>
        protected virtual void PreRender(Camera camera) {}
        /// <summary>
        /// Called after the handle is renderer
        /// </summary>
        /// <param name="camera">The camera used to render the handle</param>
        protected virtual void PostRender(Camera camera) {}
    }
}