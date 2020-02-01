﻿using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEditor;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Module that manages the toolbar for editing conditions and drawing GUI in the scene view.
    /// </summary>
    public class MARSEntityEditorModule : IModule
    {
        public class ConditionTypeData
        {
            internal readonly Dictionary<MARSEntity, HashSet<ConditionBase>> selectedConditions =
                new Dictionary<MARSEntity, HashSet<ConditionBase>>();
            internal bool groupAdjusting;
            internal bool hasConditions;
            internal ConditionIconData iconData;
        }

        static MARSEntityEditorModule s_Instance;

        readonly Dictionary<Type, ConditionTypeData> m_ConditionData = new Dictionary<Type, ConditionTypeData>();
        readonly HashSet<MARSEntity> m_SelectedEntities = new HashSet<MARSEntity>();
        readonly HashSet<ConditionBase> m_SelectedConditions = new HashSet<ConditionBase>();
        bool m_HasActiveCondition;
        bool m_TransformGizmoHidden;
        Tool m_CachedTool;

        public static MARSEntityEditorModule instance { get { return s_Instance; } }
        public Dictionary<Type, ConditionTypeData> conditionData { get { return m_ConditionData; } }
        public bool hasActiveCondition { get { return m_HasActiveCondition; } }
        public int selectedEntityCount { get { return m_SelectedEntities.Count; } }
        public bool adjustingInSimView { get; private set; }

        public void LoadModule()
        {
            s_Instance = this;
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += UpdateConditionData;
            // We need to re-cache values for selection after since they can be lost after assembly reload.
            AssemblyReloadEvents.afterAssemblyReload += UpdateConditionData;

            foreach (var kvp in MARSUIResources.instance.ConditionIcons)
            {
                var data = new ConditionTypeData();
                data.iconData = kvp.Value;
                m_ConditionData.Add(kvp.Key, data);
            }
        }

        public void UnloadModule()
        {
            s_Instance = null;
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= UpdateConditionData;
            AssemblyReloadEvents.afterAssemblyReload -= UpdateConditionData;
            m_SelectedConditions.Clear();
            m_SelectedEntities.Clear();

            foreach (var kvp in m_ConditionData)
            {
                var conditionTypeData = kvp.Value;
                foreach (var kvp2 in conditionTypeData.selectedConditions)
                {
                    kvp2.Value.Clear();
                }
                conditionTypeData.selectedConditions.Clear();
            }
            m_ConditionData.Clear();
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors)
            {
                var entityEditor = editor as MARSEntityEditor;
                if (entityEditor != null)
                {
                    entityEditor.EditorOnSceneGUI(sceneView);
                    break;
                }
            }
        }

        public void HideToolWhenAdjusting()
        {
            var adjustingSelected = false;
            foreach (var condition in m_SelectedConditions)
            {
                // Disable adjusting if another tool is selected while adjusting
                if (m_TransformGizmoHidden && Tools.current != Tool.None)
                    condition.adjusting = false;

                adjustingSelected |= condition.adjusting;
            }

            // Check if another tool has been selected
            if (m_TransformGizmoHidden && Tools.current != Tool.None)
                m_TransformGizmoHidden = false;

            if (adjustingSelected && !m_TransformGizmoHidden)
            {
                // Cache previous tool and set current to none if adjusting something in the selection
                m_CachedTool = Tools.current;
                m_TransformGizmoHidden = true;
                Tools.current = Tool.None;
            }
            else if (!adjustingSelected && m_TransformGizmoHidden)
            {
                // Restore previous tool
                Tools.current = m_CachedTool;
                m_TransformGizmoHidden = false;
            }
        }

        public void UpdateConditionData()
        {
            m_SelectedConditions.Clear();
            m_SelectedEntities.Clear();
            m_HasActiveCondition = false;

            foreach (var kvp in m_ConditionData)
            {
                var conditionTypeData = kvp.Value;
                foreach (var entityConditions in conditionTypeData.selectedConditions)
                {
                    entityConditions.Value.Clear();
                }

                conditionTypeData.selectedConditions.Clear();
                conditionTypeData.hasConditions = false;
            }

            // Gather all entities selected with inspectors
            foreach (var editor in ActiveEditorTracker.sharedTracker.activeEditors)
            {
                var entityEditor = editor as MARSEntityEditor;
                if (entityEditor == null)
                    continue;

                foreach (var editorTarget in entityEditor.targets)
                {
                    var entity = editorTarget as MARSEntity;
                    if (entity != null)
                        m_SelectedEntities.Add(entity);
                }
            }

            // Gather all conditions on selected entities
            // Sort them by type and cache the data
            foreach (var entity in m_SelectedEntities)
            {
                var selectedEntityConditions = entity.GetComponents<ConditionBase>();
                if (selectedEntityConditions.Length < 1)
                    continue;

                var selectedConditionData = new Dictionary<Type, HashSet<ConditionBase>>();
                foreach (var selectedCondition in selectedEntityConditions)
                {
                    var conditionType = selectedCondition.GetType();
                    HashSet<ConditionBase> selectedConditionSet;
                    if (!selectedConditionData.TryGetValue(conditionType, out selectedConditionSet))
                    {
                        selectedConditionSet = new HashSet<ConditionBase>();
                        selectedConditionData.Add(conditionType, selectedConditionSet);
                    }

                    selectedConditionSet.Add(selectedCondition);
                    m_SelectedConditions.Add(selectedCondition);

                    // Only show types that have an icon
                    ConditionTypeData existingConditionData;
                    if (m_ConditionData.TryGetValue(conditionType, out existingConditionData))
                    {
                        existingConditionData.hasConditions = true;
                        m_HasActiveCondition = true;

                        HashSet<ConditionBase> existingConditionSet;
                        var existingConditions = existingConditionData.selectedConditions;
                        if (!existingConditions.TryGetValue(entity, out existingConditionSet))
                        {
                            existingConditionSet = new HashSet<ConditionBase>();
                            existingConditions.Add(entity, existingConditionSet);
                        }

                        existingConditionSet.Add(selectedCondition);
                    }
                }
            }
        }

        public void GetAdjustingStateForSelected()
        {
            adjustingInSimView = false;

            foreach (var conditionDataOfType in m_ConditionData)
            {
                // When the entities have a mixed adjusting state, initial click sets them all to true.
                // So if there is at least 1 not adjusting, initial state is false.
                var conditionTypeData = conditionDataOfType.Value;
                var groupAdjustingState = true;
                foreach (var entityConditions in conditionTypeData.selectedConditions)
                {
                    foreach (var condition in entityConditions.Value)
                    {
                        if (condition == null)
                            continue;

                        var entityIsSimulated = SimulatedObjectsManager.IsSimulatedObject(condition.gameObject);
                        var adjusting = condition.adjusting;
                        groupAdjustingState &= adjusting;
                        adjustingInSimView |= adjusting && entityIsSimulated;
                    }
                }
                conditionTypeData.groupAdjusting = groupAdjustingState;
            }
        }
    }
}
