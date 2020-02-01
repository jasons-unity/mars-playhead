using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    [CustomEditor(typeof(SynthesizedPlane))]
    [CanEditMultipleObjects]
    public class SynthesizedPlaneEditor : Editor
    {
        static readonly string[] k_ExcludeProperties = {"m_Script"};

        List<SynthesizedPlane> m_SynthesizedPlanes = new List<SynthesizedPlane>();

        void OnEnable()
        {
            foreach (var obj in targets)
            {
                var synthesizedPlane = obj as SynthesizedPlane;
                m_SynthesizedPlanes.Add(synthesizedPlane);
            }
        }

        public override void OnInspectorGUI()
        {
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                serializedObject.Update();

                DrawPropertiesExcluding(serializedObject, k_ExcludeProperties);

                serializedObject.ApplyModifiedProperties();

                if (change.changed)
                {
                    Undo.RecordObjects(targets, "Check Synthesized Planes");
                    foreach (var synthesizedPlane in m_SynthesizedPlanes)
                    {
                        synthesizedPlane.ValidateUsingMeshCache();
                    }
                }
            }
        }
    }
}
