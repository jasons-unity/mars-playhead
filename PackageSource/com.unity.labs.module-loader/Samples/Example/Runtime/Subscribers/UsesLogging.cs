using UnityEngine;

namespace Unity.Labs.ModuleLoader.Example
{
    class UsesLogging : MonoBehaviour, IUsesLogging
    {
#if !FI_AUTOFILL
        public IProvidesLogging provider { get; set; }
#endif

        void Start()
        {
            this.Log("Logging Subscriber Start");
        }
    }
}
