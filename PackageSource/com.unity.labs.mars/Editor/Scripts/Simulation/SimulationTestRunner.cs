using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Unity.Labs.MARS
{
    public class SimulationTestRunner : EditorWindow, IHasCustomMenu
    {
        class ResultData
        {
            public GUIContent nameLabel;
            public Texture preview;
            public int unmatchedCount;
            public int matchedCount;
        }

        public const string WindowTitle = "Sim Test Runner";
        const string k_NotSceneModeString = "Please switch the Simulation View environment mode to Synthetic to simulate";
        const string k_NoMARSSessionString = "The active scene is not a MARS scene, so we can't simulate it. " +
            "To begin, add a MARS Session.";

        static readonly Vector2Int k_OneOneRatio = new Vector2Int(256, 256);
        static readonly Vector2Int k_FourThreeRatio = new Vector2Int(342, 256);

        static readonly string[] k_PreviewRatioOptions = {"1:1", "4:3"};

        static readonly string k_TypeName = typeof(SimulationTestRunner).FullName;

        static SimulationTestRunner s_Instance;

        static bool s_HasRunSimulation;
        static int s_VisitedCount;
        static Vector2Int s_TextureSize;
        static Texture s_FallbackTexture;

        static readonly GUILayoutOption k_MatchLabelWidth = GUILayout.Width(84);
        static readonly GUILayoutOption k_MatchCountWidth = GUILayout.Width(32);

        readonly Dictionary<string, ResultData> m_PreviewTextures = new Dictionary<string, ResultData>();

        Vector2 m_ScrollPosition;
        [NonSerialized]
        bool m_CurrentlySimulating;
        Material m_OriginalSkybox;

        string m_DisabledSceneText;

        MARSSession m_Session;
        Scene m_ActiveScene;

        MiniSimulationView m_MiniSimulationView;
        int m_CachedSceneIndex;

        readonly Dictionary<int, KeyValuePair<string, bool>> m_IconEnabledOriginalStates = new Dictionary<int, KeyValuePair<string, bool>>();

        static int previewRatioIndex
        {
            get { return EditorPrefsUtils.GetInt(k_TypeName); }
            set { EditorPrefsUtils.SetInt(k_TypeName, value); }
        }

        [MenuItem(MenuConstants.MenuPrefix + WindowTitle, priority = MenuConstants.SimTestRunnerPriority)]
        public static void InitWindow()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = WindowTitle, active = true});
            var instance = GetWindow<SimulationTestRunner>("SimulationTestRunner");
            instance.Show();
        }

        public void AddItemsToMenu(GenericMenu menu) { this.MARSCustomMenuOptions(menu); }

        public void OnEnable()
        {
            titleContent.text = WindowTitle;
            SwitchRatio();
            m_OriginalSkybox = RenderSettings.skybox;
            m_ActiveScene = SceneManager.GetActiveScene();
            m_Session = MARSUtils.GetMARSSession(m_ActiveScene);
            EditorApplication.delayCall += SetupTextures;
#if UNITY_2018_2_OR_NEWER
            EditorSceneManager.activeSceneChangedInEditMode += OnActiveSceneChange;
#else
            SceneManager.activeSceneChanged += OnActiveSceneChange;
#endif
        }

        void OnDestroy()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = WindowTitle, active = false});
        }

        void OnActiveSceneChange(Scene previous, Scene current)
        {
            SetupTextures();
        }

        void SetupTextures()
        {
            s_FallbackTexture = new Texture2D(s_TextureSize.x, s_TextureSize.y);
            m_PreviewTextures.Clear();
            var environmentManager = MARSEnvironmentManager.instance;
            environmentManager.UpdateSimulatedEnvironmentCandidates();
            var environments = environmentManager.EnvironmentPrefabPaths;
            foreach (var path in environments)
            {
                var texture = new Texture2D(s_TextureSize.x, s_TextureSize.y);
                var result = new ResultData
                {
                    nameLabel = new GUIContent(),
                    preview = texture,
                    unmatchedCount = 0
                };

                m_PreviewTextures.Add(path, result);
            }

            var widthMultiplier = s_TextureSize.x + 10;
            minSize = new Vector2(widthMultiplier * 3, 312);
            maxSize = new Vector2(widthMultiplier * environments.Count, 320);
        }

        void StartPreview()
        {
            s_HasRunSimulation = false;
            s_VisitedCount = 0;

            m_MiniSimulationView = new MiniSimulationView(this, s_TextureSize.x, s_TextureSize.y);

            QuerySimulationModule.simulationDone += SaveScreenTask;
            m_CurrentlySimulating = true;
            m_IconEnabledOriginalStates.Clear();
            EditorUtils.ToggleCommonIcons(false, m_IconEnabledOriginalStates);

            EditorEvents.MultiSimulationStarted.Send(new MultiSimulationStartedArgs());

            m_CachedSceneIndex = MARSEnvironmentManager.instance.CurrentSyntheticEnvironmentIndex;
            MARSEnvironmentManager.instance.SetupNextEnvironmentAndRestartSimulation(true); // start the loop
        }

        void StopPreview()
        {
            s_HasRunSimulation = true;
            s_VisitedCount = int.MaxValue;

            // Need to dispose of mini sim view this also stops usage of sim scene
            m_MiniSimulationView.Dispose();

            RenderSettings.skybox = m_OriginalSkybox;
            m_CurrentlySimulating = false;
            EditorUtils.ToggleClassIcons(m_IconEnabledOriginalStates);
            QuerySimulationModule.simulationDone -= SaveScreenTask;
            if (RenderSettings.skybox == null)
                RenderSettings.skybox = m_OriginalSkybox;

            Repaint();

            // Reopen starting scene environment
            MARSEnvironmentManager.instance.SetSyntheticEnvironment(m_CachedSceneIndex);
            MARSEnvironmentManager.instance.RefreshEnvironmentAndRestartSimulation();
        }

        public void OnGUI()
        {
            if (m_Session == null)
                m_Session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());

            var isMarsScene = m_Session != null;
            var isPrefabScene = SimulationSettings.environmentMode == EnvironmentMode.Synthetic;

            // Need a active sim view to use the camera.
            var isSimViewActive = SimulationSceneModule.instance != null;

            using (new GUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(m_CurrentlySimulating || !isMarsScene || !isPrefabScene || !isSimViewActive))
                {
                    if (GUILayout.Button("Simulate Environments", GUILayout.Width(188)))
                        StartPreview();

                    using (var check = new EditorGUI.ChangeCheckScope())
                    {
                        previewRatioIndex =
                            EditorGUILayout.Popup(previewRatioIndex, k_PreviewRatioOptions, GUILayout.Width(48));

                        if (check.changed)
                            SwitchRatio();
                    }
                }

                if (!isPrefabScene)
                    EditorGUILayout.HelpBox(k_NotSceneModeString, MessageType.Info);

                if (MARSUtils.NoActiveSessionHintBox(!isMarsScene, k_NoMARSSessionString, MessageType.Info))
                    m_Session = MARSUtils.GetMARSSession(SceneManager.GetActiveScene());
            }

            using (var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollPosition))
            {
                m_ScrollPosition = scrollScope.scrollPosition;

                using (new GUILayout.HorizontalScope())
                {
                    using (new EditorGUI.DisabledScope(!isMarsScene || !isPrefabScene))
                    {
                        var envScenes = MARSEnvironmentManager.instance.EnvironmentPrefabPaths;
                        foreach (var environment in envScenes)
                        {
                            DrawSceneResult(environment);
                            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));
                        }

                    }
                }
            }
        }

        // this gets called every time a new environment is done evaluating until all are done
        void SaveScreenTask()
        {
            var environmentManager = MARSEnvironmentManager.instance;
            if (s_VisitedCount >= environmentManager.EnvironmentPrefabPaths.Count)
            {
                StopPreview();
                return;
            }

            var path = environmentManager.EnvironmentPrefabPaths[environmentManager.CurrentSyntheticEnvironmentIndex];
            var result = m_PreviewTextures[path];

            // save the current sim view output as a texture
            result.preview = RenderTextureToPreview(m_MiniSimulationView.SingleFrameRepaint());

            result.nameLabel.text = environmentManager.SyntheticEnvironmentName;

            var data = MARSQueryBackend.instance.Pipeline.Data;
            var updatingIndices = data.updatingIndices;
            result.matchedCount = updatingIndices.Count;
            result.unmatchedCount = data.conditionMatchSets.Count(set => set.Count == 0);

            s_VisitedCount++;
            Repaint();
            environmentManager.SetupNextEnvironmentAndRestartSimulation(true);
        }

        void DrawSceneResult(string path)
        {
            using (new GUILayout.VerticalScope())
            {
                ResultData result;
                if (!m_PreviewTextures.TryGetValue(path, out result))
                    return;

                EditorGUILayout.LabelField(result.nameLabel, EditorStyles.boldLabel);

                using (new GUILayout.HorizontalScope(GUILayout.Width(240)))
                {
                    using (new EditorGUI.IndentLevelScope())
                    {
                        EditorGUILayout.LabelField("Matched", k_MatchLabelWidth);
                        EditorGUILayout.LabelField(result.matchedCount.ToString(), k_MatchCountWidth);
                        EditorGUILayout.LabelField("Unmatched", k_MatchLabelWidth);
                        EditorGUILayout.LabelField(result.unmatchedCount.ToString(), k_MatchCountWidth);
                    }
                }

                var rect = GUILayoutUtility.GetRect(s_TextureSize.x, s_TextureSize.y, GUILayout.ExpandWidth(false));
                m_PreviewTextures[path] = result;
                var previewTexture = result.preview != null ? result.preview : s_FallbackTexture;

                GUIContent content;
                if (s_HasRunSimulation)
                    content = new GUIContent(previewTexture, "Click to open this environment scene");
                else
                    content = new GUIContent(previewTexture, "After running simulations, click to open this environment scene");

                if (GUI.Button(rect, content, GUIStyle.none) && s_HasRunSimulation)
                {
                    if (SimulationSettings.environmentMode != EnvironmentMode.Synthetic)
                        SimulationSettings.environmentMode = EnvironmentMode.Synthetic;

                    MARSEnvironmentManager.instance.SetupEnvironmentAndRestartSimulation(path);
                }
            }
        }

        public static Texture2D RenderTextureToPreview(RenderTexture rt)
        {
            var currentRenderTexture = RenderTexture.active;
            var texture = new Texture2D(rt.width, rt.height);
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture.Apply();
            RenderTexture.active = currentRenderTexture;
            return texture;
        }

        void SwitchRatio()
        {
            switch (previewRatioIndex)
            {
                case 0:
                    s_TextureSize = k_OneOneRatio;
                    break;
                case 1:
                    s_TextureSize = k_FourThreeRatio;
                    break;
            }

            SetupTextures();
        }
    }
}
