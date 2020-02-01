namespace Unity.Cloud.Clients
{
    public interface ICloudPlatform
    {
        string GetAuthenticationToken();

        string GetProjectIdentifier();
        void SetProjectIdentifier(string id);
    }
}
