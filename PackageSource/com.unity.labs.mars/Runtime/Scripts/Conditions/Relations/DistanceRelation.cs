using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [MonoBehaviourComponentMenu(typeof(DistanceRelation), "Relation/Distance")]
    public class DistanceRelation : BoundedFloatRelation<Pose>, IUsesCameraOffset
    {
        const float k_DefaultMin = 0.25f;
        const float k_DefaultMax = 1.25f;

        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Pose, TraitDefinitions.Pose };

        // these values only change when scale or bounds change, so we cache them for re-use
        float m_RangeCenterPoint;
        float m_HalfRange;

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
                if (value != m_Adjusting)
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
            m_Minimum = k_DefaultMin;
            m_Maximum = k_DefaultMax;
        }
#endif

        public void OnEnable()
        {
            CacheRange();
        }

        void OnDisable()
        {
            adjusting = false;
        }

        void OnAdjustingChanged()
        {
            var handleModule = ModuleLoaderCore.instance.GetModule<RuntimeHandleContextModule>();
            if (!Application.isPlaying || handleModule == null)
                return;

            if (m_Adjusting && m_Handle == null)
            {
                m_Handle = handleModule.CreateHandle(MARSRuntimePrefabs.instance.DistanceHandle);
                var distanceHandle = m_Handle.GetComponent<DistanceHandle>();
                distanceHandle.DistanceRelation = this;
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

        void CacheRange()
        {
            var range = m_Maximum - m_Minimum;
            m_HalfRange = range * 0.5f;
            m_RangeCenterPoint = m_Minimum + range * m_RatingConfig.center;
        }

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

        public override float RateDataMatch(ref Pose child1Data, ref Pose child2Data)
        {
            var deadZone = m_RatingConfig.deadZone;
            var center = m_RatingConfig.center;
            var position1 = child1Data.position;
            var position2 = child2Data.position;

            // these next 4 lines do the same thing as Vector3.Distance(), but faster
            var distX = position1.x - position2.x;
            var distY = position1.y - position2.y;
            var distZ = position1.z - position2.z;
            var distance = Mathf.Sqrt(distX * distX + distY * distY + distZ * distZ);

            var min = m_Minimum;
            var max = m_Maximum;
            if (m_MinBounded)
            {
                if (m_MaxBounded)
                {
                    if (distance < min || distance > max)
                        return 0f;

                    var signedMiddleDiff = distance - m_RangeCenterPoint;
                    var middleDiff = signedMiddleDiff > 0 ? signedMiddleDiff : -signedMiddleDiff;
                    var halfPortion = middleDiff / m_HalfRange;

                    // if our diff is within the dead zone and doesn't fail, we can early out with a perfect match
                    if (halfPortion < deadZone)
                        return 1f;

                    float interpolationFactor = 1f;
                    if (center != 0.5f)
                    {
                        var pointAboveCenter = signedMiddleDiff > 0f;
                        if(!pointAboveCenter)
                            interpolationFactor = center * 2f;
                        else
                            interpolationFactor = (1f - center) * 2f;
                    }

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
                return distance > min ? 1f : 0f;
            }
            if (m_MaxBounded)
            {
                // if we're only max bounded, return a simple pass / fail
                return distance < max ? 1f : 0f;
            }

            // if we're not bounded on either side somehow, data always matches
            return 1f;
        }
    }
}
