using UnityEditor;

namespace Unity.Labs.MARS
{
    [ComponentEditor(typeof(SemanticTagCondition))]
    public class SemanticTagConditionInspector : ComponentInspector
    {
        SerializedPropertyData m_TraitNameProperty;
        SerializedPropertyData m_MatchRuleProperty;

        public override void OnEnable()
        {
            base.OnEnable();
            m_TraitNameProperty = serializedObject.FindSerializedPropertyData("m_TraitName");
            m_MatchRuleProperty = serializedObject.FindSerializedPropertyData("m_MatchRule");
        }

        public override void OnInspectorGUI()
        {
            if (isDirty)
                CleanUp();

            serializedObject.Update();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUIUtils.PropertyField(m_TraitNameProperty);

                if (check.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                    MARSSession.Instance.CheckCapabilities();        // make sure to update scene requirements
                    isDirty = true;
                }
            }

            EditorGUIUtils.PropertyField(m_MatchRuleProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
