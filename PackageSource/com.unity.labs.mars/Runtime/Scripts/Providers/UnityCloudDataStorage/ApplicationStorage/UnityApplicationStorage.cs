using System;
using Unity.Cloud.Clients;
using Unity.Cloud.Clients.Unity;

namespace Unity.Cloud.ApplicationStorage.Client
{
    public static class UnityApplicationStorage
    {
        static UnityApplicationStorage()
        {
            CloudPlatform = new UnityCloudPlatform();
            KeyValue = new ApplicationStorageKeyValueClient(new UnityHttpPlatform(), "https://unity-labs-projectmars-test.appspot.com", CloudPlatform);
        }

        public static ApplicationStorageKeyValueClient KeyValue { get; private set; }
        public static UnityCloudPlatform CloudPlatform { get; private set; }
    }
}
