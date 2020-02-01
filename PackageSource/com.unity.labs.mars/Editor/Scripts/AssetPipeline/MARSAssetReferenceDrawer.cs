#if INCLUDE_ADDRESSABLES
using AddressableAssets;
#else
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
#endif

using System;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEditor;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Draws inspector GUI for MARSAssetReference properties
    /// Note: this code is largely copied from AddressableAssets and should remain as close to the original as possible
    /// https://github.com/Unity-Technologies/AddressableAssets/blob/master/Editor/GUI/AssetReferenceDrawer.cs
    /// </summary>
    [CustomPropertyDrawer(typeof(MARSAssetReference))]
    class MARSAssetReferenceDrawer
#if INCLUDE_ADDRESSABLES
        : AssetReferenceDrawer
#else
        : PropertyDrawer
#endif
    {
#if INCLUDE_ADDRESSABLES
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            base.OnGUI(position, property, label);

            if (property.serializedObject.ApplyModifiedProperties())
                OnModified(property);
        }
#else
        public string newGuid;
        public string newGuidPropertyPath;
        string assetName;
        Rect smallPos;

        public const string k_noAssetString = "None (AddressableAsset)";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label.text = ObjectNames.NicifyVariableName(property.propertyPath);
            EditorGUI.BeginProperty(position, label, property);

            var guidProp = property.FindPropertyRelative("assetGUID");
            var objProp = property.FindPropertyRelative("_cachedAsset");

            if (!string.IsNullOrEmpty(newGuid) && newGuidPropertyPath == property.propertyPath)
            {
                if (newGuid == k_noAssetString)
                {
                    guidProp.stringValue = string.Empty;
                    objProp.objectReferenceValue = null;

                    newGuid = string.Empty;
                }
                else
                {
                    guidProp.stringValue = newGuid;

                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetDatabase.GUIDToAssetPath(newGuid));
                    objProp.objectReferenceValue = obj;

                    newGuid = string.Empty;
                }
            }

            assetName = k_noAssetString;
            Texture2D icon = null;

            var cachedAsset = objProp.objectReferenceValue;
            if (cachedAsset)
            {
                assetName = cachedAsset.name;
                icon = AssetPreview.GetMiniThumbnail(cachedAsset);
            }

            smallPos = EditorGUI.PrefixLabel(position, label);
            if (EditorGUI.DropdownButton(smallPos, new GUIContent(assetName, icon, "Addressable Asset Reference"), FocusType.Keyboard))
            {
                newGuidPropertyPath = property.propertyPath;
                //PopupWindow.Show(smallPos, new AssetReferencePopup(this));
            }


            if (Event.current.type == EventType.DragUpdated && position.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            }
            if (Event.current.type == EventType.DragPerform && position.Contains(Event.current.mousePosition))
            {
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length == 1)
                {
                    UnityEngine.Object obj = null;
                    if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length == 1)
                        obj = DragAndDrop.objectReferences[0];
                    var newPath = DragAndDrop.paths[0];
                    guidProp.stringValue = AssetDatabase.AssetPathToGUID(newPath);
                    objProp.objectReferenceValue = obj;
                }
            }
            EditorGUI.EndProperty();

            if (property.serializedObject.ApplyModifiedProperties())
                OnModified(property);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return base.GetPropertyHeight(property, label);
        }

        class AssetReferencePopup : PopupWindowContent
        {
            AssetReferenceTreeView tree;
            [SerializeField]
            TreeViewState treeState;

            string currentName = string.Empty;
            MARSAssetReferenceDrawer m_drawer;

            SearchField searchField;

            public AssetReferencePopup(MARSAssetReferenceDrawer drawer)
            {
                m_drawer = drawer;
                searchField = new SearchField();
            }

            public override void OnClose()
            {
                base.OnClose();
            }

            public override void OnOpen()
            {
                searchField.SetFocus();
                base.OnOpen();
            }

            public override Vector2 GetWindowSize()
            {
                Vector2 result = base.GetWindowSize();
                result.x = m_drawer.smallPos.width;
                return result;
            }

            public override void OnGUI(Rect rect)
            {
                const int border = 4;
                const int topPadding = 12;
                const int searchHeight = 20;
                const int remainTop = topPadding + searchHeight + border;
                var searchRect = new Rect(border, topPadding, rect.width - border * 2, searchHeight);
                var remainingRect = new Rect(border, topPadding + searchHeight + border, rect.width - border * 2, rect.height - remainTop - border);
                currentName = searchField.OnGUI(searchRect, currentName);


                if (tree == null)
                {
                    if (treeState == null)
                        treeState = new TreeViewState();
                    tree = new AssetReferenceTreeView(treeState, m_drawer);
                    tree.Reload();
                }
                tree.searchString = currentName;
                tree.OnGUI(remainingRect);
            }

            private class AssetRefTreeViewItem : TreeViewItem
            {
                public string guid;
                public AssetRefTreeViewItem(int id, int depth, string displayName, string g, string path) : base(id, depth, displayName)
                {
                    guid = g;
                    icon = AssetDatabase.GetCachedIcon(path) as Texture2D;
                }
            }
            private class AssetReferenceTreeView : TreeView
            {
                MARSAssetReferenceDrawer m_drawer;
                public AssetReferenceTreeView(TreeViewState state, MARSAssetReferenceDrawer drawer) : base(state)
                {
                    m_drawer = drawer;
                    showBorder = true;
                    showAlternatingRowBackgrounds = true;
                }

                protected override bool CanMultiSelect(TreeViewItem item)
                {
                    return false;
                }

                protected override void SelectionChanged(IList<int> selectedIds)
                {
                    if (selectedIds.Count == 1)
                    {
                        var assetRefItem = FindItem(selectedIds[0], rootItem) as AssetRefTreeViewItem;
                        if (!string.IsNullOrEmpty(assetRefItem.guid))
                        {
                            m_drawer.newGuid = assetRefItem.guid;
                        }
                        else
                        {
                            m_drawer.newGuid = k_noAssetString;
                        }
                        PopupWindow.focusedWindow.Close();
                    }
                }

                protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
                {
                    if (string.IsNullOrEmpty(searchString))
                    {
                        return base.BuildRows(root);
                    }
                    else
                    {
                        List<TreeViewItem> rows = new List<TreeViewItem>();

                        foreach (var child in rootItem.children)
                        {
                            if (child.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                                rows.Add(child);
                        }

                        return rows;
                    }
                }

                protected override TreeViewItem BuildRoot()
                {
                    var root = new TreeViewItem(-1, -1);

                    return root;
                }
            }
        }
#endif

        // TODO: Update on build/play to account for changes after assignment
        static void OnModified(SerializedProperty property)
        {
            var boundsProp = property.FindPropertyRelative("m_ProxyBounds");
            var assetProp = property.FindPropertyRelative("_cachedAsset");
            var asset = assetProp.objectReferenceValue;
            if (!asset)
                return;

            var go = asset as GameObject;
            if (go)
            {
                boundsProp.boundsValue = BoundsUtils.GetBounds(go.transform);
                boundsProp.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
