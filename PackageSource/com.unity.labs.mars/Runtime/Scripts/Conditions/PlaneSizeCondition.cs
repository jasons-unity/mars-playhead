using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a situation where a given plane must have a size within a certain range
    /// </summary>
    [DisallowMultipleComponent]
    [ComponentTooltip("Requires the object to be a plane within the specified size range.")]
    [MonoBehaviourComponentMenu(typeof(PlaneSizeCondition), "Condition/Plane Size")]
    public class PlaneSizeCondition : BoundedRangeCondition<Vector2>, ISpatialCondition
    {
        const float k_MinPaddingWarning = .05f;

#if UNITY_EDITOR
        // a millimeter on either side is much smaller than any platform will detect a plane for
        static readonly Vector2 k_LowestMinimumBound = new Vector2(0.001f, 0.001f);
#endif

        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Bounds2D };

        [OptionalConstraint(nameof(m_MinBounded))]
        [SerializeField]
        [Tooltip("Sets the minimum size of the plane's extents")]
        Vector2 m_MinimumSize = Vector2.one * 0.5f;

        [OptionalConstraint(nameof(m_MaxBounded))]
        [SerializeField]
        [Tooltip("Sets the maximum size of the plane's extents")]
        Vector2 m_MaximumSize = Vector2.one * 1.5f;

        GameObject m_Handle;
        bool m_Adjusting;

        public bool noMinMaxWarning { get; private set; }
        public bool smallMinMaxRangeWarning { get; private set; }

        /// <summary>
        /// Minimum size of the plane's extents
        /// </summary>
        public Vector2 minimumSize
        {
            get { return m_MinimumSize; }
            set { m_MinimumSize = value; }
        }

        /// <summary>
        /// Maximum size of the plane's extents
        /// </summary>
        public Vector2 maximumSize
        {
            get { return m_MaximumSize; }
            set { m_MaximumSize = value; }
        }

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
                m_Handle = handleModule.CreateHandle(MARSRuntimePrefabs.instance.PlaneSizeHandle);

                var planeSizeHandle = m_Handle.GetComponent<PlaneSizeHandle>();
                planeSizeHandle.PlaneSizeCondition = this;
            }
            else if (!m_Adjusting && m_Handle != null)
            {
                handleModule.DestroyHandle(m_Handle);
            }
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            minimumSize = Vector2.Max(k_LowestMinimumBound, minimumSize);
            maximumSize = Vector2.Max(minBounded ? minimumSize : k_LowestMinimumBound, maximumSize);

            noMinMaxWarning = !m_MinBounded && !m_MaxBounded;
            smallMinMaxRangeWarning = m_MaxBounded && m_MinBounded &&
                                      (m_MaximumSize.x - m_MinimumSize.x < k_MinPaddingWarning ||
                                       m_MaximumSize.y - m_MinimumSize.y < k_MinPaddingWarning);
            drawWarning = noMinMaxWarning || smallMinMaxRangeWarning;
        }

        public override void ScaleParameters(float scale)
        {
            m_MinimumSize *= scale;
            m_MaximumSize *= scale;
        }
#endif

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

        public override float RateDataMatch(ref Vector2 data)
        {
            var dataX = data.x;
            var dataY = data.y;
            var minX = m_MinimumSize.x;
            var minY = m_MinimumSize.y;
            var maxX = m_MaximumSize.x;
            var maxY = m_MaximumSize.y;
            if (m_MinBounded)
            {
                // if we have both a min & max, we can determine where in that range it falls.
                if (m_MaxBounded)
                {
                    if (data.x < minX || data.x > maxX)
                    {
                        // check if the flipped version could pass
                        if (data.y < minX || data.y > maxX)
                            return 0f;
                    }

                    if (data.y < minY || data.y > maxY)
                    {
                        if (data.x < minY || data.x > maxY)
                            return 0f;
                    }

                    var rangeX = maxX - minX;
                    var rangeY = maxY - minY;
                    var halfRangeX = rangeX * 0.5f;
                    var halfRangeY = rangeY * 0.5f;

                    var center = m_RatingConfig.center;
                    var rangeCenterX = minX + rangeX * center;
                    var rangeCenterY = minY + rangeY * center;

                    var diffDxAx = dataX - rangeCenterX;
                    var diffDyAy = dataY - rangeCenterY;

                    var middleDiffXIsPositive = diffDxAx > 0;
                    var middleDiffYIsPositive = diffDyAy > 0;

                    var middleDiffX = middleDiffXIsPositive ? diffDxAx : -diffDxAx;
                    var middleDiffY = middleDiffYIsPositive ? diffDyAy : -diffDyAy;

                    var portionOfX = middleDiffX / halfRangeX;
                    var portionOfY = middleDiffY / halfRangeY;

                    var diffDyAx = dataY - rangeCenterX;
                    var diffDxAy = dataX - rangeCenterY;
                    var middleDiffXFlipped = diffDyAx > 0 ? diffDyAx : -diffDyAx;
                    var middleDiffYFlipped = diffDxAy > 0 ? diffDxAy : -diffDxAy;

                    var portionOfXFlipped = middleDiffXFlipped / halfRangeY;
                    var portionOfYFlipped = middleDiffYFlipped / halfRangeX;

                    // if the overall diff from ideal is less when flipped, that's our better match
                    if (portionOfXFlipped < 1f && portionOfYFlipped < 1f)
                    {
                        var flippedMatchesBetter = portionOfX + portionOfY > portionOfXFlipped + portionOfYFlipped;
                        if (flippedMatchesBetter)
                        {
                            portionOfX = portionOfXFlipped;
                            portionOfY = portionOfYFlipped;
                            middleDiffX = middleDiffXFlipped;
                            middleDiffY = middleDiffYFlipped;
                        }
                    }

                    var deadZone = m_RatingConfig.deadZone;
                    if (portionOfX < deadZone && portionOfY < deadZone)
                        return 1f;

                    var xInterpolationFactor = 1f;
                    var yInterpolationFactor = 1f;
                    if (center != 0.5f)
                    {
                        xInterpolationFactor = middleDiffXIsPositive ? (1f - center) * 2f : center * 2f;
                        yInterpolationFactor = middleDiffYIsPositive ? (1f - center) * 2f : center * 2f;
                    }

                    var adjustedRangeX = halfRangeX * xInterpolationFactor;
                    var adjustedRangeY = halfRangeY * yInterpolationFactor;
                    var adjustedDeadZoneX = deadZone / xInterpolationFactor;
                    var adjustedDeadZoneY = deadZone / yInterpolationFactor;

                    portionOfX = middleDiffX / adjustedRangeX;
                    portionOfY = middleDiffY / adjustedRangeY;

                    var tX = (portionOfX - adjustedDeadZoneX) / (1f - adjustedDeadZoneX);
                    var tY = (portionOfY - adjustedDeadZoneY) / (1f - adjustedDeadZoneY);
                    if (tX > 1f && tY > 1f)
                        return 0f;

                    // if either t parameter is less than 0 according to the above calculation,
                    // that means it's actually within the deadzone and a perfect match on that axis.
                    if (tX < 0f)
                        tX = 0f;
                    if (tY < 0f)
                        tY = 0f;

                    var averageLerp = (tX + tY) * 0.5f;
                    if (averageLerp > 1f)
                        averageLerp = 1f;

                    var rating = 1f + MARSDatabase.MinimumRatingMinusOne * averageLerp;     // inlined lerp
                    if (rating > 1f)
                        rating = 1f;

                    return rating;
                }

                // if not max bounded, the range is infinite, so we can't give a more fine-grained answer than yes/no
                if (dataX > minX && dataY > minY)
                    return 1f;
                if (dataX > minY && dataY > minX)        // reverse it if we didn't pass
                    return 1f;

                return 0f;
            }
            if (m_MaxBounded)
            {
                if (dataX < maxX && dataY < maxY)
                    return 1f;
                if (dataX < maxY && dataY < maxX)       // flip it
                    return 1f;

                return 0f;
            }

            // if we're not bounded on either side somehow, data always matches
            return 1f;
        }
    }
}
