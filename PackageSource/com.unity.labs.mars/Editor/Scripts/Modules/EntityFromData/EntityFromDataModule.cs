using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Creates queries from objects in the simulation view that have components that implement ICreateConditions.
    /// Can be used by dropping an object onto data in simulation view, or by selecting one or multiple objects and choosing the
    /// "Create Entity From Selection" in the gameobject create menu
    /// </summary>
    public class EntityFromDataModule : IModuleDependency<ScenePlacementModule>, IModuleDependency<SimulatedObjectsManager>,
        IUsesFunctionalityInjection, IRequiresTraits<bool>
    {
        internal struct PotentialCondition
        {
            public ICreatesConditionsBase conditionCreator;
            public bool use;
        }

        class FloorConditionCreator : ICreatesConditions
        {
            public Type ConditionType { get { return typeof(SemanticTagCondition); } }
            public string ConditionName { get { return "Semantic Tag"; } }
            public string ValueString { get { return $"\"{TraitNames.Floor}\""; } }
            public int Order { get { return int.MaxValue / 2; } }

            public void CreateIdealConditions(GameObject go)
            {
                var condition = go.AddComponent<SemanticTagCondition>();
                condition.SetTraitName(TraitNames.Floor);
            }

            public void ConformCondition(ICondition condition)
            {
                var tagCondition = condition as SemanticTagCondition;
                if (tagCondition == null)
                    return;

                tagCondition.matchRule = SemanticTagMatchRule.Match;
                tagCondition.SetTraitName(TraitNames.Floor);
            }
        }

        static readonly TraitRequirement[] k_RequiredTraits = { new TraitRequirement(TraitDefinitions.Floor, false) };

        static EntityFromDataModule s_Instance;
        static FloorConditionCreator s_FloorConditionCreator = new FloorConditionCreator();

        List<GameObject> m_DataGameObjects = new List<GameObject>();
        GameObject m_PlacedObject;
        ScenePlacementModule m_ScenePlacementModule;
        SimulatedObjectsManager m_SimulatedObjectsManager;
        CreateEntityFromDataWindow m_Window;

        internal readonly Dictionary<GameObject, List<PotentialCondition>> potentialEntities = new Dictionary<GameObject, List<PotentialCondition>>();

#if !FI_AUTOFILL
        IProvidesFunctionalityInjection IFunctionalitySubscriber<IProvidesFunctionalityInjection>.provider { get; set; }
#endif

        public TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

        public void LoadModule()
        {
            s_Instance = this;
        }

        public void UnloadModule()
        {
            if (m_ScenePlacementModule != null)
                m_ScenePlacementModule.objectDropped -= OnObjectDropped;
        }

        public void ConnectDependency(ScenePlacementModule dependency)
        {
            m_ScenePlacementModule = dependency;
            m_ScenePlacementModule.objectDropped += OnObjectDropped;
        }

        public void ConnectDependency(SimulatedObjectsManager dependency) { m_SimulatedObjectsManager = dependency; }

        void OpenCreateWindow()
        {
            potentialEntities.Clear();
            var simulationIsland = QuerySimulationModule.instance.functionalityIsland;
            foreach (var dataObject in m_DataGameObjects)
            {
                var potentialConditions = new List<PotentialCondition>();

                // Since the floor tag can be added through a reasoning API, we have to check if it exists in the database
                var isFloor = false;
                var synthPlane = dataObject.GetComponent<SynthesizedPlane>();
                if (synthPlane != null)
                {
                    bool value;
                    if (this.TryGetTraitValue(synthPlane.dataID, TraitNames.Floor, out value) && value)
                    {
                        isFloor = true;
                        potentialConditions.Add(new PotentialCondition
                        {
                            conditionCreator = s_FloorConditionCreator,
                            use = true
                        });
                    }
                }

                var conditionCreators = dataObject.GetComponents<ICreatesConditionsBase>();
                foreach (var conditionCreator in conditionCreators)
                {
                    // SynthesizedPose creates an ElevationRelation, which we don't want for the floor
                    var poseCreator = conditionCreator as SynthesizedPose;
                    simulationIsland.InjectFunctionalitySingle(conditionCreator);
                    if (poseCreator != null)
                    {
                        if (isFloor)
                            continue;

                        // Big hack - 0 height elevations don't make sense, this is a floor tag at the moment
                        if (poseCreator.elevationFromFloor <= 0.001f)
                        {
                            isFloor = true;
                            potentialConditions.Add(new PotentialCondition
                            {
                                conditionCreator = s_FloorConditionCreator,
                                use = true
                            });
                            continue;
                        }
                    }

                    potentialConditions.Add(new PotentialCondition
                    {
                        conditionCreator = conditionCreator,
                        use = true
                    });
                }

                if (potentialConditions.Count > 0)
                {
                    potentialConditions.Sort((a, b) =>
                    {
                        var orderCompare = a.conditionCreator.Order.CompareTo(b.conditionCreator.Order);
                        if (orderCompare != 0)
                            return orderCompare;

                        return a.conditionCreator.ConditionName.CompareTo(b.conditionCreator.ConditionName);
                    });

                    potentialEntities.Add(dataObject, potentialConditions);
                }
            }

            if (m_Window == null)
            {
                m_Window = ScriptableObject.CreateInstance<CreateEntityFromDataWindow>();
                m_Window.module = this;
                m_Window.create = CreateEntity;
                m_Window.cancel = CancelCreate;
            }

            m_Window.ShowUtility();
            EditorApplication.delayCall += m_Window.Focus; // Not sure why this doesn't work without delay call
        }

        void CancelCreate()
        {
            if (m_PlacedObject != null)
                UnityObjectUtils.Destroy(m_PlacedObject);
        }

        void CreateEntity()
        {
            var selection = new List<Object>();
            var normalSceneView = SimulationView.NormalSceneView != null ? SimulationView.NormalSceneView : SceneView.lastActiveSceneView;

            foreach (var kvp in potentialEntities)
            {
                var potentialConditions = kvp.Value;
                var menuCommand = new MenuCommand(null);

                var realWorldGO = CreateMenuItems.CreateProxyObject(menuCommand);
                realWorldGO.name = GenerateRealWorldObjectName(kvp.Key, potentialConditions);
                var realWorldObject = realWorldGO.GetComponent<Proxy>();
                realWorldObject.exclusivity = Exclusivity.ReadOnly; // Make it read-only so it will match when simulation restarts
                realWorldGO.transform.rotation = kvp.Key.transform.rotation;
                ProxyGroup set = null;
                var rootEntityGO = realWorldGO;

                // See if we're making a set
                var relations =
                    potentialConditions.Where(condition => condition.use && condition.conditionCreator is ICreatesRelations).ToList();
                if (relations.Count > 0)
                {
                    var setName = GenerateSetName(relations);
                    set = new GameObject(setName).AddComponent<ProxyGroup>();
                    realWorldGO.transform.SetParent(set.transform, false);
                    set.SetChildRequired(realWorldObject, true);
                    rootEntityGO = set.gameObject;
                }

                rootEntityGO.transform.position = normalSceneView.pivot;
                selection.Add(rootEntityGO);

                foreach (var potentialCondition in potentialConditions)
                {
                    try
                    {
                        if (potentialCondition.use)
                        {
                            var conditionCreator = potentialCondition.conditionCreator as ICreatesConditions;
                            if (conditionCreator != null)
                                conditionCreator.CreateIdealConditions(realWorldGO);
                            else
                            {
                                var relationsCreator = potentialCondition.conditionCreator as ICreatesRelations;
                                if (relationsCreator != null)
                                    relationsCreator.CreateIdealRelation(set, realWorldObject);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("{0}\n{1}", e.Message, e.StackTrace);
                    }
                }

                if (set != null)
                {
                    set.RepopulateChildList();
                    if (set.childCount < 2)
                    {
                        // None of the set creators actually created another child, so it doesn't need to be a set
                        realWorldGO.transform.SetParent(null);
                        UnityObjectUtils.Destroy(set.gameObject);
                    }
                    else
                    {
                        set.ApplyHueToChildren();
                    }
                }

                if (m_PlacedObject != null)
                {
                    var relativePos = m_PlacedObject.transform.position - m_PlacedObject.transform.parent.position;
                    m_PlacedObject.transform.SetParent(realWorldGO.transform);
                    m_PlacedObject.transform.position = realWorldGO.transform.position + relativePos;
                    m_PlacedObject = null;
                }

                if (rootEntityGO != null)
                    Undo.RegisterCreatedObjectUndo(rootEntityGO, "Create MARS object");
            }

            m_DataGameObjects.Clear();
            Selection.objects = selection.ToArray();
            EditorGUIUtility.PingObject(Selection.activeGameObject);
            normalSceneView.FrameSelected();
            m_SimulatedObjectsManager.DirtySimulatableScene();
        }

        // Generate a descriptive name of the Real World Object created from synthetic data.
        // We name the generated object after any traits, as "Trait1, trait2, trait3".  If no traits exist, just use the name of the data object.
        static string GenerateRealWorldObjectName(GameObject dataObject, List<PotentialCondition> potentialConditions)
        {
            var traitNames = potentialConditions
                .Where(condition => condition.use && condition.conditionCreator is SynthesizedSemanticTag)
                .Select(synthTag => ((SynthesizedSemanticTag) synthTag.conditionCreator).TraitName)
                .ToList();

            if (traitNames.Count == 0)
                return dataObject.name;

            traitNames[0] = traitNames[0].FirstToUpper();

            return string.Join(", ", traitNames);
        }

        // Generate a descriptive name of the Set created from synthetic data.
        // We name the generated object after the relations, as "Relation1, relation2".
        static string GenerateSetName(List<PotentialCondition> relations)
        {
            var relationNames = new List<string>();
            foreach (var potentialCondition in relations)
            {
                relationNames.Add(potentialCondition.conditionCreator.ConditionName);
            }

            relationNames[0] = relationNames[0].FirstToUpper();

            return string.Join(", ", relationNames);
        }

        void OnObjectDropped(GameObject droppedObject, GameObject target)
        {
            if (target.GetComponentInParent<SimulatedObject>() != null)
            {
                if (m_Window != null)
                    m_Window.Close();

                var marsSession = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
                var createSession = false;
                if (marsSession == null)
                {
                    const string message = "The scene you are editing does not contain a MARS Session. Would you like to add one now?";
                    createSession = EditorUtility.DisplayDialog("No MARS Session", message, "Add MARS Session", "Cancel");

                    if (!createSession)
                    {
                        // Destroy the object and consume the GUI drop event so the editor doesn't try to use the destroyed object
                        UnityObjectUtils.Destroy(droppedObject);
                        Event.current.Use();
                        return;
                    }

                    MARSSession.EnsureSessionInActiveScene();
                    QuerySimulationModule.instance.SimulateOneShot(); // Simulate immediately so that sim data has functionality from new session
                }

                m_PlacedObject = droppedObject;
                m_DataGameObjects.Clear();
                m_DataGameObjects.Add(target);
                OpenCreateWindow();
            }
        }
    }
}
