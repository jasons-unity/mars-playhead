using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
#endif

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a situation that depends on some other MARSEntity
    /// </summary>
    [ExecuteInEditMode]
    public abstract class DependencyCondition : Condition
    {
        const string k_MissingDependencyErrorFormat = "{0} on {1} is has no dependency assigned, and will never be fulfilled.";

        [SerializeField]
        [Tooltip("Specifies the real world object that this condition depends on before it can be fulfilled")]
        Proxy m_Dependency;

        [SerializeField]
        [Tooltip("When enabled, an explicit dependency reference is used.")]
        protected bool m_UseExplicitDependency;

        public virtual bool useExplicitDependency { get { return m_UseExplicitDependency; } }

        /// <summary>
        /// The real world object that this condition depends on before it can be fulfilled
        /// </summary>
        public Proxy dependency
        {
            get { return m_Dependency; }
            set { m_Dependency = value; }
        }

        /// <summary>
        /// Whether the dependency's Game Object is active and its client (if it has one) is currently tracking any real world data
        /// </summary>
        public bool dependencySatisfied
        {
            get
            {
                if (dependency == null)
                    return false;

                return dependency.gameObject.activeInHierarchy && dependency.queryState == QueryState.Tracking;
            }
        }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly HashSet<Proxy> k_DependenciesFromEntity = new HashSet<Proxy>();

        protected virtual void Awake()
        {
            if (dependency == null && Application.isPlaying)
                Debug.LogErrorFormat(this, k_MissingDependencyErrorFormat, GetType().Name, name);
        }

        /// <summary>
        /// Create a specified dependency condition on baseObject, dependent on dependencyEntity
        /// </summary>
        /// <param name="dependencyType">The specific DependencyCondition to create</param>
        /// <param name="baseObject">The realWorldObject to add the dependency component to</param>
        /// <param name="dependencyEntity">The realWorldObject this conditions depends on</param>
        public static void CreateDependency(Type dependencyType, Proxy baseObject, Proxy dependencyEntity)
        {
            k_DependenciesFromEntity.Clear();
            GatherDependenciesRecursively(dependencyEntity.gameObject);
            if (!k_DependenciesFromEntity.Contains(baseObject.GetComponent<Proxy>()))
            {
#if UNITY_EDITOR
                var dependencyCondition = (DependencyCondition) Undo.AddComponent(baseObject.gameObject, dependencyType);
                dependencyCondition.adjusting = true;
                dependencyCondition.m_UseExplicitDependency = true;
                dependencyCondition.dependency = dependencyEntity;
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
#endif
            }
            else
            {
                Debug.LogError("Attempted to create a circular dependency, which is not supported.");
            }
        }

        static void GatherDependenciesRecursively(GameObject go)
        {
            foreach (var dependency in go.GetComponents<DependencyCondition>())
            {
                if (k_DependenciesFromEntity.Add(dependency.dependency))
                {
                    GatherDependenciesRecursively(dependency.dependency.gameObject);
                }
            }
        }

        public override bool CheckTraitPasses(SynthesizedTrait trait) { return false; }
    }
}
