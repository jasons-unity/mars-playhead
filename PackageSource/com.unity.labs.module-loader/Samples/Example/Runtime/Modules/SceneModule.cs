using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.ModuleLoader.Example
{
    class SceneModule : IModuleBehaviorCallbacks, IModuleDependency<FunctionalityInjectionModule>
    {
        FunctionalityInjectionModule m_FIModule;

        readonly HashSet<Type> m_SubscriberTypes = new HashSet<Type>();
        readonly List<object> m_MonoBehaviourObjects = new List<object>();

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<MonoBehaviour> k_MonoBehaviours = new List<MonoBehaviour>();

        public void LoadModule() { }

        public void UnloadModule() { }

        public void ConnectDependency(FunctionalityInjectionModule dependency) { m_FIModule = dependency; }

        /// <summary>
        /// Gets all MonoBehaviours in the scene, including ones on inactive objects
        /// </summary>
        void CollectAllMonoBehaviors()
        {
            var activeScene = SceneManager.GetActiveScene();
            foreach (var gameObject in activeScene.GetRootGameObjects())
            {
                GetMonoBehaviorsRecursively(gameObject);
            }
        }

        void GetMonoBehaviorsRecursively(GameObject gameObject)
        {
            gameObject.GetComponents(k_MonoBehaviours);
            foreach (var behaviour in k_MonoBehaviours)
            {
                m_MonoBehaviourObjects.Add(behaviour);
                if (behaviour is IFunctionalitySubscriber)
                    m_SubscriberTypes.Add(behaviour.GetType());
            }

            foreach (Transform child in gameObject.transform)
            {
                GetMonoBehaviorsRecursively(child.gameObject);
            }
        }

        public void OnBehaviorAwake()
        {
            CollectAllMonoBehaviors();

            var activeIsland = m_FIModule.activeIsland;
            activeIsland.SetupDefaultProviders(m_SubscriberTypes);
            activeIsland.InjectFunctionality(m_MonoBehaviourObjects);
        }

        public void OnBehaviorEnable() { }
        public void OnBehaviorDestroy() { }
        public void OnBehaviorStart() { }
        public void OnBehaviorUpdate() { }
        public void OnBehaviorDisable() { }
    }
}
