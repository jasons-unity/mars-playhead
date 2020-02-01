using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public static class ImageMarkerEditorUtils
    {
        /// <summary>
        /// Draw info contents (texture thumbnail, marker tag and size) for an image marker inside a library
        /// </summary>
        /// <param name="loadedLibrary">library to draw the marker</param>
        /// <param name="imageMarkerLibraryIndex">Index of the marker inside the library</param>
        /// <param name="selectedMarkerSizeOptionIndex">Selected option for pre-set physical sizes</param>
        /// <param name="offsetRight">How much space to leave to draw the marker info to the left side</param>
        /// /// <returns>
        ///   <para>The rect that covers the drawn ImageMarker info</para>
        /// </returns>
        public static Rect DrawImageMarkerInfoContentsAtIndex(MarsMarkerLibrary loadedLibrary, int imageMarkerLibraryIndex, ref int selectedMarkerSizeOptionIndex, int offsetRight = 0)
        {
            var drawnMarkerInfoRect = Rect.zero;
            if (loadedLibrary.Count == 0)
                return drawnMarkerInfoRect;
            
            if (imageMarkerLibraryIndex == MarkerConditionInspector.UnselectedMarkerIndex)
            {
                EditorGUILayout.HelpBox(MarkerConditionInspector.styles.selectMarkerDefinitionStr, MessageType.Info);
                return GUILayoutUtility.GetLastRect();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(offsetRight);
                using (new EditorGUILayout.VerticalScope())
                {
                    using (var checkObjectField = new EditorGUI.ChangeCheckScope())
                    {
                        var texture2D = EditorGUILayout.ObjectField(MarkerConditionInspector.styles.textureAsset,
                            loadedLibrary[imageMarkerLibraryIndex].Texture,
                            typeof(Texture2D),
                            false,
                            GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Texture2D;

                        if (checkObjectField.changed)
                        {
                            Undo.RecordObject(loadedLibrary, "Change marker texture");
                            loadedLibrary.SetTexture(imageMarkerLibraryIndex, texture2D);
                        }
                    }

                    var textureFieldRect = GUILayoutUtility.GetLastRect();
                    drawnMarkerInfoRect = textureFieldRect;

                    using (var checkMarkerLabel = new EditorGUI.ChangeCheckScope())
                    {
                        var markerLabel = EditorGUILayout.TextField(MarkerConditionInspector.styles.markerID, loadedLibrary[imageMarkerLibraryIndex].Label);
                        if (checkMarkerLabel.changed)
                        {
                            Undo.RecordObject(loadedLibrary, "Change marker label");
                            loadedLibrary.SetLabel(imageMarkerLibraryIndex, markerLabel);
                        }

                    }
                    using (var checkSize = new EditorGUI.ChangeCheckScope())
                    {
                        selectedMarkerSizeOptionIndex = EditorGUILayout.Popup(MarkerConditionInspector.styles.markerSize,selectedMarkerSizeOptionIndex,MarkerConstants.MarkerSizeOptions);

                        if (checkSize.changed && selectedMarkerSizeOptionIndex != 0) // != 0: Selecting 'Custom' should preserve the currently set size
                        {
                            Undo.RecordObject(loadedLibrary, "Change marker Size option");
                            loadedLibrary.SetSize(imageMarkerLibraryIndex, MarkerConstants.MarkerSizeOptionsValuesInMeters[selectedMarkerSizeOptionIndex]);
                        }
                    }

                    Vector2 setMarkerSize;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        // if offset == 0 looks better with flexspace.
                        if (offsetRight == 0)
                            GUILayout.FlexibleSpace();
                        using (var checkVec2 = new EditorGUI.ChangeCheckScope())
                        {
                            setMarkerSize = EditorGUILayout.Vector2Field("", loadedLibrary[imageMarkerLibraryIndex].Size);
                            setMarkerSize = new Vector2(
                                Mathf.Max(MarkerConstants.MinimumPhysicalMarkerSizeWidthInMeters, setMarkerSize.x),
                                Mathf.Max(MarkerConstants.MinimumPhysicalMarkerSizeHeightInMeters, setMarkerSize.y));

                            if (checkVec2.changed)
                            {
                                Undo.RecordObject(loadedLibrary, "Change marker Size");
                                loadedLibrary.SetSize(imageMarkerLibraryIndex, setMarkerSize);
                                selectedMarkerSizeOptionIndex = GetSelectedMarsMarkerSizeOption(setMarkerSize);
                            }
                        }
                    }

                    if (setMarkerSize.x <= MarkerConstants.MinimumPhysicalMarkerSizeWidthInMeters ||
                        setMarkerSize.y <= MarkerConstants.MinimumPhysicalMarkerSizeHeightInMeters)
                    {
                        EditorGUILayout.HelpBox(MarkerConditionInspector.styles.minimumMarkerDefinitionDimensionsStr, MessageType.Warning);
                    }

                    var lastUIElementRect = GUILayoutUtility.GetLastRect();

                    drawnMarkerInfoRect.width = textureFieldRect.width + offsetRight;
                    drawnMarkerInfoRect.height = lastUIElementRect.y - textureFieldRect.y + lastUIElementRect.height;
                }
            }
            return drawnMarkerInfoRect;
        }

        public static int GetSelectedMarsMarkerSizeOption(Vector2 sizeToCompare)
        {
            // Start from 1 since 0 is the custom option
            for (var i = 1; i < MarkerConstants.MarkerSizeOptionsValuesInMeters.Length; i++)
            {
                if (sizeToCompare == MarkerConstants.MarkerSizeOptionsValuesInMeters[i])
                    return i;
            }

            return 0;
        }
    }
}
