using Unity.Labs.Utils;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Inspector and scene handles for angle and axis condition
    /// </summary>
    [ComponentEditor(typeof(AngleAxisCondition))]
    public class AngleAxisConditionInspector : DependencyConditionInspector
    {
        // Inspector uses Pitch Yaw Roll enum for convenience even though axis in condition is a Vector3 type.
        enum RotationAxes
        {
            Pitch,
            Yaw,
            Roll
        }

        const string k_UndoString = "Change Orientation Range";
        const string k_NoDependencyAssignedWarning =
            "The condition has no dependency assigned. The rotation will be compared to the world space axes, " +
            "which may change based on initial device position.";

        const float k_ConstantScreenSizeHandleScale = 2.5f;
        const float k_MinHandleSize = 0.04f;
        const float k_AngleLineLength = 1.0f;
        const float k_ArcHandleRadius = 0.8f;
        const float k_DegreeLabelRadius = 1.15f;

        const float k_DrawNonProjectedAngleThreshold = 5f;
        const float k_DashedForwardLineSize = 4f;
        const float k_DashedProjectionLineSize = 2f;
        const float k_NoninteractiveAlphaMultiplier = 0.5f;
        const float k_PreviewModeAlphaMultiplier = 0.5f;
        static readonly Color k_RangeOutlineColor = new Color(0.6f, 0.9f, 0.6f, 1f);
        static readonly Color k_InsideRangeArcColor = new Color(0.2f, 1.0f, 0.3f, 0.4f);
        static readonly Color k_OutsideRangeArcColor = new Color(0.2f, 0.7f, 0.3f, 0.12f);
        static readonly Color k_LocalForwardColor = new Color(0.1f, 0.5f, 1f, 1f);
        static readonly Color k_DependentForwardColor = new Color(0.1f, 0.3f, 1f, 1f);

        readonly ArcHandle m_ArcMaxHandle = new ArcHandle();
        readonly ArcHandle m_ArcMinHandle = new ArcHandle();
        AngleAxisCondition m_Condition;
        Quaternion? m_PreviousRotation;
        bool m_RangeWasModified;
        Matrix4x4 m_OriginalHandlesMatrix;

        SerializedPropertyData m_DependencyProperty;
        SerializedPropertyData m_RelativeToDependencyProperty;
        SerializedPropertyData m_MinAngleProperty;
        SerializedPropertyData m_MaxAngleProperty;
        SerializedPropertyData m_MinBoundedProperty;
        SerializedPropertyData m_MaxBoundedProperty;
        SerializedPropertyData m_AxisProperty;
        SerializedPropertyData m_OffsetProperty;
        SerializedPropertyData m_MirrorProperty;

        MaxAttribute m_MaxAttribute;
        MinAttribute m_MinAttribute;

        // This property converts between the enum and the condition's axis Vector3 value
        RotationAxes axisPropertyAsEnum
        {
            get
            {
                if (m_AxisProperty.Value.vector3Value == Vector3.up)
                    return RotationAxes.Yaw;

                if (m_AxisProperty.Value.vector3Value == Vector3.right)
                    return RotationAxes.Pitch;

                return RotationAxes.Roll;
            }
            set
            {
                switch (value)
                {
                    case RotationAxes.Pitch:
                        m_AxisProperty.Value.vector3Value = Vector3.right;
                        break;
                    case RotationAxes.Yaw:
                        m_AxisProperty.Value.vector3Value = Vector3.up;
                        break;
                    case RotationAxes.Roll:
                        m_AxisProperty.Value.vector3Value = Vector3.forward;
                        break;
                }
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            m_Condition = (AngleAxisCondition) target;
            m_PreviousRotation = targetTransform.rotation;

            m_DependencyProperty = serializedObject.FindSerializedPropertyData("m_Dependency");
            m_RelativeToDependencyProperty = serializedObject.FindSerializedPropertyData("m_RelativeToDependency");
            m_MinAngleProperty = serializedObject.FindSerializedPropertyData("m_MinimumAngle");
            m_MaxAngleProperty = serializedObject.FindSerializedPropertyData("m_MaximumAngle");
            m_MinBoundedProperty = serializedObject.FindSerializedPropertyData("m_MinBounded");
            m_MaxBoundedProperty = serializedObject.FindSerializedPropertyData("m_MaxBounded");
            m_MirrorProperty = serializedObject.FindSerializedPropertyData("m_Mirror");
            m_AxisProperty = serializedObject.FindSerializedPropertyData("m_Axis");
            m_OffsetProperty = serializedObject.FindSerializedPropertyData("m_OffsetAngle");

            m_MaxAttribute = new MaxAttribute(m_MaxAngleProperty.Value.floatValue);
            m_MinAttribute = new MinAttribute(m_MinAngleProperty.Value.floatValue);
            m_MinAngleProperty.AddAttribute(m_MaxAttribute);
            m_MaxAngleProperty.AddAttribute(m_MinAttribute);
        }

        protected override void OnConditionInspectorGUI()
        {
            using (var check = new EditorGUI.ChangeCheckScope())
            {
                serializedObject.Update();

                EditorGUIUtils.PropertyField(m_DependencyProperty);

                using (new EditorGUI.DisabledScope(m_DependencyProperty.Value.objectReferenceValue == null))
                {
                    EditorGUIUtils.PropertyField(m_RelativeToDependencyProperty);
                }

                //TODO better enum popup
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUIUtils.DrawCheckboxFillerRect();

                    axisPropertyAsEnum = (RotationAxes)EditorGUILayout.EnumPopup("Axis", axisPropertyAsEnum);
                }

                EditorGUIUtils.PropertyField(m_MirrorProperty);

                using (new EditorGUI.DisabledScope(!m_MirrorProperty.Value.boolValue))
                {
                    EditorGUIUtils.PropertyField(m_MinBoundedProperty, m_MinAngleProperty, true);
                    EditorGUIUtils.PropertyField(m_MaxBoundedProperty, m_MaxAngleProperty, true);
                }

                EditorGUIUtils.PropertyField(m_OffsetProperty);

                if (check.changed)
                {
                    if (m_HandleMode == HandleMode.Hidden)
                        m_HandleMode = HandleMode.Preview;

                    serializedObject.ApplyModifiedProperties();
                }
            }

            if (m_Condition.noMinMaxWarning)
                EditorGUIUtils.Warning(k_NoMinOrMaxWarning);
            else if (m_Condition.smallMinMaxRangeWarning)
                EditorGUIUtils.Warning(k_SmallMinMaxRangeWarning);

            if (m_Condition.noDependencyAssignedWarning)
                EditorGUIUtils.Warning(k_NoDependencyAssignedWarning);
        }

        protected override void OnConditionSceneGUI()
        {
            // Need to cache the handles matrix to restore it after drawing plane size handles
            m_OriginalHandlesMatrix = Handles.matrix;
            UpdateHandleMode();

            if (m_HandleMode != HandleMode.Hidden)
                DrawNonInteractiveHandles();

            if (m_HandleMode == HandleMode.Interactive)
                DrawInteractiveHandles();

            if (Event.current.button == 0 && Event.current.type == EventType.MouseUp)
            {
                m_RangeWasModified = false;
            }

            if (m_PreviousRotation.HasValue && m_PreviousRotation != targetTransform.rotation)
            {
                if (m_HandleMode != HandleMode.Interactive)
                    m_HandleMode = HandleMode.Preview;
            }
            m_PreviousRotation = targetTransform.rotation;
            Handles.matrix = m_OriginalHandlesMatrix;
        }

        void SetHandleMatrix(Vector3 position)
        {
            Handles.matrix = Matrix4x4.TRS(
                                 position,
                                 m_Condition.ConditionRotation(targetTransform.rotation),
                                 Vector3.one) *
                             Matrix4x4.Rotate(
                                 Quaternion.FromToRotation(Vector3.up, m_Condition.axis)) *
                             Matrix4x4.Rotate(
                                 Quaternion.AngleAxis(m_Condition.offsetAngle, Vector3.up));

            var objectTransform = (m_Condition.relativeToDependency && m_Condition.dependency != null)
                ? m_Condition.dependency.transform : targetTransform;
            var bounds = BoundsUtils.GetBounds(objectTransform);
            var boundsExtent = bounds.extents != Vector3.zero
                ? Vector3.ProjectOnPlane(bounds.extents, m_Condition.axis).magnitude
                : Mathf.Max(HandleUtility.GetHandleSize(Vector3.zero) * k_ConstantScreenSizeHandleScale, k_MinHandleSize);

            Handles.matrix *= Matrix4x4.Scale(boundsExtent * Vector3.one);
        }

        protected void DrawNonInteractiveHandles()
        {
            var transparency = (m_HandleMode == HandleMode.Preview) ? k_PreviewModeAlphaMultiplier : 1.0f;
            var handlePosition = targetTransform.position;
            if (m_Condition.dependency != null && m_Condition.relativeToDependency)
                handlePosition = m_Condition.dependency.transform.position;

            SetHandleMatrix(handlePosition);
            DrawRangeArcs(transparency);
            DrawAngleLines();

            // Draw ranges and lines on the other object
            handlePosition = targetTransform.position;
            if (m_Condition.dependency != null && !m_Condition.relativeToDependency)
                handlePosition = m_Condition.dependency.transform.position;

            SetHandleMatrix(handlePosition);
            DrawRangeArcs(k_NoninteractiveAlphaMultiplier * k_PreviewModeAlphaMultiplier);
            DrawAngleLines();
        }

        protected void DrawInteractiveHandles()
        {
            DrawHandles();

            if (m_Condition.mirror)
                DrawHandles(true);

            m_Condition.OnValidate();
        }

        void DrawHandles(bool mirrored = false)
        {
            var mirror = mirrored ? -1 : 1;
            var handlePosition = targetTransform.position;
            if (m_Condition.dependency != null && m_Condition.relativeToDependency)
                handlePosition = m_Condition.dependency.transform.position;

            SetHandleMatrix(handlePosition);

            m_ArcMinHandle.radius = k_ArcHandleRadius;
            m_ArcMaxHandle.radius = k_ArcHandleRadius;
            if (m_Condition.minBounded)
            {
                Handles.color = k_RangeOutlineColor;
                m_ArcMinHandle.wireframeColor = Color.clear;
                m_ArcMinHandle.fillColor = Color.clear;
                m_ArcMinHandle.angle = mirror * m_Condition.minimumAngle;
                EditorGUI.BeginChangeCheck();
                m_ArcMinHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, k_UndoString);
                    m_Condition.minimumAngle = mirror * m_ArcMinHandle.angle;
                    m_Condition.maximumAngle = Mathf.Max( m_Condition.maximumAngle, m_Condition.minimumAngle);
                    m_RangeWasModified = true;
                }
            }

            if (m_Condition.maxBounded)
            {
                Handles.color = k_RangeOutlineColor;
                m_ArcMaxHandle.fillColor = Color.clear;
                var startOffset = m_Condition.minBounded ? m_Condition.minimumAngle : 0f;
                var prevMatrix = Handles.matrix;
                Handles.matrix = prevMatrix * Matrix4x4.Rotate(Quaternion.AngleAxis(mirror * startOffset, Vector3.up));
                m_ArcMaxHandle.angle = mirror * (m_Condition.maximumAngle - startOffset);
                EditorGUI.BeginChangeCheck();
                m_ArcMaxHandle.DrawHandle();
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, k_UndoString);
                    m_Condition.maximumAngle = mirror * m_ArcMaxHandle.angle + startOffset;
                    m_Condition.minimumAngle  = Mathf.Min( m_Condition.minimumAngle, m_Condition.maximumAngle);
                    m_RangeWasModified = true;
                }

                Handles.matrix = prevMatrix;
            }

            if (m_RangeWasModified)
            {
                DrawRangeLabels();
            }
        }

        void DrawRangeArcs(float alphaMultiplier = 1.0f)
        {
            var min = m_Condition.minBounded ? m_Condition.minimumAngle : 0f;
            var max = m_Condition.maxBounded ? m_Condition.maximumAngle : 180f;
            var arcColor = m_Condition.InRange(targetTransform.rotation) ? k_InsideRangeArcColor : k_OutsideRangeArcColor;
            arcColor.a *= alphaMultiplier;
            Handles.color = arcColor;
            Handles.DrawSolidArc(Vector3.zero, Vector3.up,
                Quaternion.AngleAxis(min, Vector3.up) * Vector3.forward,
                max - min, k_ArcHandleRadius);

            if (m_Condition.mirror)
            {
                Handles.DrawSolidArc(Vector3.zero, Vector3.up,
                    Quaternion.AngleAxis(-min, Vector3.up) * Vector3.forward,
                    min - max, k_ArcHandleRadius);
            }
        }

        void DrawRangeLabels()
        {
            HandleUtils.Label(
                Vector3.forward * k_DegreeLabelRadius,
                EditorGUIUtils.FormatAngleDegreesString(0f));

            if (m_Condition.minBounded)
            {
                var minAngleLineEnd = Quaternion.AngleAxis(m_Condition.minimumAngle, Vector3.up) * Vector3.forward;
                Handles.color = k_RangeOutlineColor;
                Handles.DrawLine(Vector3.zero, minAngleLineEnd * k_AngleLineLength);
                HandleUtils.Label(
                    minAngleLineEnd * k_DegreeLabelRadius,
                    EditorGUIUtils.FormatAngleDegreesString(m_Condition.minimumAngle));

                if (m_Condition.mirror)
                {
                    minAngleLineEnd = Quaternion.AngleAxis(-m_Condition.minimumAngle, Vector3.up) * Vector3.forward;
                    Handles.DrawLine(Vector3.zero, minAngleLineEnd * k_AngleLineLength);
                    HandleUtils.Label(
                        minAngleLineEnd * k_DegreeLabelRadius,
                        EditorGUIUtils.FormatAngleDegreesString(m_Condition.minimumAngle));
                }
            }

            if (m_Condition.maxBounded)
            {
                var maxAngleLineEnd = Quaternion.AngleAxis(m_Condition.maximumAngle, Vector3.up) * Vector3.forward;
                Handles.color = k_RangeOutlineColor;
                Handles.DrawLine(Vector3.zero, maxAngleLineEnd * k_AngleLineLength);
                HandleUtils.Label(
                    maxAngleLineEnd * k_DegreeLabelRadius,
                    EditorGUIUtils.FormatAngleDegreesString(m_Condition.maximumAngle));

                if (m_Condition.mirror)
                {
                    maxAngleLineEnd = Quaternion.AngleAxis(-m_Condition.maximumAngle, Vector3.up) * Vector3.forward;
                    Handles.DrawLine(Vector3.zero, maxAngleLineEnd * k_AngleLineLength);
                    HandleUtils.Label(
                        maxAngleLineEnd * k_DegreeLabelRadius,
                        EditorGUIUtils.FormatAngleDegreesString(m_Condition.maximumAngle));
                }
            }
        }

        void DrawAngleLines()
        {
            Handles.color = Handles.yAxisColor;
            Handles.DrawLine(-0.25f * Vector3.up, 0.25f * Vector3.up);

            Handles.color = m_Condition.relativeToDependency ? k_DependentForwardColor : k_LocalForwardColor;
            HandleUtils.DrawDottedLine(Vector3.zero, Vector3.forward * k_AngleLineLength, k_DashedForwardLineSize);

            var forwardHandleSpace = Handles.inverseMatrix.MultiplyVector(m_Condition.VectorToCompare(targetTransform.rotation)).normalized;
            var projectedHandleSpace = Handles.inverseMatrix.MultiplyVector(m_Condition.ProjectedVectorToCompare(targetTransform.rotation)).normalized;
            Handles.color = m_Condition.relativeToDependency ? k_LocalForwardColor : k_DependentForwardColor;
            HandleUtils.DrawLine(Vector3.zero, projectedHandleSpace * k_AngleLineLength);

            if (Vector3.Angle(forwardHandleSpace, projectedHandleSpace) > k_DrawNonProjectedAngleThreshold)
            {
                HandleUtils.DrawDottedLine(Vector3.zero, forwardHandleSpace * k_AngleLineLength, k_DashedProjectionLineSize);
                HandleUtils.DrawDottedLine(forwardHandleSpace * k_AngleLineLength, projectedHandleSpace * k_AngleLineLength, k_DashedProjectionLineSize);
            }
        }
    }
}
