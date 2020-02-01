using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.MARS
{
    public class CloudDataStorageTest : MonoBehaviour, IUsesCloudDataStorage
    {
#if !FI_AUTOFILL
        IProvidesCloudDataStorage IFunctionalitySubscriber<IProvidesCloudDataStorage>.provider { get; set; }
#endif

        public Text lastValueFromCloud;
        public Text connectionStatus;
        float delay = 1.0f;

        public void SetValueTest(string newValue)
        {
            this.CloudSaveAsync("TestString", "1234", newValue, (success) =>
            {
                if (success)
                {
                    Debug.Log("Key value set.");
                }
                else
                {
                    Debug.Log("Failed to set value");
                }

            });
        }

        public void GetValueTest()
        {
            this.CloudLoadAsync("TestString", "1234", (success, value) =>
            {
                if (success)
                {
                    Debug.Log("Got a value of " + value);
                    lastValueFromCloud.text = value;
                }
                else
                {
                    Debug.Log("failed to get value.");
                }
            });
        }

        public bool GetConnected() { return this.IsConnected(); }

        void Update()
        {
            delay -= Time.deltaTime;
            if (delay < 0.0f)
            {
                delay = 1.0f;
                connectionStatus.text = GetConnected() ? "Connected" : "Disconnected - set project to connect to Unity Cloud";
            }
        }
    }
}
