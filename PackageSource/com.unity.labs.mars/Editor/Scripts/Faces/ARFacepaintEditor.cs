using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Unity.Labs.MARS
{
    [CustomPropertyDrawer(typeof(DecalLayer))]
    public class DecalLayerDrawer : PropertyDrawer
    {
        int fieldCount = 4;


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return fieldCount * EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var textureProperty = property.FindPropertyRelative("texture");
            var blendModeProperty = property.FindPropertyRelative("blendMode");
            var transformPositionOffsetProperty = property.FindPropertyRelative("transformPositionOffset");
            var sizeProperty = property.FindPropertyRelative("size");

            EditorGUILayout.BeginVertical();
            Rect guiRect = Rect.zero;

            guiRect.x = position.x;
            guiRect.y = position.y;
            guiRect.width = position.width;
            guiRect.height = position.height / 4;

            EditorGUI.PropertyField(guiRect, textureProperty, new GUIContent("Texture"), false);

            guiRect.y += guiRect.height;

            EditorGUI.PropertyField(guiRect, blendModeProperty, new GUIContent("Blend Mode"), false);

            guiRect.y += guiRect.height;

            EditorGUI.PropertyField(guiRect, transformPositionOffsetProperty, new GUIContent("Position Offset"), false);

            guiRect.y += guiRect.height;

            EditorGUI.PropertyField(guiRect, sizeProperty, new GUIContent("Size"), false);

            EditorGUILayout.EndVertical();
        }
    } 

    [CustomEditor(typeof(ARFacepaint))]
    public class ARFacepaintEditor : Editor
    {
        ARFacepaint scriptInstance;

        SerializedProperty serializedDecalList;

        ReorderableList decalList;

        void OnEnable()
        {
            if (target == null || serializedObject == null)
                return;

            scriptInstance = (ARFacepaint) target;

            serializedDecalList = serializedObject.FindProperty("decalLayerList");

            InitializeDecalList();
        }

        public override void OnInspectorGUI()
        {
            if (target == null || serializedObject == null)
                return;

            serializedObject.Update();

            decalList.DoLayoutList();

            serializedObject.ApplyModifiedProperties();
        }

        void InitializeDecalList()
        {
            decalList = new ReorderableList(serializedObject, serializedDecalList, true, true, true, true);

            decalList.elementHeight = 4 * EditorGUIUtility.singleLineHeight;

            decalList.drawHeaderCallback = (Rect rect) =>
            {
                EditorGUI.LabelField(rect, "Decal List");
            };

            decalList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var serializedDecal = decalList.serializedProperty.GetArrayElementAtIndex(index);

                EditorGUI.PropertyField(rect, serializedDecal, GUIContent.none, false);
            };

            decalList.onAddCallback = (ReorderableList list) => 
            {
                int index = list.serializedProperty.arraySize;

                list.serializedProperty.InsertArrayElementAtIndex(index);

                var serializedDecal = list.serializedProperty.GetArrayElementAtIndex(index);

                var textureProperty = serializedDecal.FindPropertyRelative("texture");
                var blendModeProperty = serializedDecal.FindPropertyRelative("blendMode");
                var transformPositionOffsetProperty = serializedDecal.FindPropertyRelative("transformPositionOffset");
                var sizeProperty = serializedDecal.FindPropertyRelative("size");
                var materialInstanceProperty = serializedDecal.FindPropertyRelative("materialInstance");

                textureProperty.objectReferenceValue = null;
                blendModeProperty.enumValueIndex = 0;
                transformPositionOffsetProperty.vector2Value = new Vector2(0.5f, 0.5f);
                sizeProperty.vector2Value = Vector2.one;
                materialInstanceProperty.objectReferenceValue = null;

                scriptInstance.ResetDecalLayerMaterialsList();
            };

            decalList.onRemoveCallback = (ReorderableList list) =>
            {
                scriptInstance.RemoveDecalLayer(list.index);

                ReorderableList.defaultBehaviours.DoRemoveButton(list);
            };

            decalList.onReorderCallback = (ReorderableList list) =>
            {
                scriptInstance.ResetDecalLayerMaterialsList();
            };
        }
    }
}
