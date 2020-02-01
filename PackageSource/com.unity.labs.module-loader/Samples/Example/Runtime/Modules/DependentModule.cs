using UnityEngine;

namespace Unity.Labs.ModuleLoader.Example
{
    [ModuleOrder(1)]
    class DependentModule : IModuleDependency<SettingsModule>, IModuleDependency<VanillaModule>
    {
        VanillaModule m_Vanilla;
        SettingsModule m_SettingsModule;

        public void ConnectDependency(VanillaModule dependency)
        {
            m_Vanilla = dependency;
            if (m_SettingsModule.logging)
                Debug.LogFormat("Connecting Dependency Module with {0}", dependency);
        }

        public void ConnectDependency(SettingsModule dependency)
        {
            m_SettingsModule = dependency;
        }

        public void LoadModule()
        {
            if (m_SettingsModule.logging)
                Debug.LogFormat("DependentModule loaded with {0}", m_Vanilla);
        }

        public void UnloadModule()
        {
            if (m_SettingsModule.logging)
                Debug.LogFormat("DependentModule unloaded with {0}", m_Vanilla);
        }
    }
}
