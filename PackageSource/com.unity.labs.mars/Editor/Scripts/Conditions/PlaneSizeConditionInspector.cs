using Unity.Labs.MARS.Query;
using UnityEditor;
using UnityEngine;
using UnityEditor.PrefabHandles;
using UnityEditor.SceneManagement;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Inspector and scene handles for plane size condition
    /// </summary>
    [ComponentEditor(typeof(PlaneSizeCondition))]
    public class PlaneSizeConditionInspector : SpatialConditionInspector
    {
        PlaneSizeCondition m_PlaneSizeCondition;
        SerializedPropertyData m_MinSizeProperty;
        SerializedPropertyData m_MaxSizeProperty;
        SerializedPropertyData m_MinBoundedProperty;
        SerializedPropertyData m_MaxBoundedProperty;

        PlaneSizeHandle m_Handle;
        bool m_HandleChanged;

        public override void OnEnable()
        {
            base.OnEnable();
            m_PlaneSizeCondition = (PlaneSizeCondition) target;

            m_MinSizeProperty = serializedObject.FindSerializedPropertyData("m_MinimumSize");
            m_MaxSizeProperty = serializedObject.FindSerializedPropertyData("m_MaximumSize");
            m_MinBoundedProperty = serializedObject.FindSerializedPropertyData("m_MinBounded");
            m_MaxBoundedProperty = serializedObject.FindSerializedPropertyData("m_MaxBounded");

            var handleInstance = SceneViewContext.activeViewContext.CreateHandle(MARSRuntimePrefabs.instance.PlaneSizeHandle);
            m_Handle = handleInstance.GetComponent<PlaneSizeHandle>();
            m_Handle.PlaneSizeCondition = m_PlaneSizeCondition;
            m_Handle.HandleChanged += () => m_HandleChanged = true;
            CleanUp();
        }

        public override void OnDisable()
        {
            SceneViewContext.activeViewContext.DestroyHandle(m_Handle.gameObject);
        }

        protected override void OnConditionSceneGUI()
        {
            UpdateHandleMode();
            SetHandleVisibility();

            if (m_HandleChanged)
            {
                m_HandleChanged = false;
                GUI.changed = true;
            }
        }

        void SetHandleVisibility()
        {
            if (m_Handle == null)
                return;

            m_Handle.gameObject.SetActive(
                m_PlaneSizeCondition.isActiveAndEnabled &&
                m_HandleMode != HandleMode.Hidden &&
                StageUtility.IsGameObjectRenderedByCamera(m_PlaneSizeCondition.gameObject, SceneView.lastActiveSceneView.camera));
        }

        protected override void OnConditionInspectorGUI()
        {
            UpdateHandleMode();
            SetHandleVisibility();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var data = new Vector2();
                var hasData = CompareToDataModule.IsComparing && CompareToDataModule.TryGetCurrentDataForTrait(m_PlaneSizeCondition.traitName, out data);
                if (hasData)
                {
                    DrawPropertiesComparedTo(data);
                }
                else
                {
                    EditorGUIUtils.PropertyField(m_MinBoundedProperty, m_MinSizeProperty);
                    EditorGUIUtils.PropertyField(m_MaxBoundedProperty, m_MaxSizeProperty);
                }

                if (check.changed)
                {
                    if (m_HandleMode == HandleMode.Hidden)
                        m_HandleMode = HandleMode.Preview;

                    serializedObject.ApplyModifiedProperties();
                }
            }

            if (m_PlaneSizeCondition.noMinMaxWarning)
                EditorGUIUtils.Warning(k_NoMinOrMaxWarning);
            else if (m_PlaneSizeCondition.smallMinMaxRangeWarning)
                EditorGUIUtils.Warning(k_SmallMinMaxRangeWarning);
        }

        protected void DrawPropertiesComparedTo(Vector2 data)
        {
            var pass = true;
            if (m_PlaneSizeCondition.minBounded)
            {
                var temp = m_PlaneSizeCondition.maxBounded;
                m_PlaneSizeCondition.maxBounded = false;
                if (!m_PlaneSizeCondition.PassesCondition(ref data))
                    pass = false;

                m_PlaneSizeCondition.maxBounded = temp;
            }

            Color? color = null;
            if (!pass)
                color = Color.red;

            EditorGUIUtils.PropertyField(m_MinBoundedProperty, m_MinSizeProperty, overrideColor: color);

            pass = true;
            if (m_PlaneSizeCondition.maxBounded)
            {
                var temp = m_PlaneSizeCondition.minBounded;
                m_PlaneSizeCondition.minBounded = false;
                if (!m_PlaneSizeCondition.PassesCondition(ref data))
                    pass = false;

                m_PlaneSizeCondition.minBounded = temp;
            }

            color = null;
            if (!pass)
                color = Color.red;

            EditorGUIUtils.PropertyField(m_MaxBoundedProperty, m_MaxSizeProperty, overrideColor: color);
        }
    }
}
