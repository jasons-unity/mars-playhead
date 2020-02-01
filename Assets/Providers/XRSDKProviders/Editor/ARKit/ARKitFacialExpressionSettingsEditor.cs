using UnityEditor;

namespace Unity.Labs.MARS.Providers
{
    [CustomEditor(typeof(ARKitFacialExpressionSettings))]
    public class ARKitFacialExpressionSettingsEditor : Editor
    {
        SerializedProperty m_ThresholdsProperty;
        SerializedProperty m_CooldownProperty;
        SerializedProperty m_SmoothingFilter;

        void OnEnable()
        {
            m_ThresholdsProperty = serializedObject.FindProperty("m_Thresholds");
            m_CooldownProperty = serializedObject.FindProperty("m_EventCooldownInSeconds");
            m_SmoothingFilter = serializedObject.FindProperty("m_ExpressionChangeSmoothingFilter");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            ARKitFaceEditorUtils.ExpressionThresholdFields(m_ThresholdsProperty);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var cooldown = EditorGUILayout.FloatField("Event Cooldown In Seconds", m_CooldownProperty.floatValue);
                if (check.changed)
                    m_CooldownProperty.floatValue = cooldown;
            }

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var smoothing = EditorGUILayout.FloatField("Expression Change Smoothing Filter", m_SmoothingFilter.floatValue);
                if (check.changed)
                    m_SmoothingFilter.floatValue = smoothing;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
