using System;
using System.Text;
using UnityEngine;

namespace Unity.Cloud.Clients
{
    public class RestClient
    {
        public RestClient(IHttpPlatform httpPlatform, string baseUri)
        {
            m_BaseUri = baseUri;
            m_HttpPlatform = httpPlatform;
        }

        string m_BaseUri;

        IHttpPlatform m_HttpPlatform;

        public void Request(HttpPlatformMethod method, string path, string authToken, string content, Action<float, float> progressCallback, Action<bool, int, string> callback)
        {
            string uri = string.Format("{0}/{1}", m_BaseUri, path);
            string authorization = null;
            if (authToken != null)
            {
                authorization = string.Format("Bearer {0}", authToken);
            }

            byte[] contentBytes = null;
            if (content != null)
            {
                content = string.Format("\"{0}\"", content);
                contentBytes = Encoding.UTF8.GetBytes(content);
            }

            Action<bool, int, byte[]> innerCallback = (success, statusCode, response) => { callback?.Invoke(success, statusCode, Encoding.UTF8.GetString(response)); };
            m_HttpPlatform.Request(method, uri, authorization, "application/json", contentBytes, progressCallback, innerCallback);
        }

        public void Update()
        {
            m_HttpPlatform.Update();
        }
    }
}
