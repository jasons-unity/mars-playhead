using System;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Defines the API for a Cloud Data Storage Provider
    /// This functionality provider is responsible for providing a storage in the cloud for
    /// </summary>
    public interface IProvidesCloudDataStorage : IFunctionalityProvider
    {
        /// <summary>
        /// Get the current state of the connection to the cloud storage
        /// </summary>
        /// <returns>True if the Cloud Storage is connected to this client, false otherwise.</returns>
        bool IsConnected();

        /// <summary>
        /// Set the current authentication token
        /// </summary>
        void SetAPIKey(string key);

        /// <summary>
        /// Get the current authentication token
        /// </summary>
        string GetAPIKey();

        /// <summary>
        /// Set the current project identifier
        /// </summary>
        void SetProjectIdentifier(string id);

        /// <summary>
        /// Set the current project identifier
        /// </summary>
        string GetProjectIdentifier();

        /// <summary>
        /// Save to the cloud asynchronously the data of an object of a certain type with a specified key
        /// </summary>
        /// <param name="typeName"> string describing the type. </param>
        /// <param name="key"> string that uniquely identifies this instance of the type. </param>
        /// <param name="serializedObject"> string serialization of the object being saved. </param>
        /// <param name="callback"> a callback when the asynchronous call is done to show whether it was successful. </param>
        void CloudSaveAsync(string typeName, string key, string serializedObject, Action<bool> callback);

        /// <summary>
        /// Load from the cloud asynchronously the data of an object of a certain type which was saved with a known key
        /// </summary>
        /// <param name="typeName"> string describing the type. </param>
        /// <param name="key"> string that uniquely identifies this instance of the type. </param>
        /// <param name="callback">a callback which returns whether the operation was successful, as well as the serialized string of the object if it was. </param>
        void CloudLoadAsync(string typeName, string key, Action<bool, string> callback );

    }
}
