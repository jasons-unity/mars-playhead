using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Labs.MARS.Query;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public partial class QueryPipelineViewer : EditorWindow
    {
        public const string windowTitle = "Query Pipeline Viewer";

        static MARSQueryBackend s_QueryBackend;
        static ParallelQueryData s_QueryData;
        static StandaloneQueryPipeline s_Pipeline;

        static readonly StringBuilder k_String = new StringBuilder();

        static readonly Vector2 k_MinSize = new Vector2(400f, 300f);
        static readonly Vector2 k_MaxSize = new Vector2(400f, 3000f);

        static readonly GUILayoutOption k_MatchColumnWidth = GUILayout.Width(120);
        static readonly GUILayoutOption k_ValueColumnWidth = GUILayout.Width(140);
        static readonly GUILayoutOption k_MiddleColumnWidth = GUILayout.Width(216);
        static readonly GUILayoutOption k_CallStateColumnWidth = GUILayout.Width(36);
        static readonly GUILayoutOption k_TraitValueColumnWidth = GUILayout.Width(256);

        bool m_ShowWorkingIndicesOption;
        bool m_ShowMatchIndices;
        bool m_OptionsFoldout;
        bool m_ShowActiveView;                // whether to show the stage-by-stage active state view
        bool m_ShowDataSearch = true;         // whether to show the data search view

        readonly List<bool> m_StageFoldoutStates = new List<bool>();
        readonly List<bool> m_IndicesFoldoutStates = new List<bool>();
        readonly List<bool> m_RatingIndicesFoldoutStates = new List<bool>();
        readonly List<bool> m_ResultsFoldoutStates = new List<bool>();

        GUIStyle m_WrappedLabelStyle;
        GUIStyle m_BoldFoldoutStyle;
        bool m_LayoutReady;
        Vector2 m_ScrollPosition;

        [MenuItem(MenuConstants.DevMenuPrefix + windowTitle, priority = MenuConstants.QueryPipelineViewerPriority)]
        public static void ShowWindow()
        {
            var window = GetWindow<QueryPipelineViewer>();
            window.InitStates();
            window.Show();
        }

        public void OnEnable()
        {
            titleContent = new GUIContent("Query Pipeline Viewer");
            CacheReferences();
            InitStates();

            minSize = k_MinSize;
            maxSize = k_MaxSize;

            m_ShowSearchHint = MARSUserPreferences.instance.ShowQuerySearchHint;

            MARSQueryBackend.instance.onQueryMatchFound += GetValuesAndRepaint;
            MARSEnvironmentManager.onEnvironmentSetup += GetValuesAndRepaint;
        }

        void OnDisable()
        {
            if (s_Pipeline != null)
                s_Pipeline.onStageGroupCompletion -= Repaint;

            MARSQueryBackend.instance.onQueryMatchFound -= GetValuesAndRepaint;
            MARSEnvironmentManager.onEnvironmentSetup -= GetValuesAndRepaint;
        }

        void InitStates()
        {
            if (s_QueryData == null)
            {
                CacheReferences();
                return;
            }

            var stageCount = s_Pipeline.Stages.Length - 1;
            for (var index = 0; index < stageCount; index++)
            {
                m_StageFoldoutStates.Add(true);        // default to expanding all stages
                m_IndicesFoldoutStates.Add(false);     // default to not showing working indices
            }
        }

        void InitStyles()
        {
            m_WrappedLabelStyle = MARSEditorGUI.Styles.LeftAlignedWrapLabel;
            m_BoldFoldoutStyle = MARSEditorGUI.Styles.BoldFoldout;
        }

        void CacheReferences()
        {
            s_QueryBackend = MARSQueryBackend.instance;
            if (s_QueryBackend == null)
                return;

            s_Pipeline = s_QueryBackend.Pipeline;
            if (s_Pipeline == null)
                return;

            s_Pipeline.onStageGroupCompletion += Repaint;
            s_QueryData = s_Pipeline.Data;
        }

        bool m_SkipNextRepaint;

        void OnGUI()
        {
            if (s_QueryData == null || s_Pipeline == null)
            {
                CacheReferences();
                m_LayoutReady = false;
                return;
            }

            if (m_StageFoldoutStates == null || m_StageFoldoutStates.Count == 0
                                             || m_IndicesFoldoutStates.Count < s_QueryData.Count)
            {
                InitStates();
                m_LayoutReady = false;
                return;
            }

            if (!m_LayoutReady)
            {
                if (Event.current.type == EventType.Layout)
                    return;

                m_LayoutReady = true;
                return;
            }

            InitStyles();

            using (var scrollView = new EditorGUILayout.ScrollViewScope(m_ScrollPosition))
            {
                m_ScrollPosition = scrollView.scrollPosition;
                m_ShowActiveView = EditorGUILayout.Foldout(m_ShowActiveView, "Show stage-by-stage current state");
                if (m_ShowActiveView)
                {
                    DrawOptions();
                    DrawMatchIdIndices();
                    DrawCacheTraitsStage();
                    DrawConditionRatingStage();
                    DrawMatchIntersectionStage();
                    DrawDataAvailabilityStage();
                    DrawMatchReductionStage();
                    DrawBestStandaloneMatchStage();
                    DrawResultFillingStage();
                    DrawAcquireHandlerStage();
                }

                m_ShowDataSearch = EditorGUILayout.Foldout(m_ShowDataSearch, "Show data search");
                if (m_ShowDataSearch)
                {
                    EditorGUILayout.Separator();
                    DrawStandaloneIndexViewer();
                }
            }
        }

        void DrawOptions()
        {
            m_OptionsFoldout =
                EditorGUILayout.Foldout(m_OptionsFoldout, "Options", true, MARSEditorGUI.Styles.BoldFoldout);

            if (m_OptionsFoldout)
            {
                using (new GUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Show Working Indices Foldouts");
                    m_ShowWorkingIndicesOption = EditorGUILayout.Toggle(m_ShowWorkingIndicesOption);
                }
            }
        }

        bool DrawStageHeader<T1, T2>(QueryStage<T1, T2> stage, int stageIndex, string titleTooltip = "")
            where T1 : DataTransform
            where T2 : DataTransform
        {
            EditorGUIUtils.DrawBoxSplitter();

            var stageTitleContent = new GUIContent(stage.Label, titleTooltip);

            var doFoldout =
                EditorGUILayout.Foldout(m_StageFoldoutStates[stageIndex], stageTitleContent, true, m_BoldFoldoutStyle);

            m_StageFoldoutStates[stageIndex] = doFoldout;

            if (!doFoldout)
                return false;

            DrawWorkingIndicesOption(stage.Transformation1, stageIndex);
            return doFoldout;
        }

        bool DrawStageHeader<T>(QueryStage<T> stage, int stageIndex, string titleTooltip = "")
            where T : DataTransform
        {
            EditorGUIUtils.DrawBoxSplitter();

            var stageTitleContent = new GUIContent(stage.Label, titleTooltip);

            var doFoldout =
                EditorGUILayout.Foldout(m_StageFoldoutStates[stageIndex], stageTitleContent, true, m_BoldFoldoutStyle);

            m_StageFoldoutStates[stageIndex] = doFoldout;

            if (!doFoldout)
                return false;

            DrawWorkingIndicesOption(stage.Transformation, stageIndex);
            return doFoldout;
        }

        void DrawWorkingIndicesOption(DataTransform transform, int stageIndex)
        {
            if (!m_ShowWorkingIndicesOption)
                return;

            const string indicesMsg = "Show working indices";
            m_IndicesFoldoutStates[stageIndex] =
                EditorGUILayout.Foldout(m_IndicesFoldoutStates[stageIndex], indicesMsg, true);

            if (m_IndicesFoldoutStates[stageIndex])
            {
                k_String.Length = 0;
                if (transform.WorkingIndices.Count == 0)
                {
                    k_String.Append("No query indices in working set");
                }
                else
                {
                    var endIndex = transform.WorkingIndices.Count - 1;
                    for (var i = 0; i < endIndex; i++)
                    {
                        var index = transform.WorkingIndices[i];
                        k_String.AppendFormat("{0}, ", index);
                    }

                    k_String.Append(endIndex);
                }

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.LabelField(k_String.ToString());
                }
            }
        }

        void DrawMatchIdIndices()
        {
            m_ShowMatchIndices = EditorGUILayout.Foldout(m_ShowMatchIndices, "Show Query Match Indices", true,
                MARSEditorGUI.Styles.BoldFoldout);
            if (!m_ShowMatchIndices)
                return;

            var queryMatchIds = s_QueryData.queryMatchIds;
            foreach (var i in s_QueryData.ValidIndices)
            {
                var queryMatchId = queryMatchIds[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(queryMatchId.ToString(), EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    EditorGUILayout.LabelField(i.ToString());
                }

                EditorGUIUtils.DrawSplitter();
            }
        }

        void DrawCacheTraitsStage()
        {
            const string titleTooltip = "Find trait data for each condition";
            if (!DrawStageHeader(s_Pipeline.CacheTraitReferencesStage, 0, titleTooltip))
                return;

            var queryMatchIds = s_QueryData.queryMatchIds;
            var collections = s_QueryData.cachedTraits;
            foreach (var i in s_QueryData.ValidIndices)
            {
                var queryMatchId = queryMatchIds[i];
                var collection = collections[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(queryMatchId.ToString(), EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    EditorGUILayout.LabelField(collection.fulfilled ? "Cache filled" : "Cache unfilled",
                        k_MiddleColumnWidth);
                }

                EditorGUIUtils.DrawSplitter();
            }
        }

        void DrawConditionRatingStage()
        {
            const string titleTooltip = "Call RateDataMatch() for all conditions and find which ones match";
            if (!DrawStageHeader(s_Pipeline.ConditionRatingStage, 1, titleTooltip))
                return;

            var collections = s_QueryData.conditionRatings;
            var queryMatchIds = s_QueryData.queryMatchIds;
            foreach (var i in s_Pipeline.ConditionRatingStage.Transformation.WorkingIndices)
            {
                var queryMatchId = queryMatchIds[i];
                var queryRatings = collections[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(queryMatchId.ToString(), EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    var matchMsg = string.Format("{0} conditions to rate", queryRatings.totalConditionCount);
                    EditorGUILayout.LabelField(matchMsg, m_WrappedLabelStyle, k_MiddleColumnWidth);
                }

                EditorGUIUtils.DrawSplitter();
            }
        }

        void DrawMatchIntersectionStage()
        {
            const string titleTooltip = "Find which data IDs had matches in all conditions";
            if (!DrawStageHeader(s_Pipeline.FindMatchProposalsStage, 2, titleTooltip))
                return;

            var matchSets = s_QueryData.conditionMatchSets;
            var queryMatchIds = s_QueryData.queryMatchIds;
            foreach (var i in s_Pipeline.FindMatchProposalsStage.Transformation.WorkingIndices)
            {
                var queryMatchId = queryMatchIds[i];
                var matchSet = matchSets[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(queryMatchId.ToString(), EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    var matchMsg = string.Format("{0} matching IDs", matchSet.Count);
                    EditorGUILayout.LabelField(matchMsg, m_WrappedLabelStyle, k_MiddleColumnWidth);
                }

                EditorGUIUtils.DrawSplitter();
            }
        }

        void DrawDataAvailabilityStage()
        {
            const string titleTooltip =
                "Filters out any data that is not available for use due to data ownership rules";
            var stage = s_Pipeline.DataAvailabilityStage;
            if (!DrawStageHeader(stage, 3, titleTooltip))
                return;

            var availabilityDebugResults = stage.Transformation.DebugFilteredResults;
            foreach (var kvp in availabilityDebugResults)
            {
                var queryMatchId = kvp.Key;
                var filteredSet = kvp.Value;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(queryMatchId.ToString(), EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    var matchMsg = string.Format("{0} IDs filtered", filteredSet.Count);
                    EditorGUILayout.LabelField(matchMsg, m_WrappedLabelStyle, k_MiddleColumnWidth);
                }

                EditorGUIUtils.DrawSplitter();
            }
        }

        void DrawMatchReductionStage()
        {
            const string titleTooltip =
                "Takes all possible matches for each query and reduces their scores to a number";
            var stage = s_Pipeline.MatchReductionStage;
            if (!DrawStageHeader(stage, 4, titleTooltip))
                return;

            var queryMatchIds = s_QueryData.queryMatchIds;
            if (queryMatchIds.Count == 0)
                return;

            if (stage.Transformation.WorkingIndices.Count > 0)
            {
                var maxIndex = stage.Transformation.WorkingIndices.Max();
                if (maxIndex >= m_RatingIndicesFoldoutStates.Count)
                {
                    var diff = maxIndex - m_RatingIndicesFoldoutStates.Count;
                    for (var i = 0; i <= diff; i++)
                    {
                        m_RatingIndicesFoldoutStates.Add(false);
                    }
                }
            }

            foreach (var i in stage.Transformation.WorkingIndices)
            {
                var queryMatchId = queryMatchIds[i];
                var reducedRatings = s_QueryData.reducedConditionRatings[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(queryMatchId.ToString(), EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    var matchMsg = string.Format("{0} rating entries", reducedRatings.Count);
                    EditorGUILayout.LabelField(matchMsg);
                }

                if (reducedRatings.Count > 0)
                {
                    var doFoldout = EditorGUILayout.Foldout(m_RatingIndicesFoldoutStates[i], "show ratings", true);
                    m_RatingIndicesFoldoutStates[i] = doFoldout;
                    if (doFoldout)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            DrawRatingsDictionary(reducedRatings);
                        }

                        EditorGUILayout.Space();
                    }
                }

                EditorGUIUtils.DrawSplitter();
            }
        }

        static void DrawStageDetailsHeader(Action drawAfterMatchId)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Query Match ID", EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                drawAfterMatchId();
            }
        }

        static void DrawRatingsDictionary(Dictionary<int, float> dictionary)
        {
            EditorGUIUtils.DrawDictionary(dictionary, "Data ID", "Match Rating", k_MatchColumnWidth, k_ValueColumnWidth, "F4");
        }

        void DrawBestStandaloneMatchStage()
        {
            const string titleTooltip =
                "Sort out any conflicts in the data assignments and find the final best matches";
            var stage = s_Pipeline.BestStandaloneMatchStage;
            if (!DrawStageHeader(stage, 5, titleTooltip))
                return;

            var queryMatchIds = s_QueryData.queryMatchIds;
            if (queryMatchIds.Count == 0)
                return;

            var bestMatchDataIds = s_QueryData.bestMatchDataIds;
            DrawStageDetailsHeader(() =>
            {
                EditorGUILayout.LabelField("Assigned Data ID", EditorStyles.miniBoldLabel, k_MiddleColumnWidth);
            });

            foreach (var i in stage.Transformation.WorkingIndices)
            {
                var queryMatchId = queryMatchIds[i];
                var dataId = bestMatchDataIds[i];
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(queryMatchId.ToString(), EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    var resultLabel = dataId < 0 ? "none" : dataId.ToString();
                    EditorGUILayout.LabelField(resultLabel, m_WrappedLabelStyle);
                }

                EditorGUIUtils.DrawSplitter();
            }
        }

        void DrawResultFillingStage()
        {
            const string titleTooltip = "Takes the assigned match ID and puts all trait values for it in the result";
            var stage = s_Pipeline.ResultFillStage;
            if (!DrawStageHeader(stage, 6, titleTooltip))
                return;

            var queryMatchIds = s_QueryData.queryMatchIds;
            var results = s_QueryData.queryResults;

            if (stage.Transformation.WorkingIndices.Count > 0)
            {
                var maxIndex = stage.Transformation.WorkingIndices.Max();
                if (maxIndex >= m_ResultsFoldoutStates.Count)
                {
                    var diff = maxIndex - m_ResultsFoldoutStates.Count;
                    for (var i = 0; i <= diff; i++)
                    {
                        m_ResultsFoldoutStates.Add(false);
                    }
                }
            }

            foreach (var i in stage.Transformation.WorkingIndices)
            {
                var queryMatchId = queryMatchIds[i];
                EditorGUILayout.LabelField(queryMatchId.ToString(), EditorStyles.miniBoldLabel, k_MatchColumnWidth);

                var doFoldout = EditorGUILayout.Foldout(m_ResultsFoldoutStates[i], "show query result", true);
                if (doFoldout)
                {
                    DrawQueryResult(results[i]);
                }

                m_ResultsFoldoutStates[i] = doFoldout;
                EditorGUIUtils.DrawSplitter();
            }
        }

        void DrawQueryResult(QueryResult result)
        {
            using (new EditorGUI.IndentLevelScope())
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Trait Name", EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    EditorGUILayout.LabelField("Value", EditorStyles.miniBoldLabel, k_TraitValueColumnWidth);
                }
            }
        }

        void DrawTraits<T>(Dictionary<string, T> traits)
        {
            foreach (var trait in traits)
            {
                DrawTrait(trait);
                EditorGUILayout.Space();
            }
        }

        void DrawTrait<T>(KeyValuePair<string, T> kvp)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(kvp.Key, k_MatchColumnWidth);
                EditorGUILayout.LabelField(kvp.Value.ToString(), m_WrappedLabelStyle, k_TraitValueColumnWidth);
            }
        }

        void DrawAcquireHandlerStage()
        {
            const string titleTooltip = "Calls queries' acquire handlers with their results";
            var stage = s_Pipeline.AcquireHandlingStage;
            if (!DrawStageHeader(stage, 7, titleTooltip))
                return;

            var queryMatchIds = s_QueryData.queryMatchIds;
            var handlers = s_QueryData.acquireHandlers;
            var transformation = stage.Transformation1;
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Query Match ID", EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                EditorGUILayout.LabelField("Method Name", EditorStyles.miniBoldLabel, k_MiddleColumnWidth);
                EditorGUILayout.LabelField("Called", EditorStyles.miniBoldLabel, k_CallStateColumnWidth);
            }

            foreach (var i in transformation.WorkingIndices)
            {
                var queryMatchId = queryMatchIds[i];
                var handler = handlers[i];
                var method = handler.Method;
                var targetName = method.Name;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(queryMatchId.ToString(), EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    EditorGUILayout.LabelField(targetName, m_WrappedLabelStyle, k_MiddleColumnWidth);

                    bool callState;
                    transformation.DebugHandlerCallStates.TryGetValue(i, out callState);

                    var callStateLabel = callState ? "✓" : "╳";
                    EditorGUILayout.LabelField(callStateLabel, k_CallStateColumnWidth);
                }

                EditorGUIUtils.DrawSplitter();
            }
        }
    }
}
