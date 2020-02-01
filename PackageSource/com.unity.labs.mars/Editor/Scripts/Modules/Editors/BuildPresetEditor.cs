using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BuildPreset))]
    public class BuildPresetEditor : Editor
    {
        BuildPreset m_BuildPreset;

        SerializedProperty m_DefaultOrientationProp;
        SerializedProperty m_PortraitProp;
        SerializedProperty m_PortraitUpsideDownProp;
        SerializedProperty m_LandscapeRightProp;
        SerializedProperty m_LandscapeLeftProp;
        SerializedProperty m_ARCoreEnabledProp;

        void OnEnable()
        {
            m_BuildPreset = (BuildPreset)target;

            m_DefaultOrientationProp = serializedObject.FindProperty("m_DefaultOrientation");
            m_PortraitProp = serializedObject.FindProperty("m_Portrait");
            m_PortraitUpsideDownProp = serializedObject.FindProperty("m_PortraitUpsideDown");
            m_LandscapeRightProp = serializedObject.FindProperty("m_LandscapeRight");
            m_LandscapeLeftProp = serializedObject.FindProperty("m_LandscapeLeft");
            m_ARCoreEnabledProp = serializedObject.FindProperty("m_ARCoreEnabled");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_DefaultOrientationProp);
            if (m_BuildPreset.defaultInterfaceOrientation == UIOrientation.AutoRotation)
            {
                GUILayout.Label("Allowed Orientations for Auto Rotation", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_PortraitProp);
                EditorGUILayout.PropertyField(m_PortraitUpsideDownProp);
                EditorGUILayout.PropertyField(m_LandscapeRightProp);
                EditorGUILayout.PropertyField(m_LandscapeLeftProp);
            }

            EditorGUILayout.PropertyField(m_ARCoreEnabledProp);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
