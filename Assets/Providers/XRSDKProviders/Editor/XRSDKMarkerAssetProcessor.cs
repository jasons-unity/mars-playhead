using System;
using System.Collections.Generic;
using System.IO;
using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEditor.XR.ARSubsystems;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Unity.Labs.MARS.Providers
{
    public class XRSDKMarkerAssetProcessor : IModuleAssetCallbacks
    {
        static readonly HashSet<XRReferenceImageLibrary> k_DeletedLibraries = new HashSet<XRReferenceImageLibrary>();

        public void LoadModule() { }

        public void UnloadModule() { }

        public void OnWillCreateAsset(string path)
        {
            EditorApplication.delayCall += () =>
            {
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType != typeof(MarsMarkerLibrary))
                    return;

                var marsMarkerLibrary = AssetDatabase.LoadAssetAtPath<MarsMarkerLibrary>(path);
                HandleAssetSave(marsMarkerLibrary);
            };
        }

        static XRReferenceImageLibrary CreateARFoundationMarkerLibrary(MarsMarkerLibrary markerLibrary)
        {
            var xrReferenceImageLibrary = ScriptableObject.CreateInstance<XRReferenceImageLibrary>();
            var count = markerLibrary.Count;
            for (var i = 0; i < count; i++)
            {
                var definition = markerLibrary[i];
                var refImage = AddRefImageFromMarker(xrReferenceImageLibrary, definition);
                markerLibrary.SetGuid(i, refImage.guid);
            }

            var pathName = AssetDatabase.GetAssetPath(markerLibrary);
            xrReferenceImageLibrary.name = Path.GetFileNameWithoutExtension(pathName) + "_arf";
            xrReferenceImageLibrary.hideFlags = HideFlags.NotEditable;
            var filename = xrReferenceImageLibrary.name + Path.GetExtension(pathName);
            var dirName = Path.GetDirectoryName(pathName);
            var assetPathName = Path.Combine(dirName, filename);
            AssetDatabase.CreateAsset(xrReferenceImageLibrary, assetPathName);
            MarkerProviderSettings.instance.Add(markerLibrary, xrReferenceImageLibrary);
            return xrReferenceImageLibrary;
        }

        public string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType != typeof(MarsMarkerLibrary))
                    continue;

                var marsMarkerLibrary = AssetDatabase.LoadAssetAtPath<MarsMarkerLibrary>(path);
                HandleAssetSave(marsMarkerLibrary);
            }

            return paths;
        }

        static void HandleAssetSave(MarsMarkerLibrary marsMarkerLibrary)
        {
            bool needsSave;
            if (MarkerProviderSettings.instance.TryFind(marsMarkerLibrary, out var xrReferenceImageLibrary) && xrReferenceImageLibrary)
            {
                // Find what markers need to added, removed or updated in this library
                needsSave = ContentsSync(marsMarkerLibrary, xrReferenceImageLibrary);
            }
            else
            {
                xrReferenceImageLibrary = CreateARFoundationMarkerLibrary(marsMarkerLibrary);
                needsSave = true;
            }

            if (needsSave)
            {
                EditorUtility.SetDirty(MarkerProviderSettings.instance);
                EditorUtility.SetDirty(xrReferenceImageLibrary);
            }
        }

        static bool ContentsSync(MarsMarkerLibrary marsMarkerLibrary, XRReferenceImageLibrary xrReferenceImageLibrary)
        {
            var needsSave = false;

            // Find all changes and synchronize
            var referenceImages = new Dictionary<Guid, XRReferenceImage>();
            foreach (var xrRefImage in xrReferenceImageLibrary)
            {
                referenceImages.Add(xrRefImage.guid, xrRefImage);
            }

            var markersToAdd = new List<MarsMarkerDefinition>();
            var markersToUpdateDictionary = new Dictionary<MarsMarkerDefinition, XRReferenceImage>();

            foreach (var marsDefinition in marsMarkerLibrary)
            {
                if (referenceImages.ContainsKey(marsDefinition.MarkerId))
                {
                    var refImage = referenceImages[marsDefinition.MarkerId];
                    if (refImage.name != marsDefinition.Label || refImage.specifySize != marsDefinition.SpecifySize ||
                        refImage.size != marsDefinition.Size || refImage.texture != marsDefinition.Texture)
                    {
                        markersToUpdateDictionary.Add(marsDefinition, refImage);
                        needsSave = true;
                    }

                    referenceImages.Remove(marsDefinition.MarkerId);
                }
                else
                {
                    markersToAdd.Add(marsDefinition);
                    needsSave = true;
                }
            }

            // Whatever is left in referenceImages needs to be deleted, since none of the marker definitions match
            needsSave = needsSave || referenceImages.Count > 0;
            foreach (var refImageEntry in referenceImages)
            {
                var index = xrReferenceImageLibrary.indexOf(refImageEntry.Value);
                xrReferenceImageLibrary.RemoveAt(index);
            }

            foreach (var marsMarkerDefinition in markersToAdd)
            {
                var refImage = AddRefImageFromMarker(xrReferenceImageLibrary, marsMarkerDefinition);
                marsMarkerLibrary.SetGuid(marsMarkerLibrary.IndexOf(marsMarkerDefinition), refImage.guid);
            }

            foreach (var dictEntry in markersToUpdateDictionary)
            {
                var index = xrReferenceImageLibrary.indexOf(dictEntry.Value);
                UpdateRefImage(xrReferenceImageLibrary, index, dictEntry.Key);
            }

            return needsSave;
        }

        public AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options)
        {
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (assetType == typeof(MarsMarkerLibrary))
            {
                var marsMarkerLibrary = AssetDatabase.LoadAssetAtPath<MarsMarkerLibrary>(path);
                HandleAssetDelete(marsMarkerLibrary);
            }

            if (assetType == typeof(XRReferenceImageLibrary))
            {
                var xrLibrary = AssetDatabase.LoadAssetAtPath<XRReferenceImageLibrary>(path);
                return HandleAssetDelete(xrLibrary);
            }

            return AssetDeleteResult.DidNotDelete;
        }

        static void HandleAssetDelete(MarsMarkerLibrary markerLibrary)
        {
            if (MarkerProviderSettings.instance.TryFind(markerLibrary, out var xrReferenceImageLibrary))
            {
                var settings = MarkerProviderSettings.instance;
                settings.Remove(markerLibrary);
                var pathName = AssetDatabase.GetAssetPath(xrReferenceImageLibrary);
                AssetDatabase.DeleteAsset(pathName);
                k_DeletedLibraries.Add(xrReferenceImageLibrary);
            }
        }

        static AssetDeleteResult HandleAssetDelete(XRReferenceImageLibrary xrLibrary)
        {
            if (MarkerProviderSettings.instance.TryFind(xrLibrary, out var marsLibrary))
            {
                if (marsLibrary != null)
                    Debug.LogWarning($"You must delete the MarsMarkerLibrary {marsLibrary} before deleting its associated XRReferenceImageLibrary", marsLibrary);

                // Return DidDelete to prevent deleting the asset in this case
                return AssetDeleteResult.DidDelete;
            }

            if (k_DeletedLibraries.Remove(xrLibrary))
                return AssetDeleteResult.DidDelete;

            return AssetDeleteResult.DidNotDelete;
        }

        static XRReferenceImage AddRefImageFromMarker(XRReferenceImageLibrary xrReferenceImageLibrary,
            MarsMarkerDefinition marsMarkerDefinition)
        {
            xrReferenceImageLibrary.Add();
            // Just added entry is last
            var index = xrReferenceImageLibrary.count - 1;
            UpdateRefImage(xrReferenceImageLibrary, index, marsMarkerDefinition);
            return xrReferenceImageLibrary[index];
        }

        static void UpdateRefImage(XRReferenceImageLibrary xrReferenceImageLibrary, int index, MarsMarkerDefinition marsMarkerDefinition)
        {
            xrReferenceImageLibrary.SetName(index, marsMarkerDefinition.Label);
            xrReferenceImageLibrary.SetSpecifySize(index, marsMarkerDefinition.SpecifySize);
            xrReferenceImageLibrary.SetSize(index, marsMarkerDefinition.Size);
            xrReferenceImageLibrary.SetTexture(index, marsMarkerDefinition.Texture, true);
        }
    }
}
