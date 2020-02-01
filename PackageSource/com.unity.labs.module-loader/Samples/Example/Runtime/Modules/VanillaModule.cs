using UnityEngine;

namespace Unity.Labs.ModuleLoader.Example
{
    // Use this attribute to control loading order
    [ModuleOrder(0)]
    class VanillaModule : IModule
    {
        public void LoadModule()
        {
            // We would normally use a module dependency but to keep this class simple, we just use the Settings Module instance directly
            if (SettingsModule.instance.logging)
                Debug.Log("VanillaModule loaded");
        }

        public void UnloadModule()
        {
            if (SettingsModule.instance.logging)
                Debug.Log("VanillaModule unloaded");
        }
    }
}
