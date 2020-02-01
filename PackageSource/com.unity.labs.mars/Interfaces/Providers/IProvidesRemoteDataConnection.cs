using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS {
    /// <summary>
    /// Defines the API for a remote data connection.
    /// </summary>
    public interface IProvidesRemoteDataConnection : IFunctionalityProvider
    {
        /// <summary>
        /// Get the current state of the remote data connection.
        /// </summary>
        /// <returns>True if the data remote is connected to this client, false otherwise.</returns>
        bool IsConnected();

        /// <summary>
        /// Start a remote data connection.
        /// </summary>
        void ConnectRemote();

        /// <summary>
        /// End a remote data connection.
        /// </summary>
        void DisconnectRemote();

        /// <summary>
        /// Update the provider with the data coming from the remote.
        /// </summary>
        void UpdateRemote();
    }
}
