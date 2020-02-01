using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.Cloud.Clients
{
    public class UnityCloudPlatform : ICloudPlatform
    {
        string m_ProjectIdentifier;
        string m_AuthenticationToken;

        public UnityCloudPlatform()
        {
#if UNITY_EDITOR
            m_AuthenticationToken = CloudProjectSettings.accessToken;
#endif
            m_ProjectIdentifier = Application.cloudProjectId;
        }

        public string GetAuthenticationToken() { return m_AuthenticationToken; }

        public string GetProjectIdentifier() { return m_ProjectIdentifier; }

        public void SetProjectIdentifier(string id) { m_ProjectIdentifier = id; }
        public void SetAuthenticationToken(string token) { m_AuthenticationToken = token; }
    }
}
