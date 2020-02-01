using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [RequireComponent(typeof(ProxyGroup))]
    public abstract class RelationBase : ConditionBase
    {
        [SerializeField]
        protected Proxy m_Child1;

        [SerializeField]
        protected Proxy m_Child2;

        ProxyGroup m_ProxyGroup;

        public IMRObject child1 { get { return m_Child1; } }
        public IMRObject child2 { get { return m_Child2; } }
        public Transform child1Transform { get { return m_Child1 == null ? null : m_Child1.transform; } }
        public Transform child2Transform { get { return m_Child2 == null ? null : m_Child2.transform; } }

        public Proxy child1Proxy
        {
            get { return m_Child1; }
            set { m_Child1 = value; }
        }

        public Proxy child2Proxy
        {
            get { return m_Child2; }
            set { m_Child2 = value; }
        }

        public ProxyGroup proxyGroup
        {
            get
            {
                if (m_ProxyGroup == null)
                    m_ProxyGroup = GetComponent<ProxyGroup>();

                return m_ProxyGroup;
            }
        }

        // Local method use only -- created here to reduce garbage collection
        static readonly List<Proxy> k_ChildObjects = new List<Proxy>();

#if UNITY_EDITOR
        public virtual void Reset()
        {
            proxyGroup.GetChildList(k_ChildObjects);
            if (k_ChildObjects.Count < 2)
                return;

            m_Child1 = k_ChildObjects[0];
            m_Child2 = k_ChildObjects[1];
            k_ChildObjects.Clear();
        }

        public override void OnValidate()
        {
            base.OnValidate();
            EnsureChildClients();
        }

        public void EnsureChildClients()
        {
            if (m_Child1 != null && !m_Child1.gameObject.transform.IsChildOf(transform))
                m_Child1 = null;

            if (m_Child2 != null && !m_Child2.gameObject.transform.IsChildOf(transform))
                m_Child2 = null;
        }
#endif
    }

    public abstract class Relation : RelationBase, IRelation
    {
        public string child1TraitName
        {
            get
            {
                var traits = GetRequiredTraits();
                if (traits != null && traits.Length == 2)
                    return traits[0].TraitName;

                LogRequiredTraitsError();
                return "";
            }
        }

        public string child2TraitName
        {
            get
            {
                var traits = GetRequiredTraits();
                if (traits != null && traits.Length == 2)
                    return traits[1].TraitName;

                LogRequiredTraitsError();
                return "";
            }
        }

        public abstract TraitRequirement[] GetRequiredTraits();

        void LogRequiredTraitsError()
        {
            const string requiredTraitsFieldName = IRequiresTraitsMethods.StaticRequiredTraitsFieldName;
            Debug.LogError(
                $"{GetType().Name}.GetRequiredTraits() must return exactly two TraitRequirements - the first " +
                "defines the trait for the Relation's first child, and the second defines the trait for the second child. " +
                $"Your implementation of GetRequiredTraits() must return the field {requiredTraitsFieldName}. " +
                "ConditionsAnalyzer should check that this field contains exactly two TraitRequirements.");
        }
    }

    public abstract class Relation<T> : Relation, IRelation<T>
    {
        public abstract float RateDataMatch(ref T child1Data, ref T child2Data);
    }

    /// <summary>
    /// Template for a relation between two proxies that uses more than one trait per proxy
    /// </summary>
    /// <typeparam name="T1">The type containing the first child's traits</typeparam>
    /// <typeparam name="T2">The type containing the second child's traits</typeparam>
    public abstract class Relation<T1, T2> : RelationBase, IRelation<T1, T2>
        where T1 : struct, IRelationChildValues
        where T2 : struct, IRelationChildValues
    {
        public abstract string[] Child1TraitNames { get; }
        public abstract string[] Child2TraitNames { get; }

        public abstract float RateDataMatch(ref T1 child1, ref T2 child2);

#if UNITY_EDITOR
        public override void OnValidate()
        {
            this.ValidateTraitNames<T1, T2>();
        }
#endif
    }
}
