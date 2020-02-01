namespace Unity.Labs.ModuleLoader
{
    /// <summary>
    /// Provides functionality for an IFunctionalitySubscriber
    /// </summary>
    public interface IFunctionalityProvider
    {
        /// <summary>
        /// Called when the provider is loaded into a <c>FunctionalityIsland</c>
        /// </summary>
        void LoadProvider();

        /// <summary>
        /// Called by the <c>FunctionalityIsland</c> containing this provider when injecting functionality on an object
        /// </summary>
        /// <param name="obj">The object onto which functionality is being injected. If this implements a subscriber
        /// interface that subscribes to functionality provided by this object, it will set itself as the provider</param>
        void ConnectSubscriber(object obj);

        /// <summary>
        /// Called when the provider is unloaded by the containing <c>FunctionalityIsland</c>
        /// </summary>
        void UnloadProvider();
    }
}
