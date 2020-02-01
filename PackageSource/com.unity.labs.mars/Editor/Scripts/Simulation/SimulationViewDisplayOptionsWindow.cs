using System;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Popup Window for Simulation View's display options.
    /// </summary>
    public class SimulationViewDisplayOptionsWindow : MARSEditorGUI.MARSPopupWindow
    {
        class Styles
        {
            public readonly GUIContent showSceneOptionsGUIContent;
            public readonly GUIContent simContentActiveContent;
            public readonly GUIContent simEnvironmentActiveContent;
            public readonly GUIContent desaturateInactiveContent;

            public readonly GUIContent showLayerOptionsGUIContent;
            public readonly GUIContent environmentLabelGUIContent;
            public readonly GUIContent simulatedDataOptionGUIContent;
            public readonly GUIContent showSimulatedDataOptionGUIContent;
            public readonly GUIContent contentObjectsOptionGUIContent;
            public readonly GUIContent showContentObjectsOptionGUIContent;

            public Styles()
            {
                showSceneOptionsGUIContent = new GUIContent("Simulation Scene Options", "Simulation scene display options");
                simContentActiveContent = new GUIContent("Augmented Active",  "Sets the simulated augmented scene for active editing.");
                simEnvironmentActiveContent = new GUIContent("Environment Active",  "Sets the simulated environment scene for active editing.");
                desaturateInactiveContent = new GUIContent("Desaturate inactive", "Desaturate the objects in the inactive scene.");

                showLayerOptionsGUIContent = new GUIContent("Layer Options", "Show simulation view display options");
                environmentLabelGUIContent = new GUIContent("Simulated Environment");
                simulatedDataOptionGUIContent = new GUIContent("Simulated Data");
                showSimulatedDataOptionGUIContent = new GUIContent{ tooltip = "Show the simulation data visualizations in the simulation views." };
                contentObjectsOptionGUIContent = new GUIContent("Augmented Objects");
                showContentObjectsOptionGUIContent = new GUIContent{ tooltip = "Show the augmented objects in the simulation views." };
            }
        }

        const float k_WindowWidth = 240;
        const int k_IndentWidth = 12;
        const int k_LabelPadding = 8;
        static Styles s_Styles;

        readonly SimulationView m_View;

        // Delay creation of Styles till first access
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        static float windowHeight => 158;

        public SimulationViewDisplayOptionsWindow(SimulationView view)
        {
            m_View = view;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(k_WindowWidth, windowHeight);
        }

        protected override void Draw()
        {
            var marsStyles = MARSEditorGUI.Styles;
            using (new EditorGUILayout.VerticalScope(marsStyles.AreaAlignmentLargeMargin))
            {
                var labelSize = Mathf.Max(
                    marsStyles.LabelLeftAligned.CalcSize(styles.environmentLabelGUIContent).x,
                    marsStyles.LabelLeftAligned.CalcSize(styles.showSimulatedDataOptionGUIContent).x,
                    marsStyles.LabelLeftAligned.CalcSize(styles.showContentObjectsOptionGUIContent).x);
                var labelHeight = marsStyles.LabelLeftAligned.CalcHeight(styles.environmentLabelGUIContent, labelSize);

                EditorGUILayout.LabelField(styles.showSceneOptionsGUIContent, marsStyles.LabelLeftAligned);

                DrawSimulationViewSceneSwitchButton(labelSize, labelHeight, m_View);

                DrawLine(labelSize, labelHeight, styles.desaturateInactiveContent, m_View.DesaturateInactive, visible =>
                {
                    m_View.DesaturateInactive = visible;
                });

                EditorGUILayout.LabelField(styles.showLayerOptionsGUIContent, marsStyles.LabelLeftAligned);

                // Simulated Environment display option
                DrawLine(labelSize, labelHeight, styles.environmentLabelGUIContent, SimulationSettings.showSimulatedEnvironment, visible =>
                {
                    SimulationSettings.showSimulatedEnvironment = visible;
                });

                // Simulated Data display option
                DrawLine(labelSize, labelHeight, styles.simulatedDataOptionGUIContent, SimulationSettings.showSimulatedData, visible =>
                {
                    SimulationSettings.showSimulatedData = visible;
                });

                // Content layer display and lock option
                var simObjectsVisible = ((LayerMask)Tools.visibleLayers).Contains(SimulatedObjectsManager.SimulatedObjectsLayer);
                var lockSimObject = ((LayerMask)Tools.lockedLayers).Contains(SimulatedObjectsManager.SimulatedObjectsLayer);
                Action<bool> lockAction = locked =>
                {
                    EditorGUIUtils.SetLayerLockState(SimulatedObjectsManager.SimulatedObjectsLayer, locked);
                };

                DrawLine(labelSize, labelHeight, styles.contentObjectsOptionGUIContent, simObjectsVisible, visible =>
                {
                    EditorGUIUtils.SetLayerVisibleState(SimulatedObjectsManager.SimulatedObjectsLayer, visible);
                },
                styles.showContentObjectsOptionGUIContent, lockSimObject, lockAction);
            }
        }

        static void DrawLine(float labelSize, float labelHeight, GUIContent visibilityContent, bool objectVisible, Action<bool> setVisibility,
            GUIContent lockContent = null, bool objectLocked = false, Action<bool> setLocked = null)
        {
            var marsStyles = MARSEditorGUI.Styles;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayoutUtility.GetRect(GUIContent.none, marsStyles.LabelLeftAligned,
                    GUILayout.Width(k_IndentWidth));

                EditorGUILayout.LabelField(visibilityContent, marsStyles.LabelLeftAligned,
                    GUILayout.Width(labelSize + k_IndentWidth + k_LabelPadding));

                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    objectVisible = EditorGUIUtils.ImageToggle(objectVisible, styles.showContentObjectsOptionGUIContent,
                        marsStyles.AnimationVisibilityToggleOn, marsStyles.AnimationVisibilityToggleOff,
                        marsStyles.SingleLineAlignment, GUILayout.Height(labelHeight),
                        GUILayout.Width(labelHeight));

                    if (changed.changed)
                        setVisibility(objectVisible);
                }

                if (setLocked == null)
                    return;

                var inLock = MARSEditorGUI.InternalEditorStyles.InLock;
                var inLockHeight = inLock.CalcHeight(lockContent, inLock.fixedWidth);
                var lockRect = GUILayoutUtility.GetRect(inLock.fixedWidth, inLockHeight, marsStyles.SingleLineAlignment);
                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    objectLocked = GUI.Toggle(lockRect, objectLocked, lockContent, MARSEditorGUI.InternalEditorStyles.InLock);

                    if (changed.changed)
                        setLocked(objectLocked);
                }
            }
        }

        static void DrawSimulationViewSceneSwitchButton(float labelSize, float labelHeight, SimulationView view)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayoutUtility.GetRect(GUIContent.none, MARSEditorGUI.Styles.LabelLeftAligned,
                    GUILayout.Width(k_IndentWidth));

                if (GUILayout.Button(view.backgroundSceneActive ? styles.simEnvironmentActiveContent : styles.simContentActiveContent,
                    EditorStyles.miniButton, GUILayout.Width(labelSize), GUILayout.Height(labelHeight)))
                {
                    view.backgroundSceneActive = !view.backgroundSceneActive;
                }
            }
        }
    }
}
