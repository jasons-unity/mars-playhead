using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Compares the angle between this object and another object in a particular rotation axis (yaw, pitch, or roll),
    /// and checks if the rotation is within a range of valid angles
    /// </summary>
    public class AngleAxisCondition : DependencyCondition, ICondition<Pose>
    {
        const float k_MinRangeWarning = 3f;

        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Pose };

        [SerializeField]
        [Tooltip("If enabled, the rotation axis will be in the dependent object's local space instead of this object.")]
        bool m_RelativeToDependency;

        [SerializeField]
        [Tooltip("Specifies the axis around which rotations will be compared.")]
        Vector3 m_Axis = Vector3.up;

        [SerializeField]
        [Tooltip("If enabled, the range is mirrored in the negative rotation direction.")]
        bool m_Mirror = true;

        [SerializeField]
        [Tooltip("Sets the minimum angle difference in degrees between the rotation and the dependency rotation.")]
        float m_MinimumAngle;

        [SerializeField]
        [Tooltip("Sets the maximum angle difference in degrees between the rotation and the dependency rotation.")]
        float m_MaximumAngle = 45.0f;

        [SerializeField]
        bool m_MinBounded;

        [SerializeField]
        bool m_MaxBounded = true;

        [SerializeField]
        [Range(-180f, 180f)]
        [Tooltip("Controls how much the forward direction is offset in degrees.")]
        float m_OffsetAngle;

        /// <summary>
        /// Whether the rotation axis to use for comparison is based on the plane's local space. Otherwise uses dependency's rotation space
        /// </summary>
        public bool relativeToDependency
        {
            get { return m_RelativeToDependency; }
            set { m_RelativeToDependency = value; }
        }

        /// <summary>
        /// The axis around which the rotations will be compared
        /// </summary>
        public Vector3 axis
        {
            get { return m_Axis; }
            set { m_Axis = value; }
        }

        /// <summary>
        /// Whether to treat clockwise and counter-clockwise rotations as the same thing
        /// </summary>
        public bool mirror
        {
            get { return m_Mirror; }
            set { m_Mirror = value; }
        }

        /// <summary>
        /// The minimum angle difference in degrees between the rotation and the dependency rotation.
        /// </summary>
        public float minimumAngle
        {
            get { return m_MinimumAngle; }
            set { m_MinimumAngle = value; }
        }

        /// <summary>
        /// The maximum angle difference in degrees between the rotation and the dependency rotation.
        /// </summary>
        public float maximumAngle
        {
            get { return m_MaximumAngle; }
            set { m_MaximumAngle = value; }
        }

        /// <summary>
        /// Whether the angle must be at least a certain value
        /// </summary>
        public bool minBounded
        {
            get { return m_MinBounded; }
            set { m_MinBounded = value; }
        }

        /// <summary>
        /// Whether the angle must be at most a certain value
        /// </summary>
        public bool maxBounded
        {
            get { return m_MaxBounded; }
            set { m_MaxBounded = value; }
        }

        /// <summary>
        /// The angle offset from the forward direction that corresponds to 0 degrees
        /// </summary>
        public float offsetAngle
        {
            get { return m_OffsetAngle; }
            set { m_OffsetAngle = value; }
        }

        public bool noMinMaxWarning { get; private set; }
        public bool smallMinMaxRangeWarning { get; private set; }
        public bool noDependencyAssignedWarning { get; private set; }

        /// <summary>
        /// The direction to consider forward for this condition based on the rotation axis, BEFORE the offset angle is applied
        /// </summary>
        public Vector3 forward
        {
            get
            {
                return Quaternion.FromToRotation(Vector3.up, m_Axis) * Vector3.forward;
            }
        }

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

        public float RateDataMatch(ref Pose data)
        {
            if (!dependencySatisfied)
                return 0f;

            // stub implementation in anticipation of removing this condition
            return InRange(data.rotation) ? 1f : 0f;
        }

        /// <summary>
        /// The world rotation to apply to this condition, depends on whether the condition is set to local space or relative to dependent.
        /// </summary>
        public Quaternion ConditionRotation(Quaternion compareRotation)
        {
            if (dependency == null)
                return Quaternion.identity;

            if (m_RelativeToDependency)
                return dependency.transform.rotation;

            return compareRotation;
        }

        /// <summary>
        /// The direction vector in world space (not projected) that is being compared to the min and max range
        /// </summary>
        public Vector3 VectorToCompare(Quaternion compareRotation)
        {
            // If in local space, the vector is determined by the dependency, otherwise use this object's forward
            if (!m_RelativeToDependency && dependency != null)
            {
                return dependency.transform.TransformVector(forward);
            }

            return compareRotation * forward;

        }

        /// <summary>
        /// The direction vector that is being compared projected and normalized onto the plane of rotation
        /// </summary>
        public Vector3 ProjectedVectorToCompare(Quaternion compareRotation)
        {
            return Vector3.ProjectOnPlane(VectorToCompare(compareRotation),
                ConditionRotation(compareRotation) * m_Axis).normalized;
        }

        /// <summary>
        /// The actual angle difference between a rotation and dependent object
        /// </summary>
        public float AngleDifference(Quaternion compareRotation)
        {
            var signedDif = Vector3.SignedAngle(
                ConditionRotation(compareRotation) * Quaternion.AngleAxis(m_OffsetAngle, m_Axis) * forward,
                ProjectedVectorToCompare(compareRotation),
                ConditionRotation(compareRotation) * m_Axis);

            if (m_Mirror)
            {
                signedDif = Mathf.Abs(signedDif);
            }
            else
            {
                if (signedDif - 360 > m_MinimumAngle)
                    signedDif -= 360f;

                if (signedDif + 360 < m_MaximumAngle)
                    signedDif += 360f;
            }

            return signedDif;
        }

        /// <summary>
        /// Whether a rotation is within the min and max range set in this condition
        /// </summary>
        public bool InRange(Quaternion compareRotation)
        {
            if ((!m_MinBounded || AngleDifference(compareRotation) >= m_MinimumAngle) &&
                (!m_MaxBounded || AngleDifference(compareRotation) <= m_MaximumAngle))
                return true;

            return false;
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();

             // If not mirrored, there must be both a max and min angle to split the possible angles into a valid and invalid regions
            if (!m_Mirror)
            {
                m_MinBounded = true;
                m_MaxBounded = true;

                // Wrap values back around if greater than 360 or less than -360
                if (m_MinimumAngle > 360f || m_MaximumAngle > 360f)
                {
                    m_MinimumAngle -= 360f;
                    m_MaximumAngle -= 360f;
                }

                if (m_MinimumAngle < -360f || m_MaximumAngle < -360f)
                {
                    m_MinimumAngle += 360f;
                    m_MaximumAngle += 360f;
                }
            }

            m_MinimumAngle = Mathf.Clamp(m_MinimumAngle,
                m_Mirror ? 0f : Mathf.NegativeInfinity,
                m_Mirror ? 180f : m_MaximumAngle);

            m_MaximumAngle = Mathf.Clamp(m_MaximumAngle,
                minBounded ? m_MinimumAngle : 0f,
                m_Mirror ? 180f : m_MinimumAngle + 360f);

            noDependencyAssignedWarning = dependency == null;
            noMinMaxWarning = !m_MaxBounded && !m_MinBounded;

            if (m_MaxBounded || m_MinBounded)
            {
                var range = 0f;
                if (maxBounded && minBounded)
                    range = m_MaximumAngle - m_MinimumAngle;
                else if (maxBounded)
                    range = 2f * m_MaximumAngle;
                else if (minBounded)
                    range = 360f - 2f * m_MinimumAngle;

                smallMinMaxRangeWarning = range < k_MinRangeWarning;
            }

            drawWarning = noMinMaxWarning || smallMinMaxRangeWarning || noDependencyAssignedWarning;
        }
#endif
    }
}
