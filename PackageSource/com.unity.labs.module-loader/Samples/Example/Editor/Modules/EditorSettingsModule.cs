using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.ModuleLoader.Example
{
    // Some editor modules want serialized settings--but we don't want these assets to go into builds!
    [ScriptableSettingsPath(SettingsModule.SettingsPath)]
    class EditorSettingsModule : EditorScriptableSettings<EditorSettingsModule>, IModuleDependency<SettingsModule>
    {
        [SerializeField]
        int m_Setting = 1337;

        SettingsModule m_SettingsModule;

        public void ConnectDependency(SettingsModule dependency)
        {
            m_SettingsModule = dependency;
        }

        public void LoadModule()
        {
            if (m_SettingsModule.logging)
                Debug.LogFormat("Editor Settings Module loaded. Setting is: {0}", m_Setting);
        }

        public void UnloadModule()
        {
            if (m_SettingsModule.logging)
                Debug.LogFormat("Editor Settings Module unloaded. Setting is: {0}", m_Setting);
        }
    }
}
