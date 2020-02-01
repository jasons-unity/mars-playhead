using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    [CustomEditor(typeof(MARSSession))]
    public class MARSSessionEditor : Editor
    {
        class Styles
        {
            public readonly GUIContent scaleEntityPositionsContent;
            public readonly GUIContent scaleEntityChildrenContent;
            public readonly GUIContent scaleSceneAudioContent;
            public readonly GUIContent scaleSceneLightingContent;
            public readonly GUIContent scaleClippingPlanesContent;
            public readonly GUIContent advancedSettingsContent;
            public readonly GUIContent functionalityIslandContent;
            public readonly GUIContent deleteButtonContent;

            public Styles()
            {
                scaleEntityPositionsContent = new GUIContent("Scale entity positions",
                    "When enabled, changing the world scale will also scale the world positions of all MARSEntities in the scene.");
                scaleEntityChildrenContent = new GUIContent("Scale entity children",
                    "When enabled, changing the world scale will also scale the local positions and scales of all children of MARSEntities in the scene.");
                scaleSceneAudioContent = new GUIContent("Scale audio",
                    "When enabled, changing the world scale will also scale the range of audio sources and reverb zones in the scene.");
                scaleSceneLightingContent = new GUIContent("Scale lighting",
                    "When enabled, changing the world scale will also scale the range of lights in the scene.");
                scaleClippingPlanesContent = new GUIContent("Scale clipping planes",
                    "When enabled, changing the world scale will also scale the MARS camera's clipping planes.");
                advancedSettingsContent = new GUIContent("Advanced", "Advanced settings.");
                functionalityIslandContent = new GUIContent("Functionality island", "Set this to override the default Functionality Island when this scene is loaded.");
                deleteButtonContent = new GUIContent("Delete");
            }
        }

        static readonly string k_TypeName = typeof(MARSSessionEditor).FullName;

        const string k_WorldScaleLabel = "World Scale";
        const float k_IconSize = 32f;
        const float k_ScaleInputWidth = 52f;
        const float k_ScaleSliderSnapThreshold = 0.015f;
        const int k_ScalePrecision = 1000;
        const int k_LabelOffset = 2; // Foldout elements are indented 2px farther than Label elements.

        const string k_NoSessionMessage = "Current scene does not have a MARS Session. Add a MARS Session to set world scale";

        const string k_EntityScaleWarning = "In order to preserve the relationships between entity positions and their condition parameters, " +
                                            "changing the world scale without scaling entity positions will scale all spatial condition parameters.";

        const string k_WorldScaleManualURL = "https://blogs.unity3d.com/2017/11/16/dealing-with-scale-in-ar/";

        static Styles s_Styles;

        bool m_DisplayingAdvancedOptions;
        bool m_DisplayingImageMarkerLibrary;

        List<int> m_MarkerSizeOptionIndexes;

        SerializedObject m_UserPrefsSerializedObject;
        SerializedProperty m_ScaleEntityPositionsProperty;
        SerializedProperty m_ScaleEntityChildrenProperty;
        SerializedProperty m_ScaleSceneAudioProperty;
        SerializedProperty m_ScaleSceneLightingProperty;
        SerializedProperty m_ScaleClippingPlanesProperty;
        SerializedProperty m_Requirements;
        SerializedProperty m_Island;
        SerializedProperty m_BuildSettings;
        SerializedProperty m_MarkerLibrary;

        // Delay creation of Styles till first access
        static Styles styles { get { return s_Styles ?? (s_Styles = new Styles()); } }

        static bool worldScaleExpanded
        {
            get { return EditorPrefsUtils.GetBool(k_TypeName, true); }
            set { EditorPrefsUtils.SetBool(k_TypeName, value); }
        }

        void OnEnable()
        {
            m_UserPrefsSerializedObject = new SerializedObject(MARSUserPreferences.instance);
            m_ScaleEntityPositionsProperty = m_UserPrefsSerializedObject.FindProperty("m_ScaleEntityPositions");
            m_ScaleEntityChildrenProperty = m_UserPrefsSerializedObject.FindProperty("m_ScaleEntityChildren");
            m_ScaleSceneAudioProperty = m_UserPrefsSerializedObject.FindProperty("m_ScaleSceneAudio");
            m_ScaleSceneLightingProperty = m_UserPrefsSerializedObject.FindProperty("m_ScaleSceneLighting");
            m_ScaleClippingPlanesProperty = m_UserPrefsSerializedObject.FindProperty("m_ScaleClippingPlanes");
            m_Requirements = serializedObject.FindProperty("m_Requirements");
            m_Island = serializedObject.FindProperty("m_Island");
            m_BuildSettings = serializedObject.FindProperty("m_BuildSettings");
            m_MarkerLibrary = serializedObject.FindProperty("m_MarkerLibrary");

            m_MarkerSizeOptionIndexes = new List<int>();

            var loadedMarkerLibrary = m_MarkerLibrary.objectReferenceValue as MarsMarkerLibrary;
            if (loadedMarkerLibrary != null && loadedMarkerLibrary.Count != m_MarkerSizeOptionIndexes.Count)
                SyncMarkerLibraryOptionIndexesWithLoadedLibrary(loadedMarkerLibrary);
        }

        void OnDisable()
        {
            MARSWorldScaleModule.worldScaleControlsShown = false;
            Tools.hidden = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            Tools.hidden = Selection.activeGameObject == ((MARSSession)target).gameObject && Tools.current == Tool.Scale;

            EditorGUILayout.Space();
            EditorGUIUtils.DrawSplitter();

            worldScaleExpanded = EditorGUIUtils.DrawFoldoutUI(
                worldScaleExpanded,
                true,
                k_WorldScaleLabel,
                MARSEditorGUI.Styles.HeaderToggle,
                MARSEditorGUI.Styles.HeaderLabel,
                OnWorldScaleGUI,
                RecordWorldScaleFoldoutAnalyticsEvent,
                OpenWorldScaleHelp,
                OpenWorldScalePopupOptions,
                AddWorldScaleItemsToTabMenu
            );

            MARSWorldScaleModule.worldScaleControlsShown = worldScaleExpanded;

            EditorGUIUtils.DrawSplitter();

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(m_Requirements, true);
            }

            m_DisplayingAdvancedOptions = EditorGUILayout.Foldout(m_DisplayingAdvancedOptions,
                styles.advancedSettingsContent, true);
            if (m_DisplayingAdvancedOptions)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_Island, styles.functionalityIslandContent);
                    EditorGUILayout.PropertyField(m_BuildSettings);
                }
            }

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                Rect labelWidthRect;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("", GUILayout.Width(EditorGUIUtility.labelWidth));
                    labelWidthRect = GUILayoutUtility.GetLastRect();
                    EditorGUILayout.PropertyField(m_MarkerLibrary, new GUIContent(""));
                }

                if (m_MarkerLibrary.objectReferenceValue != null)
                {
                    m_DisplayingImageMarkerLibrary = EditorGUI.Foldout(labelWidthRect, m_DisplayingImageMarkerLibrary,
                        m_MarkerLibrary.displayName, true);

                    if (m_DisplayingImageMarkerLibrary)
                    {
                        GUILayout.Space(5);
                        EditorGUIUtils.DrawSplitter();
                        GUILayout.Space(2);

                        DrawMarkerLibrary();
                    }
                }
                else
                {
                    labelWidthRect.x += k_LabelOffset;
                    GUI.Label(labelWidthRect, m_MarkerLibrary.displayName);
                }

                var markerLibraryModule = ModuleLoaderCore.instance.GetModule<MARSMarkerLibraryModule>();
                if (changed.changed && markerLibraryModule != null)
                {
                    markerLibraryModule.UpdateMarkerDataFromLibraries();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        void SyncMarkerLibraryOptionIndexesWithLoadedLibrary(MarsMarkerLibrary loadedLib)
        {
            while (m_MarkerSizeOptionIndexes.Count != loadedLib.Count)
            {
                if (m_MarkerSizeOptionIndexes.Count < loadedLib.Count)
                {
                    m_MarkerSizeOptionIndexes.Add(m_MarkerSizeOptionIndexes.Count > 0
                        ? ImageMarkerEditorUtils.GetSelectedMarsMarkerSizeOption(
                            loadedLib[m_MarkerSizeOptionIndexes.Count - 1].Size) : 1); // 1 => by default is a postcard
                }
                else
                {
                    m_MarkerSizeOptionIndexes.RemoveAt(m_MarkerSizeOptionIndexes.Count - 1);
                }
            }
        }

        void DrawMarkerLibrary()
        {
            const int imageMarkerTextureSize = 70;
            const int offsetImageMarkerInfoUI = 90;
            const int spaceBetweenGUISplitters = 5;

            var loadedMarkerLibrary = m_MarkerLibrary.objectReferenceValue as MarsMarkerLibrary;
            if (loadedMarkerLibrary == null)
                return;

            // Sync selected options with current image marker library in case user has other inspector and adds/removes
            // image markers from the current library.
            if (m_MarkerSizeOptionIndexes.Count != loadedMarkerLibrary.Count)
                SyncMarkerLibraryOptionIndexesWithLoadedLibrary(loadedMarkerLibrary);

            for (var i = 0; i < loadedMarkerLibrary.Count; i++)
            {
                GUILayout.Space(spaceBetweenGUISplitters);

                var currentSelectedOptionIndex = m_MarkerSizeOptionIndexes[i];
                var imageMarkerInfoRect = ImageMarkerEditorUtils.DrawImageMarkerInfoContentsAtIndex(loadedMarkerLibrary,
                    i, ref currentSelectedOptionIndex, offsetImageMarkerInfoUI);
                m_MarkerSizeOptionIndexes[i] = currentSelectedOptionIndex;

                var textureToDraw = loadedMarkerLibrary[i].Texture;

                var texturePos = new Rect(15, imageMarkerInfoRect.y, imageMarkerTextureSize,
                    imageMarkerTextureSize);
                if (textureToDraw != null)
                    GUI.DrawTexture(texturePos, textureToDraw);
                else
                    EditorGUI.HelpBox(texturePos, "\n\n      None    ", MessageType.None);

                const string deleteFromSession = "Delete marker form MARSSession.";
                if (GUILayout.Button(styles.deleteButtonContent, GUILayout.Width(imageMarkerTextureSize)))
                {
                    Undo.RecordObject(loadedMarkerLibrary, deleteFromSession);
                    loadedMarkerLibrary.RemoveAt(i);
                }

                GUILayout.Space(spaceBetweenGUISplitters);
                EditorGUIUtils.DrawSplitter();
            }
        }

        void OnWorldScaleGUI()
        {
            m_UserPrefsSerializedObject.Update();
            using (new EditorGUILayout.VerticalScope(MARSEditorGUI.Styles.NoMarginScopeAlignment))
            {
                var scene = SceneManager.GetActiveScene();
                var isSession = MARSUtils.GetMARSSession(scene) != null;
                var spacePadding = MARSEditorGUI.Styles.AreaAlignmentWithPadding.padding.vertical;

                GUILayout.Space(spacePadding);

                if (isSession )
                    DrawScaleArea();

                DrawWorldScaleHelpArea(isSession);
                GUILayout.Space(spacePadding);
            }
        }

        static void DrawScaleArea()
        {
            using (new EditorGUILayout.VerticalScope(MARSEditorGUI.Styles.NoVerticalMarginScopeAlignment))
            {
                var scaleModule = MARSWorldScaleModule.instance;

                var worldScale = MARSWorldScaleModule.GetWorldScale();
                using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode))
                {
                    using (new EditorGUILayout.HorizontalScope(MARSEditorGUI.Styles.NoVerticalMarginScopeAlignment))
                    {
                        using (new EditorGUILayout.VerticalScope())
                        {
                            Rect minScaleIconRect, maxScaleIconRect;
                            using (new EditorGUILayout.HorizontalScope())
                            {
                                GUILayoutOption[] layoutOptions = { GUILayout.Width(k_IconSize), GUILayout.Height(k_IconSize) };
                                GUILayout.Box(scaleModule.smallScaleIcon.scaleIcon, GUIStyle.none, layoutOptions);
                                minScaleIconRect = GUILayoutUtility.GetLastRect();
                                GUILayout.FlexibleSpace();
                                GUILayout.Box(scaleModule.largeScaleIcon.scaleIcon, GUIStyle.none, layoutOptions);
                                maxScaleIconRect = GUILayoutUtility.GetLastRect();
                            }

                            // Since the user can override their min & max scale exponents, here we calculate the fraction
                            // between min & max where 0 falls, so we know where to position the 1:1 scale icon & slider snap.
                            var scaleExponentRange = (float)Mathf.Abs(scaleModule.maxScaleExponent - scaleModule.minScaleExponent);
                            var zeroPoint = Mathf.Abs(scaleModule.minScaleExponent) / scaleExponentRange;
                            var scaleOneIconRect = minScaleIconRect;
                            scaleOneIconRect.x = minScaleIconRect.x + (maxScaleIconRect.x - minScaleIconRect.x) * zeroPoint;

                            if (scaleModule.visualsZeroIndex > -1)
                                GUI.Box(scaleOneIconRect, scaleModule.scaleOneIcon.scaleIcon, GUIStyle.none);

                            using (var change = new EditorGUI.ChangeCheckScope())
                            {
                                var sliderValue = GUILayout.HorizontalSlider(Mathf.InverseLerp(
                                    scaleModule.minScaleExponent, scaleModule.maxScaleExponent,
                                    Mathf.Log10(worldScale)), 0f, 1f);

                                if (Mathf.Abs(sliderValue - zeroPoint) < k_ScaleSliderSnapThreshold)
                                    sliderValue = zeroPoint;

                                if (change.changed)
                                {
                                    worldScale = Mathf.Pow(10f, Mathf.Lerp(scaleModule.minScaleExponent,
                                        scaleModule.maxScaleExponent, sliderValue));
                                    scaleModule.AdjustWorldScale(worldScale);
                                }
                            }
                        }

                        using (new EditorGUILayout.VerticalScope())
                        {
                            GUILayout.Space(k_IconSize + MARSEditorGUI.Styles.SingleLineButton.padding.vertical);

                            using (var change = new EditorGUI.ChangeCheckScope())
                            {
                                var roundScale = Mathf.Round(worldScale * k_ScalePrecision) / k_ScalePrecision;
                                var fieldValue = EditorGUILayout.DelayedFloatField(roundScale,
                                    GUILayout.Width(k_ScaleInputWidth));

                                if (change.changed)
                                {
                                    worldScale = Mathf.Clamp(fieldValue, scaleModule.minScale, scaleModule.maxScale);
                                    scaleModule.AdjustWorldScale(worldScale);
                                }
                            }
                        }
                    }

                    scaleModule.UpdateScaleReference();
                }
            }
        }

        static void DrawWorldScaleHelpArea(bool isMARSSession)
        {
            using (new EditorGUILayout.VerticalScope(MARSEditorGUI.Styles.NoVerticalMarginScopeAlignment))
            {
                if (!isMARSSession)
                    EditorGUILayout.HelpBox(k_NoSessionMessage, MessageType.Info);
                else if (!MARSUserPreferences.instance.scaleEntityPositions)
                    EditorGUILayout.HelpBox(k_EntityScaleWarning, MessageType.Info);
            }
        }

        GenericMenu OpenWorldScalePopupOptions()
        {
            var menu = new GenericMenu();
            var userPrefs = MARSUserPreferences.instance;

            menu.AddItem(styles.scaleEntityPositionsContent, userPrefs.scaleEntityPositions, () =>
            {
                m_ScaleEntityPositionsProperty.boolValue = !userPrefs.scaleEntityPositions;
                m_UserPrefsSerializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(styles.scaleSceneAudioContent, userPrefs.scaleSceneAudio, () =>
            {
                m_ScaleSceneAudioProperty.boolValue = !userPrefs.scaleSceneAudio;
                m_UserPrefsSerializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(styles.scaleEntityChildrenContent, userPrefs.scaleEntityChildren, () =>
            {
                m_ScaleEntityChildrenProperty.boolValue = !userPrefs.scaleEntityChildren;
                m_UserPrefsSerializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(styles.scaleSceneLightingContent, userPrefs.scaleSceneLighting, () =>
            {
                m_ScaleSceneLightingProperty.boolValue = !userPrefs.scaleSceneLighting;
                m_UserPrefsSerializedObject.ApplyModifiedProperties();
            });

            menu.AddItem(styles.scaleClippingPlanesContent, userPrefs.scaleClippingPlanes, () =>
            {
                m_ScaleClippingPlanesProperty.boolValue = !userPrefs.scaleClippingPlanes;
                m_UserPrefsSerializedObject.ApplyModifiedProperties();
            });

            return menu;
        }

        static void OpenWorldScaleHelp()
        {
            Application.OpenURL(k_WorldScaleManualURL);
        }

        static GenericMenu AddWorldScaleItemsToTabMenu()
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Select World Scale Module"), false, () =>
            {
                Selection.activeObject = MARSWorldScaleModule.instance;
                EditorGUIUtility.PingObject(Selection.activeObject);
            } );
            return menu;
        }

        static void RecordWorldScaleFoldoutAnalyticsEvent(bool expanded)
        {
            EditorEvents.UiComponentUsed.Send(new UiComponentArgs
            {
                label = string.Format("MARS Session / {0}", k_WorldScaleLabel),
                active = expanded
            });
        }
    }
}
