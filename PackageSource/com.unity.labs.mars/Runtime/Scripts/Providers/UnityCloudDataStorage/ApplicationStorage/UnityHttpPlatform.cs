using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Unity.Cloud.Clients.Unity
{
    public class UnityHttpPlatform : IHttpPlatform
    {
        class UnityHttpPlatformRequest
        {
            public Action<bool, int, byte[]> Callback { get; set; }

            public Action<float, float> ProgressCallback { get; set; }

            public UnityWebRequest WebRequest { get; set; }

            public bool IsComplete { get; set; }
        }

        public UnityHttpPlatform()
        {
            requests = new List<UnityHttpPlatformRequest>();
            newRequests = new List<UnityHttpPlatformRequest>();
        }

        List<UnityHttpPlatformRequest> requests;
        List<UnityHttpPlatformRequest> newRequests;

        public void Request(HttpPlatformMethod method, string uri, string authorization, string contentType, byte[] content, Action<float, float> progressCallback, Action<bool, int, byte[]> callback)
        {
            // Method String
            string methodString = "GET";
            if (method == HttpPlatformMethod.Post)
            {
                methodString = "POST";
            }
            else if (method == HttpPlatformMethod.Put)
            {
                methodString = "PUT";
            }
            else if (method == HttpPlatformMethod.Delete)
            {
                methodString = "Delete";
            }

            // Web Request
            UnityWebRequest webRequest = new UnityWebRequest(uri, methodString);

            // Authorization
            if (authorization != null)
            {
                webRequest.SetRequestHeader("Authorization", authorization);
            }

            // Content
            if (method == HttpPlatformMethod.Post || method == HttpPlatformMethod.Put)
            {
                if (contentType != null)
                {
                    webRequest.SetRequestHeader("Content-Type", contentType);
                    webRequest.uploadHandler = new UploadHandlerRaw(content);
                }
            }

            // Download Handler
            webRequest.downloadHandler = new DownloadHandlerBuffer();

            // Send
            webRequest.SendWebRequest();

            // Request
            UnityHttpPlatformRequest request = new UnityHttpPlatformRequest();
            request.WebRequest = webRequest;
            request.ProgressCallback = progressCallback;
            request.Callback = callback;
            //do not add directly to the requests in case we're iterating over it (re-entrant)
            newRequests.Add(request);
        }

        public void Update()
        {
            if (newRequests.Count > 0)
            {
                requests.AddRange(newRequests);
                newRequests.Clear();
            }
            // Complete
            foreach (UnityHttpPlatformRequest request in requests)
            {
                if (!request.IsComplete)
                {
                    if (request.WebRequest.isDone)
                    {
                        request.IsComplete = true;
                        request.Callback?.Invoke(request.WebRequest.error == null, (int)request.WebRequest.responseCode, request.WebRequest.downloadHandler?.data);
                    }
                    else
                    {
                        request.ProgressCallback?.Invoke(request.WebRequest.uploadProgress, request.WebRequest.downloadProgress);
                    }
                }
            }

            // Clean Up
            int i = 0;
            while (i < requests.Count)
            {
                UnityHttpPlatformRequest request = requests[i];
                if (request.IsComplete)
                {
                    requests.RemoveAt(i);
                }
                else
                {
                    i++;
                }
            }
        }
    }
}
