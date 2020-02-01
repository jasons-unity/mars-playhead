using UnityEditor;
using UnityEditor.PrefabHandles;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ComponentEditor(typeof(ElevationRelation))]
    public class ElevationRelationInspector : RelationInspector
    {
        static readonly GUIContent k_Child1Content = new GUIContent("Upper", "Specifies the upper object in this relation");
        static readonly GUIContent k_Child2Content = new GUIContent("Lower", "Specifies the lower object in this relation");

        const string k_MinElevationLabel = "Minimum Elevation";
        const string k_MaxElevationLabel = "Maximum Elevation";

        ElevationRelation m_ElevationRelation;
        SerializedPropertyData m_MinElevationProperty;
        SerializedPropertyData m_MaxElevationProperty;
        SerializedPropertyData m_MinBoundedProperty;
        SerializedPropertyData m_MaxBoundedProperty;
        Vector3 m_ElevationHandleStartPos;

        ElevationHandle m_Handle;
        bool m_HandleChanged;

        protected override GUIContent child1Content { get { return k_Child1Content; } }
        protected override GUIContent child2Content { get { return k_Child2Content; } }
        public override void OnEnable()
        {
            base.OnEnable();

            m_ElevationRelation = (ElevationRelation)target;
            m_MinElevationProperty = serializedObject.FindSerializedPropertyData("m_Minimum");
            m_MaxElevationProperty = serializedObject.FindSerializedPropertyData("m_Maximum");
            m_MinBoundedProperty = serializedObject.FindSerializedPropertyData("m_MinBounded");
            m_MaxBoundedProperty = serializedObject.FindSerializedPropertyData("m_MaxBounded");

            var handleInstance = SceneViewContext.activeViewContext.CreateHandle(MARSRuntimePrefabs.instance.ElevationHandle);
            m_Handle = handleInstance.GetComponent<ElevationHandle>();
            m_Handle.ElevationRelation = m_ElevationRelation;
            m_Handle.HandleChanged += () => m_HandleChanged = true;
            CleanUp();
        }

        public override void OnDisable()
        {
            SceneViewContext.activeViewContext.DestroyHandle(m_Handle.gameObject);
        }

        protected override void OnConditionInspectorGUI()
        {
            base.OnConditionInspectorGUI();

            UpdateHandleMode();
            SetHandleVisibility();

            serializedObject.Update();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUIUtils.PropertyField(m_MinBoundedProperty, m_MinElevationProperty, k_MinElevationLabel);
                EditorGUIUtils.PropertyField(m_MaxBoundedProperty, m_MaxElevationProperty, k_MaxElevationLabel);

                if (check.changed)
                {
                    if (m_HandleMode == HandleMode.Hidden)
                        m_HandleMode = HandleMode.Preview;

                    serializedObject.ApplyModifiedProperties();
                }
            }

            if (m_ElevationRelation.noMinMaxWarning)
                EditorGUIUtils.Warning(k_NoMinOrMaxWarning);
            if (m_ElevationRelation.smallMinMaxRangeWarning)
                EditorGUIUtils.Warning(k_SmallMinMaxRangeWarning);
        }

        protected override void OnConditionSceneGUI()
        {
            base.OnConditionSceneGUI();

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
                m_ElevationRelation.isActiveAndEnabled &&
                m_HandleMode != HandleMode.Hidden &&
                StageUtility.IsGameObjectRenderedByCamera(m_ElevationRelation.gameObject, SceneView.lastActiveSceneView.camera));
        }
    }
}
