using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.ModuleLoader.Example
{
    [ModuleOrder(-1)]
    [ScriptableSettingsPath(SettingsPath)]
    class SettingsModule : ScriptableSettings<SettingsModule>, IModule
    {
        public const string SettingsPath = "ModuleLoaderExampleSettings";

#pragma warning disable 649
        [SerializeField]
        bool m_Logging = true;

        [SerializeField]
        int m_Setting = 42;
#pragma warning restore 649

        public bool logging { get { return m_Logging; } }

        public void LoadModule()
        {
            if (m_Logging)
                Debug.LogFormat("Settings Module loaded. Setting is: {0}", m_Setting);
        }

        public void UnloadModule()
        {
            if (m_Logging)
                Debug.LogFormat("Settings Module unloaded. Setting is: {0}", m_Setting);
        }
    }
}
