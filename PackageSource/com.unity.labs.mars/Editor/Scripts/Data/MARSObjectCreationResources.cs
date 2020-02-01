using System;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS
{
    [ScriptableSettingsPath("MARS/Editor")]
    public class MARSObjectCreationResources : EditorScriptableSettings<MARSObjectCreationResources>
    {
        [Serializable]
        internal class ObjectCreationButton
        {
            [SerializeField]
            string m_Name;

            [SerializeField]
            GameObject m_Prefab;

            [SerializeField]
            DarkLightIconPair m_Icon;

            [SerializeField]
            string m_Tooltip;

            public string Name => m_Name;
            public GameObject Prefab => m_Prefab;
            public Texture2D Icon => m_Icon.Icon;
            public string Tooltip => m_Tooltip;

            public ObjectCreationButton()
            {
                m_Name = "Prefab Missing";
                m_Tooltip = "Button not set up.";
            }

            public ObjectCreationButton(GameObject prefab, DarkLightIconPair icon, string tooltip)
            {
                m_Name = prefab != null ? ObjectNames.NicifyVariableName(prefab.name) : "Prefab Missing";
                m_Prefab = prefab;
                m_Icon = icon;
                m_Tooltip = tooltip;
            }

            public void CreatePrefab(string groupTitle)
            {
                if (m_Prefab == null)
                    return;

                MARSSession.EnsureRuntimeState();
                EditorEvents.CreationMenuItemUsed.Send(new MarsMenuEventArgs { label = $"{groupTitle}/{m_Name}"});
                var name = GameObjectUtility.GetUniqueNameForSibling(null, m_Name);
                var go = Instantiate(m_Prefab);
                foreach (var colorComponent in go.GetComponentsInChildren<IHasEditorColor>())
                {
                    colorComponent.SetNewColor(true, true);
                }

                go.name = name;
                Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
                Selection.activeGameObject = go;
            }

            public GUIContent CreationButtonContent()
            {
                return new GUIContent(m_Name, m_Icon.Icon, m_Tooltip);
            }
        }

        [Serializable]
        internal class ObjectCreationButtonSection
        {
            [SerializeField]
            string m_Name;

            [SerializeField]
            ObjectCreationButton[] m_Buttons;

            [SerializeField]
            bool m_Expanded;

            public string Name => m_Name;

            public ObjectCreationButton[] Buttons => m_Buttons;

            public bool Expanded
            {
                get => m_Expanded;
                set => m_Expanded = value;
            }

            public ObjectCreationButtonSection()
            {
                m_Name = "New Group";
                m_Expanded = true;
                m_Buttons = new[] { new ObjectCreationButton() };
            }

            public ObjectCreationButtonSection(string name, bool expanded, ObjectCreationButton[] buttons)
            {
                m_Name = name;
                m_Buttons = buttons;
                m_Expanded = expanded;
            }
        }

        [SerializeField]
        ObjectCreationButtonSection[] m_ObjectCreationButtonSections;

        internal ObjectCreationButtonSection[] ObjectCreationButtonSections => m_ObjectCreationButtonSections;

        protected override void OnLoaded()
        {
            base.OnLoaded();
            if (m_ObjectCreationButtonSections == null || m_ObjectCreationButtonSections.Length < 1)
            {
                ResetToDefault();
            }
        }

        void Reset()
        {
            ResetToDefault();

            s_Instance = this;
        }

        public void ResetToDefault()
        {
            m_ObjectCreationButtonSections = new[]
            {
                new ObjectCreationButtonSection("Templates", true, new[]
                {
                    new ObjectCreationButton(MARSEditorPrefabs.instance.HorizontalPlanePrefab,
                        MARSUIResources.instance.ProxyObjectIconPair,
                        "A GameObject representing a plane with a horizontal facing."),
                    new ObjectCreationButton(MARSEditorPrefabs.instance.VerticalPlanePrefab,
                        MARSUIResources.instance.ProxyObjectIconPair,
                        "A GameObject representing a plane with a vertical facing."),
                    new ObjectCreationButton(MARSEditorPrefabs.instance.ImageMarkerPrefab,
                        MARSUIResources.instance.MarkerIconsTrackingPair,
                        "A Game Object representing an Image Marker."),
                    new ObjectCreationButton(MARSEditorPrefabs.instance.FaceMaskPrefab,
                        MARSUIResources.instance.FaceIconsTrackingPair,
                        "A Game Object representing a Face Mask.")
                }),
                new ObjectCreationButtonSection("Primitives", true, new[]
                {
                    new ObjectCreationButton(MARSEditorPrefabs.instance.ProxyObjectPrefab,
                        MARSUIResources.instance.ProxyObjectIconPair,
                        "A GameObject representing a proxy for an object in the real world."),
                    new ObjectCreationButton(MARSEditorPrefabs.instance.ProxyGroupPrefab,
                        MARSUIResources.instance.SetIconPair,
                        "A GameObject grouping multiple Proxy Objects."),
                    new ObjectCreationButton(MARSEditorPrefabs.instance.ReplicatorPrefab,
                        MARSUIResources.instance.ReplicatorIconPair,
                        "A manager that will spawn multiple instances of the child Proxy or ProxyGroup."),
                    new ObjectCreationButton(MARSEditorPrefabs.instance.SyntheticPrefab,
                        MARSUIResources.instance.SyntheticObjectIconPair,
                        "A virtual object that is added to the MARS system like real world data.")
                }),
                new ObjectCreationButtonSection("Visualizers", true, new[]
                {
                    new ObjectCreationButton(MARSEditorPrefabs.instance.PlaneVisualsPrefab,
                        MARSUIResources.instance.CreationPanelDefaultIconPair,
                        "A GameObject that visualizes detected planes (in Editor & build)."),
                    new ObjectCreationButton(MARSEditorPrefabs.instance.PointCloudVisualsPrefab,
                        MARSUIResources.instance.CreationPanelDefaultIconPair,
                        "A GameObject that visualizes the detected point cloud (in Editor & build)."),
                    new ObjectCreationButton(MARSEditorPrefabs.instance.FaceLandmarkVisualsPrefab,
                        MARSUIResources.instance.CreationPanelDefaultIconPair,
                        "A GameObject that visualizes detected face landmarks (in Editor & build).")
                })
            };
        }

        void OnValidate()
        {
            if (m_ObjectCreationButtonSections == null || m_ObjectCreationButtonSections.Length < 1)
            {
                ResetToDefault();
            }
        }
    }
}
