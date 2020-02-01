using System;
using Unity.Cloud.ApplicationStorage.Client;
using Unity.Labs.ModuleLoader;
using Unity.Cloud.Clients;
using Unity.Cloud.Clients.Unity;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ScriptableSettingsPath(MARSCore.UserSettingsFolder)]
    public class UnityCloudDataStorageModule : ScriptableSettings<UnityCloudDataStorageModule>, IModuleBehaviorCallbacks, IProvidesCloudDataStorage
    {
#if UNITY_EDITOR
        [Serializable]
        class APIKeyResponse
        {
#pragma warning disable 649
            public string apiKey;
#pragma warning restore 649
        }
#endif

        const string k_BaseUri = "https://build-api.cloud.unity3d.com";
        const string k_APIKeyPath = "api/v1/users/me/apiKey";

        static RestClient s_RestClient = new RestClient(new UnityHttpPlatform(), k_BaseUri);

#pragma warning disable 649
        [SerializeField]
        string m_APIKey;
#pragma warning restore 649

        public void SetAPIKey(string key) { UnityApplicationStorage.CloudPlatform.SetAuthenticationToken(key);  }
        public string GetAPIKey() { return UnityApplicationStorage.CloudPlatform.GetAuthenticationToken();  }
        public void SetProjectIdentifier(string id) { UnityApplicationStorage.CloudPlatform.SetProjectIdentifier(id); }
        public string GetProjectIdentifier() { return UnityApplicationStorage.CloudPlatform.GetProjectIdentifier();  }

        public void CloudSaveAsync(string typeName, string key, string serializedObject, Action<bool> callback)
        {
            UnityApplicationStorage.KeyValue.SetValue(typeName, key, serializedObject, callback);
        }

        public void CloudLoadAsync(string typeName, string key, Action<bool, string> callback)
        {
            UnityApplicationStorage.KeyValue.GetValue(typeName, key, callback);
        }

        public bool IsConnected()
        {
            return UnityApplicationStorage.KeyValue.IsValidProject();
        }

        public int GetLastStatusCode()
        {
            return UnityApplicationStorage.KeyValue.LastStatusCode;
        }

        public void LoadModule()
        {
#if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;

            if (string.IsNullOrEmpty(m_APIKey))
            {
                GetAPIKey((success, apiKey) =>
                {
                    var so = new SerializedObject(this);
                    so.FindProperty("m_APIKey").stringValue = apiKey;
                    so.ApplyModifiedProperties();
                });
            }
#endif

            SetAPIKey(m_APIKey);
        }

        public void UnloadModule()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
#endif
        }

#if UNITY_EDITOR
        void EditorUpdate()
        {
            if (!EditorApplication.isPlaying)
                OnBehaviorUpdate();
        }
#endif

        public void LoadProvider()
        {
        }

        public void ConnectSubscriber(object obj)
        {
#if !FI_AUTOFILL
            var cloudDataSubscriber = obj as IFunctionalitySubscriber<IProvidesCloudDataStorage>;
            if (cloudDataSubscriber != null)
                cloudDataSubscriber.provider = this;
#endif
        }

        public void UnloadProvider()
        {
        }

        public void OnBehaviorAwake() { }
        public void OnBehaviorEnable() { }
        public void OnBehaviorStart() { }

        public void OnBehaviorUpdate()
        {
            UnityApplicationStorage.KeyValue.Update();
            s_RestClient.Update();
        }
        public void OnBehaviorDisable() { }
        public void OnBehaviorDestroy() { }

#if UNITY_EDITOR
        public static void GetAPIKey(Action<bool, string> callback)
        {
            s_RestClient.Request(HttpPlatformMethod.Get, k_APIKeyPath, CloudProjectSettings.accessToken, null,
                null, (success, code, response) =>
                {
                    callback(success, success ? JsonUtility.FromJson<APIKeyResponse>(response).apiKey : null);
                });
        }
#endif
    }
}
