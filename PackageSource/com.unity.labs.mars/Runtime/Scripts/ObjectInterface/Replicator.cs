using System.Collections.Generic;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [MonoBehaviourComponentMenu(typeof(Replicator), "Replicator")]
    public class Replicator : MARSEntity, IUsesFunctionalityInjection
    {
        const string k_TooManyError = "Replicator '{0}' must contain only a single Proxy or Proxy Group as its child!";
        const string k_TooFewError = "Replicator '{0}' does not contain a single Proxy or Proxy Group as its child, and cannot function!";

        const string k_NoReplicatorRecursion = "You cannot replicate a Replicator.";
        const string k_ReplicatorAncestorError = "There is a Replicator ancestor of this Replicator named {0}! " + k_NoReplicatorRecursion;
        const string k_ReplicatorChildError = "There is a Replicator child of this Replicator named {0}! " + k_NoReplicatorRecursion;
        const string k_OnDeckName = "(Next to be replicated)";

#pragma warning disable 649
        [SerializeField]
        [Tooltip("Sets the maximum number of GameObjects that can be spawned. A value of 0 indicates no maximum.")]
        int m_MaxInstances;
#pragma warning restore 649

        [SerializeField]
        [Tooltip("When enabled, each instance will be spawned as a child of this transform")]
        bool m_SpawnAsChild = true;

        int m_InstanceCount = 1;
        QueryMatchID m_MasterQuery;
        QueryMatchID m_CurrentQuery;

        string m_BaseName;

        readonly Dictionary<long, GameObject> m_Spawns = new Dictionary<long, GameObject>();
        readonly List<ISimulatable> m_OriginalSimulatables = new List<ISimulatable>();
        GameObject m_NextSpawn;
        Transform m_FISpawnContainer;

        bool m_SetTarget;
        bool m_ReacquireOnLoss;
        int m_CanAcquireCount;
        Transform m_OriginalTransform;

        static readonly List<Replicator> k_ReplicatorComponents = new List<Replicator>();

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
#endif

        public GameObject currentSpawn { get; private set; }

        /// <summary>
        /// The total number of instances spawned, included ones seeking a match
        /// </summary>
        public int instanceCount { get { return m_InstanceCount; } }

        /// <summary>
        /// The number of spawned instances that are currently matched
        /// </summary>
        public int matchCount { get; private set; }

        // Local method use only -- created here to reduce garbage collection
        static readonly List<IFunctionalitySubscriber> k_ChildSubscribers = new List<IFunctionalitySubscriber>();
        static readonly List<ISimulatable> k_SpawnedSimulatables = new List<ISimulatable>();

        void OnEnable()
        {
            m_Spawns.Clear();
            matchCount = 0;
            m_InstanceCount = 1;

            // Find a compatible template to spawn
            if (!FindInitialSpawn())
            {
                enabled = false;
                return;
            }

            var spawnContainerGO = new GameObject();
            m_FISpawnContainer = spawnContainerGO.transform;
            m_FISpawnContainer.parent = transform;
            m_FISpawnContainer.localPosition = Vector3.zero;
            spawnContainerGO.SetActive(false);
            spawnContainerGO.hideFlags = HideFlags.HideAndDontSave;
            m_FISpawnContainer.hideFlags = HideFlags.HideAndDontSave;

            // Get a QueryID to use for all spawns
            m_MasterQuery = QueryMatchID.Generate();
            m_CurrentQuery = m_MasterQuery;

            // Prepare the next entry to spawn
            InjectFunctionalityOnSpawn(currentSpawn);
            PrepareNewSpawn();
        }

        void OnDisable()
        {
            if (m_FISpawnContainer != null)
                UnityObjectUtils.Destroy(m_FISpawnContainer.gameObject);
        }

        internal GameObject CheckReplicatorSetup(ref string errorMessage)
        {
            // See if we have too many children at this level
            if (transform.childCount > 1)
            {
                errorMessage = string.Format(k_TooManyError, name);
                return null;
            }

            // See if we have too few children at this level
            if (transform.childCount < 1)
            {
                errorMessage = string.Format(k_TooFewError, name);
                return null;
            }

            GetComponentsInParent(true, k_ReplicatorComponents);
            if (k_ReplicatorComponents.Count >= 2)
            {
                // use the name of the first one found that's not on this object
                errorMessage = string.Format(k_ReplicatorAncestorError, k_ReplicatorComponents[1].name);
                return null;
            }

            GetComponentsInChildren(true, k_ReplicatorComponents);
            if (k_ReplicatorComponents.Count >= 2)
            {
                errorMessage = string.Format(k_ReplicatorChildError, k_ReplicatorComponents[1].name);
                return null;
            }

            // Try to get a ProxyGroup
            var potentialSet = GetComponentInChildren<ProxyGroup>();
            if (potentialSet != null)
            {
                if (potentialSet.transform.parent == transform)
                    return potentialSet.gameObject;
            }

            var potentialSpawn = GetComponentInChildren<Proxy>();
            if (potentialSpawn != null)
            {
                if (potentialSpawn.transform.parent == transform)
                    return potentialSpawn.gameObject;
            }

            errorMessage = string.Format(k_TooFewError, name);
            return null;
        }

        bool FindInitialSpawn()
        {
            if (currentSpawn != null)
                return true;

            var errorMessage = string.Empty;
            var potentialSpawn = CheckReplicatorSetup(ref errorMessage);
            if (potentialSpawn == null)
            {
                Debug.LogWarning(errorMessage);
                return false;
            }

            if (potentialSpawn.GetComponent<ProxyGroup>() != null)
                m_SetTarget = true;

            currentSpawn = potentialSpawn;
            m_BaseName = currentSpawn.name;
            m_CanAcquireCount = 1;

            // Setup the initial spawn as the original
            AddSpawnSourceToSimulationManager(currentSpawn);

            return true;
        }

        void PrepareNewSpawn()
        {
            if (currentSpawn == null)
                return;

            m_CurrentQuery = m_CurrentQuery.NextMatch();
            m_Spawns[m_CurrentQuery.matchID] = currentSpawn;

            // If there is still room, duplicate the current entry
            if ((m_MaxInstances <= 0 || m_InstanceCount < m_MaxInstances) && m_NextSpawn == null)
            {
                m_NextSpawn = GameObjectUtils.Instantiate(currentSpawn, m_FISpawnContainer);
                InjectFunctionalityOnSpawn(m_NextSpawn);
                AddSpawnToSimulationManager(m_NextSpawn);
                m_NextSpawn.name = m_BaseName + k_OnDeckName;
                m_NextSpawn.SetActive(false);
                m_NextSpawn.transform.parent = transform;
            }

            // Let the current entry know it can now spawn using the given QueryID
            if (!m_SpawnAsChild)
            {
                currentSpawn.transform.parent = transform.parent;
            }

            // Give it the master query
            // Add a function that performs this query again as an onAcquireEvent
            if (!m_SetTarget)
            {
                var realWorldObject = currentSpawn.GetComponent<Proxy>();
                realWorldObject.Initialize();
                var queryArgs = realWorldObject.PerformQuery(m_CurrentQuery);
                m_ReacquireOnLoss = queryArgs.commonQueryData.reacquireOnLoss;

                queryArgs.onAcquire += OnSingleSpawnAcquired;
                queryArgs.onTimeout += OnSingleSpawnTimeout;
                queryArgs.onLoss += OnSingleSpawnLoss;
            }
            else
            {
                var set = currentSpawn.GetComponent<ProxyGroup>();
                set.Initialize();
                var queryArgs = set.PerformQuery(m_CurrentQuery);
                m_ReacquireOnLoss = queryArgs.commonQueryData.reacquireOnLoss;

                queryArgs.onAcquire += OnSetSpawnAcquired;
                queryArgs.onTimeout += OnSetSpawnTimeout;
                queryArgs.onLoss += OnSetSpawnLoss;
            }
        }

        void OnSingleSpawnAcquired(QueryResult queryData)
        {
            matchCount++;
            m_CanAcquireCount--;

            // Don't prepare a new spawn if there are other spawns that can acquire
            if (m_CanAcquireCount > 0)
                return;

            m_InstanceCount++;
            currentSpawn = m_NextSpawn;
            if (currentSpawn == null)
                return;

            currentSpawn.name = m_BaseName;
            currentSpawn.SetActive(true);
            m_CanAcquireCount++;

            m_NextSpawn = null;
            PrepareNewSpawn();
        }

        void OnSingleSpawnTimeout(QueryArgs queryArgs)
        {
            m_InstanceCount++;

            // Simply try the query again
            PrepareNewSpawn();
        }

        void OnSingleSpawnLoss(QueryResult queryData)
        {
            var matchID = queryData.queryMatchId.matchID;
            GameObject spawn;
            if (!m_Spawns.TryGetValue(matchID, out spawn))
                return;

            if (m_ReacquireOnLoss)
            {
                m_CanAcquireCount++;
            }
            else
            {
                // Destroy spawns that are lost and do not try to reacquire
                UnityObjectUtils.Destroy(spawn);
                m_Spawns.Remove(matchID);
                m_InstanceCount--;
            }

            matchCount--;
        }

        void OnSetSpawnAcquired(SetQueryResult queryData)
        {
            matchCount++;
            m_CanAcquireCount--;

            // Don't prepare a new spawn if there are other spawns that can acquire
            if (m_CanAcquireCount > 0)
                return;

            m_InstanceCount++;
            currentSpawn = m_NextSpawn;
            if (currentSpawn == null)
                return;

            currentSpawn.name = m_BaseName;
            currentSpawn.SetActive(true);
            m_CanAcquireCount++;

            m_NextSpawn = null;
            PrepareNewSpawn();
        }

        void OnSetSpawnTimeout(SetQueryArgs queryData)
        {
            m_InstanceCount++;

            // Simply try the query again
            PrepareNewSpawn();
        }

        void OnSetSpawnLoss(SetQueryResult queryData)
        {
            var matchID = queryData.queryMatchId.matchID;
            GameObject spawn;
            if (!m_Spawns.TryGetValue(matchID, out spawn))
                return;

            if (m_ReacquireOnLoss)
            {
                m_CanAcquireCount++;
            }
            else
            {
                // Destroy spawns that are lost and do not try to reacquire
                UnityObjectUtils.Destroy(spawn);
                m_Spawns.Remove(matchID);
                m_InstanceCount--;
            }

            matchCount--;
        }

        void InjectFunctionalityOnSpawn(GameObject target)
        {
            target.GetComponentsInChildren(true, k_ChildSubscribers);
            foreach (var currentSubscriber in k_ChildSubscribers)
            {
                this.InjectFunctionalitySingle(currentSubscriber);
            }
            k_ChildSubscribers.Clear();
        }

        void AddSpawnSourceToSimulationManager(GameObject spawn)
        {
#if UNITY_EDITOR
            m_OriginalSimulatables.Clear();
            m_OriginalTransform = spawn.transform;
            spawn.GetComponentsInChildren(m_OriginalSimulatables);
#endif
        }

        void AddSpawnToSimulationManager(GameObject spawn)
        {
#if UNITY_EDITOR
            if (EditorOnlyDelegates.AddSpawnedTransformToSimulationManager == null
                || EditorOnlyDelegates.AddSpawnedSimulatableToSimulationManager == null )
                return;

            EditorOnlyDelegates.AddSpawnedTransformToSimulationManager(spawn.transform, m_OriginalTransform);

            spawn.GetComponentsInChildren(k_SpawnedSimulatables);
            for (var i = 0; i < m_OriginalSimulatables.Count; i++)
            {
                var originalSimulatable = m_OriginalSimulatables[i];
                var spawnedSimulatable = k_SpawnedSimulatables[i];

                EditorOnlyDelegates.AddSpawnedSimulatableToSimulationManager(spawnedSimulatable, originalSimulatable);
            }
            k_SpawnedSimulatables.Clear();
#endif
        }
    }
}
