using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Labs.MARS
{
    public static class FaceEditorUtils
    {
        public static void LandmarkTransformFields(Object target, List<Transform> landmarkTransforms)
        {
            EditorGUILayout.LabelField("Landmarks", EditorStyles.boldLabel);
            for (var i = 0; i < landmarkTransforms.Count; ++i)
            {
                EditorGUI.BeginChangeCheck();
                var landmark = (MRFaceLandmark)i;
                var transform = (Transform)EditorGUILayout.ObjectField(
                    landmark.ToString(), landmarkTransforms[i], typeof(Transform), true);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Change Landmark Transform");
                    landmarkTransforms[i] = transform;
                }
            }
        }
    }
}
