using UnityEngine;

namespace Unity.Labs.ModuleLoader.Example
{
    // ReSharper disable once UnusedMember.Global
    class LoggingProvider : IProvidesLogging
    {
        public void LoadProvider() { }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var loggingSubscriber = obj as IUsesLogging;
            if (loggingSubscriber != null)
                loggingSubscriber.provider = this;
#endif
        }

        public void UnloadProvider() { }

        public void Log(object obj)
        {
            Debug.Log(obj);
        }
    }
}
