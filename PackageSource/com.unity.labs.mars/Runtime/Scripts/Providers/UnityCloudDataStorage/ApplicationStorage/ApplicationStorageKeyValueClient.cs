using System;
using Unity.Cloud.Clients;

namespace Unity.Cloud.ApplicationStorage.Client
{
    public class ApplicationStorageKeyValueClient : RestClient
    {
        const string k_GetValueFormattedUrl = "api/storage/keyvalue/projects/{0}/types/{1}/keys/{2}/data";
        const string k_SetValueFormattedUrl = "api/storage/keyvalue/projects/{0}/types/{1}/keys/{2}";
        public ApplicationStorageKeyValueClient(IHttpPlatform httpPlatform, string baseUri, ICloudPlatform cloudPlatform) : base(httpPlatform, baseUri)
        {
            this.cloudPlatform = cloudPlatform;
        }

        ICloudPlatform cloudPlatform { get; set; }
        public int LastStatusCode { get; private set; }

        public void GetValue(string type, string key, Action<bool, string> callback)
        {
            string path = string.Format(k_GetValueFormattedUrl, cloudPlatform.GetProjectIdentifier(), type, key);
            Request(HttpPlatformMethod.Get, path, cloudPlatform.GetAuthenticationToken(), null, null,
                (success, statusCode, result) => { LastStatusCode = statusCode; callback(success, result); });
        }

        public void SetValue(string type, string key, string value, Action<bool> callback)
        {
            string path = string.Format(k_SetValueFormattedUrl, cloudPlatform.GetProjectIdentifier(), type, key);
            Request(HttpPlatformMethod.Post, path, cloudPlatform.GetAuthenticationToken(), value, null,
                (success, statusCode, result) => { LastStatusCode = statusCode; callback(success); });
        }

        public bool IsValidProject()
        {
            return cloudPlatform.GetProjectIdentifier() != "";
        }
    }
}
