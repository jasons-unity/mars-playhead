#if INCLUDE_ADDRESSABLES
using AddressableAssets;
#endif

using System;
using Unity.Labs.Utils;
using UnityObject = UnityEngine.Object;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Holds a MARS Asset Reference
    /// Note: this code is largely copied from AddressableAssets and should remain as close to the original as possible
    /// https://github.com/Unity-Technologies/AddressableAssets/blob/master/Runtime/AssetReference.cs
    /// </summary>
    [Serializable]
    public class MARSAssetReference
#if INCLUDE_ADDRESSABLES
        : AssetReference
#elif UNITY_EDITOR
        : ISerializationCallbackReceiver
#endif
    {
        const string k_LoadingFormat = "{0} (Loading)";

#pragma warning disable 649
#if !INCLUDE_ADDRESSABLES
        [SerializeField]
        public string assetType;
        /// <summary>
        /// TODO - doc
        /// </summary>
        [SerializeField]
        public string assetGUID;
        [SerializeField]
        internal UnityObject _cachedAsset;
#endif

        [SerializeField]
        Bounds m_ProxyBounds;
#pragma warning restore 649

#if !INCLUDE_ADDRESSABLES
        public override string ToString()
        {
            return "[" + assetGUID + "]" + _cachedAsset;
        }

#if UNITY_EDITOR
        public void OnBeforeSerialize()
        {
            try
            {
                assetType = UnityEditor.AssetDatabase.GetMainAssetTypeAtPath(UnityEditor.AssetDatabase.GUIDToAssetPath(assetGUID)).FullName;
            }
            catch (System.Exception)
            {
                //   assetType = string.Empty;
            }

            // TODO: Ensure _cachedAsset exists
            var go = _cachedAsset as GameObject;
            if (go)
                m_ProxyBounds = BoundsUtils.GetBounds(go.transform);
        }

        public void OnAfterDeserialize()
        {

        }
#endif
#endif

        public TObject Instantiate<TObject>(Action<TObject> complete)
            where TObject : UnityObject
        {
#if INCLUDE_ADDRESSABLES
            InstantiateAsync<TObject>().completed += obj =>
            {
                complete(obj.result);
            };
#else
            MARSAssetModule.DelayInstantiate((TObject)_cachedAsset, complete);
#endif
            return default(TObject); // Default implementation has no proxy
        }

        public GameObject Instantiate(Action<GameObject> complete)
        {
#if INCLUDE_ADDRESSABLES
            var assetName = string.Format(k_LoadingFormat, assetGUID);
            InstantiateAsync<GameObject>().completed += obj =>
            {
                complete(obj.result);
            };
#else
            var assetName = string.Format(k_LoadingFormat, _cachedAsset.name);
            MARSAssetModule.DelayInstantiate((GameObject)_cachedAsset, complete);
#endif
            var proxyCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var parent = new GameObject(); // To account for local offset
            parent.name = assetName;
            var transform = proxyCube.transform;
            transform.parent = parent.transform;
            transform.localPosition = m_ProxyBounds.center;
            transform.localScale = m_ProxyBounds.size;
            return parent;
        }
    }
}
