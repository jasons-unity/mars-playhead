using UnityEngine;

namespace Unity.Labs.ModuleLoader.Example
{
    class BehaviorModule : MonoBehaviour, IModuleDependency<SettingsModule>
    {
        SettingsModule m_SettingsModule;

        public void ConnectDependency(SettingsModule dependency)
        {
            m_SettingsModule = dependency;
        }

        void Awake()
        {
            // We don't see this method called with default HideAndDontSave HideFlags
            if (m_SettingsModule.logging)
                Debug.Log("BehaviorModule Awake");
        }

        void Update()
        {
            if (m_SettingsModule.logging)
                Debug.Log("BehaviorModule Update");
        }

        void OnDestroy()
        {
            // We don't see this method called with default HideAndDontSave HideFlags
            if (m_SettingsModule.logging)
                Debug.Log("BehaviorModule OnDestroy");
        }

        public void LoadModule()
        {
            if (m_SettingsModule.logging)
                Debug.Log("BehaviorModule loaded");
        }

        public void UnloadModule()
        {
            if (m_SettingsModule.logging)
                Debug.Log("BehaviorModule unloaded");
        }
    }
}
