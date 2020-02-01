using System;

namespace Unity.Cloud.Clients
{
    public interface IHttpPlatform
    {
        void Request(HttpPlatformMethod method, string uri, string authorization, string contentType, byte[] content, Action<float, float> progressCallback, Action<bool, int, byte[]> callback);

        void Update();
    }
}