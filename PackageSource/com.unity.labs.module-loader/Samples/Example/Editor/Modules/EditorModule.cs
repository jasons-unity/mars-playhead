using UnityEngine;

namespace Unity.Labs.ModuleLoader.Example
{
    // Some modules only need to exist in the editor assembly
    // ReSharper disable once UnusedMember.Global
    class EditorModule : IModuleDependency<SettingsModule>
    {
        SettingsModule m_SettingsModule;

        public void ConnectDependency(SettingsModule dependency)
        {
            m_SettingsModule = dependency;
        }

        public void LoadModule()
        {
            if (m_SettingsModule.logging)
                Debug.Log("EditorModule loaded");
        }

        public void UnloadModule()
        {
            if (m_SettingsModule.logging)
                Debug.Log("EditorModule unloaded");
        }
    }
}
