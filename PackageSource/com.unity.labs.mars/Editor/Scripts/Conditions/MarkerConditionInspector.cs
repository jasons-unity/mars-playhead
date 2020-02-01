using System;
using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ComponentEditor(typeof(MarkerCondition))]
    public class MarkerConditionInspector : ComponentInspector
    {
        const int k_SpaceBetweenImagesHorizontal = 10;
        const int k_SpaceBetweenImagesVertical = 20;
        const int k_ImageBrowserHeight = 200;
        const int k_ImageMarkersSize = 57;
        const int k_EdgeSubtraction = 35;
        
        static readonly int k_UnselectedMarkerIndex = -1;
        public static int UnselectedMarkerIndex => k_UnselectedMarkerIndex;

        const int k_MarkerSelectionThickness = 1;
        static readonly Color k_MarkerSelectionColor = new Color(0.212f, 0.353f, 1);

        const string k_NoMarkerLibraryModuleFoundStr = "Marker Library Module not Loaded.\n" +
            "Cannot provide more information on Marker Definition";

        const string k_NoCurrentMarkerLibraryAssigned = "No marker library assigned in this scene.\n" +
            "On the MARS Session, select an existing library or create a new one via Assets -> Create -> MARS -> Marker Library.";

        private const string k_CurrentMarkerGuidDoesntMatchCurrentLibrary = "Current internal marker ID doesnt match any of the " +
                                                                            "IDs in the current library. It might belong to other library.";

        public class Styles
        {
            public readonly string minimumMarkerDefinitionDimensionsStr =
                $"Dimensions must be at least {MarkerConstants.MinimumPhysicalMarkerSizeInCentimeters.x.ToString()}cm " +
                $"x {MarkerConstants.MinimumPhysicalMarkerSizeInCentimeters.y.ToString()}cm.";
            public readonly string selectMarkerDefinitionStr = "Select an image marker to match queries against it.";
            
            public readonly GUIContent addImageMarkerButton;
            public readonly GUIContent removeImageMarkerButton;
            public readonly GUIContent selectLoadedImageMarkerLibrary;
            public readonly GUIContent topMarkerLibraryContent;
            public readonly GUIContent textureAsset;
            public readonly GUIContent markerID;
            public readonly GUIContent markerSize;

            public readonly GUIStyle topMarkerLibraryStyle;
            public readonly GUIStyle footerButtonStyle;

            internal Styles()
            {
                addImageMarkerButton = EditorGUIUtility.IconContent("Toolbar Plus",
                    "Adds an image marker to the current marker library loaded for this scene.");
                removeImageMarkerButton = EditorGUIUtility.IconContent("Toolbar Minus",
                    "Removes the current selected image marker from the current marker library loaded.");
                selectLoadedImageMarkerLibrary = new GUIContent("...",
                    "Selects the current image marker library loaded for this scene.");
                topMarkerLibraryContent = new GUIContent("  Marker Library",
                    "Image marker library loaded for this scene.");
                textureAsset = new GUIContent("Texture asset", "");
                markerID = new GUIContent("Marker Label",
                    "(Optional) A label on this image which you can use to identify in scripts");
                markerSize = new GUIContent("Size (meters)",
                    "Physical size in meters the image marker would have.");

                topMarkerLibraryStyle = GUI.skin.GetStyle("Box");
                topMarkerLibraryStyle.alignment = TextAnchor.MiddleLeft;
                topMarkerLibraryStyle.normal.textColor = Color.white;

                footerButtonStyle = new GUIStyle("RL FooterButton");
            }
        }

        static Styles s_Styles;
        public static Styles styles { get { return s_Styles ?? (s_Styles = new Styles()); } }


        Rect m_ImageMarkerScrollAreaRectGlobal;
        int m_SelectedMarkerSizeOptionIndex;

        MarsMarkerLibrary m_CurrentMarkerLibrary;
        int m_SelectedImageMarkerIndex = k_UnselectedMarkerIndex;

        Vector2 m_ImageBrowserScrollPos;
        bool m_FlagSelectingMarker;

        SerializedProperty m_MarkerGuidProperty;

        public override void OnEnable()
        {
            base.OnEnable();
            const string markerGuidProp = "m_MarkerGuid";
            m_MarkerGuidProperty = serializedObject.FindProperty(markerGuidProp);

            var markerLibraryModule = ModuleLoaderCore.instance.GetModule<MARSMarkerLibraryModule>();
            if (markerLibraryModule != null)
                markerLibraryModule.UpdateMarkerDataFromLibraries();

            m_CurrentMarkerLibrary = MARSSession.Instance.MarkerLibrary;
            if (m_CurrentMarkerLibrary == null)
                return;

            TryFindCurrentSelectedImageMarkerIndex();
        }
        bool TryFindCurrentSelectedImageMarkerIndex()
        {
            if (m_CurrentMarkerLibrary == null)
            {
                m_SelectedImageMarkerIndex = k_UnselectedMarkerIndex;
                return false;
            }

            for (var i = 0; i < m_CurrentMarkerLibrary.Count; i++)
            {
                if (m_MarkerGuidProperty.stringValue == m_CurrentMarkerLibrary[i].MarkerId.ToString())
                {
                    m_SelectedImageMarkerIndex = i;
                    return true;
                }
            }
            m_SelectedImageMarkerIndex = k_UnselectedMarkerIndex;
            return false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var markerLibraryModule = ModuleLoaderCore.instance.GetModule<MARSMarkerLibraryModule>();
                if (markerLibraryModule == null)
                {
                    EditorGUILayout.HelpBox(k_NoMarkerLibraryModuleFoundStr, MessageType.Info);
                    return;
                }

                // Set here in case we have several inspectors and user removes marker library reference
                // from MARSSession while visualizing
                m_CurrentMarkerLibrary = MARSSession.Instance.MarkerLibrary;
                if (m_CurrentMarkerLibrary == null)
                {
                    EditorGUILayout.HelpBox(k_NoCurrentMarkerLibraryAssigned, MessageType.Info);
                    return;
                }
                
                // This is necessary in order to preserve the selection focus of this marker if we remove a marker
                // from outside this custom inspector (like the Marker library inspector) 
                if (m_SelectedImageMarkerIndex < 0 || m_SelectedImageMarkerIndex >= m_CurrentMarkerLibrary.Count ||
                    m_MarkerGuidProperty.stringValue != m_CurrentMarkerLibrary[m_SelectedImageMarkerIndex].MarkerId.ToString())
                {
                    if (!string.IsNullOrEmpty(m_MarkerGuidProperty.stringValue) && !TryFindCurrentSelectedImageMarkerIndex())
                    {
                        EditorGUILayout.HelpBox(k_CurrentMarkerGuidDoesntMatchCurrentLibrary, MessageType.Info);
                    }
                }

                DrawImageMarkerLibraryInspector();
                
                ImageMarkerEditorUtils.DrawImageMarkerInfoContentsAtIndex(m_CurrentMarkerLibrary, m_SelectedImageMarkerIndex, ref m_SelectedMarkerSizeOptionIndex);

                if (check.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    MARSSession.Instance.CheckCapabilities();
                    isDirty = true;
                }
            }
        }

        void DrawImageMarkerLibraryInspector()
        {
            var imageMarkerInspectorWidth = EditorGUIUtility.currentViewWidth - k_EdgeSubtraction;
            GUILayout.Box("", GUILayout.Height(k_ImageBrowserHeight),
                GUILayout.Width(imageMarkerInspectorWidth));
            var scrollAreaPosRect = GUILayoutUtility.GetLastRect();

            // +4 space to fit buttons on top if img marker lib box
            var verticalBoxOffset = EditorGUIUtility.singleLineHeight + 4;
            scrollAreaPosRect = new Rect(scrollAreaPosRect.x, scrollAreaPosRect.y + verticalBoxOffset,
                scrollAreaPosRect.width - 1, scrollAreaPosRect.height - verticalBoxOffset - 1); // -1 to see box edges.

            var texturesToPlaceHorizontally = ((int)scrollAreaPosRect.width - k_EdgeSubtraction + k_SpaceBetweenImagesHorizontal * 0.5f)
                / (k_ImageMarkersSize + k_SpaceBetweenImagesHorizontal);
            var texturesToPlaceVertically = Mathf.CeilToInt(m_CurrentMarkerLibrary.Count / texturesToPlaceHorizontally);

            var scrollViewHeight = texturesToPlaceVertically * (k_ImageMarkersSize + k_SpaceBetweenImagesHorizontal
                + EditorGUIUtility.singleLineHeight);
            var scrollAreaRect = new Rect(0, verticalBoxOffset,
                scrollAreaPosRect.width - k_EdgeSubtraction,
                scrollViewHeight + verticalBoxOffset - EditorGUIUtility.singleLineHeight);

            var offsetX = (((scrollAreaRect.width + k_SpaceBetweenImagesHorizontal * 0.5f)
                / (k_ImageMarkersSize + k_SpaceBetweenImagesHorizontal)) / texturesToPlaceHorizontally) - 1;

            DrawTopImageMarkerInspectorBox(scrollAreaPosRect, verticalBoxOffset);

            using (var scrollScope = new GUI.ScrollViewScope(scrollAreaPosRect, m_ImageBrowserScrollPos,
                scrollAreaRect, false, true))
            {
                // This rectangle compensates the movement of the scroll position with the scrollAreaRect
                // so we can catch the mouse pressing correctly and not select image markers clipped by the scroll area.
                m_ImageMarkerScrollAreaRectGlobal = new Rect(0, verticalBoxOffset + m_ImageBrowserScrollPos.y,
                    scrollAreaPosRect.width, scrollAreaPosRect.height);

                m_ImageBrowserScrollPos = scrollScope.scrollPosition;
                var currentEvent = Event.current;

                var counter = 0;
                for (var j = 0; j < texturesToPlaceVertically; j++)
                {
                    for (var i = 0; i < texturesToPlaceHorizontally; i++)
                    {
                        var xPos = (k_SpaceBetweenImagesHorizontal + i * (k_ImageMarkersSize
                            + k_SpaceBetweenImagesHorizontal)) + (offsetX * scrollAreaRect.width * 0.5f);
                        var yPos = EditorGUIUtility.singleLineHeight + (k_SpaceBetweenImagesVertical * 0.5f + j
                            * (k_ImageMarkersSize + k_SpaceBetweenImagesVertical));

                        if (counter < m_CurrentMarkerLibrary.Count)
                            DrawImageMarker(xPos, yPos, counter, currentEvent);

                        counter++;
                    }
                }
                // Process here since we don't care if user drag/drops somewhere outside of img marker library box
                ProcessDragAndDropTexturesToMarkerConditionInspector(currentEvent);
            }

            DrawAddRemoveImageMarkerButtons(imageMarkerInspectorWidth);
        }

        void DrawAddRemoveImageMarkerButtons(float imageMarkerInspectorWidth)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                const int addRemoveButtonSize = 30;
                const int pickerID = 0xC0FFEE;
                const string objectSelectorClosed = "ObjectSelectorClosed";

                GUILayout.Space(imageMarkerInspectorWidth - addRemoveButtonSize * 2);
                if (GUILayout.Button(styles.addImageMarkerButton, styles.footerButtonStyle,
                    GUILayout.Width(addRemoveButtonSize), GUILayout.Height(addRemoveButtonSize)))
                {
                    EditorGUIUtility.ShowObjectPicker<Texture2D>(null, false, "", pickerID);
                    m_FlagSelectingMarker = true;
                }

                if (Event.current.commandName == objectSelectorClosed && EditorGUIUtility.GetObjectPickerControlID() == pickerID && m_FlagSelectingMarker)
                {
                    var selectedTexture = EditorGUIUtility.GetObjectPickerObject() as Texture2D;
                
                    const string addEmptyImage = "Add an empty image marker";
                    Undo.RecordObject(m_CurrentMarkerLibrary, addEmptyImage);

                    m_CurrentMarkerLibrary.CreateAndAdd();
                    m_SelectedMarkerSizeOptionIndex = 1; // By default the new created marker has a postcard size.

                    m_CurrentMarkerLibrary.SetGuid(m_CurrentMarkerLibrary.Count - 1, Guid.NewGuid());
                    m_CurrentMarkerLibrary.SetTexture(m_CurrentMarkerLibrary.Count - 1, selectedTexture);
                    
                    EditorUtility.SetDirty(m_CurrentMarkerLibrary);
                    m_CurrentMarkerLibrary.SaveMarkerLibrary();
                    m_FlagSelectingMarker = false;
                }

                using (new EditorGUI.DisabledScope(m_CurrentMarkerLibrary.Count <= 0 || m_SelectedImageMarkerIndex == k_UnselectedMarkerIndex))
                {
                    if (GUILayout.Button(styles.removeImageMarkerButton, styles.footerButtonStyle,
                        GUILayout.Width(addRemoveButtonSize), GUILayout.Height(addRemoveButtonSize)))
                    {
                        const string removeSelectedImage = "Remove selected image marker";
                        Undo.RecordObject(m_CurrentMarkerLibrary, removeSelectedImage);

                        m_CurrentMarkerLibrary.RemoveAt(m_SelectedImageMarkerIndex);
                        
                        EditorUtility.SetDirty(m_CurrentMarkerLibrary);
                        m_CurrentMarkerLibrary.SaveMarkerLibrary();

                        m_SelectedImageMarkerIndex = k_UnselectedMarkerIndex;
                    }
                }
            }
        }

        void DrawTopImageMarkerInspectorBox(Rect imageMarkerPos, float verticalBoxOffset)
        {
            var topImageMarkerBoxRect = new Rect(imageMarkerPos.x, imageMarkerPos.y - verticalBoxOffset,
                imageMarkerPos.width, verticalBoxOffset);

            GUI.Box(topImageMarkerBoxRect, styles.topMarkerLibraryContent, styles.topMarkerLibraryStyle);

            if (GUI.Button(
                new Rect(topImageMarkerBoxRect.x + GUI.skin.box.CalcSize(styles.topMarkerLibraryContent).x,
                    topImageMarkerBoxRect.y + 2, 19, 14),
                styles.selectLoadedImageMarkerLibrary))
            {
                Selection.activeObject = m_CurrentMarkerLibrary;
            }
        }

        // Stub until multiple image markers are supported per marker condition.
        void DrawMultipleImageMarkerActionButtons(Rect topBoxRect)
        {
            const int imageMarkerTopBoxButtonSize = 75;
            const string activateAll = "Activate All";
            const string deselectAll = "Deselect All";
            if (GUI.Button(new Rect(9 + topBoxRect.width - imageMarkerTopBoxButtonSize * 2, topBoxRect.y + 1,
                imageMarkerTopBoxButtonSize, topBoxRect.height - 3), activateAll))
            {
                Debug.Log(activateAll);
            }

            if (GUI.Button(new Rect(13 + topBoxRect.width - imageMarkerTopBoxButtonSize, topBoxRect.y + 1,
                imageMarkerTopBoxButtonSize, topBoxRect.height - 3), deselectAll))
            {
                Debug.Log(deselectAll);
            }
        }

        void DrawImageMarker(float xPos, float yPos, int markerIndex, Event currentEvent)
        {
            var currentMarkerDefinition = m_CurrentMarkerLibrary[markerIndex];

            var imageMarkerRect = new Rect(xPos, yPos, k_ImageMarkersSize, k_ImageMarkersSize);
            var markerSelectionRect = new Rect(imageMarkerRect.x - 2, imageMarkerRect.y - 2,
                imageMarkerRect.width + 4, imageMarkerRect.height + 2 + EditorGUIUtility.singleLineHeight);

            if (currentEvent.button == 0 && currentEvent.isMouse && imageMarkerRect.Contains(currentEvent.mousePosition)
                && m_ImageMarkerScrollAreaRectGlobal.Contains(currentEvent.mousePosition))
            {
                m_SelectedImageMarkerIndex = markerIndex;
                m_MarkerGuidProperty.stringValue = currentMarkerDefinition.MarkerId.ToString();

                m_SelectedMarkerSizeOptionIndex = ImageMarkerEditorUtils.GetSelectedMarsMarkerSizeOption(m_CurrentMarkerLibrary[m_SelectedImageMarkerIndex].Size);
                serializedObject.ApplyModifiedProperties();
                // Without this we will get Control's Argument exception if the selected marker has a size of
                // MarsMarkerDefinition.MinimumPhysicalMarkerSizeInMeters since the GUI changes generating a helpbox warning
                GUIUtility.ExitGUI();
            }

            if (currentMarkerDefinition.Texture)
                GUI.DrawTexture(imageMarkerRect, currentMarkerDefinition.Texture);
            else
                EditorGUI.HelpBox(imageMarkerRect, "\n\n    None    ", MessageType.None);


            using (new EditorGUI.DisabledScope(currentMarkerDefinition.Label == MarsMarkerLibrary.DefaultMarkerDefinitionLabel))
            {
                GUI.Label(new Rect(xPos, yPos + k_ImageMarkersSize, k_ImageMarkersSize,
                    EditorGUIUtility.singleLineHeight), currentMarkerDefinition.Label, EditorStyles.wordWrappedLabel);
            }

            if (markerIndex == m_SelectedImageMarkerIndex)
            {
                if(m_CurrentMarkerLibrary[markerIndex].MarkerId == Guid.Empty)
                    Debug.LogError("ERROR: Marker library should be rebuilt since it contains empty guid.");
                
                DrawHollowRectangle(markerSelectionRect, k_MarkerSelectionThickness, k_MarkerSelectionColor);
                Repaint();
            }
        }

        static void DrawHollowRectangle(Rect pos, float thickness, Color color)
        {
            var left = new Rect(pos.x, pos.y, thickness, pos.height);
            EditorGUI.DrawRect(left, color);
            var top = new Rect(pos.x, pos.y, pos.width, thickness);
            EditorGUI.DrawRect(top, color);
            var right = new Rect(pos.x + pos.width - thickness, pos.y, thickness, pos.height);
            EditorGUI.DrawRect(right, color);
            var bottom = new Rect(pos.x, pos.y + pos.height - thickness, pos.width, thickness);
            EditorGUI.DrawRect(bottom, color);
        }

        void ProcessDragAndDropTexturesToMarkerConditionInspector(Event currentEvent)
        {
            if (currentEvent.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.DragPerform)
            {
                Undo.RecordObject(m_CurrentMarkerLibrary, "Drag images to marker library");
                for (var i = 0; i < DragAndDrop.objectReferences.Length; i++)
                {
                    var draggedTexture = DragAndDrop.objectReferences[i] as Texture2D;
                    if (draggedTexture != null)
                    {
                        m_CurrentMarkerLibrary.CreateAndAdd();
                       
                        EditorUtility.SetDirty(m_CurrentMarkerLibrary);
                        m_CurrentMarkerLibrary.SaveMarkerLibrary();
                        
                        m_CurrentMarkerLibrary.SetGuid(m_CurrentMarkerLibrary.Count - 1, Guid.NewGuid());
                        m_CurrentMarkerLibrary.SetTexture(m_CurrentMarkerLibrary.Count - 1, draggedTexture);
                    }
                }

                currentEvent.Use();
            }
        }
    }
}
