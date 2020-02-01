using System;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides cloud data storage service that can save and load data from the cloud.
    /// </summary>
    public interface IUsesCloudDataStorage : IFunctionalitySubscriber<IProvidesCloudDataStorage>
    {
    }

    public static class IUsesCloudDataStorageMethods
    {
        /// <summary>
        /// Get the current state of the connection to the cloud storage
        /// </summary>
        /// <returns>True if the Cloud Storage is connected to this client, false otherwise.</returns>
        public static bool IsConnected(this IUsesCloudDataStorage obj)
        {
#if !FI_AUTOFILL
            return obj.provider.IsConnected();
#else
            return default(bool);
#endif
        }

        /// <summary>
        /// Set the current API Key
        /// </summary>
        public static void SetAPIKey(this IUsesCloudDataStorage obj, string token)
        {
#if !FI_AUTOFILL
            obj.provider.SetAPIKey(token);
#endif
        }

        /// <summary>
        /// Get the current API Key
        /// </summary>
        public static string GetAPIKey(this IUsesCloudDataStorage obj)
        {
#if !FI_AUTOFILL
            return obj.provider.GetAPIKey();
#else
            return default(string);
#endif
        }

        /// <summary>
        /// Set the current project identifier
        /// </summary>
        public static void SetProjectIdentifier(this IUsesCloudDataStorage obj, string id)
        {
#if !FI_AUTOFILL
            obj.provider.SetProjectIdentifier(id);
#endif
        }

        /// <summary>
        /// Get the current project identifier
        /// </summary>
        public static string GetProjectIdentifier(this IUsesCloudDataStorage obj)
        {
#if !FI_AUTOFILL
            return obj.provider.GetProjectIdentifier();
#else
            return default(string);
#endif
        }

        /// <summary>
        /// Save to the cloud asynchronously the data of an object of a certain type with a specified key
        /// </summary>
        /// <param name="typeName"> string describing the type. </param>
        /// <param name="key"> string that uniquely identifies this instance of the type. </param>
        /// <param name="serializedObject"> string serialization of the object being saved. </param>
        /// <param name="callback"> a callback when the asynchronous call is done to show whether it was successful. </param>
        public static void CloudSaveAsync(this IUsesCloudDataStorage obj, string typeName, string key, string serializedObject, Action<bool> callback)
        {
#if !FI_AUTOFILL
            obj.provider.CloudSaveAsync(typeName, key, serializedObject, callback);
#else
            callback(false);
#endif
        }

        /// <summary>
        /// Load from the cloud asynchronously the data of an object of a certain type which was saved with a known key
        /// </summary>
        /// <param name="typeName"> string describing the type. </param>
        /// <param name="key"> string that uniquely identifies this instance of the type. </param>
        /// <param name="callback">a callback which returns whether the operation was successful, as well as the serialized string of the object if it was. </param>
        public static void CloudLoadAsync(this IUsesCloudDataStorage obj, string typeName, string key, Action<bool, string> callback)
        {
#if !FI_AUTOFILL
            obj.provider.CloudLoadAsync(typeName, key, callback);
#else
            callback(false, "");
#endif
        }
    }
}
