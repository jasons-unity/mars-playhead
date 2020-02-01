#if INCLUDE_MARS
using UnityEditor;

namespace Unity.Labs.MARS
{
    [CustomEditor(typeof(ULSFacialExpressionSettings))]
    class ULSFacialExpressionSettingsEditor : Editor
    {
        SerializedProperty m_ThresholdsProperty;
        SerializedProperty m_ExpressionDistanceMinimumsProperty;
        SerializedProperty m_ExpressionDistanceMaximumsProperty;
        SerializedProperty m_ExpressionDistanceReverseStatesProperty;

        SerializedProperty m_MaxHeadXProperty;
        SerializedProperty m_MaxHeadYProperty;
        SerializedProperty m_MaxHeadZProperty;

        void OnEnable()
        {
            m_ThresholdsProperty = serializedObject.FindProperty("m_Thresholds");
            m_ExpressionDistanceMinimumsProperty = serializedObject.FindProperty("m_ExpressionDistanceMinimums");
            m_ExpressionDistanceMaximumsProperty = serializedObject.FindProperty("m_ExpressionDistanceMaximums");
            m_ExpressionDistanceReverseStatesProperty = serializedObject.FindProperty("m_ExpressionDistanceReverseStates");
            m_MaxHeadXProperty = serializedObject.FindProperty("m_MaxHeadAngleX");
            m_MaxHeadYProperty = serializedObject.FindProperty("m_MaxHeadAngleY");
            m_MaxHeadZProperty = serializedObject.FindProperty("m_MaxHeadAngleZ");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("Maximum Head Angles", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Beyond these angles, expressions will not be calculated because the landmark data is less reliable", MessageType.Info);
            EditorGUILayout.PropertyField(m_MaxHeadXProperty);
            EditorGUILayout.PropertyField(m_MaxHeadYProperty);
            EditorGUILayout.PropertyField(m_MaxHeadZProperty);

            EditorGUILayout.Separator();
            ULSFaceEditorUtils.ExpressionThresholdFields(m_ThresholdsProperty);
            EditorGUILayout.Separator();

            ULSFaceEditorUtils.ULSExpressionDistanceRangeFields(m_ExpressionDistanceReverseStatesProperty,
                m_ExpressionDistanceMinimumsProperty, m_ExpressionDistanceMaximumsProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
