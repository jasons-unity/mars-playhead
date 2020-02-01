using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using Unity.Labs.Utils;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a link between one real-world object and a Unity GameObject
    /// </summary>
    [DisallowMultipleComponent]
    [ComponentTooltip("Represents a link between one real-world object and a Unity GameObject")]
    [MonoBehaviourComponentMenu(typeof(Proxy), "Proxy")]
    public class Proxy : MARSEntity, IUsesQueryResults, IUsesDevQueryResults, IMRObject, IHasEditorColor
    {
        /// <summary>
        /// Holds cached visibility data about a single child object
        /// </summary>
        struct ObjectVisibilityInfo
        {
            public bool initialState;
            public int disableCalls;
        }

        // Local method use only -- created here to reduce garbage collection
        static readonly List<GameObject> k_EnableList = new List<GameObject>();
        static readonly List<GameObject> k_DisableList = new List<GameObject>();
        static readonly List<IRequiresTraits> k_TraitRequirers = new List<IRequiresTraits>();
        static readonly List<TraitRequirement> k_AccumulatedRequirements = new List<TraitRequirement>();

        [SerializeField]
        [Tooltip("A color that will be associated with this object in the editor.")]
        [HideInInspector] // Will be drawn by the custom editor
        Color m_Color;

        [SerializeField]
        [HideInInspector]
        int m_ColorIndex;

        [SerializeField]
        [Tooltip("Behavior around timing and recovery of queries")]
        CommonQueryData m_CommonQueryData;

        [SerializeField]
        [Tooltip("Sets how data used in this query should be reserved")]
        Exclusivity m_Exclusivity = Exclusivity.Reserved;

        // All active conditions on the object
        Conditions m_Conditions;

        // Original state of child objects for activation/deactivation
        readonly Dictionary<GameObject, ObjectVisibilityInfo> m_VisibilityStates = new Dictionary<GameObject, ObjectVisibilityInfo>();
        bool m_VisibilityChanges;

        QueryState m_QueryState = QueryState.Unknown;

        // Whether or not this object makes its own queries or is controlled by a greater construct
        bool m_ControlsQueryLifeCycle = true;

        // All actions the Real World Object will take, categorized by event
        readonly List<IMatchAcquireHandler> m_AcquireHandlers = new List<IMatchAcquireHandler>();
        readonly List<IMatchUpdateHandler> m_UpdateHandlers = new List<IMatchUpdateHandler>();
        readonly List<IMatchLossHandler> m_LossHandlers = new List<IMatchLossHandler>();
        readonly List<IMatchTimeoutHandler> m_TimeoutHandlers = new List<IMatchTimeoutHandler>();
        readonly List<IMatchVisibilityHandler> m_VisibilityHandlers = new List<IMatchVisibilityHandler>();

        /// <summary>
        /// A color that will be associated with this object in the editor.
        /// </summary>
        public Color color
        {
            get { return m_Color; }
            set { m_Color = value; }
        }

        public int colorIndex
        {
            get { return m_ColorIndex; }
            set { m_ColorIndex = value; }
        }

        /// <summary>
        /// What part of the query lifecycle this Real World Object is in
        /// </summary>
        public QueryState queryState { get { return m_QueryState; } }

        /// <summary>
        /// The identifier for this Object's query
        /// </summary>
        public QueryMatchID queryID { get; private set; }

        /// <summary>
        /// Can the data captured by this Real World Object's query be used by another query?
        /// </summary>
        public Exclusivity exclusivity
        {
            get { return m_Exclusivity; }
            set { m_Exclusivity = value; }
        }

        /// <summary>
        /// Data filters for this Object's query
        /// </summary>
        public Conditions conditions
        {
            get
            {
                if (m_Conditions == null)
                    m_Conditions = new Conditions(this);

                return m_Conditions;
            }
        }

        void Awake()
        {
            m_ControlsQueryLifeCycle = GetComponentInParent<ProxyGroup>() == null && GetComponentInParent<Replicator>() == null;
        }

        void OnValidate()
        {
             this.SetNewColorIfDefault();
        }

        void OnEnable()
        {
            if (m_ControlsQueryLifeCycle == false)
                return;

            Initialize();
            PerformQuery(QueryMatchID.NullQuery);
        }

        void OnDisable()
        {
            // Disabling the object releases the query, if it has one
            if (m_QueryState != QueryState.Unavailable && m_QueryState != QueryState.Unknown && !queryID.IsNullQuery())
            {
                this.UnregisterQuery(queryID);
                queryID = QueryMatchID.NullQuery;
            }

            m_QueryState = QueryState.Unknown;
            RestoreChildStates();
            m_VisibilityStates.Clear();
            m_AcquireHandlers.Clear();
            m_UpdateHandlers.Clear();
            m_LossHandlers.Clear();
            m_TimeoutHandlers.Clear();
            m_VisibilityHandlers.Clear();
        }

        void Reset() { m_CommonQueryData.reacquireOnLoss = true; }

        internal void Initialize()
        {
            InitializeWithoutState();
            UpdateQueryState(queryState, null, true);
        }

        internal void InitializeWithoutState()
        {
            // We don't double-query if we're already tracking
            if (m_QueryState != QueryState.Unavailable && m_QueryState != QueryState.Unknown)
            {
                Debug.LogError(string.Format("{0} is already tracking, cannot query again!", name));
                return;
            }

            // Cache all the actions that will be called
            using (var componentFilter = new CachedComponentFilter<IAction, Proxy>(this, includeDisabled: false))
            {
                componentFilter.StoreMatchingComponents(m_AcquireHandlers);
                componentFilter.StoreMatchingComponents(m_UpdateHandlers);
                componentFilter.StoreMatchingComponents(m_LossHandlers);
                componentFilter.StoreMatchingComponents(m_TimeoutHandlers);
                componentFilter.StoreMatchingComponents(m_VisibilityHandlers);
            }
        }

        /// <summary>
        /// Request the MARS scene this proxy is in to be evaluated for matches
        /// </summary>
        /// <param name="onEvaluationComplete">An optional callback, executed when the evaluation process completes</param>
        /// <returns>A description of the system's response to the request</returns>
        public MarsSceneEvaluationRequestResponse RequestEvaluation(Action onEvaluationComplete = null)
        {
            return this.RequestSceneEvaluation(onEvaluationComplete);
        }

        public ContextTraitRequirements GetRequirements()
        {
            k_TraitRequirers.Clear();
            // Cache all the trait requirements for actions
            using (var componentFilter = new CachedComponentFilter<IRequiresTraits, Proxy>(this, includeDisabled: false))
            {
                componentFilter.StoreMatchingComponents(k_TraitRequirers);
            }

            k_AccumulatedRequirements.Clear();
            foreach (var requirer in k_TraitRequirers)
            {
                // Conditions require traits but we only need to gather requirements from actions
                if (requirer is ICondition)
                    continue;

                var individualRequired = requirer.GetRequiredTraits();
                k_AccumulatedRequirements.AddRange(individualRequired);
            }

            return new ContextTraitRequirements(k_AccumulatedRequirements);
        }

        internal QueryArgs PerformQuery(QueryMatchID reservedID)
        {
            if (!queryID.IsNullQuery())
                return null;

            var queryArgs = new QueryArgs
            {
                exclusivity = exclusivity,
                commonQueryData = m_CommonQueryData,
                conditions = conditions,
                traitRequirements = GetRequirements(),
                onAcquire = OnClientMatchAcquire,
                onLoss = OnClientMatchLoss
            };

            if (m_UpdateHandlers.Count > 0)
                queryArgs.onMatchUpdate = OnClientMatchUpdate;
            if (m_CommonQueryData.currentTimeOut > 0)
                queryArgs.onTimeout = OnClientMatchTimeout;

            if (reservedID.IsNullQuery())
                queryID = this.RegisterQuery(queryArgs);
            else
            {
                queryID = reservedID;
                this.RegisterQuery(reservedID, queryArgs);
            }

#if UNITY_EDITOR
            QueryObjectMapping.Map[queryID.queryID] = gameObject;
#endif

            // Return the query args used in case the caller wants to tweak it more
            return queryArgs;
        }

        /// <summary>
        /// Called when a query match has been found.
        /// This also forwards the event to the appropriate event handlers for this client.
        /// </summary>
        /// <param name="queryData">Data associated with this event</param>
        internal void OnClientMatchAcquire(QueryResult queryData)
        {
            UpdateQueryState(QueryState.Acquiring);
            foreach (var handler in m_AcquireHandlers)
            {
#if UNITY_EDITOR
                try
                {
                    handler.OnMatchAcquire(queryData);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
#else
                handler.OnMatchAcquire(queryData);
#endif
            }
            UpdateQueryState(QueryState.Tracking, queryData);
        }

        /// <summary>
        /// Called when a query match's data has updated.
        /// This also forwards the event to the appropriate event handlers for this client.
        /// </summary>
        /// <param name="queryData">Data associated with this event</param>
        internal void OnClientMatchUpdate(QueryResult queryData)
        {
            foreach (var handler in m_UpdateHandlers)
            {
                handler.OnMatchUpdate(queryData);
            }
            UpdateQueryState(queryState, queryData, true);
        }

        /// <summary>
        /// Called when a query match has been lost.
        /// This also forwards the event to the appropriate event handlers for this client.
        /// </summary>
        /// <param name="queryData">Data associated with this event</param>
        internal void OnClientMatchLoss(QueryResult queryData)
        {
            foreach (var handler in m_LossHandlers)
            {
                handler.OnMatchLoss(queryData);
            }

            UpdateQueryState(m_CommonQueryData.reacquireOnLoss ? QueryState.Resuming : QueryState.Unavailable);
        }

        /// <summary>
        /// Called when no query match has been found in time.
        /// This also forwards the event to the appropriate event handlers for this client.
        /// </summary>
        /// <param name="queryArgs">The original query associated with this object</param>
        internal void OnClientMatchTimeout(QueryArgs queryArgs)
        {
            UpdateQueryState(QueryState.Unavailable);

            foreach (var handler in m_TimeoutHandlers)
            {
                handler.OnMatchTimeout(queryArgs);
            }
        }

        internal void UpdateQueryState(QueryState state, QueryResult queryData = null, bool forceUpdate = false)
        {
            if (m_QueryState != state || forceUpdate)
            {
                m_QueryState = state;

                // Go through each active handler
                // Get which objects are having their activation state explicitly changed
                foreach (var handler in m_VisibilityHandlers)
                {
                    try
                    {
                        handler.FilterVisibleObjects(state, queryData, k_EnableList, k_DisableList);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }

                // Update reference counts
                // If object was not in dictionary before, store its initial state
                foreach (var currentObject in k_DisableList)
                {
                    ObjectVisibilityInfo activationData;
                    if (!m_VisibilityStates.TryGetValue(currentObject, out activationData))
                        m_VisibilityStates.Add(currentObject, new ObjectVisibilityInfo { initialState = currentObject.activeSelf, disableCalls = 1 });
                    else
                    {
                        activationData.disableCalls++;
                        m_VisibilityStates[currentObject] = activationData;
                    }
                }
                foreach (var currentObject in k_EnableList)
                {
                    ObjectVisibilityInfo activationData;
                    if (!m_VisibilityStates.TryGetValue(currentObject, out activationData))
                        m_VisibilityStates.Add(currentObject, new ObjectVisibilityInfo { initialState = currentObject.activeSelf, disableCalls = -1 });
                    else
                    {
                        activationData.disableCalls--;
                        m_VisibilityStates[currentObject] = activationData;
                    }
                }

                if (k_EnableList.Count > 0 || k_DisableList.Count > 0)
                    m_VisibilityChanges = true;

                k_EnableList.Clear();
                k_DisableList.Clear();

                UpdateChildStates();

                if (state == QueryState.Unavailable || state == QueryState.Unknown)
                    queryID = QueryMatchID.NullQuery;
            }
        }

        void UpdateChildStates()
        {
            if (m_VisibilityChanges)
            {
                // Go through dictionary and set visibility as needed
                foreach (var currentObjectPair in m_VisibilityStates)
                {
                    var currentObject = currentObjectPair.Key;
                    // Skip destroyed objects
                    if (currentObject == null)
                        continue;

                    var activationData = currentObjectPair.Value;
                    currentObject.SetActive(activationData.disableCalls <= 0 && activationData.initialState);
                }
                m_VisibilityChanges = false;
            }
        }

        void RestoreChildStates()
        {
            foreach (var currentObjectPair in m_VisibilityStates)
            {
                var currentObject = currentObjectPair.Key;
                if (currentObject == null)
                    continue;

                currentObject.SetActive(currentObjectPair.Value.initialState);
            }
        }
    }
}
