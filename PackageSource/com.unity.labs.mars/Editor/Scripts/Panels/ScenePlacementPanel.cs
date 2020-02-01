// In 19.3+, scene placement options are placed in the editor toolbar.
// This API is new in 19.3, so in previous versions we put these tools in a MARS panel.
#if !UNITY_2019_3_OR_NEWER
using Unity.Labs.Utils;
using System;
using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [PanelOrder(PanelOrders.ScenePlacementOrder)]
    public class ScenePlacementPanel : PanelView
    {
        class Styles
        {
            const string k_SnapToPivotToolTip = "Hold Shift to Snap to Pivot (Alt to invert)";
            const string k_OrientToSurfaceToolTip = "Hold Shift to Orient To Surface (Ctl to invert)";

            public readonly GUIContent orientToSurfaceIcon;
            public readonly GUIContent pivotSnappingIcon;
            public readonly GUIContent[] axisIcons;
            public readonly GUIStyle miniButton;

            public Styles()
            {
                var uiResources = MARSUIResources.instance;
                orientToSurfaceIcon = new GUIContent(uiResources.OrientToSurfaceIcon, k_OrientToSurfaceToolTip);
                pivotSnappingIcon = new GUIContent(uiResources.PivotSnappingIcon, k_SnapToPivotToolTip);
                axisIcons = new[]
                {
                    new GUIContent(uiResources.XUpIcon, "Positive X is oriented out from surface"),
                    new GUIContent(uiResources.XDownIcon, "Negative X is oriented out from surface"),
                    new GUIContent(uiResources.YUpIcon, "Positive Y is oriented out from surface"),
                    new GUIContent(uiResources.YDownIcon, "Negative Y is oriented out from surface"),
                    new GUIContent(uiResources.ZUpIcon, "Positive Z is oriented out from surface"),
                    new GUIContent(uiResources.ZDownIcon, "Negative Z is oriented out from surface")
                };

                miniButton = new GUIStyle(EditorStyles.miniButton)
                {
                    margin = EditorStyles.miniButtonMid.margin
                };
            }
        }

        class AxisSelectWindow : MARSEditorGUI.MARSPopupWindow
        {
            const int k_AxisCount = 6;
            const int k_SpacingCount = k_AxisCount + 2; // + 2 for menu top and bottom spacing
            public int currentAxis;

            protected override void Draw()
            {
                for (var i = 0; i < styles.axisIcons.Length; i++)
                {
                    AxisSelectButton(i);
                }
            }

            public override Vector2 GetWindowSize()
            {
                var buttonSize = styles.miniButton.CalcSize(styles.axisIcons[0]);
                var minHeight = buttonSize.y * k_AxisCount + EditorGUIUtility.standardVerticalSpacing * k_SpacingCount;

                return new Vector2(buttonSize.x, minHeight);
            }

            void AxisSelectButton(int index)
            {
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    if (GUILayout.Toggle( currentAxis == index, styles.axisIcons[index],
                        styles.miniButton) && change.changed)
                    {
                        var scenePlacementModule = ModuleLoaderCore.instance.GetModule<ScenePlacementModule>();
                        if (scenePlacementModule != null)
                        {
                            scenePlacementModule.orientAxis = (AxisEnum) index;
                            editorWindow.Close();
                        }
                    }
                }
            }
        }

        const string k_PlacementOptionsLabel = "Placement Options";
        const string k_NoInteractionTargetsHelpMessage = "No Interaction Targets present.";
        const string k_OverrideHelpMessage = "Some options overridden";

        static readonly Vector2 k_PrefSize = new Vector2(240f, 64f);
        static readonly Vector2 k_MinSize = new Vector2(240f, 64f);
        static readonly Vector2 k_MaxSize = new Vector2(240f, 64f);

        static readonly string k_TypeName = typeof(ScenePlacementPanel).FullName;

        static Styles s_Styles;
        static AxisSelectWindow s_AxisSelectWindow;

        bool m_InteractionTargetsExist;
        bool m_HasOverride;

        /// <inheritdoc />
        public override string PanelLabel { get { return k_PlacementOptionsLabel; } }

        /// <inheritdoc />
        public override bool PanelExpanded
        {
            get { return EditorPrefsUtils.GetBool(k_TypeName, true); }
            set { EditorPrefsUtils.SetBool(k_TypeName, value); }
        }

        /// <inheritdoc />
        public override bool DrawAsWindow { get { return false; } set{} }

        /// <inheritdoc />
        public override bool AutoRepaintOnSceneChange { get { return false; } }

        /// <inheritdoc />
        public override bool UsePrefSize { get { return false; } }

        /// <inheritdoc />
        public override Vector2 PreferredSize { get { return k_PrefSize; } }

        /// <inheritdoc />
        public override Vector2 MinSize { get { return k_MinSize; } }

        /// <inheritdoc />
        public override Vector2 MaxSize { get { return k_MaxSize; } }

        /// <inheritdoc />
        public override Func<GenericMenu> TabMenuFunc { get { return null; } }

        static Styles styles { get { return s_Styles ?? (s_Styles = new Styles()); } }

        public override void OnEnable()
        {
            base.OnEnable();
            hideFlags = HideFlags.DontSave;
        }

        /// <inheritdoc />
        public override void OnGUI()
        {
            if (Event.current.type == EventType.Layout)
                m_InteractionTargetsExist = InteractionTarget.AllTargets.Count > 0;

            using (new EditorGUILayout.VerticalScope(MARSEditorGUI.Styles.AreaAlignment))
            {
                if (m_InteractionTargetsExist)
                    m_HasOverride = DrawInteractionTargetSnappingArea();

                DrawHelpBoxArea(m_HasOverride, m_InteractionTargetsExist);
            }

            base.OnGUI();
        }

        /// <inheritdoc />
        public override void Repaint()
        {
            if (EditorWindow != null)
                EditorWindow.Repaint();

            if (PanelWindow != null)
                PanelWindow.Repaint();
        }

        /// <inheritdoc />
        public override void OnSelectionChanged() { }

        static bool DrawInteractionTargetSnappingArea()
        {
            var hasOverride = false;

            using (new EditorGUILayout.HorizontalScope(MARSEditorGUI.Styles.NoVerticalMarginScopeAlignment))
            {
                var scenePlacementModule = ModuleLoaderCore.instance.GetModule<ScenePlacementModule>();
                if (scenePlacementModule == null)
                    return false;

                // Calculate the size and placement of the button recs to center align the buttons
                var axisIconSize = EditorStyles.miniButtonRight.CalcSize(styles.axisIcons[0]);
                var snapIconSize = EditorStyles.miniButtonRight.CalcSize(styles.axisIcons[1]);
                var orientIconSize = EditorStyles.miniButtonRight.CalcSize(styles.axisIcons[2]);

                var widthButtons = snapIconSize.x + orientIconSize.x + axisIconSize.x;
                var heightButtons = Mathf.Max(snapIconSize.y, orientIconSize.y, axisIconSize.y);

                // Use control rect to reserve space with GUILayout
                var buttonsRect = EditorGUILayout.GetControlRect(false, heightButtons,
                    EditorStyles.miniButtonMid, GUILayout.Width(widthButtons));
                buttonsRect.x = (EditorGUIUtility.currentViewWidth - widthButtons) / 2f;

                // Use layout to reserve vertical space since
                EditorGUILayout.GetControlRect(false, axisIconSize.y,
                    EditorStyles.miniButtonRight, GUILayout.Width(axisIconSize.x));

                using (new EditorGUI.DisabledScope(scenePlacementModule.isDragging))
                {
                    var overrides = scenePlacementModule.PlacementOverrides;
                    var snapOverride = overrides.useSnapToPivotOverride;
                    hasOverride |= snapOverride;

                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        bool snapToPivot;
                        bool orientToSurface;

                        using (new EditorGUI.DisabledScope(snapOverride))
                        {
                            buttonsRect.width = snapIconSize.x;
                            snapToPivot = GUI.Toggle(buttonsRect, scenePlacementModule.snapToPivot,
                                styles.pivotSnappingIcon, EditorStyles.miniButtonLeft);
                        }

                        var orientOverride = overrides.useOrientToSurfaceOverride;
                        hasOverride |= orientOverride;

                        using (new EditorGUI.DisabledScope(orientOverride))
                        {
                            buttonsRect.x += snapIconSize.x;
                            buttonsRect.width = orientIconSize.x;
                            orientToSurface = GUI.Toggle(buttonsRect, scenePlacementModule.orientToSurface,
                                styles.orientToSurfaceIcon, EditorStyles.miniButtonMid);
                        }

                        using (new EditorGUI.DisabledScope(!scenePlacementModule.orientToSurface))
                        {
                            var axisOverride = overrides.useAxisOverride;
                            hasOverride |= axisOverride;

                            using (new EditorGUI.DisabledScope(axisOverride))
                            {
                                var axisIndex = (int)scenePlacementModule.orientAxis;
                                var stylesAxisIcon = styles.axisIcons[axisIndex];

                                if (s_AxisSelectWindow == null)
                                    s_AxisSelectWindow = new AxisSelectWindow();

                                s_AxisSelectWindow.currentAxis = axisIndex;

                                buttonsRect.x += orientIconSize.x;
                                buttonsRect.width = axisIconSize.x;

                                if (GUI.Button(buttonsRect, stylesAxisIcon, EditorStyles.miniButtonRight))
                                {
                                    PopupWindow.Show(buttonsRect, s_AxisSelectWindow);
                                    GUIUtility.ExitGUI();
                                }
                            }
                        }

                        if (change.changed)
                        {
                            scenePlacementModule.snapToPivot = snapToPivot;
                            scenePlacementModule.orientToSurface = orientToSurface;
                        }
                    }
                }
            }

            return hasOverride;
        }

        static void DrawHelpBoxArea(bool hasOverride, bool hasInteractionTarget)
        {
            using (new EditorGUILayout.VerticalScope(MARSEditorGUI.Styles.NoVerticalMarginScopeAlignment))
            {
                if (hasOverride)
                    EditorGUILayout.HelpBox(k_OverrideHelpMessage, MessageType.Info);

                if (!hasInteractionTarget)
                    EditorGUILayout.HelpBox(k_NoInteractionTargetsHelpMessage, MessageType.Info);
            }
        }
    }
}
#endif
