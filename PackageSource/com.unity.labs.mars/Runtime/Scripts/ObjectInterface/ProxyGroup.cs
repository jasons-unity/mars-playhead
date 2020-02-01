using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Query;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Unity.Labs.MARS
{
    [DisallowMultipleComponent]
    [ComponentTooltip("Represents a grouping of related proxies to match simultaneously")]
    [MonoBehaviourComponentMenu(typeof(ProxyGroup), "Proxy Group")]
    public class ProxyGroup : MARSEntity, IUsesSetQueryResults, IHasEditorColor
    {
        [Serializable]
        struct SetChildMetaData
        {
            public Proxy arObject;
            public bool required;
        }

        [SerializeField]
        [Tooltip("A color that will be associated with this set in the editor.")]
        [HideInInspector] // Will be drawn by the custom editor
        Color m_Color;

        [SerializeField]
        [HideInInspector]
        int m_ColorIndex;

        [SerializeField]
        [Tooltip("Behavior around timing and recovery of queries")]
        CommonQueryData m_CommonQueryData;

        [SerializeField]
        [HideInInspector]
        [Tooltip("The list of AR objects that are considered part of this set.")]
        List<SetChildMetaData> m_ChildObjects = new List<SetChildMetaData>();

        readonly List<ISetMatchAcquireHandler> m_AcquireHandlers = new List<ISetMatchAcquireHandler>();
        readonly List<ISetMatchUpdateHandler> m_UpdateHandlers = new List<ISetMatchUpdateHandler>();
        readonly List<ISetMatchLossHandler> m_LossHandlers = new List<ISetMatchLossHandler>();
        readonly List<ISetMatchTimeoutHandler> m_TimeoutHandlers = new List<ISetMatchTimeoutHandler>();

        Relations m_Relations;
        Dictionary<IMRObject, SetChildArgs> m_Children = new Dictionary<IMRObject, SetChildArgs>();
        Dictionary<Proxy, QueryArgs> m_ChildrenQueryArgs = new Dictionary<Proxy, QueryArgs>();
        int m_TrackedInstancesCount;
        QueryState m_QueryState = QueryState.Unknown;

        static readonly List<Proxy> k_ChildObjects = new List<Proxy>();
        static readonly List<Relation> k_Relations = new List<Relation>();
        static readonly List<MultiRelationBase> k_MultiRelations = new List<MultiRelationBase>();

        /// <summary>
        /// A color that will be associated with this object in the editor.
        /// </summary>
        public Color color { get { return m_Color; } set { m_Color = value; } }

        public int colorIndex
        {
            get { return m_ColorIndex; }
            set { m_ColorIndex = value; }
        }

        /// <summary>
        /// What part of the query lifecycle this set is in
        /// </summary>
        public QueryState queryState { get { return m_QueryState; } }

        /// <summary>
        /// The identifier for this Object's query
        /// </summary>
        public QueryMatchID queryID { get; private set; }

        /// <summary>
        /// The number of children this set currently has in its list. Call RepopulateChildList before getting this value
        /// to ensure it is most updated.
        /// </summary>
        /// <value> Number of child objects of the set</value>
        public int childCount { get { return m_ChildObjects.Count; } }

        void Reset() { m_CommonQueryData.reacquireOnLoss = true; }

#if UNITY_EDITOR
        public void OnValidate()
        {
            RepopulateChildList();
            this.SetNewColorIfDefault();
        }

        public void GetChildList(List<Proxy> children)
        {
            children.Clear();
            var needRepopulate = false;
            foreach (var currentChild in m_ChildObjects)
            {
                if (currentChild.arObject != null)
                {
                    children.Add(currentChild.arObject);
                }
                else
                {
                    needRepopulate = true;
                    break;
                }
            }

            if (needRepopulate)
            {
                RepopulateChildList();
                GetChildList(children);
            }
        }
#endif

        public void RepopulateChildList()
        {
            k_ChildObjects.Clear();
            GetComponentsInChildren(k_ChildObjects);

            // Remove invalid entries
            var childCounter = 0;
            while (childCounter < m_ChildObjects.Count)
            {
                var currentChild = m_ChildObjects[childCounter];
                if (currentChild.arObject == null)
                {
                    m_ChildObjects.RemoveAt(childCounter);
                }
                else
                {
                    var currentChildGo = currentChild.arObject.gameObject;
                    if (!currentChildGo.transform.IsChildOf(transform))
                    {
                        m_ChildObjects.RemoveAt(childCounter);
                    }
                    else
                    {
                        k_ChildObjects.Remove(currentChild.arObject);
                        childCounter++;
                    }
                }
            }

            // Add new entries
            foreach (var newChild in k_ChildObjects)
            {
                m_ChildObjects.Add(new SetChildMetaData { arObject = newChild, required = false });
            }
            k_ChildObjects.Clear();
        }

        public void SetChildRequired(Proxy child, bool required)
        {
            RepopulateChildList();
            for (var i = 0; i < m_ChildObjects.Count; ++i)
            {
                if (m_ChildObjects[i].arObject == child)
                {
                    m_ChildObjects[i] = new SetChildMetaData { arObject = child, required = required };
                    return;
                }
            }

            Debug.LogWarningFormat("Real World Object '{0}' is not a child of this set.", child.name);
        }

        /// <summary>
        /// Request the MARS scene this proxy group is in to be evaluated for matches
        /// </summary>
        /// <param name="onEvaluationComplete">An optional callback, executed when the evaluation process completes</param>
        /// <returns>A description of the system's response to the request</returns>
        public MarsSceneEvaluationRequestResponse RequestEvaluation(Action onEvaluationComplete = null)
        {
            return this.RequestSceneEvaluation(onEvaluationComplete);
        }

        /// <summary>
        /// Whether or not this object makes its own queries or is controlled by a greater construct
        /// </summary>
        bool controlsQueryLifeCycle
        {
            get
            {
                return GetComponentInParent<Replicator>() == null;
            }
        }

        void OnEnable()
        {
            if (controlsQueryLifeCycle == false)
                return;

            Initialize();
            PerformQuery(QueryMatchID.NullQuery);
        }

        internal void Initialize()
        {
            // We don't double-query if we're already tracking
            if (m_QueryState != QueryState.Unavailable && m_QueryState != QueryState.Unknown)
            {
                Debug.LogError($"{name} is already tracking, cannot query again!");
                return;
            }
            UpdateQueryState(queryState, true);
        }

        static bool MultiRelationChildrenValid(List<MultiRelationBase> multiRelations)
        {
            foreach (var relation in multiRelations)
            {
                IMRObject child1 = null, child2 = null;
                foreach(var iRelation in relation.HostedComponents)
                {
                    if (!(iRelation is SubRelation subRelation))
                        return false;

                    if (child1 == null)
                        child1 = subRelation.child1;
                    if (child2 == null)
                        child2 = subRelation.child2;

                    // all sub-relations in a multi-relation must refer to the same child context objects
                    if (subRelation.child1 != child1 || subRelation.child2 != child2)
                        return false;
                }
            }

            return true;
        }

        internal SetQueryArgs PerformQuery(QueryMatchID reservedID)
        {
            if (!queryID.IsNullQuery())
                return null;

            GetComponentsInChildren(m_AcquireHandlers);
            GetComponentsInChildren(m_UpdateHandlers);
            GetComponentsInChildren(m_LossHandlers);
            GetComponentsInChildren(m_TimeoutHandlers);

#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                RepopulateChildList();
                GetComponents(k_Relations);
                GetComponents(k_MultiRelations);

                if (!MultiRelationChildrenValid(k_MultiRelations))
                    return null;

                foreach (var relation in k_Relations)
                {
                    relation.EnsureChildClients();
                    if (relation.child1 != null && relation.child2 != null)
                        continue;

                    Debug.LogWarning($"{relation.GetType().Name} in Set '{name}' has an invalid child reference");
                    k_Relations.Clear();
                    return null;
                }

                k_Relations.Clear();
            }
#endif

            if (m_ChildObjects.Count < 2)
            {
                Debug.LogWarning($"{name} must have at least 2 child Real World Objects for the Set");
                return null;
            }

            BuildChildArgs(true);

            m_Relations = new Relations(gameObject, m_Children);
            var queryArgs = new SetQueryArgs
            {
                commonQueryData = m_CommonQueryData,
                relations = m_Relations,
                // We always need acquire and loss events, in order to update tracking state
                onAcquire = OnMatchAcquire,
                onLoss = OnMatchLoss
            };

            if (m_UpdateHandlers.Count > 0 || GetComponentInChildren<IMatchUpdateHandler>() != null) // Children could still need regular updates
                queryArgs.onMatchUpdate = OnMatchUpdate;
            if (m_CommonQueryData.currentTimeOut > 0)
                queryArgs.onTimeout = OnMatchTimeout;

            if (reservedID.IsNullQuery())
                queryID = this.RegisterSetQuery(queryArgs);
            else
            {
                queryID = reservedID;
                this.RegisterSetQuery(reservedID, queryArgs);
            }

#if UNITY_EDITOR
            QueryObjectMapping.Sets[queryID.queryID] = gameObject;
#endif
            return queryArgs;
        }

        internal Dictionary<IMRObject, SetChildArgs> BuildChildArgs(bool performQuery = false)
        {
            for (var i = 0; i < m_ChildObjects.Count; ++i)
            {
                var child = m_ChildObjects[i].arObject;
                if (child == null)
                {
                    Debug.LogWarning($"{name} is Null at child object {i} for the Set");
                    return null;
                }

                if(performQuery)
                    child.Initialize();
                else
                    child.InitializeWithoutState();

                var required = m_ChildObjects[i].required;
                m_Children[child] = new SetChildArgs(child.conditions, child.exclusivity, required, child.GetRequirements());

                m_ChildrenQueryArgs[child] = new QueryArgs
                {
                    exclusivity = child.exclusivity,
                    commonQueryData = m_CommonQueryData,
                    conditions = child.conditions
                };
            }

            return m_Children;
        }

        void OnDisable()
        {
            if (!queryID.IsNullQuery())
            {
                this.UnregisterSetQuery(queryID, true);
                queryID = QueryMatchID.NullQuery;
                m_QueryState = QueryState.Unavailable;  // We do this manually so we do not accidentally toggle all the active states off
            }

            m_AcquireHandlers.Clear();
            m_UpdateHandlers.Clear();
            m_LossHandlers.Clear();
            m_TimeoutHandlers.Clear();

#if UNITY_EDITOR
            QueryObjectMapping.Sets.Remove(queryID.queryID);
#endif
        }

        /// <summary>
        /// Called when a query match has been found.
        /// This also forwards the event to the appropriate event handlers for this client.
        /// </summary>
        /// <param name="queryData">Data associated with this event</param>
        void OnMatchAcquire(SetQueryResult queryData)
        {
            m_TrackedInstancesCount++;
            UpdateQueryState(QueryState.Tracking);

            foreach (var childData in m_ChildObjects)
            {
                var child = childData.arObject;
                child.OnClientMatchAcquire(queryData.childResults[child]);
            }

            foreach (var handler in m_AcquireHandlers)
            {
#if UNITY_EDITOR
                try
                {
#endif
                    handler.OnSetMatchAcquire(queryData);
#if UNITY_EDITOR
                }
                catch (Exception e)
                {
                    Debug.LogError("Caught exception in Set: " + e.Message + "\n" + e.StackTrace);
                }
#endif
            }
        }

        /// <summary>
        /// Called when a query match's data has updated.
        /// This also forwards the event to the appropriate event handlers for this client.
        /// If any non-required children have been lost in this update, they will receive loss events.
        /// </summary>
        /// <param name="queryData">Data associated with this event</param>
        void OnMatchUpdate(SetQueryResult queryData)
        {
            var nonRequiredChildrenLost = queryData.nonRequiredChildrenLost;
            foreach (var childData in m_ChildObjects)
            {
                var child = childData.arObject;
                // Non-required children could be missing from child results if they were previously lost.
                QueryResult childResult;
                if (queryData.childResults.TryGetValue(child, out childResult))
                {
                    if (nonRequiredChildrenLost.Contains(child))
                        child.OnClientMatchLoss(childResult);
                    else
                        child.OnClientMatchUpdate(childResult);
                }
            }

            foreach (var handler in m_UpdateHandlers)
            {
                handler.OnSetMatchUpdate(queryData);
            }
        }

        /// <summary>
        /// Called when a query match has been lost.
        /// This also forwards the event to the appropriate event handlers for this client.
        /// </summary>
        /// <param name="queryData">Data associated with this event</param>
        void OnMatchLoss(SetQueryResult queryData)
        {
            m_TrackedInstancesCount--;
            if (m_TrackedInstancesCount == 0)
                UpdateQueryState(m_CommonQueryData.reacquireOnLoss ? QueryState.Unknown : QueryState.Unavailable);

            foreach (var childData in m_ChildObjects)
            {
                var child = childData.arObject;
                // Non-required children could be missing from child results if they were previously lost.
                QueryResult childResult;
                if (queryData.childResults.TryGetValue(child, out childResult))
                    child.OnClientMatchLoss(childResult);
            }

            foreach (var handler in m_LossHandlers)
            {
                handler.OnSetMatchLoss(queryData);
            }
        }

        /// <summary>
        /// Called when no query match has been found in time.
        /// This also forwards the event to the appropriate event handlers for this client.
        /// </summary>
        /// <param name="queryArgs">The original query associated with this object</param>
        void OnMatchTimeout(SetQueryArgs queryArgs)
        {
            UpdateQueryState(QueryState.Unavailable);

            foreach (var childData in m_ChildObjects)
            {
                var child = childData.arObject;
                child.OnClientMatchTimeout(m_ChildrenQueryArgs[child]);
            }

            foreach (var handler in m_TimeoutHandlers)
            {
                handler.OnSetMatchTimeout(queryArgs);
            }
        }

        internal void UpdateQueryState(QueryState state, bool forceUpdate = false)
        {
            if (m_QueryState != state || forceUpdate)
            {
                m_QueryState = state;
            }
        }
    }
}
