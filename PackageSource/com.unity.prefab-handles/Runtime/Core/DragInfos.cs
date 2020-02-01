namespace UnityEngine.PrefabHandles
{
    /// <summary>
    /// Information of the drag begin event.
    /// </summary>
    public struct DragBeginInfo
    {
        public DragTranslation translation;

        /// <summary>
        /// Create a new DragBeginInfo.
        /// </summary>
        /// <param name="screen">The information of the drag on screen</param>
        public DragBeginInfo(DragTranslation translation) : this()
        {
            this.translation = translation;
        }
    }

    /// <summary>
    /// Information of the drag update event.
    /// </summary>
    public struct DragUpdateInfo
    {
        /// <summary>
        /// The information of the drag on screen.
        /// </summary>
        public DragTranslation translation { get; private set; }

        /// <summary>
        /// Create a new DragUpdateInfo.
        /// </summary>
        /// <param name="screen">The information of the drag on screen</param>
        public DragUpdateInfo(DragTranslation translation) : this()
        {
            this.translation = translation;
        }
    }

    /// <summary>
    /// Information of the drag end event.
    /// </summary>
    public struct DragEndInfo
    {
        /// <summary>
        /// The information of the drag on screen.
        /// </summary>
        public DragTranslation translation;

        /// <summary>
        /// Create a new DragEndInfo.
        /// </summary>
        /// <param name="screen">The information of the drag on screen</param>
        public DragEndInfo(DragTranslation translation) : this()
        {
            this.translation = translation;
        }
    }
}