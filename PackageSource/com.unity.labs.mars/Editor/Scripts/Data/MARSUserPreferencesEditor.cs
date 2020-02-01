using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [CustomEditor(typeof(MARSUserPreferences))]
    public class MARSUserPreferencesEditor : Editor
    {
        MARSUserPreferencesDrawer m_PreferencesDrawer;

        void OnEnable()
        {
            m_PreferencesDrawer = new MARSUserPreferencesDrawer(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            var prefs = serializedObject.targetObject as MARSUserPreferences;

            m_PreferencesDrawer.InspectorGUI(prefs, serializedObject);
        }
    }

    public class MARSUserPreferencesDrawer
    {
        SerializedProperty m_ShowMARSHintsProperty;
		SerializedProperty m_DefaultHandleColorProperty;
        SerializedProperty m_ValidDependencyColorProperty;
        SerializedProperty m_InvalidDependencyColorProperty;
        SerializedProperty m_RelationColorProperty;
        SerializedProperty m_EditingRelationColorProperty;
        SerializedProperty m_PlaneSizeConditionColorProperty;
        SerializedProperty m_ElevationRelationColorProperty;
        SerializedProperty m_DistanceConditionColorProperty;
        SerializedProperty m_AngleAxisConditionColorProperty;
        SerializedProperty m_UnmatchedConditionColorProperty;
        SerializedProperty m_HighlightedSimulatedObjectColorProperty;
        SerializedProperty m_ExtractPlanesOnSaveProperty;
        SerializedProperty m_ScaleEntityPositionsProperty;
        SerializedProperty m_ScaleEntityChildrenProperty;
        SerializedProperty m_ScaleSceneAudioProperty;
        SerializedProperty m_ScaleSceneLightingProperty;
        SerializedProperty m_RestrictCameraToEnvironmentBoundsProperty;
        SerializedProperty m_TintImageMarkers;

        public MARSUserPreferencesDrawer(SerializedObject serializedObject)
        {
            m_ShowMARSHintsProperty = serializedObject.FindProperty("m_ShowMARSHints");
            m_DefaultHandleColorProperty = serializedObject.FindProperty("m_DefaultHandleColor");
            m_ValidDependencyColorProperty = serializedObject.FindProperty("m_ValidDependencyColor");
            m_InvalidDependencyColorProperty = serializedObject.FindProperty("m_InvalidDependencyColor");
            m_RelationColorProperty = serializedObject.FindProperty("m_RelationColor");
            m_EditingRelationColorProperty = serializedObject.FindProperty("m_EditingRelationColor");
            m_PlaneSizeConditionColorProperty = serializedObject.FindProperty("m_PlaneSizeConditionColor");
            m_ElevationRelationColorProperty = serializedObject.FindProperty("m_ElevationRelationColor");
            m_DistanceConditionColorProperty = serializedObject.FindProperty("m_DistanceConditionColor");
            m_AngleAxisConditionColorProperty = serializedObject.FindProperty("m_AngleAxisConditionColor");
            m_UnmatchedConditionColorProperty = serializedObject.FindProperty("m_UnmatchedConditionColor");
            m_HighlightedSimulatedObjectColorProperty = serializedObject.FindProperty("m_HighlightedSimulatedObjectColor");
            m_ExtractPlanesOnSaveProperty = serializedObject.FindProperty("m_ExtractPlanesOnSave");
            m_ScaleEntityPositionsProperty = serializedObject.FindProperty("m_ScaleEntityPositions");
            m_ScaleEntityChildrenProperty = serializedObject.FindProperty("m_ScaleEntityChildren");
            m_ScaleSceneAudioProperty = serializedObject.FindProperty("m_ScaleSceneAudio");
            m_ScaleSceneLightingProperty = serializedObject.FindProperty("m_ScaleSceneLighting");
            m_RestrictCameraToEnvironmentBoundsProperty = serializedObject.FindProperty("m_RestrictCameraToEnvironmentBounds");
            m_TintImageMarkers = serializedObject.FindProperty("m_TintImageMarkers");
        }

        public void InspectorGUI(MARSUserPreferences prefs, SerializedObject serializedObject)
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_ShowMARSHintsProperty);
            GUILayout.Space(5);

            if (GUILayout.Button("Reset All Hints", GUILayout.Width(120)))
                MARSUserPreferences.instance.ResetHints();

            EditorGUILayout.PropertyField(m_DefaultHandleColorProperty);
            EditorGUILayout.PropertyField(m_ValidDependencyColorProperty);
            EditorGUILayout.PropertyField(m_InvalidDependencyColorProperty);
            EditorGUILayout.PropertyField(m_RelationColorProperty);
            EditorGUILayout.PropertyField(m_EditingRelationColorProperty);
            EditorGUILayout.PropertyField(m_PlaneSizeConditionColorProperty);
            EditorGUILayout.PropertyField(m_ElevationRelationColorProperty);
            EditorGUILayout.PropertyField(m_DistanceConditionColorProperty);
            EditorGUILayout.PropertyField(m_AngleAxisConditionColorProperty);
            EditorGUILayout.PropertyField(m_UnmatchedConditionColorProperty);
            EditorGUILayout.PropertyField(m_HighlightedSimulatedObjectColorProperty);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Use Default Colors"))
                {
                    prefs.ResetColors();
                    EditorUtility.SetDirty(prefs);
                    serializedObject.UpdateIfRequiredOrScript();
                }
                GUILayout.FlexibleSpace();
            }

            if (EditorGUI.EndChangeCheck())
                SceneView.RepaintAll();

            GUILayout.Space(5);

            EditorGUILayout.PropertyField(m_ExtractPlanesOnSaveProperty);
            EditorGUILayout.PropertyField(m_ScaleEntityPositionsProperty);
            EditorGUILayout.PropertyField(m_ScaleEntityChildrenProperty);
            EditorGUILayout.PropertyField(m_ScaleSceneAudioProperty);
            EditorGUILayout.PropertyField(m_ScaleSceneLightingProperty);

            EditorGUILayout.PropertyField(m_RestrictCameraToEnvironmentBoundsProperty);
            
            EditorGUILayout.PropertyField(m_TintImageMarkers);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
