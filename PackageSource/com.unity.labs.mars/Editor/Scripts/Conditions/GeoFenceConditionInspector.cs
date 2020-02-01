using Unity.Labs.MARS.Providers;
using Unity.Labs.ModuleLoader;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ComponentEditor(typeof(GeoFenceCondition))]
    public class GeoFenceConditionInspector : ComponentInspector
    {
        SerializedProperty m_GeoBoundsProperty;
        SerializedProperty m_RuleProperty;
        SerializedProperty m_Latitude;
        SerializedProperty m_Longitude;

        public override void OnEnable()
        {
            base.OnEnable();
            m_RuleProperty = serializedObject.FindProperty("m_Rule");
            m_GeoBoundsProperty = serializedObject.FindProperty("m_BoundingBox");
            var center = m_GeoBoundsProperty.FindPropertyRelative("center");
            m_Latitude = center.FindPropertyRelative("latitude");
            m_Longitude = center.FindPropertyRelative("longitude");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();
            EditorGUILayout.PropertyField(m_RuleProperty);
            EditorGUILayout.PropertyField(m_GeoBoundsProperty);

            GeoLocationShortcutButtons.DrawShortcutButtons("Geolocation shortcuts", (latitude, longitude) =>
            {
                m_Latitude.doubleValue = latitude;
                m_Longitude.doubleValue = longitude;
            });

            var showHint = MARSUserPreferences.instance.showSimulatedGeoLocationHint;
            if (showHint)
            {
                showHint = MARSUtils.HintBox(showHint, "You can set the simulated geolocation in the GeoLocationModule.", "SetSimLocationInGeoModule");
                if (GUILayout.Button("Go to GeoLocationModule"))
                {
                    Selection.activeObject = ModuleLoaderCore.instance.GetModule<GeoLocationModule>();
                }

                if (!showHint)
                    MARSUserPreferences.instance.showSimulatedGeoLocationHint = showHint;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
