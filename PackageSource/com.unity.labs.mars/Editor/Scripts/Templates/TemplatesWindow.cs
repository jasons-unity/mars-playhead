using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public class TemplatesWindow : EditorWindow
    {
        const int k_ButtonsPerRow = 2;
        const int k_ButtonSize = 85;
        const int k_ButtonMargin = 8;
        const int k_RowMargin = 26;
        static readonly GUIContent k_WindowTitle;

#pragma warning disable 649
        [SerializeField]
        TemplateCollection m_TemplateCollection;
#pragma warning restore 649

        TemplateData[] m_Templates;
        float[] m_NameWidths;
        float m_WindowWidth;

        static TemplatesWindow()
        {
            k_WindowTitle = new GUIContent("Templates");
        }

        [MenuItem(MenuConstants.MenuPrefix + "Choose Template", priority = MenuConstants.TemplatePriority)]
        static void ShowTemplatesWindow()
        {
            var window = (TemplatesWindow) CreateInstance(typeof(TemplatesWindow));
            window.titleContent = k_WindowTitle;
            window.ShowUtility();
        }

        void OnEnable()
        {
            if (m_TemplateCollection == null)
                return;

            m_Templates = m_TemplateCollection.templates;
            m_WindowWidth = (k_ButtonSize + k_ButtonMargin * 2) * k_ButtonsPerRow + k_ButtonMargin * 4;
            minSize = default(Vector2);

            m_NameWidths = new float[m_Templates.Length];
            for (var i = 0; i < m_Templates.Length; i++)
            {
                var nameWidth = EditorStyles.label.CalcSize(new GUIContent(m_Templates[i].name)).x;
                m_NameWidths[i] = Mathf.Min(nameWidth, k_ButtonSize);
            }
        }

        void OnGUI()
        {
            if (m_Templates == null)
            {
                EditorGUILayout.LabelField("No Templates");
                GUIUtility.ExitGUI();
            }

            using (new GUILayout.VerticalScope())
            {
                GUILayout.Label(" Choose a Template", EditorStyles.boldLabel);
                GUILayout.Space(5);

                for (var i = 0; i < m_Templates.Length; i += k_ButtonsPerRow)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Space(k_ButtonMargin);
                        for (var j = i; j < i + k_ButtonsPerRow; j++)
                        {
                            if (j >= m_Templates.Length)
                            {
                                GUILayout.Space(k_ButtonMargin * 2 + k_ButtonSize);
                                continue;
                            }

                            DrawTemplateButton(m_Templates[j], m_NameWidths[j]);
                        }
                        GUILayout.Space(k_ButtonMargin);
                    }
                    GUILayout.Space(k_RowMargin);
                }
            }

            if (minSize == default(Vector2))
            {
                var rect = GUILayoutUtility.GetLastRect();

                if (Event.current.type == EventType.Repaint)
                    minSize = maxSize = new Vector2(m_WindowWidth, rect.height);
            }
        }

        void DrawTemplateButton(TemplateData template, float nameWidth)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(k_ButtonMargin);

                using (new GUILayout.VerticalScope())
                {
                    if (GUILayout.Button(template.icon, GUI.skin.GetStyle("Box"), GUILayout.Width(k_ButtonSize), GUILayout.Height(k_ButtonSize)))
                    {
                        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                        EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(template.scene));
                        SimulationSettings.environmentMode = template.environmentMode;
                        var moduleLoader = ModuleLoaderCore.instance;
                        if (moduleLoader != null)
                        {
                            var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
                            if (environmentManager != null)
                                environmentManager.RefreshEnvironmentAndRestartSimulation(SimulationSettings.isVideoEnvironment);
                        }
                        Close();
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(template.name, GUILayout.Width(nameWidth));
                        GUILayout.FlexibleSpace();
                    }
                }

                GUILayout.Space(k_ButtonMargin);
            }
        }
    }
}
