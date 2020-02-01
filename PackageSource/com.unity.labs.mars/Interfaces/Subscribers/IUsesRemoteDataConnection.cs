using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to a remote data connection.
    /// </summary>
    public interface IUsesRemoteDataConnection : IFunctionalitySubscriber<IProvidesRemoteDataConnection>
    {
    }

    public static class IUsesRemoteDataConnectionMethods
    {
        /// <summary>
        /// Get the current state of the remote data connection
        /// </summary>
        /// <returns>True if connected to a remote, false if not</returns>
        public static bool IsConnected(this IUsesRemoteDataConnection obj)
        {
#if FI_AUTOFILL
            return default(bool);
#else
            return obj.provider.IsConnected();
#endif
        }

        /// <summary>
        /// Connect the module to an editor remote
        /// </summary>
        public static void ConnectRemote(this IUsesRemoteDataConnection obj)
        {
#if !FI_AUTOFILL
            obj.provider.ConnectRemote();
#endif
        }

        /// <summary>
        /// Disconnect from the editor remote
        /// </summary>
        public static void DisconnectRemote(this IUsesRemoteDataConnection obj)
        {
#if !FI_AUTOFILL
            obj.provider.DisconnectRemote();
#endif
        }

        /// <summary>
        /// Update the connection to the editor remote
        /// </summary>
        public static void UpdateRemote(this IUsesRemoteDataConnection obj)
        {
#if !FI_AUTOFILL
            obj.provider.UpdateRemote();
#endif
        }
    }
}
