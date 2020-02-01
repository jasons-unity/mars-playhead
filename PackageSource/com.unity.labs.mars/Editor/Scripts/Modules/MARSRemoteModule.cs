using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Module for managing and interfacing with a remote data connection in the editor
    /// </summary>
    public class MARSRemoteModule : IModuleBehaviorCallbacks, IUsesRemoteDataConnection
    {
#if !FI_AUTOFILL
        IProvidesRemoteDataConnection IFunctionalitySubscriber<IProvidesRemoteDataConnection>.provider { get; set; }
#endif

        bool m_HasRemoteModule;

        public void LoadModule()
        {
            m_HasRemoteModule = this.HasProvider<IProvidesRemoteDataConnection>();
        }

        public void UnloadModule()
        {
            RemoteDisconnect();
        }

        public void OnBehaviorAwake() { }

        public void OnBehaviorEnable() { }

        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
        {
            RemoteUpdate();
        }

        public void OnBehaviorDisable()
        {
            RemoteDisconnect();
        }

        public void OnBehaviorDestroy() { }

        /// <summary>
        /// Is the module connected an editor remote
        /// </summary>
        public bool RemoteActive => m_HasRemoteModule && this.IsConnected();

        /// <summary>
        /// Connect the module to an editor remote
        /// </summary>
        public void RemoteConnect()
        {
            if (m_HasRemoteModule)
                this.ConnectRemote();
        }

        /// <summary>
        /// Disconnect from the editor remote
        /// </summary>
        public void RemoteDisconnect()
        {
            if (m_HasRemoteModule)
                this.DisconnectRemote();
        }

        /// <summary>
        /// Update the connection to the editor remote
        /// </summary>
        public void RemoteUpdate()
        {
            if (m_HasRemoteModule)
                this.UpdateRemote();
        }
    }
}
