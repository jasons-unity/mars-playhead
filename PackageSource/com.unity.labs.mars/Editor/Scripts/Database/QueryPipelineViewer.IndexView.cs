using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public partial class QueryPipelineViewer
    {
        const string k_DismissHintAnalyticsLabel = "DismissHint PipelineIndexViewer";
        const string k_SearchHintText = "You can search for query data in two ways.\n" +
                                        "1) by QueryMatchID - input query ID & match ID below, " +
                                        "and if a match is found, it will be shown below.\n" +
                                        "2) by index - This accesses whatever query's data resides at that index. " +
                                        "This is useful if you want to examine the state of the backend data in general.";

        static readonly GUIContent k_ViewIndexContent = new GUIContent
        {
            text = "Viewing Index",
            tooltip = "The index into standalone query data to show"
        };

        static readonly GUIContent k_QueryIdContent = new GUIContent
        {
            text = "Query ID",
            tooltip = "The number associated with a query"
        };

        static readonly GUIContent k_MatchIdContent = new GUIContent
        {
            text = "Match ID",
            tooltip = "The number associated with a specific instance / match of a query"
        };

        static readonly GUILayoutOption k_InputWidthOption = GUILayout.Width(240);

        int m_SelectedStandaloneIndex;

        GameObject m_SourceGameObject;
        string m_SourceObjectName = "unknown";
        // value cache for standalone query data
        QueryMatchID m_QueryMatchId;
        Exclusivity m_Exclusivity;
        float m_UpdateMatchInterval;
        float m_TimeoutLeft;
        bool m_ReacquireOnLoss;
        Conditions m_Conditions;
        CachedTraitCollection m_CachedTraits;
        Dictionary<int, float> m_ReducedConditionRatings;
        int m_BestMatchDataId;
        QueryResult m_QueryResult;
        Action<QueryResult> m_AcquireHandler;
        Action<QueryResult> m_UpdateHandler;
        Action<QueryResult> m_LossHandler;
        Action<QueryArgs> m_TimeoutHandler;

        int m_SearchQueryId = 1;
        int m_SearchQueryMatchId = 1;
        int m_PreviousSearchQueryId;
        int m_PreviousSearchQueryMatchId;
        QueryMatchID m_SearchMatchId;

        bool m_ShowConditions;
        bool m_SearchMatchFound = true;
        bool m_ShowStandaloneRatings;
        bool m_ShowStandaloneQueryResult;
        bool m_ShowEventHandlers;
        bool m_ShowSearchHint;

        void DrawStandaloneIndexViewer()
        {
            EditorGUILayout.Space();
            EditorGUIUtils.DrawBoxSplitter();
            EditorGUILayout.Space();

            if (s_QueryData.Count == 0)
            {
                CacheReferences();
                EditorGUILayout.LabelField("No data in standalone queries");
                return;
            }

            m_ShowSearchHint = MARSUtils.HintBox(m_ShowSearchHint, k_SearchHintText, k_DismissHintAnalyticsLabel);

            EditorGUILayout.LabelField("Search For Match", EditorStyles.miniBoldLabel, k_MatchColumnWidth);
            using (new EditorGUI.IndentLevelScope())
            {
                m_SearchQueryId = PositiveIntField(k_QueryIdContent, m_SearchQueryId, k_InputWidthOption);
                m_SearchQueryMatchId = PositiveIntField(k_MatchIdContent, m_SearchQueryMatchId, k_InputWidthOption);

                if (m_SearchQueryId != m_PreviousSearchQueryId ||
                    m_SearchQueryMatchId != m_PreviousSearchQueryMatchId)
                {
                    m_SearchMatchId = new QueryMatchID(m_SearchQueryId, m_SearchQueryMatchId);

                    int index;
                    m_SearchMatchFound = s_QueryData.matchIdToIndex.TryGetValue(m_SearchMatchId, out index);
                    if (m_SearchMatchFound)
                    {
                        m_SelectedStandaloneIndex = index;
                        GetStandaloneIndexValues(index);

                        if (QueryObjectMapping.Map.TryGetValue(m_SearchQueryId, out var tempGameObject))
                        {
                            m_SourceGameObject = tempGameObject;
                            m_SourceObjectName = tempGameObject.name;
                        }
                    }
                }

                m_PreviousSearchQueryId = m_SearchQueryId;
                m_PreviousSearchQueryMatchId = m_SearchQueryMatchId;
            }

            if (!m_SearchMatchFound)
            {
                EditorGUILayout.HelpBox($"No match found for {m_SearchMatchId.ToString()}!", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();

            var selectedIndex = PositiveIntField(k_ViewIndexContent, m_SelectedStandaloneIndex, k_InputWidthOption);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, s_QueryData.Count - 1);
            if (selectedIndex != m_SelectedStandaloneIndex)
            {
                m_SelectedStandaloneIndex = selectedIndex;
                GetStandaloneIndexValues(selectedIndex);
                // fill in the match search UI with whatever match is at this index if searching by index
                m_SearchQueryId = m_QueryMatchId.queryID;
                m_SearchQueryMatchId = m_QueryMatchId.matchID;
                m_PreviousSearchQueryId = m_QueryMatchId.queryID;
                m_PreviousSearchQueryMatchId = m_QueryMatchId.matchID;
                if (QueryObjectMapping.Map.TryGetValue(m_SearchQueryId, out var tempGameObject))
                {
                    m_SourceGameObject = tempGameObject;
                    m_SourceObjectName = tempGameObject.name;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            DrawLabelValue("Source Object", m_SourceObjectName);
            DrawLabelValue("Query Match", m_QueryMatchId.ToString());

            DrawLabelValue("Assigned Data ID", m_BestMatchDataId.ToString());
            DrawLabelValue("Exclusivity", m_Exclusivity.ToString());
            DrawTraitCacheData(m_CachedTraits);
            DrawConditions(m_Conditions);
            DrawReducedRatings();
            DrawQueryResultFoldout(m_QueryResult);

            DrawLabelValue("Reacquire on Loss", m_ReacquireOnLoss.ToString());
            DrawLabelValue("Timeout left", m_TimeoutLeft.ToString());
            DrawLabelValue("Match update interval", m_UpdateMatchInterval.ToString());
            DrawHandlers(m_AcquireHandler, m_UpdateHandler, m_LossHandler, m_TimeoutHandler);
        }

        static int PositiveIntField(GUIContent label, int input, params GUILayoutOption[] options)
        {
            return Mathf.Clamp(EditorGUILayout.IntField(label, input, options), 0, int.MaxValue);
        }

        void DrawConditions(Conditions conditions)
        {
            if (conditions == null)
                return;

            DrawLabelValue("Condition Count", conditions.Count.ToString());

            m_ShowConditions = EditorGUILayout.Foldout(m_ShowConditions, "show per-type counts", true);
            if (!m_ShowConditions)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                if(conditions.TryGetType(out ICondition<int>[] intConditions))
                    DrawLabelValue("int", intConditions.Length.ToString());

                if(conditions.TryGetType(out ICondition<float>[] floatConditions))
                    DrawLabelValue("float", floatConditions.Length.ToString());

                if(conditions.TryGetType(out ISemanticTagCondition[] semanticTagConditions))
                    DrawLabelValue("semantic tag", semanticTagConditions.Length.ToString());

                if(conditions.TryGetType(out ICondition<Vector2>[] vector2Conditions))
                    DrawLabelValue("vector2", vector2Conditions.Length.ToString());

                if(conditions.TryGetType(out ICondition<Vector3>[] vector3Conditions))
                    DrawLabelValue("vector3", vector3Conditions.Length.ToString());

                if(conditions.TryGetType(out ICondition<string>[] stringConditions))
                    DrawLabelValue("string", stringConditions.Length.ToString());

                if(conditions.TryGetType(out ICondition<Pose>[] poseConditions))
                    DrawLabelValue("pose", poseConditions.Length.ToString());
            }
        }

        static void DrawLabelValue(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                EditorGUILayout.LabelField(value, EditorStyles.miniLabel, k_TraitValueColumnWidth);
            }
        }

        static void DrawHandlers(Action<QueryResult> acquire, Action<QueryResult> update,
                          Action<QueryResult> loss, Action<QueryArgs> timeout)
        {
            const string none = "None";
            DrawLabelValue("Acquire Handler", acquire == null ? none : acquire.Method.Name);
            DrawLabelValue("Update Handler", update  == null ? none : update.Method.Name);
            DrawLabelValue("Loss Handler", loss == null ? none : loss.Method.Name);
            DrawLabelValue("Timeout Handler", timeout == null? none : timeout.Method.Name);
        }

        void DrawQueryResultFoldout(QueryResult result)
        {
            if (result == null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Query Result", EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    EditorGUILayout.LabelField("null", EditorStyles.miniBoldLabel, k_TraitValueColumnWidth);
                }
                return;
            }

            m_ShowStandaloneQueryResult = EditorGUILayout.Foldout(m_ShowStandaloneQueryResult, "show query result", true);
            if (!m_ShowStandaloneQueryResult)
                return;

            using (new EditorGUI.IndentLevelScope())
            {
                DrawQueryResult(result);
            }
        }

        void DrawReducedRatings()
        {
            if (m_ReducedConditionRatings != null)
            {
                m_ShowStandaloneRatings = EditorGUILayout.Foldout(m_ShowStandaloneRatings, "show ratings", true);
                if (!m_ShowStandaloneRatings)
                    return;

                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUIUtils.DrawDictionary(m_ReducedConditionRatings,
                        "Data ID", "Match Rating", k_MatchColumnWidth, k_TraitValueColumnWidth, "F4");
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Reduced ratings", EditorStyles.miniBoldLabel, k_MatchColumnWidth);
                    EditorGUILayout.LabelField("null", EditorStyles.miniLabel, k_TraitValueColumnWidth);
                }
            }
        }

        static void DrawTraitCacheData(CachedTraitCollection collection)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("trait cache", EditorStyles.miniBoldLabel, k_MatchColumnWidth);

                string label;
                if (collection == null)
                    label = "null";
                else
                    label = collection.fulfilled ? "fulfilled" : "unfulfilled";

                EditorGUILayout.LabelField(label, k_MiddleColumnWidth);
            }
        }

        void GetStandaloneIndexValues(int i)
        {
            m_QueryMatchId = s_QueryData.queryMatchIds[i];
            m_Exclusivity = s_QueryData.exclusivities[i];
            m_UpdateMatchInterval = s_QueryData.updateMatchInterval[i];
            m_TimeoutLeft = s_QueryData.timeOuts[i];
            m_ReacquireOnLoss = s_QueryData.reAcquireOnLoss[i];
            m_CachedTraits = s_QueryData.cachedTraits[i];
            m_Conditions = s_QueryData.conditions[i];
            // don't fetch the ConditionRatingsData yet - only if we want a detailed per-condition rating inspection
            // don't bother with fetching the "match set" HashSet - that info is in the reduced ratings.
            m_ReducedConditionRatings = s_QueryData.reducedConditionRatings[i];
            m_BestMatchDataId = s_QueryData.bestMatchDataIds[i];
            m_QueryResult = s_QueryData.queryResults[i];
            m_AcquireHandler = s_QueryData.acquireHandlers[i];
            m_UpdateHandler = s_QueryData.updateHandlers[i];
            m_LossHandler = s_QueryData.lossHandlers[i];
            m_TimeoutHandler = s_QueryData.timeoutHandlers[i];
        }

        // used for updating the view when the environment changes
        void GetValuesAndRepaint(QueryMatchID id, int dataId)
        {
            m_SearchMatchFound = s_QueryData.matchIdToIndex.TryGetValue(m_SearchMatchId, out var index);
            if (m_SearchMatchFound)
            {
                m_SelectedStandaloneIndex = index;
                GetStandaloneIndexValues(index);
            }

            Repaint();
        }

        void GetValuesAndRepaint()
        {
            CacheReferences();
            m_SearchMatchFound = s_QueryData.matchIdToIndex.TryGetValue(m_SearchMatchId, out var index);
            if (m_SearchMatchFound)
            {
                m_SelectedStandaloneIndex = index;
                GetStandaloneIndexValues(index);
            }

            Repaint();
        }
    }
}
