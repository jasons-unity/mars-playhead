using Unity.Labs.Utils;
using UnityEditor;

namespace Unity.Labs.MARS.Providers
{
    [CustomEditor(typeof(GeoLocationModule))]
    public class GeoLocationModuleEditor : Editor
    {
        SerializedProperty m_CurrentLocationProperty;
        SerializedProperty m_LatitudeProperty;
        SerializedProperty m_LongitudeProperty;
        SerializedProperty m_DesiredAccuracyProperty;
        SerializedProperty m_UpdateDistanceProperty;
        SerializedProperty m_ContinuousUpdatesProperty;
        SerializedProperty m_UpdateIntervalProperty;

        public void OnEnable()
        {
            m_CurrentLocationProperty = serializedObject.FindProperty("m_CurrentLocation");
            m_LatitudeProperty = m_CurrentLocationProperty.FindPropertyRelative("latitude");
            m_LongitudeProperty = m_CurrentLocationProperty.FindPropertyRelative("longitude");
            m_DesiredAccuracyProperty = serializedObject.FindProperty("m_DesiredAccuracy");
            m_ContinuousUpdatesProperty = serializedObject.FindProperty("m_ContinuousUpdates");
            m_UpdateDistanceProperty = serializedObject.FindProperty("m_UpdateDistance");
            m_UpdateIntervalProperty = serializedObject.FindProperty("m_UpdateInterval");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(m_DesiredAccuracyProperty);
            EditorGUILayout.PropertyField(m_ContinuousUpdatesProperty);
            using (new EditorGUI.DisabledScope(!m_ContinuousUpdatesProperty.boolValue))
            {
                EditorGUILayout.PropertyField(m_UpdateDistanceProperty);
                EditorGUILayout.PropertyField(m_UpdateIntervalProperty);
            }

            EditorGUILayout.LabelField("Simulated Geolocation", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Change lat/long here to simulate different testing locations.\nValues here have no effect in builds.", MessageType.Info);
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_LatitudeProperty);
                EditorGUILayout.PropertyField(m_LongitudeProperty);
                if (changeCheck.changed)
                {
                    m_LatitudeProperty.doubleValue = MathUtility.Clamp(m_LatitudeProperty.doubleValue, -GeoLocationModule.MaxLatitude, GeoLocationModule.MaxLatitude);
                    m_LongitudeProperty.doubleValue = MathUtility.Clamp(m_LongitudeProperty.doubleValue, -GeoLocationModule.MaxLongitude, GeoLocationModule.MaxLongitude);
                    GeoLocationModule.instance.AddOrUpdateLocationTrait();
                }
            }

            GeoLocationShortcutButtons.DrawShortcutButtons("Debug Geolocation Shortcuts", (latitude, longitude) =>
            {
                m_LatitudeProperty.doubleValue = latitude;
                m_LongitudeProperty.doubleValue = longitude;
                GeoLocationModule.instance.AddOrUpdateLocationTrait();
            });

            serializedObject.ApplyModifiedProperties();
        }
    }
}
