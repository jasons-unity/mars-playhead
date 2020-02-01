using Unity.Labs.MARS.Query;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [CustomPropertyDrawer(typeof(CommonQueryData))]
    public class SharedQueryDataEditor : PropertyDrawer
    {
        const string k_OverrideName = "overrideTimeout";
        const string k_TimeoutName = "timeOut";
        const string k_IntervalName = "updateMatchInterval";
        const string k_ReacquireName = "reacquireOnLoss";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(position, property, label, false);
            if (property.isExpanded)
            {
                var overrideData = EditorGUIUtils.FindSerializedPropertyData(property.FindPropertyRelative(k_OverrideName));
                var timeout = EditorGUIUtils.FindSerializedPropertyData(property.FindPropertyRelative(k_TimeoutName));

                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUIUtils.PropertyFieldInRect(position, overrideData, timeout);
                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, property.FindPropertyRelative(k_IntervalName));
                position.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(position, property.FindPropertyRelative(k_ReacquireName));
            }
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight * 5;
            }
            else
            {
                return EditorGUIUtility.singleLineHeight;
            }
        }
    }
}
