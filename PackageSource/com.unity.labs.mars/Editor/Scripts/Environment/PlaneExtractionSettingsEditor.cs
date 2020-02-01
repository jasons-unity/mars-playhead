using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [CustomEditor(typeof(PlaneExtractionSettings))]
    public class PlaneExtractionSettingsEditor : Editor
    {
        static readonly string[] k_DontDraw = { "m_Script" };

        PlaneExtractionSettings m_Settings;

        void OnEnable()
        {
            m_Settings = target as PlaneExtractionSettings;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, k_DontDraw);
            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Extract Planes"))
            {
                PlanesExtractionManager.instance.ExtractPlanes(m_Settings);
            }
        }
    }
}
