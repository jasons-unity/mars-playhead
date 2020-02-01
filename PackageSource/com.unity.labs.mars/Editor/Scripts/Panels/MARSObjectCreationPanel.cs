using System.Collections.Generic;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [PanelOrder(PanelOrders.MARSObjectCreationOrder)]
    class MARSObjectCreationPanel : PanelView
    {
        const string k_ObjectCreationPanelLabel = "Create";
        const int k_NumColumns = 2;
        const float k_InverseNumColumns = 1 / (float) k_NumColumns;
        const float k_RowPadding = 3.5f;

        static readonly Vector2 k_PrefSize = new Vector2(240f, 240f);
        static readonly Vector2 k_MinSize = new Vector2(128f, 128f);
        static readonly Vector2 k_MaxSize = new Vector2(MARSEditorGUI.MaxWindowSize, MARSEditorGUI.MaxWindowSize);
        static readonly string k_TypeName = typeof(MARSObjectCreationPanel).FullName;

        class Styles
        {
            const int k_PaddingX = 10;
            const int k_PaddingY = 8;

            public readonly GUIStyle createObjectButtonStyle;
            public readonly GUIStyle createObjectButtonStyleFixedIcon;
            public readonly Dictionary<MARSObjectCreationResources.ObjectCreationButton, GUIContent> buttonGUIContents =
                new Dictionary<MARSObjectCreationResources.ObjectCreationButton, GUIContent>();
            public readonly GUIStyle indentAlignment;

            float m_ImageSize;
            bool m_FixedIconSizeCalculated;

            public int paddingX => k_PaddingX;

            public Styles()
            {
                createObjectButtonStyle = new GUIStyle(MARSEditorGUI.Styles.Button)
                {
                    imagePosition = ImagePosition.ImageLeft,
                    padding = new RectOffset(k_PaddingX, k_PaddingX, k_PaddingY, k_PaddingY),
                    clipping = TextClipping.Clip,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false,
                };

                createObjectButtonStyleFixedIcon = new GUIStyle(createObjectButtonStyle);

                foreach (var creationButtonSection in MARSObjectCreationResources.instance.ObjectCreationButtonSections)
                {
                    foreach (var objCreationButton in creationButtonSection.Buttons)
                    {
                        buttonGUIContents.Add(objCreationButton, objCreationButton.CreationButtonContent());
                    }
                }

                indentAlignment = new GUIStyle(MARSEditorGUI.Styles.NoMarginScopeAlignment);
                indentAlignment.margin.left += k_PaddingX;
            }

            void CalculateFixedIconStyleSize()
            {
                if (m_FixedIconSizeCalculated || Event.current == null || Event.current.type != EventType.Layout)
                    return;

                createObjectButtonStyleFixedIcon.imagePosition = ImagePosition.ImageLeft;
                createObjectButtonStyleFixedIcon.padding = new RectOffset(k_PaddingX, k_PaddingX, k_PaddingY, k_PaddingY);

                var buttonSize = createObjectButtonStyleFixedIcon.CalcSize(new GUIContent("Test", Texture2D.blackTexture));
                m_ImageSize = buttonSize.y - k_PaddingY * 2f;

                createObjectButtonStyleFixedIcon.imagePosition = ImagePosition.TextOnly;
                createObjectButtonStyleFixedIcon.padding.left += (int) m_ImageSize;

                m_FixedIconSizeCalculated = true;
            }

            public bool DrawFixedIconButton(GUIContent guiContent, params GUILayoutOption[] options)
            {
                CalculateFixedIconStyleSize();

                var buttonStyle = guiContent.image != null ? createObjectButtonStyleFixedIcon : createObjectButtonStyle;

                var value = GUILayout.Button(guiContent, buttonStyle, options);

                if (buttonStyle.imagePosition == ImagePosition.TextOnly)
                {
                    var rect = GUILayoutUtility.GetLastRect();
                    const float rectPaddingX = k_PaddingX * 0.75f;
                    rect.x += rectPaddingX;
                    rect.y += k_PaddingY;
                    rect.width = m_ImageSize;
                    rect.height = m_ImageSize;

                    GUI.DrawTexture(rect, guiContent.image, ScaleMode.ScaleToFit, true);
                }

                return value;
            }
        }

        static Styles s_Styles;

        bool m_TwoColumns;

        static Styles styles
        {
            get
            {
                if (s_Styles == null)
                    s_Styles = new Styles();

                return s_Styles;
            }
        }

        public override string PanelLabel => k_ObjectCreationPanelLabel;

        public override bool DrawAsWindow { get; set; }

        public override bool AutoRepaintOnSceneChange => false;

        public override bool UsePrefSize => false;

        public override Vector2 PreferredSize => k_PrefSize;

        public override Vector2 MinSize => k_MinSize;

        public override Vector2 MaxSize => k_MaxSize;

        public override bool PanelPopoutCanScroll => true;

        /// <inheritdoc />
        public override bool PanelExpanded
        {
            get => EditorPrefsUtils.GetBool(k_TypeName, true);
            set => EditorPrefsUtils.SetBool(k_TypeName, value);
        }

        public override void OnGUI()
        {
            var width = EditorGUIUtility.currentViewWidth - styles.paddingX;
            if (ScrollingVertical)
                width -= GUI.skin.verticalScrollbar.fixedWidth;

            var buttonWidth = width * k_InverseNumColumns - styles.createObjectButtonStyle.margin.horizontal
                * k_InverseNumColumns - k_RowPadding;

            if (MARSObjectCreationResources.instance == null)
                return;

            using (new EditorGUILayout.VerticalScope(styles.indentAlignment))
            {
                foreach (var section in MARSObjectCreationResources.instance.ObjectCreationButtonSections)
                    DrawSection(section, buttonWidth);
            }

            base.OnGUI();
        }

        static void DrawSection(MARSObjectCreationResources.ObjectCreationButtonSection section, float buttonWidth)
        {
            section.Expanded = EditorGUILayout.Foldout(section.Expanded, new GUIContent(section.Name), true);
            if (!section.Expanded)
                return;

            var buttons = section.Buttons;
            var needHorizontalEnd = true;
            for (var i = 0; i < buttons.Length; i++)
            {
                if (i % k_NumColumns == 0)
                    GUILayout.BeginHorizontal(GUIStyle.none);

                var createButton = buttons[i];

                using (new EditorGUI.DisabledScope(createButton.Prefab == null))
                {
                    if (!styles.buttonGUIContents.TryGetValue(createButton, out var buttonContents))
                    {
                        buttonContents = createButton.CreationButtonContent();
                        styles.buttonGUIContents.Add(createButton, buttonContents);
                    }

                    if (styles.DrawFixedIconButton(buttonContents, GUILayout.Width(buttonWidth)))
                    {
                        createButton.CreatePrefab(section.Name);
                    }
                }

                if (i % k_NumColumns == 1)
                {
                    GUILayout.EndHorizontal();
                    needHorizontalEnd = false;
                }
                else
                {
                    needHorizontalEnd = true;
                }
            }

            // Leave Space for an empty button and end horizontal row
            if (needHorizontalEnd)
            {
                GUILayout.Space(buttonWidth);
                GUILayout.EndHorizontal();
            }
        }

        protected override MenuItemData[] MenuItems()
        {
            return new[]
            {
                new MenuItemData
                (
                    new GUIContent
                    {
                        text = "Set up object prefabs"
                    }, false,
                    () => { Selection.activeObject = MARSObjectCreationResources.instance; }
                )
            };
        }

        public override void Repaint() { }

        public override void OnSelectionChanged() { }
    }
}
