using Unity.Labs.Utils.GUI;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public abstract class ConditionBaseInspector : ComponentInspector
    {
        class Styles
        {
            public GUIStyle editButton;

            public Styles()
            {
                editButton = new GUIStyle("Button");
                editButton.padding = new RectOffset(2, 2, 2, 2);
            }
        }

        const int k_WarningIconSize = 40;
        protected const string k_UnboundedLabel = "Unbounded";
        protected const string k_NoMinOrMaxWarning =
            "This condition has neither a minimum nor a maximum, and therefore it has no effect.";
        protected const string k_SmallMinMaxRangeWarning =
            "This condition's min/max range is very small, and therefore it will be hard to satisfy.";

        protected const string k_ButtonLabel = "Edit Condition";
        protected const string k_ButtonTooltip = "Toggle scene handles for this condition.";

        const float k_EditButtonWidth = 33;
        const float k_EditButtonHeight = 23;
        const float k_SpaceBetweenLabelAndButton = 5;

        static Styles s_Styles;
        static Styles styles { get { return s_Styles ?? (s_Styles = new Styles()); } }

        bool m_ConditionChecked;
        bool m_FlagObjSelectedFirstTime;

        public enum HandleMode { Hidden = 0, Preview, Interactive }

        protected HandleMode m_HandleMode;

        protected ConditionBase conditionBase { get; private set; }
        protected Transform targetTransform { get; private set; }
        protected MARSEntity entity { get; private set; }
        protected MARSUserPreferences marsUserPreferences { get; private set; }

        public override void OnEnable()
        {
            conditionBase = (ConditionBase)target;
            targetTransform = conditionBase.transform;
            entity = targetTransform.GetComponent<MARSEntity>();
            marsUserPreferences = MARSUserPreferences.instance;

            base.OnEnable();
        }

        public sealed override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (isDirty)
                CleanUp();

            DrawEditConditionButton();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                // Stops preview after sliding a field value in the inspector
                if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
                    m_HandleMode = HandleMode.Hidden;

                OnConditionInspectorGUI();

                if (check.changed)
                {
                    isDirty = true;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>
        /// Used to wrap the property drawers of the inspector inside a change check for Query updating.
        /// </summary>
        protected abstract void OnConditionInspectorGUI();

        protected void UpdateHandleMode()
        {
            if (conditionBase.adjusting && m_HandleMode != HandleMode.Interactive)
            {
                m_HandleMode = HandleMode.Interactive;
            }
            else if (!conditionBase.adjusting && m_HandleMode == HandleMode.Interactive)
                m_HandleMode = HandleMode.Hidden;
        }

        void DrawEditConditionButton()
        {
            ConditionIconData iconData = null;
            var hasEditIcon = MARSUIResources.instance.ConditionIcons.TryGetValue(target.GetType(), out iconData);
            if (hasEditIcon)
            {
                using(new EditorGUILayout.HorizontalScope())
                {
                    EditorGUIUtils.DrawCheckboxFillerRect();

                    var rect = EditorGUILayout.GetControlRect(true, k_EditButtonHeight, styles.editButton);
                    var buttonRect = new Rect(rect.xMin, rect.yMin, k_EditButtonWidth, k_EditButtonHeight);

                    var labelContent = new GUIContent(k_ButtonLabel, k_ButtonTooltip);
                    var labelSize = GUI.skin.label.CalcSize(labelContent);

                    var labelRect = new Rect(
                        buttonRect.xMax + k_SpaceBetweenLabelAndButton,
                        rect.yMin + (rect.height - labelSize.y) * .5f,
                        labelSize.x,
                        rect.height);

                    var buttonContent = new GUIContent(iconData.Inactive.Icon, k_ButtonTooltip);
                    conditionBase.adjusting = GUI.Toggle(buttonRect, conditionBase.adjusting, buttonContent, styles.editButton);
                    GUI.Label(labelRect, k_ButtonLabel);
                }

                EditorGUILayout.Space();
            }
        }

        void DeselectConditionIfAdjustingWhenOtherObjectSelected()
        {
            if (m_FlagObjSelectedFirstTime)
                return;

            if (conditionBase.adjusting)
                conditionBase.adjusting = false;

            m_FlagObjSelectedFirstTime = true;
        }

        public sealed override void OnSceneGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                DeselectConditionIfAdjustingWhenOtherObjectSelected();

                OnConditionSceneGUI();

                base.OnSceneGUI();

                var currentEvent = Event.current;
                if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && m_HandleMode == HandleMode.Preview
                    || target == null)                  // catch case where component has been destroyed
                {
                    m_HandleMode = HandleMode.Hidden;
                }

                if (check.changed)
                    Repaint();
            }
        }

        protected abstract void OnConditionSceneGUI();

        [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.InSelectionHierarchy)]
        static void DrawGizmos(ConditionBase condition, GizmoType gizmoType)
        {
            if (!condition.drawWarning)
                return;

            if (SceneView.currentDrawingSceneView is SimulationView)
                return;

            var transform = condition.transform;
            var screenPoint = Camera.current.WorldToScreenPoint(transform.position) / EditorGUIUtility.pixelsPerPoint;
            if (screenPoint.z >= Camera.current.nearClipPlane && screenPoint.z <= Camera.current.farClipPlane)
            {
                Handles.BeginGUI();
                screenPoint.x -= k_WarningIconSize;
                var pointHeight = ScreenGUIUtils.pointHeight;
                screenPoint.y = pointHeight - screenPoint.y - k_WarningIconSize * 2f;
                GUI.DrawTexture(new Rect(screenPoint, Vector2.one * k_WarningIconSize), MARSUIResources.instance.WarningTexture);
                Handles.EndGUI();
            }
        }
    }
}
