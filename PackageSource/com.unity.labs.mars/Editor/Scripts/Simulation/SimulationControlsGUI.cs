using System;
using System.Linq;
using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// GUI for the simulation controls
    /// </summary>
    public static class SimulationControlsGUI
    {
        /// <summary>
        /// GUI Styles, content and resources for the MARS Simulation View Header.
        /// </summary>
        class Styles
        {
            const string k_NoSwitchingTooltip = "Switching not available in this mode";

            public const float LabelWidth = 74f;
            public const float ColumnWidth = 36f;
            public const float ElementHeight = 19f;

            public readonly GUIContent showDisplayOptionsGUIContent;

            public readonly GUIContent[] viewSceneTypes;

            public readonly GUIContent modeTypeContent;
            public readonly GUIContent viewMenuContent;

            public readonly GUIContent environmentLabelContent;
            public readonly GUIContent recordingLabelContent;
            public readonly GUIContent modeLabelContent;
            public readonly GUIContent controlsLabelContent;
            public readonly GUIContent viewTypeLabelContent;

            public readonly GUIContent previousItemContent;
            public readonly GUIContent nextItemContent;

            public readonly GUIContent modeNotAvailableContent;
            public readonly GUIContent liveEnvironmentContent;
            public readonly GUIContent remoteEnvironmentContent;
            public readonly GUIContent recordedEnvironmentContent;
            public readonly GUIContent liveRecordingContent;
            public readonly GUIContent remoteRecordingContent;

            public readonly GUIContent statusLabelContent;
            public readonly GUIContent syncedStatusContent;
            public readonly GUIContent outOfSyncStatusContent;
            public readonly GUIContent resyncButtonContent;
            public readonly GUIContent resyncTextButtonContent;

            public readonly GUIContent autoSyncLabelContent;
            public readonly GUIContent autoSyncToggleContent;

            public readonly Color PlayButtonActiveColor;

            readonly GUIContent m_RecordingButtonContentInactive;
            readonly GUIContent m_RecordingButtonContentActive;
            readonly GUIContent m_PlayButtonContentInactive;
            readonly GUIContent m_PlayButtonContentActive;
            readonly GUIContent m_PauseButtonContentInactive;
            readonly GUIContent m_PauseButtonContentActive;

            readonly GUIContent m_RecordingTextButtonContentInactive;
            readonly GUIContent m_RecordingTextButtonContentActive;
            readonly GUIContent m_PlayTextButtonContentInactive;
            readonly GUIContent m_PlayTextButtonContentActive;
            readonly GUIContent m_PauseTextButtonContentInactive;
            readonly GUIContent m_PauseTextButtonContentActive;

            public Styles()
            {
                showDisplayOptionsGUIContent = EditorGUIUtility.TrIconContent("_Popup", "Show Simulation View display options");

                viewSceneTypes = (from viewType in Enum.GetNames(typeof(ViewSceneType))
                    where viewType != "None" select new GUIContent(viewType)).ToArray();

                modeTypeContent = new GUIContent("Mode");
#if UNITY_2019_3_OR_NEWER
                viewMenuContent = EditorGUIUtility.TrIconContent("_Menu", "Simulation View Controls");

                m_RecordingButtonContentInactive = EditorGUIUtility.TrTextContentWithIcon( string.Empty, "Start Record Session", "TimelineAutokey@2x");
                m_RecordingButtonContentActive = EditorGUIUtility.TrTextContentWithIcon( string.Empty, "Stop Record Session", "TimelineAutokey_active@2x");
#else
                viewMenuContent = EditorGUIUtility.TrIconContent("pane options", "Simulation View Controls");

                recordingButtonContentInactive = EditorGUIUtility.TrTextContentWithIcon( string.Empty, "Start Record Session", "TimelineAutokey");
                recordingButtonContentActive = EditorGUIUtility.TrTextContentWithIcon( string.Empty, "Stop Record Session", "TimelineAutokey_active");
#endif
                m_PlayButtonContentInactive = EditorGUIUtility.TrTextContentWithIcon(string.Empty, "Start Simulation", "PlayButton");
                m_PlayButtonContentActive = EditorGUIUtility.TrTextContentWithIcon(string.Empty, "Stop Simulation", "PlayButton On");
                m_PauseButtonContentInactive = EditorGUIUtility.TrIconContent("PauseButton", "Pause");
                m_PauseButtonContentActive = EditorGUIUtility.TrIconContent("PauseButton On", "Unpause");

#if UNITY_2019_3_OR_NEWER
                m_RecordingTextButtonContentInactive = EditorGUIUtility.TrTextContentWithIcon( "Rec", "Start Record Session", "TimelineAutokey@2x");
                m_RecordingTextButtonContentActive = EditorGUIUtility.TrTextContentWithIcon( "Rec", "Stop Record Session", "TimelineAutokey_active@2x");
#else
                recordingTextButtonContentInactive = EditorGUIUtility.TrTextContentWithIcon( "Rec", "Start Record Session", "TimelineAutokey");
                recordingTextButtonContentActive = EditorGUIUtility.TrTextContentWithIcon( "Rec", "Stop Record Session", "TimelineAutokey_active");
#endif
                m_PlayTextButtonContentInactive = EditorGUIUtility.TrTextContentWithIcon("Play", "Start Simulation", "PlayButton");
                m_PlayTextButtonContentActive = EditorGUIUtility.TrTextContentWithIcon("Play", "Stop Simulation", "PlayButton On");
                m_PauseTextButtonContentInactive = EditorGUIUtility.TrTextContentWithIcon("Pause", "Pause Simulation", "PauseButton");
                m_PauseTextButtonContentActive = EditorGUIUtility.TrTextContentWithIcon("Pause", "Unpause Simulation", "PauseButton On");

                previousItemContent = EditorGUIUtility.TrIconContent("tab_prev", "Previous Item");
                nextItemContent = EditorGUIUtility.TrIconContent("tab_next", "Next Item");

                environmentLabelContent = new GUIContent("Environment");
                recordingLabelContent = new GUIContent("Recording");
                modeLabelContent = new GUIContent("Mode");
                controlsLabelContent = new GUIContent("Controls");
                viewTypeLabelContent = new GUIContent("View Type");

                modeNotAvailableContent = new GUIContent("Not Available", k_NoSwitchingTooltip);
                recordedEnvironmentContent = new GUIContent("Recorded Environment", k_NoSwitchingTooltip);
                liveEnvironmentContent = new GUIContent("Live Environment", k_NoSwitchingTooltip);
                remoteEnvironmentContent = new GUIContent("Remote Environment", k_NoSwitchingTooltip);
                liveRecordingContent = new GUIContent("Live", k_NoSwitchingTooltip);
                remoteRecordingContent = new GUIContent("Remote", k_NoSwitchingTooltip);

                statusLabelContent = new GUIContent("Status");
                syncedStatusContent = new GUIContent("Synced", "Simulation reflects the state of the active scene");
                outOfSyncStatusContent = new GUIContent("Out of Sync", "Simulation does not reflect the state of the active scene");
                resyncButtonContent = EditorGUIUtility.TrIconContent("preAudioLoopOff", "Resync Simulation");
                resyncTextButtonContent = EditorGUIUtility.TrTextContentWithIcon("Resync", "Resync Simulation", "preAudioLoopOff");

                autoSyncLabelContent = new GUIContent("Auto Sync");
                autoSyncToggleContent = new GUIContent("", SimulationSettings.AutoSyncTooltip);

                PlayButtonActiveColor = EditorGUIUtility.isProSkin
                    ? new Color(0.29f, 0.58f, 0.88f)
                    : new Color(0.2f, 0.52f, 0.73f);
            }

            public GUIContent recordingButtonContent(bool isRecording)
            {
                return !isRecording ? m_RecordingButtonContentInactive : m_RecordingButtonContentActive;
            }

            public GUIContent playButtonContent(bool isPlaying)
            {
                return !isPlaying ? m_PlayButtonContentInactive : m_PlayButtonContentActive;
            }

            public GUIContent pauseButtonContent(bool isPaused)
            {
                return !isPaused ? m_PauseButtonContentInactive : m_PauseButtonContentActive;
            }

            public GUIContent recordingTextButtonContent(bool isRecording)
            {
                return !isRecording ? m_RecordingTextButtonContentInactive : m_RecordingTextButtonContentActive;
            }

            public GUIContent playTextButtonContent(bool isPlaying)
            {
                return !isPlaying ? m_PlayTextButtonContentInactive : m_PlayTextButtonContentActive;
            }

            public GUIContent pauseTextButtonContent(bool isPaused)
            {
                return !isPaused ? m_PauseTextButtonContentInactive : m_PauseTextButtonContentActive;
            }
        }

        static Styles s_Styles;

        // Delay creation of Styles till first access
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        public static void DrawSimulationViewToolbar(this SimulationView view)
        {
            const int toolbarWidth = 330;
            const int toolbarVerticalOffset = 32;

            const int viewGizmoWidth = 100;
            const int fullWidth = toolbarWidth + viewGizmoWidth * 2;

            var moduleLoader = ModuleLoaderCore.instance;
            var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
            var sessionRecordingModule = moduleLoader.GetModule<SessionRecordingModule>();
            var videoModule = moduleLoader.GetModule<MARSVideoModule>();

            var toolbarStyle = MARSEditorGUI.Styles.ToolbarNew;
            float toolbarStart;

            var editorStyles = MARSEditorGUI.InternalEditorStyles;

            if (view.position.width < toolbarWidth + viewGizmoWidth)
            {
                toolbarStart = 0f;
            }
            else if (view.position.width < fullWidth)
            {
                var offset = (view.position.width - fullWidth);
                toolbarStart = (view.position.width + offset) * 0.5f;
                toolbarStart -= toolbarWidth * 0.5f;
            }
            else
            {
                toolbarStart = (view.position.width * 0.5f) - (toolbarWidth * 0.5f);
            }

            var toolbarRect = new Rect(
                toolbarStart,
                toolbarVerticalOffset,
                toolbarWidth,
                toolbarStyle.fixedHeight);

            if (Event.current.type == EventType.Repaint)
                toolbarStyle.Draw(toolbarRect, GUIContent.none, false, false, false, false);

            using (new GUILayout.AreaScope(toolbarRect))
            {
                using (new EditorGUILayout.HorizontalScope(MARSEditorGUI.Styles.AreaAlignment))
                {
                    var isPlaying = querySimulationModule != null && querySimulationModule.simulating;
                    var isRecording = sessionRecordingModule != null && sessionRecordingModule.IsRecording;
                    var isPaused = videoModule != null && videoModule.IsPaused;

                    var recordingButtonContent = styles.recordingButtonContent(isRecording);
                    var playButtonContent = styles.playButtonContent(isPlaying);
                    var pauseButtonContent = styles.pauseButtonContent(isPaused);

                    const float elementsHeightAdjust = -4f;
                    const float buttonWidthAdjust = 2f;
                    var elementsHeight = toolbarRect.height + elementsHeightAdjust;
                    var buttonWidth = toolbarRect.height + buttonWidthAdjust;

                    DrawViewSelector(view, elementsHeight);

                    EnvironmentSelectElement(elementsHeight);

                    const float elementSpacingAdjust = -4f;
                    const float carouselWidth = 32f;
                    var areaRect = GUILayoutUtility.GetRect(carouselWidth, elementsHeight, editorStyles.Button, GUILayout.ExpandWidth(false));
                    areaRect.xMin += elementSpacingAdjust;
                    EnvironmentSelectCarousel(areaRect);

                    PlaybackControlsElement(recordingButtonContent, playButtonContent, pauseButtonContent, view.sceneType == ViewSceneType.Device,
                        GUILayout.Width(buttonWidth), GUILayout.Height(elementsHeight));

                    MARSEditorGUI.DrawReloadButton(styles.resyncButtonContent, editorStyles.Button,
                        GUILayout.Width(buttonWidth), GUILayout.Height(elementsHeight));

                    DrawDisplayOptions(view, elementsHeight);
                }
            }

            var current = Event.current;
            if (current.type == EventType.MouseDown)
            {
                if (toolbarRect.Contains(current.mousePosition))
                    current.Use();
            }
        }

        public static void DrawControlsWindow(bool showPlaybackControls = true, bool recordingSupported = true)
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
            var simSceneModule = moduleLoader.GetModule<SimulationSceneModule>();
            var sessionRecordingModule = moduleLoader.GetModule<SessionRecordingModule>();
            var videoModule = moduleLoader.GetModule<MARSVideoModule>();
            var simObjectsManager = moduleLoader.GetModule<SimulatedObjectsManager>();

            using (new EditorGUILayout.VerticalScope(MARSEditorGUI.Styles.AreaAlignment))
            {
                var labelStyle = MARSEditorGUI.Styles.SingleLineAlignment;
                var labelSize = labelStyle.CalcSize(styles.modeLabelContent);
                labelSize.x = Styles.LabelWidth;

                const float columnWidth = Styles.ColumnWidth;
                const float alignedHeight = Styles.ElementHeight;

                var editorStyles = MARSEditorGUI.InternalEditorStyles;

                using (new EditorGUILayout.HorizontalScope(GUIStyle.none))
                {
                    GUILayout.Label(styles.statusLabelContent, GUILayout.Width(labelSize.x));

                    var synced = simObjectsManager != null && simObjectsManager.SimulationSyncedWithScene;
                    var statusContent = synced ? styles.syncedStatusContent : styles.outOfSyncStatusContent;
                    GUILayout.Label(statusContent);

                    MARSEditorGUI.DrawReloadButton(styles.resyncTextButtonContent, editorStyles.Button,
                        GUILayout.Height(labelSize.y), GUILayout.ExpandWidth(false));
                }

                using (new EditorGUILayout.HorizontalScope(GUIStyle.none))
                {
                    GUILayout.Label(styles.autoSyncLabelContent, GUILayout.Width(labelSize.x));
                    var simulationSettings = SimulationSettings.instance;
                    var autoSync = simulationSettings.AutoSyncWithSceneChanges;
                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        autoSync = GUILayout.Toggle(autoSync, styles.autoSyncToggleContent);
                        if (check.changed)
                        {
                            simulationSettings.AutoSyncWithSceneChanges = autoSync;
                            EditorEvents.AutoSyncToggle.Send(new AutoSyncToggleArgs { enabled = autoSync });
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope(GUIStyle.none))
                {
                    GUILayout.Label(styles.modeTypeContent, GUILayout.Width(labelSize.x));
                    ModeSelectElement(alignedHeight);
                }

                using (new EditorGUILayout.HorizontalScope(GUIStyle.none))
                {
                    GUILayout.Label(styles.environmentLabelContent, GUILayout.Width(labelSize.x));
                    EnvironmentSelectElement(alignedHeight);

                    var areaRect = GUILayoutUtility.GetRect(columnWidth, alignedHeight, editorStyles.Button, GUILayout.ExpandWidth(false));
                    EnvironmentSelectCarousel(areaRect);
                }

                using (new EditorGUILayout.HorizontalScope(GUIStyle.none))
                {
                    GUILayout.Label(styles.recordingLabelContent, GUILayout.Width(labelSize.x));
                    RecordingSelectElement(alignedHeight);

                    var areaRect = GUILayoutUtility.GetRect(columnWidth, alignedHeight, editorStyles.Button, GUILayout.ExpandWidth(false));
                    RecordingSelectCarousel(areaRect);
                }

                if (!showPlaybackControls)
                    return;

                using (var horizontalScope = new EditorGUILayout.HorizontalScope(GUIStyle.none))
                {
                    var horizontalRect = horizontalScope.rect;
                    GUILayout.Label(styles.controlsLabelContent, GUILayout.Width(labelSize.x));

                    var internalStyles = MARSEditorGUI.InternalEditorStyles;

                    var isPlaying = querySimulationModule != null && querySimulationModule.simulating;
                    var isRecording = sessionRecordingModule != null && sessionRecordingModule.IsRecording;
                    var isPaused = videoModule != null && videoModule.IsPaused;

                    var recordingButtonContent = styles.recordingTextButtonContent(isRecording);
                    var playButtonContent = styles.playTextButtonContent(isPlaying);
                    var pauseButtonContent = styles.pauseTextButtonContent(isPaused);

                    var recordTextContentWidth = internalStyles.ButtonLeft.CalcSize(recordingButtonContent).x;
                    var playTextContentWidth = internalStyles.ButtonMid.CalcSize(playButtonContent).x;
                    var pauseTextContentWidth = internalStyles.ButtonRight.CalcSize(pauseButtonContent).x;

                    var minFullWidth = labelSize.x + recordTextContentWidth + playTextContentWidth
                        + pauseTextContentWidth + columnWidth;

                    var playbackEnabled = querySimulationModule != null && simSceneModule != null && simSceneModule.IsSimulationReady;

                    var anyDeviceViewExists = false;
                    foreach (var simView in SimulationView.SimulationViews)
                    {
                        if (simView.sceneType != ViewSceneType.Device)
                            continue;

                        anyDeviceViewExists = true;
                        break;
                    }

                    recordingSupported &= anyDeviceViewExists;

                    using (new EditorGUI.DisabledScope(!playbackEnabled))
                    {
                        if (horizontalRect.width < minFullWidth)
                        {
                            recordingButtonContent = styles.recordingButtonContent(isRecording);
                            playButtonContent = styles.playButtonContent(isPlaying);
                            pauseButtonContent = styles.pauseButtonContent(isPaused);
                            PlaybackControlsElement(recordingButtonContent,
                                playButtonContent, pauseButtonContent, recordingSupported, GUILayout.Height(labelSize.y));
                        }
                        else
                        {
                            PlaybackControlsElement(recordingButtonContent, playButtonContent, pauseButtonContent,
                                recordingSupported, GUILayout.Height(labelSize.y));
                        }
                    }
                }
            }
        }

        static void DrawViewSelector(ISimulationView view, float height)
        {
            var dropdownStyle = MARSEditorGUI.InternalEditorStyles.Button;
            var viewSceneButton = MARSEditorGUI.GetDropDownButtonRect(styles.viewMenuContent, dropdownStyle, height);

            if (EditorGUI.DropdownButton(viewSceneButton, styles.viewMenuContent, FocusType.Passive, dropdownStyle))
            {
                var popupWindowRect = viewSceneButton;
                popupWindowRect.x -= dropdownStyle.padding.left / 2f;
                popupWindowRect.y += dropdownStyle.padding.bottom;

                PopupWindow.Show(popupWindowRect, new MARSEditorGUI.SceneTypeWindow(view, styles.viewSceneTypes));
                GUIUtility.ExitGUI();
            }
        }

        static void DrawDisplayOptions(SimulationView view, float height)
        {
            var dropdownStyle = MARSEditorGUI.InternalEditorStyles.Button;

            var foldoutRect = MARSEditorGUI.GetDropDownButtonRect(styles.showDisplayOptionsGUIContent, dropdownStyle, height);
            if (EditorGUI.DropdownButton(foldoutRect, styles.showDisplayOptionsGUIContent, FocusType.Passive, dropdownStyle))
            {
                PopupWindow.Show(foldoutRect, new SimulationViewDisplayOptionsWindow(view));
                GUIUtility.ExitGUI();
            }
        }

        /// <summary>
        /// Draw help dialogs for a simulated device view
        /// </summary>
        /// <param name="sceneType">Current scene scene view type</param>
        public static void DrawHelpArea(ViewSceneType sceneType)
        {
            if (QuerySimulationModule.sceneIsSimulatable || sceneType == ViewSceneType.None || MARSSession.Instance != null)
                return;
            
            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                const float helpBoxWidth = 300f;
                const float helpBoxHeight = 42f;
                const float helpBackgroundFade = 0.75f;

                var helpRect = GUILayoutUtility.GetRect(helpBoxWidth, helpBoxHeight, EditorStyles.helpBox);
                EditorGUI.DrawRect(helpRect, EditorGUIUtils.GetSceneBackgroundColor() * helpBackgroundFade);
                EditorGUI.HelpBox(helpRect, "Current scene can't be simulated. Add a MARSSession to the scene to start simulation (Create -> MARS -> Session).",
                    MessageType.Warning);

                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(24f);
        }

        static void ModeSelectElement(float height)
        {
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            var guiEnabled = environmentManager != null;
            var editorStyles = MARSEditorGUI.InternalEditorStyles;

            using (new EditorGUI.DisabledScope(!guiEnabled))
            {
                if (!guiEnabled)
                {
                    var rect = EditorGUILayout.GetControlRect(false, height, editorStyles.Popup);
                    GUI.Button(rect, styles.modeNotAvailableContent);
                    return;
                }

                var previousEnvironmentMode = SimulationSettings.environmentMode;
                using (var change = new EditorGUI.ChangeCheckScope())
                {
                    using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                    {
                        SimulationSettings.environmentMode = (EnvironmentMode)EditorGUILayout.EnumPopup(
                            SimulationSettings.environmentMode, editorStyles.Popup, GUILayout.Height(height));
                    }

                    if (change.changed && SimulationSettings.environmentMode != previousEnvironmentMode)
                        environmentManager.RefreshEnvironmentAndRestartSimulation(SimulationSettings.isVideoEnvironment);
                }
            }
        }

        static void EnvironmentSelectElement(float height)
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
            var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
            var simSceneModule = moduleLoader.GetModule<SimulationSceneModule>();

            var cycleButtonsEnabled = environmentManager != null && querySimulationModule != null &&
                simSceneModule != null && simSceneModule.IsSimulationReady;
            var simulatingTemporal = cycleButtonsEnabled && querySimulationModule.simulatingTemporal;

            var editorStyles = MARSEditorGUI.InternalEditorStyles;

            using (new EditorGUI.DisabledScope(!cycleButtonsEnabled))
            {
                if (!cycleButtonsEnabled)
                {
                    var rect = EditorGUILayout.GetControlRect(false, height, editorStyles.Popup);
                    GUI.Button(rect, styles.modeNotAvailableContent);
                    return;
                }

                switch (SimulationSettings.environmentMode)
                {
                    case EnvironmentMode.Synthetic:
                    {
                        var index = EditorGUILayout.Popup(environmentManager.CurrentSyntheticEnvironmentIndex,
                            environmentManager.EnvironmentGUIContents, editorStyles.Popup, GUILayout.Height(height));

                        if (index != environmentManager.CurrentSyntheticEnvironmentIndex)
                            environmentManager.SetupEnvironmentAndRestartSimulation(index, simulatingTemporal);

                        break;
                    }
                    case EnvironmentMode.Recorded:
                    {
                        var index = EditorGUILayout.Popup(environmentManager.CurrentSampleVideoIndex,
                            environmentManager.VideoGUIContents, editorStyles.Popup, GUILayout.Height(height));

                        if (index != environmentManager.CurrentSampleVideoIndex)
                            environmentManager.SetupEnvironmentAndRestartSimulation(index, simulatingTemporal);

                        break;
                    }
                    case EnvironmentMode.Live:
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            var rect = EditorGUILayout.GetControlRect(false, height, editorStyles.Popup);
                            GUI.Button(rect, styles.liveEnvironmentContent);
                            break;
                        }
                    }
                    case EnvironmentMode.Remote:
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            var rect = EditorGUILayout.GetControlRect(false, height, editorStyles.Popup);
                            GUI.Button(rect, styles.remoteEnvironmentContent);
                            break;
                        }
                    }
                }
            }
        }

        static void EnvironmentSelectCarousel(Rect rect)
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
            var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
            var simSceneModule = moduleLoader.GetModule<SimulationSceneModule>();
            var cycleButtonsEnabled = environmentManager != null && querySimulationModule != null &&
                simSceneModule != null && simSceneModule.IsSimulationReady
                && SimulationSettings.environmentMode != EnvironmentMode.Live
                && SimulationSettings.environmentMode != EnvironmentMode.Remote;

            var editorStyles = MARSEditorGUI.InternalEditorStyles;

            using (new EditorGUI.DisabledScope(!cycleButtonsEnabled))
            {
                rect.width *= 0.5f;
                if (GUI.Button(rect, styles.previousItemContent, editorStyles.ButtonLeftIcon))
                {
                    EnvironmentCycleButtonAction(false);
                }

                rect.x += rect.width;
                if (GUI.Button(rect, styles.nextItemContent, editorStyles.ButtonRightIcon))
                {
                    EnvironmentCycleButtonAction(true);
                }
            }
        }

        static void RecordingSelectElement(float height)
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var recordingManager = moduleLoader.GetModule<SimulationRecordingManager>();
            var simSceneModule = moduleLoader.GetModule<SimulationSceneModule>();

            var cycleButtonsEnabled = recordingManager != null && simSceneModule != null && simSceneModule.IsSimulationReady;

            var editorStyles = MARSEditorGUI.InternalEditorStyles;

            if (cycleButtonsEnabled && Event.current.type == EventType.Repaint)
                recordingManager.ValidateRecordings();

            var rect = EditorGUILayout.GetControlRect(false, height, editorStyles.Popup);

            using (new EditorGUI.DisabledScope(!cycleButtonsEnabled))
            {
                if (!cycleButtonsEnabled)
                {
                    GUI.Button(rect, styles.modeNotAvailableContent);
                    return;
                }

                switch (SimulationSettings.environmentMode)
                {
                    case EnvironmentMode.Synthetic:
                    {
                        using (var check = new EditorGUI.ChangeCheckScope())
                        {
                            var index = EditorGUI.Popup(rect, recordingManager.CurrentRecordingOptionIndex,
                                recordingManager.RecordingOptionContents, editorStyles.Popup);

                            if (check.changed)
                                recordingManager.SetRecordingOptionAndRestartSimulation(index);
                        }

                        break;
                    }
                    case EnvironmentMode.Recorded:
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            GUI.Button(rect, styles.recordedEnvironmentContent);
                            break;
                        }
                    }
                    case EnvironmentMode.Live:
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            GUI.Button(rect, styles.liveRecordingContent);
                            break;
                        }
                    }
                    case EnvironmentMode.Remote:
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            GUI.Button(rect, styles.remoteRecordingContent);
                            break;
                        }
                    }
                }
            }
        }

        static void RecordingSelectCarousel(Rect rect)
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var recordingManager = moduleLoader.GetModule<SimulationRecordingManager>();
            var simSceneModule = moduleLoader.GetModule<SimulationSceneModule>();

            var editorStyles = MARSEditorGUI.InternalEditorStyles;

            var cycleButtonsEnabled = recordingManager != null && simSceneModule != null
                && simSceneModule.IsSimulationReady && recordingManager.CurrentRecordingsCount > 0
                && SimulationSettings.environmentMode == EnvironmentMode.Synthetic
                && recordingManager.CurrentRecordingOptionIndex != 0;

            using (new EditorGUI.DisabledScope(!cycleButtonsEnabled))
            {
                rect.width *= 0.5f;
                if (GUI.Button(rect, styles.previousItemContent, editorStyles.ButtonLeftIcon))
                {
                    RecordingCycleButtonAction(false);
                }

                rect.x += rect.width;
                if (GUI.Button(rect, styles.nextItemContent, editorStyles.ButtonRightIcon))
                {
                    RecordingCycleButtonAction(true);
                }
            }
        }

        static bool ButtonToggle(bool active, GUIContent content, GUIStyle guiStyle, params GUILayoutOption[] options)
        {
            var value = GUILayout.Toggle(active, content, guiStyle, options);
            return value != active;
        }

        static void PlaybackControlsElement(GUIContent recordingContent, GUIContent playContent,
            GUIContent pauseContent, bool recodingSupported, params GUILayoutOption[] options)
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var sessionRecordingModule = moduleLoader.GetModule<SessionRecordingModule>();
            var recordingManager = moduleLoader.GetModule<SimulationRecordingManager>();
            var videoModule = moduleLoader.GetModule<MARSVideoModule>();
            var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
            var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();

            var mode = SimulationSettings.environmentMode;
            var isPlaying = querySimulationModule != null && querySimulationModule.simulating;
            var isRecording = sessionRecordingModule != null && sessionRecordingModule.IsRecording;
            var isPaused = videoModule != null && videoModule.IsPaused;

            if (querySimulationModule == null)
            {
                StopPlayingSimulation(isRecording);

                if (videoModule != null && isPaused)
                    videoModule.SetPauseVideo(false);

                StopRecording();

                isPaused = false;
                isPlaying = false;
                isRecording = false;
            }

            var internalStyles = MARSEditorGUI.InternalEditorStyles;

            GUI.backgroundColor = isRecording ? styles.PlayButtonActiveColor : Color.white;

            using (new EditorGUI.DisabledScope(!recodingSupported || recordingManager == null ||
                environmentManager == null || mode != EnvironmentMode.Synthetic || MARSSession.Instance == null))
            {
                if (ButtonToggle(isRecording, recordingContent, internalStyles.ButtonLeft, options))
                {
                    isPaused = false;
                    videoModule.SetPauseVideo(false);

                    if (!isRecording) // start recording
                    {
                        StartPlayingSimulation(true);
                    }
                    else
                    {
                        StopPlayingSimulation(true);
                        isRecording = false;
                        isPlaying = false;
                    }
                }
            }

            GUI.backgroundColor = isPlaying ? styles.PlayButtonActiveColor : Color.white;

            using (new EditorGUI.DisabledScope(recordingManager == null || MARSSession.Instance == null))
            {
                if (ButtonToggle(isPlaying, playContent, internalStyles.ButtonMid, options))
                {
                    if (!isPlaying) // start playing
                    {
                        StartPlayingSimulation(isRecording);
                        isPlaying = true;
                    }
                    else
                    {
                        StopPlayingSimulation(isRecording);
                        isPlaying = false;
                        isPaused = false;
                    }
                }
            }

            GUI.backgroundColor = isPaused ? styles.PlayButtonActiveColor : Color.white;

            using (new EditorGUI.DisabledScope(!isPlaying || videoModule == null || mode != EnvironmentMode.Recorded))
            {
                if (ButtonToggle(isPaused, pauseContent, internalStyles.ButtonRight, options ))
                {
                    videoModule.SetPauseVideo(!isPaused, isPlaying);
                }
            }

            GUI.backgroundColor =  Color.white;
        }

        public static void ViewSelectionElement(ISimulationView view, GUIContent[] contents)
        {
            var editorStyles = MARSEditorGUI.InternalEditorStyles;

            using (new EditorGUILayout.HorizontalScope(GUIStyle.none))
            {
                GUILayout.Label(styles.viewTypeLabelContent, GUILayout.Width(Styles.LabelWidth));

                var index = EditorGUILayout.Popup((int)view.sceneType,
                    contents, editorStyles.Popup, GUILayout.Height(Styles.ElementHeight));

                if (index != (int)view.sceneType)
                    view.sceneType = (ViewSceneType)index;
            }
        }

        static void EnvironmentCycleButtonAction(bool forward)
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
            var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
            if (environmentManager == null || querySimulationModule == null)
                return;

            var simulatingTemporal = querySimulationModule.simulatingTemporal;

            switch (SimulationSettings.environmentMode)
            {
                case EnvironmentMode.Synthetic:
                    environmentManager.SetupNextEnvironmentAndRestartSimulation(forward, simulatingTemporal);
                    EditorEvents.EnvironmentCycle.Send(new EnvironmentCycleArgs
                    {
                        label = environmentManager.SyntheticEnvironmentName,
                        mode = (int)SimulationSettings.environmentMode
                    });
                    break;
                case EnvironmentMode.Recorded:
                    environmentManager.SetupNextEnvironmentAndRestartSimulation(forward, simulatingTemporal);
                    EditorEvents.EnvironmentCycle.Send(new EnvironmentCycleArgs
                    {
                        label = environmentManager.SampleVideoNames[environmentManager.CurrentSampleVideoIndex],
                        mode = (int)SimulationSettings.environmentMode
                    });
                    break;
                case EnvironmentMode.Live: case EnvironmentMode.Remote:
                    break;
            }
        }

        static void RecordingCycleButtonAction(bool forward)
        {
            var recordingManager = ModuleLoaderCore.instance.GetModule<SimulationRecordingManager>();
            if (recordingManager == null)
                return;

            switch (SimulationSettings.environmentMode)
            {
                case EnvironmentMode.Synthetic:
                {
                    if (forward)
                        recordingManager.SetupNextRecordingAndRestartSimulation();
                    else
                        recordingManager.SetupPrevRecordingAndRestartSimulation();

                    EditorEvents.SyntheticRecordingCycle.Send(new SyntheticRecordingCycleArgs
                    {
                        label = recordingManager.CurrentRecordingName
                    });
                    break;
                }
                case EnvironmentMode.Recorded:
                    break;
                case EnvironmentMode.Live:
                    break;
                case EnvironmentMode.Remote:
                    break;
            }
        }

        static void StartPlayingSimulation(bool isRecording)
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
            var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();

            if (querySimulationModule == null || environmentManager == null)
                return;

            var remoteModule = moduleLoader.GetModule<MARSRemoteModule>();
            var recordingManager = moduleLoader.GetModule<SimulationRecordingManager>();
            var videoModule = moduleLoader.GetModule<MARSVideoModule>();
            var mode = SimulationSettings.environmentMode;

            var isPaused = videoModule != null && videoModule.IsPaused;

            var useRemote = remoteModule != null && mode == EnvironmentMode.Remote &&
                !SceneWatchdogModule.instance.currentSceneIsFaceScene;

            if (isRecording && recordingManager != null)
            {
                isPaused = false;
                videoModule.SetPauseVideo(false);
                recordingManager.DisableRecordingPlayback = true;
            }
            else if (isRecording)
            {
                isRecording = false;
            }

            if (useRemote)
                remoteModule.RemoteConnect();

            querySimulationModule.StartTemporalSimulation();

            if (videoModule != null && mode == EnvironmentMode.Recorded)
            {
                videoModule.videoPlayer.Play();
            }

            if (isRecording)
                StartRecording();

            videoModule.SetPauseVideo(isPaused);
        }

        static void StopPlayingSimulation(bool isRecording)
        {
            var moduleLoader = ModuleLoaderCore.instance;
            var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
            var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
            var remoteModule = moduleLoader.GetModule<MARSRemoteModule>();
            var videoModule = moduleLoader.GetModule<MARSVideoModule>();

            if (videoModule != null)
                videoModule.SetPauseVideo(false);

            if (querySimulationModule == null || environmentManager == null)
                return;

            var mode = SimulationSettings.environmentMode;

            var useRemote = remoteModule != null && mode == EnvironmentMode.Remote &&
                !SceneWatchdogModule.instance.currentSceneIsFaceScene;

            if (useRemote)
                remoteModule.RemoteDisconnect();

            if (SimulationSettings.autoResetDevicePose)
                environmentManager.ResetDeviceStartingPose();
            else
                environmentManager.UpdateDeviceStartingPose();

            if (isRecording)
                StopRecording();

            querySimulationModule.StopTemporalSimulation();

            if (videoModule != null && mode == EnvironmentMode.Recorded)
                videoModule.videoPlayer.Stop();

            querySimulationModule.RestartSimulationIfNeeded(); // Start up a one-shot simulation
        }

        static void StartRecording()
        {
            var sessionRecordingModule = ModuleLoaderCore.instance.GetModule<SessionRecordingModule>();
            if (sessionRecordingModule == null)
                return;

            sessionRecordingModule.RegisterRecorderType<CameraPoseRecorder>();
            sessionRecordingModule.RegisterRecorderType<PlaneFindingRecorder>();
            sessionRecordingModule.ToggleRecording();

            // Discard recording if simulation stops without the user explicitly clicking stop
            QuerySimulationModule.onTemporalSimulationStop += CancelRecording;
        }

        static void StopRecording()
        {
            QuerySimulationModule.onTemporalSimulationStop -= CancelRecording;

            var moduleLoader = ModuleLoaderCore.instance;
            var sessionRecordingModule = moduleLoader.GetModule<SessionRecordingModule>();
            var simRecordingManager = moduleLoader.GetModule<SimulationRecordingManager>();
            if (sessionRecordingModule == null || simRecordingManager == null || !sessionRecordingModule.IsRecording)
                return;

            simRecordingManager.DisableRecordingPlayback = false;
            sessionRecordingModule.ToggleRecording();

            simRecordingManager.TrySaveSyntheticRecording();
        }

        static void CancelRecording()
        {
            QuerySimulationModule.onTemporalSimulationStop -= CancelRecording;

            var moduleLoader = ModuleLoaderCore.instance;
            var sessionRecordingModule = moduleLoader.GetModule<SessionRecordingModule>();
            var simRecordingManager = moduleLoader.GetModule<SimulationRecordingManager>();
            if (sessionRecordingModule == null || simRecordingManager == null)
                return;

            simRecordingManager.DisableRecordingPlayback = false;
            sessionRecordingModule.CancelRecording();
        }
    }
}
