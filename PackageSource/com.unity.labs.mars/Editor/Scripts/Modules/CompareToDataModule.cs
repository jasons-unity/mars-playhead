using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Compares data chosen in the simulation view to conditions and allows conforming the conditions to match.
    /// </summary>
    public class CompareToDataModule : IModuleDependency<SimulationSceneModule>, IModuleDependency<SimulatedObjectsManager>
    {
        const string k_AreYouSureString = "Are you sure you want to conform all conditions to match the selected data?";
        const string k_ConformConditionActionString = "Modify Condition";
        const string k_ConfirmActionString = "Do it!";
        const string k_CancelActionString = "Cancel";
        const string k_NoCompareStringFormat = "No \'{0}\' data to compare.";
        const string k_ClickOnDataHintString = "Click on data in simulation view to compare against the conditions.";
        const int k_RaycastHitCount = 32;

        static readonly GUIContent k_StopComparingButtonContent = new GUIContent("Stop Comparing", "Stop comparing to simulated data.");
        static readonly GUIContent k_StartComparingButtonContent = new GUIContent("Compare in Simulation View", "Select simulated data to compare it with this entity's conditions.");
        static readonly GUIContent k_ConformAllButtonContent = new GUIContent("Update All", "Modify all conditions to match the simulated data.");
        static readonly Color k_ConditionFailsTextColor = Color.red;
        static readonly GUIContent k_ConformButtonContent = new GUIContent("Update");
        static readonly RaycastHit[] k_RaycastHits = new RaycastHit[k_RaycastHitCount];

        static GUIStyle s_BoxStyle;
        static GUIStyle s_DataLabelStyle;
        static CompareToDataModule s_Instance;
        static SimulationSceneModule m_SimulationSceneModule;
        static SimulatedObjectsManager m_SimulatedObjectsManager;

        Dictionary<string, SynthesizedTrait> m_ConditionTraitToSynthData = new Dictionary<string, SynthesizedTrait>();
        Editor m_CurrentEditor;
        bool m_Comparing;
        bool m_PickingCompare;
        InteractionTarget m_HoveringInteractionTarget;
        InteractionTarget m_SelectedInteractionTarget;
        SimulatedObject m_SimulatedDataObject;

        /// <summary>
        /// Get whether the module is currently comparing data
        /// </summary>
        public static bool IsComparing
        {
            get
            {
                return s_Instance != null && s_Instance.m_Comparing;
            }
        }

        public void ConnectDependency(SimulationSceneModule dependency) { m_SimulationSceneModule = dependency; }

        public void ConnectDependency(SimulatedObjectsManager dependency) { m_SimulatedObjectsManager = dependency; }

        public void LoadModule()
        {
            s_Instance = this;
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
        }

        public void UnloadModule()
        {
            MARSEntityEditor.drawAfterComponent -= DrawAfterComponent;
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        void OnSelectionChanged()
        {
            StopComparing();
        }

        /// <summary>
        /// Draws the comparison UI if needed for the provided component inspector
        /// </summary>
        /// <param name="componentInspector">The component inspector that was just drawn.</param>
        public static void DrawAfterComponent(ComponentInspector componentInspector)
        {
            if (s_Instance == null || !s_Instance.m_Comparing)
                return;

            var condition = componentInspector.target as Condition;
            if (condition == null || condition.traitName == null || !condition.enabled)
                return;

            // Ignore dependency conditions because we can't simply test if they pass from a single synth trait
            if (condition is DependencyCondition)
                return;

            // If sim scene has been closed but still drawing components, stop comparing
            if (!m_SimulationSceneModule.IsSimulationReady)
            {
                s_Instance.StopComparing();
                return;
            }

            s_Instance.DrawCompare(condition);
        }

        void DrawCompare(Condition condition)
        {
            var labelText = string.Format(k_NoCompareStringFormat, condition.traitName);
            CreateGUIStylesIfNeeded();

            SynthesizedTrait synthTrait;
            ICreatesConditions iCreateConditions = null;
            var traitPassesCondition = false;
            var tagCondition = condition as SemanticTagCondition;
            if (m_ConditionTraitToSynthData.TryGetValue(condition.traitName, out synthTrait) && synthTrait != null)
            {
                traitPassesCondition = condition.CheckTraitPasses(synthTrait);
                iCreateConditions = synthTrait as ICreatesConditions;
                if (iCreateConditions != null && iCreateConditions.ConditionType.IsInstanceOfType(condition))
                {
                    labelText = iCreateConditions.ValueString;
                }
            }
            else if (tagCondition != null)
            {
                // If this an exclude tag condition for a trait that doesn't exist, consider that a pass
                if (tagCondition.matchRule == SemanticTagMatchRule.Exclude)
                    traitPassesCondition = true;
            }

            using (new EditorGUILayout.HorizontalScope(s_BoxStyle))
            {
                if (!traitPassesCondition)
                    s_DataLabelStyle.normal.textColor = k_ConditionFailsTextColor;
                else
                    s_DataLabelStyle.normal.textColor = GUI.skin.label.normal.textColor;

                var formattedLabelText = labelText;
                if (m_SimulatedDataObject != null)
                {
                    if (tagCondition != null)
                    {
                        var format = synthTrait != null ? "Selected is tagged {0}" : "Selected is not tagged {0}";
                        formattedLabelText = string.Format(format, condition.traitName);
                    }
                    else
                    {
                        formattedLabelText = string.Format("Selected {0} is {1}", condition.traitName, labelText);
                    }
                }
                GUILayout.Label(formattedLabelText, s_DataLabelStyle);

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(traitPassesCondition || m_SimulatedDataObject == null))
                {
                    if (GUILayout.Button(k_ConformButtonContent, MARSEditorGUI.Styles.MiniFontButton))
                    {
                        Undo.RecordObject(condition, k_ConformConditionActionString);
                        if (iCreateConditions != null)
                            iCreateConditions.ConformCondition(condition);
                        else
                            condition.enabled = false;
                    }
                }
            }
        }

        static void CreateGUIStylesIfNeeded()
        {
            if (s_DataLabelStyle == null)
                s_DataLabelStyle = new GUIStyle(EditorStyles.miniLabel);

            if (s_BoxStyle == null)
                s_BoxStyle = new GUIStyle("box");
        }

        void UpdateSimulatedData()
        {
            m_ConditionTraitToSynthData.Clear();

            if (m_SimulatedDataObject == null)
                return;

            foreach (var synthTrait in m_SimulatedDataObject.traits)
            {
                m_ConditionTraitToSynthData.Add(synthTrait.TraitName, synthTrait);
            }
        }

        /// <summary>
        /// Gets the current data to compare with for a particular trait
        /// </summary>
        /// <param name="traitName"> Name of the trait</param>
        /// <param name="result">The result that is found, will be the default value of T if not found</param>
        /// <typeparam name="T">Type of the data that the trait uses</typeparam>
        /// <returns>True if there is data for this trait to compare with</returns>
        public static bool TryGetCurrentDataForTrait<T>(string traitName, out T result)
        {
            SynthesizedTrait synthTrait;
            if (s_Instance != null && s_Instance.m_ConditionTraitToSynthData.TryGetValue(traitName, out synthTrait)
                && synthTrait != null
                && synthTrait is SynthesizedTrait<T>)
            {
                result = ((SynthesizedTrait<T>)synthTrait).GetTraitData();
                return true;
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Draws the controls for controlling the comparison module for a particular editor
        /// </summary>
        /// <param name="editor">The editor that the controls are being draw inside.</param>
        public static void DrawControls(Editor editor)
        {
            // Comparing only works with a single data object, so don't show the compare controls unless this is a RealWorldObject
            if (((MARSEntity)editor.target).GetComponent<Proxy>() == null)
                return;

            CreateGUIStylesIfNeeded();

            GUILayout.Space(6f);
            s_Instance.m_CurrentEditor = editor;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Compare Mode", MARSEditorGUI.Styles.LabelLeftAligned);

                if (s_Instance.m_Comparing)
                {
                    if (GUILayout.Button(k_ConformAllButtonContent, MARSEditorGUI.Styles.MiniFontButton))
                        s_Instance.ConformAll();

                    if (GUILayout.Button(k_StopComparingButtonContent, MARSEditorGUI.Styles.MiniFontButton))
                            s_Instance.StopComparing();

                }
                else
                {
                    if (GUILayout.Button(k_StartComparingButtonContent, MARSEditorGUI.Styles.MiniFontButton))
                        s_Instance.StartComparing();
                }
            }

            if (s_Instance.m_PickingCompare)
            {
                EditorGUILayout.HelpBox(k_ClickOnDataHintString, MessageType.Info);
            }
        }

        void ConformAll()
        {
            var entity = (MARSEntity)m_CurrentEditor.target;
            if (!EditorUtility.DisplayDialog(k_ConformConditionActionString, k_AreYouSureString, k_ConfirmActionString, k_CancelActionString))
                return;

            foreach (var condition in entity.GetComponents<Condition>())
            {
                if (condition == null || condition.traitName == null || !condition.enabled)
                    continue;

                SynthesizedTrait synthTrait;
                if (!m_ConditionTraitToSynthData.TryGetValue(condition.traitName, out synthTrait) || synthTrait == null)
                {
                    // If there is match tag condition for a tag that does not exist on the data, conform by disabling it
                    var tagCondition = condition as SemanticTagCondition;
                    if (tagCondition != null && tagCondition.matchRule == SemanticTagMatchRule.Match)
                        tagCondition.enabled = false;

                    continue;
                }

                var iCreateConditions = synthTrait as ICreatesConditions;
                if (iCreateConditions == null)
                    continue;

                var traitPassesCondition = condition.CheckTraitPasses(synthTrait);
                if (!traitPassesCondition)
                {
                    Undo.RecordObject(condition, k_ConformConditionActionString);
                    iCreateConditions.ConformCondition(condition);
                }
            }

            m_SimulatedObjectsManager.DirtySimulatableScene();
            Undo.IncrementCurrentGroup();
        }

        void StartComparing()
        {
            m_Comparing = true;
            m_PickingCompare = true;
            m_SimulatedDataObject = null;
            MARSEntityEditor.drawAfterComponent += DrawAfterComponent;
        }

        void StopComparing()
        {
            if (m_SelectedInteractionTarget != null)
                m_SelectedInteractionTarget.SetSelected(false);

            m_Comparing = false;
            m_PickingCompare = false;
            m_ConditionTraitToSynthData.Clear();
            MARSEntityEditor.drawAfterComponent -= DrawAfterComponent;
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (EditorWindow.mouseOverWindow != sceneView || !(sceneView is SimulationView))
                return;

            if (m_PickingCompare)
            {
                EditorGUIUtility.AddCursorRect(new Rect(Vector2.zero, sceneView.position.size), MouseCursor.Zoom);

                InteractionTarget closestInteractionTarget = null;
                SimulatedObject closestSimulatedObject = null;
                var closestDistance = float.MaxValue;

                var currentSceneCamera = sceneView.camera;
                var farClip = currentSceneCamera.farClipPlane;
                var physicsScene = currentSceneCamera.scene.IsValid() ? currentSceneCamera.scene.GetPhysicsScene()
                    : Physics.defaultPhysicsScene;

                var ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

                var hitCount = physicsScene.Raycast(ray.origin, ray.direction, k_RaycastHits, farClip);
                var simDataFound = false;
                for (var i = 0; i < hitCount; i++)
                {
                    var hit = k_RaycastHits[i];
                    var interactionTarget = hit.collider.gameObject.GetComponent<InteractionTarget>();
                    if (interactionTarget == null)
                        continue;

                    var simObject = interactionTarget.gameObject.GetComponent<SimulatedObject>();
                    if (simObject == null)
                        continue;

                    if (hit.distance < closestDistance)
                    {
                        closestInteractionTarget = interactionTarget;
                        closestSimulatedObject = simObject;
                        closestDistance = hit.distance;
                        simDataFound = true;
                    }
                }

                if (!simDataFound || closestInteractionTarget != m_HoveringInteractionTarget)
                {
                    if (m_HoveringInteractionTarget != null)
                        m_HoveringInteractionTarget.SetHovered(false);

                    m_HoveringInteractionTarget = closestInteractionTarget;

                    if (simDataFound && m_HoveringInteractionTarget != null)
                        m_HoveringInteractionTarget.SetHovered(true);
                }

                if (Event.current.type == EventType.MouseDown)
                {
                    m_PickingCompare = false;
                    if (simDataFound)
                    {
                        m_SelectedInteractionTarget = m_HoveringInteractionTarget;
                        m_SelectedInteractionTarget.SetHovered(false);
                        m_SelectedInteractionTarget.SetSelected(true);
                    }
                    else
                    {
                        StopComparing();
                    }
                }

                EditorUtils.ConsumeMouseInput();

                if (simDataFound)
                {
                    if (closestSimulatedObject != m_SimulatedDataObject)
                    {
                        m_SimulatedDataObject = closestSimulatedObject;
                        if (m_CurrentEditor)
                            m_CurrentEditor.Repaint();

                        UpdateSimulatedData();
                    }
                }
            }
            else if (m_HoveringInteractionTarget != null)
            {
                m_HoveringInteractionTarget.SetHovered(false);
            }
        }
    }
}
