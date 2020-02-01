using System;
using System.Collections;
using Unity.Labs.ModuleLoader;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS
{
    public class MARSAssetModule : MonoBehaviour, IModule
    {
        // Singleton module is a holdover until we can use Addressables
        // This whole class will go away unless we find we need it for other reasons
        static MARSAssetModule instance;

        public void LoadModule()
        {
            instance = this;
        }

        public void UnloadModule()
        {
            instance = null;
        }

        public static void DelayInstantiate<TObject>(TObject original, Action<TObject> complete, float delay = 0.5f)
            where TObject : UnityObject
        {
            instance.StartCoroutine(_DelayInstantiate(original, complete, delay));
        }

        static IEnumerator _DelayInstantiate<TObject>(TObject original, Action<TObject> complete, float delay)
            where TObject : UnityObject
        {
            yield return new WaitForSeconds(delay);
            complete(Instantiate(original));
        }
    }
}
