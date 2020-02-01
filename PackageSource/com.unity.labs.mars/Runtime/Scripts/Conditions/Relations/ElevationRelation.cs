using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [MonoBehaviourComponentMenu(typeof(ElevationRelation), "Relation/Elevation")]
    public class ElevationRelation : BoundedFloatRelation<Pose>, IUsesCameraOffset
    {
        const float k_DefaultMinElevation = 0.25f;
        const float k_DefaultMaxElevation = 1.25f;

        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Pose, TraitDefinitions.Pose };

        float m_HalfRange;
        float m_RangeCenterPoint;

        GameObject m_Handle;
        bool m_Adjusting;

#if !FI_AUTOFILL
        public IProvidesCameraOffset provider { get; set; }
#endif
        public override bool adjusting
        {
            get { return m_Adjusting; }
            set
            {
                if (value != adjusting)
                {
                    m_Adjusting = value;
                    OnAdjustingChanged();
                }
            }
        }

#if UNITY_EDITOR
        public override void Reset()
        {
            base.Reset();
            m_Minimum = k_DefaultMinElevation;
            m_Maximum = k_DefaultMaxElevation;
        }
#endif

        void OnAdjustingChanged()
        {
            var handleModule = ModuleLoaderCore.instance.GetModule<RuntimeHandleContextModule>();
            if (!Application.isPlaying || handleModule == null)
                return;

            if (m_Adjusting && m_Handle == null)
            {
                m_Handle = handleModule.CreateHandle(MARSRuntimePrefabs.instance.ElevationHandle);
                var elevationHandle = m_Handle.GetComponent<ElevationHandle>();
                elevationHandle.ElevationRelation = this;
            }
            else if (!m_Adjusting && m_Handle != null)
            {
                handleModule.DestroyHandle(m_Handle);
            }
        }

        public override void OnRatingConfigChange()
        {
            CacheRange();
        }

        public void OnEnable()
        {
            CacheRange();
        }

        void OnDisable()
        {
            adjusting = false;
        }

        // We re-use 2 values in our rating calculation that only change when scale or bounds change:
        // half-range & center point
        // instead of calculating each time we rate a match, cache these values when they change.
        void CacheRange()
        {
            var range = m_Maximum - m_Minimum;
            m_HalfRange = range * 0.5f;
            m_RangeCenterPoint = m_Minimum + range * m_RatingConfig.center;
        }

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

        public override float RateDataMatch(ref Pose child1Data, ref Pose child2Data)
        {
            var config = m_RatingConfig;
            var deadZone = config.deadZone;
            var center = config.center;
            var child1Elevation = child1Data.position.y;
            var child2Elevation = child2Data.position.y;
            var elevationDiff = child1Elevation - child2Elevation;
            if (m_MinBounded)
            {
                if (m_MaxBounded)
                {
                    // out of bounds - fail this match
                    if (elevationDiff < m_Minimum || elevationDiff > m_Maximum)
                        return 0f;

                    var signedMiddleDiff = elevationDiff - m_RangeCenterPoint;
                    var middleDiffIsPositive = signedMiddleDiff > 0f;
                    var middleDiff = middleDiffIsPositive ? signedMiddleDiff : -signedMiddleDiff;
                    var halfPortion = middleDiff / m_HalfRange;

                    // if our diff is within the dead zone and doesn't fail, we can early out with a perfect match
                    if (halfPortion < deadZone)
                        return 1f;

                    float interpolationFactor = 1f;
                    if (center != 0.5f)
                        interpolationFactor = middleDiffIsPositive ? (1f - center) * 2f : center * 2f;

                    var adjustedRange = m_HalfRange * interpolationFactor;
                    var adjustedDeadZone = deadZone / interpolationFactor;
                    var portion = middleDiff / adjustedRange;
                    var t = (portion - adjustedDeadZone) / (1f - adjustedDeadZone);
                    if (t > 1f || t < 0f)
                        return 0f;

                    // inlined Mathf.SmoothStep(1f, min, t)
                    t = -2f * t * t * t + 3f * t * t;
                    var rating = MARSDatabase.MinimumPassingConditionRating * t + 1f * (1f - t);
                    if (rating > 1f)
                        rating = 1f;

                    return rating;
                }

                // if we're only min bounded, return a simple pass / fail
                return elevationDiff > m_Minimum ? 1f : 0f;
            }
            if (m_MaxBounded)
            {
                // if we're only max bounded, return a simple pass / fail
                return elevationDiff < m_Maximum ? 1f : 0f;
            }

            return 1f;
        }
    }
}
