using System;
using System.Collections.Generic;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Unity.Labs.MARS.Providers
{
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    public class MarkerProviderSettings : ScriptableSettings<MarkerProviderSettings>, ISerializationCallbackReceiver
    {
        /// <summary>
        /// We use this MarkerProvider ScriptableSettings for two purposes:
        /// 1. At build time, to provide a reference to all the XRReferenceImageLibraries we have created
        ///    from MarsMarkerLibraries so that it gets included in the build since it is
        ///    referenced here.
        /// 2. At load time or runtime, we need to provide a mapping from the MarsMarkerLibrary to its
        ///     corresponding XRReferenceImageLibrary, so we recreate the dictionary that was used
        ///     to build this asset.
        /// </summary>

        [SerializeField]
        XRReferenceImageLibrary[] m_XrReferenceImageLibraries = new XRReferenceImageLibrary[0];

        [SerializeField]
        MarsMarkerLibrary[] m_MarsMarkerLibraries = new MarsMarkerLibrary[0];

        internal readonly Dictionary<MarsMarkerLibrary, XRReferenceImageLibrary> MarsToXRLibraryMap = new Dictionary<MarsMarkerLibrary, XRReferenceImageLibrary>();
        readonly Dictionary<XRReferenceImageLibrary, MarsMarkerLibrary> m_XRToMarsLibraryMap = new Dictionary<XRReferenceImageLibrary, MarsMarkerLibrary>();

        public bool TryFind(MarsMarkerLibrary activeMarsLibrary, out XRReferenceImageLibrary xrReferenceImageLibrary)
        {
            return MarsToXRLibraryMap.TryGetValue(activeMarsLibrary, out xrReferenceImageLibrary);
        }

        public bool TryFind(XRReferenceImageLibrary xrLibrary, out MarsMarkerLibrary marsLibrary)
        {
            return m_XRToMarsLibraryMap.TryGetValue(xrLibrary, out marsLibrary);
        }

        public void Add(MarsMarkerLibrary marsLibrary, XRReferenceImageLibrary xrLibrary)
        {
            MarsToXRLibraryMap[marsLibrary] = xrLibrary;
            m_XRToMarsLibraryMap[xrLibrary] = marsLibrary;
        }

        /// <summary>
        /// Implement this method to receive a callback before Unity serializes your object.
        /// </summary>
        public void OnBeforeSerialize()
        {
            var marsLibraryList = new List<MarsMarkerLibrary>();
            var xrLibraryList = new List<XRReferenceImageLibrary>();
            var hasNull = false;
            foreach (var kvp in MarsToXRLibraryMap)
            {
                var marsLibrary = kvp.Key;
                if (marsLibrary == null)
                {
                    Debug.LogWarning("Encountered null MARSMarkerLibrary while saving MarkerProviderSettings. This can happen if a MARSMarkerLibrary asset is deleted without being removed from the mapping.");
                    hasNull = true;
                    continue;
                }

                var xrLibrary = kvp.Value;
                if (marsLibrary == null)
                {
                    Debug.LogWarning("Encountered null XRReferenceImageLibrary while saving MarkerProviderSettings. This can happen if an XRReferenceImageLibrary asset is deleted without being removed from the mapping.");
                    hasNull = true;
                    continue;
                }

                marsLibraryList.Add(marsLibrary);
                xrLibraryList.Add(xrLibrary);
            }

            m_MarsMarkerLibraries = marsLibraryList.ToArray();
            m_XrReferenceImageLibraries = xrLibraryList.ToArray();

            if (hasNull)
            {
                MarsToXRLibraryMap.Clear();
                m_XRToMarsLibraryMap.Clear();
                var length = m_MarsMarkerLibraries.Length;
                for (var i = 0; i < length; i++)
                {
                    var marsLibrary = m_MarsMarkerLibraries[i];
                    var xrLibrary = m_XrReferenceImageLibraries[i];
                    MarsToXRLibraryMap[marsLibrary] = xrLibrary;
                    m_XRToMarsLibraryMap[xrLibrary] = marsLibrary;
                }
            }
        }

        /// <summary>
        /// Implement this method to receive a callback after Unity deserializes your object.
        /// </summary>
        public void OnAfterDeserialize()
        {
            var marsLibraryLength = m_MarsMarkerLibraries.Length;
            var xrLibraryLength = m_XrReferenceImageLibraries.Length;
            if (m_XrReferenceImageLibraries.Length != marsLibraryLength)
                Debug.LogWarning("Length mismatch between MARSMarkerLibraries and XRReferenceImageLibraries. This can happen if the MarkerProviderSettings asset is modified outside of Unity. Missing and mismatched will be removed when MarkerProviderSettings is saved.");

            for (var i = 0; i < marsLibraryLength; i++)
            {
                var marsLibrary = m_MarsMarkerLibraries[i];
                if (marsLibrary == null)
                {
                    Debug.LogWarning("Encountered empty array element when deserializing MARSMarkerLibrary list. This can happen if a MARSMarkerLibrary asset is deleted or its GUID changed outside of Unity.");
                    continue;
                }

                if (i >= xrLibraryLength)
                    break;

                var xrLibrary = m_XrReferenceImageLibraries[i];
                if (xrLibrary == null)
                {
                    Debug.LogWarning("Encountered empty array element when deserializing XRReferenceLibrary list. This can happen if a XRReferenceLibrary asset is deleted or its GUID changed outside of Unity.");
                    continue;
                }

                MarsToXRLibraryMap[marsLibrary] = xrLibrary;
                m_XRToMarsLibraryMap[xrLibrary] = marsLibrary;
            }
        }

        public void Remove(MarsMarkerLibrary marsLibrary)
        {
            if (MarsToXRLibraryMap.TryGetValue(marsLibrary, out var xrLibrary))
            {
                MarsToXRLibraryMap.Remove(marsLibrary);
                m_XRToMarsLibraryMap.Remove(xrLibrary);
            }
        }
    }
}
