using System;
using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    public class MARSMarkerLibraryModule : ScriptableSettings<MARSMarkerLibraryModule>, IModule
    {
        static readonly GUIContent[] k_NoMarkerDefinitions = { new GUIContent("No Library or Marker Definitions",
            "The current scene either does not have a Marker Library or that Library is empty") };
        static readonly GUIContent k_ContentNotFound = new GUIContent("MarkerID not found in any Marker Library.");
        static readonly GUIContent k_SelectFromActiveLibrary = new GUIContent("Select from Active Library");

        readonly Dictionary<Guid, MarsMarkerLibrary> m_MarkerIDToLibrary = new Dictionary<Guid, MarsMarkerLibrary>();
        readonly Dictionary<int, Guid> m_GUIContentIDToMarkerID = new Dictionary<int, Guid>();
        readonly Dictionary<Guid, int> m_MarkerIDToGUIContentID = new Dictionary<Guid, int>();

        MarsMarkerLibrary[] m_MarkerLibraries;
        GUIContent[] m_MarkerDefinitionsContent;

        public void LoadModule()
        {
            UpdateMarkerDataFromLibraries();
        }

        public void UnloadModule()
        {
            m_MarkerIDToLibrary.Clear();
            m_GUIContentIDToMarkerID.Clear();
            m_MarkerIDToGUIContentID.Clear();
        }

        public void UpdateMarkerDataFromLibraries()
        {
            m_MarkerIDToLibrary.Clear();
            m_GUIContentIDToMarkerID.Clear();
            m_MarkerIDToGUIContentID.Clear();

            m_MarkerLibraries = Resources.FindObjectsOfTypeAll(typeof(MarsMarkerLibrary)) as MarsMarkerLibrary[];
            if (m_MarkerLibraries == null || m_MarkerLibraries.Length == 0)
                return;

            MarsMarkerLibrary activeLibrary = null;
            if (MARSSession.Instance != null && MARSSession.Instance.MarkerLibrary != null)
                activeLibrary = MARSSession.Instance.MarkerLibrary;

            var activeFound = false;
            foreach (var markerLibrary in m_MarkerLibraries)
            {
                if (markerLibrary == null)
                    continue;

                var isActive = activeLibrary == markerLibrary;
                if (isActive)
                {
                    m_MarkerDefinitionsContent = new GUIContent[markerLibrary.Count + 1];
                    m_MarkerDefinitionsContent[0] = k_SelectFromActiveLibrary;
                    activeFound = true;
                }

                for (var i = 0; i < markerLibrary.Count; i++)
                {
                    var markerDefinition = markerLibrary[i];
                    m_MarkerIDToLibrary[markerDefinition.MarkerId] = markerLibrary;

                    if (!isActive)
                        continue;

                    m_MarkerDefinitionsContent[i + 1] = new GUIContent(markerDefinition.Label, markerDefinition.Texture,
                        markerDefinition.Texture == null ?
                            $"Marker Definition with Marker ID {markerDefinition.MarkerId}" :
                            $"Marker Definition with texture {AssetDatabase.GetAssetPath(markerDefinition.Texture)}");
                    m_GUIContentIDToMarkerID[i + 1] = markerDefinition.MarkerId;
                    m_MarkerIDToGUIContentID[markerDefinition.MarkerId] = i + 1;
                }
            }

            if (!activeFound)
            {
                m_MarkerDefinitionsContent = new GUIContent[1];
                m_MarkerDefinitionsContent[0] = k_SelectFromActiveLibrary;
            }
        }

        public void GetMarkerGuiContent(Guid markerID, out GUIContent markerContent)
        {
            if (!m_MarkerIDToGUIContentID.TryGetValue(markerID, out var contentID))
            {
                if (m_MarkerIDToLibrary.TryGetValue(markerID, out var markerLibrary))
                {
                    markerContent = k_NoMarkerDefinitions[0];
                    return;
                }

                markerContent = k_ContentNotFound;
                return;
            }

            markerContent = m_MarkerDefinitionsContent[contentID];
        }

        public string DrawActiveMarkerPopup(Guid markerID)
        {
            if (!m_MarkerIDToGUIContentID.TryGetValue(markerID, out var currentID))
                currentID = 0;

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newID = EditorGUILayout.Popup(GUIContent.none, currentID, m_MarkerDefinitionsContent);
                if (check.changed && newID != 0)
                {
                    m_GUIContentIDToMarkerID.TryGetValue(newID, out var newMarkerID);
                    return newMarkerID.ToString();
                }
            }

            return markerID.ToString();
        }
    }
}
