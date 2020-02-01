using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    /// <summary>
    /// Creates the data for a 2D bounds trait
    /// When added to a synthesized object, adds extents based on the object's scale to its representation in the database
    /// </summary>
    public class SynthesizedBounds2D : SynthesizedTrait<Vector2>, IUsesCameraOffset, ICreatesConditions
    {
        const float k_ConformPadding = 0.01f;
        const float k_DefaultConditionFuzziness = 0.25f;

        [SerializeField]
        [HideInInspector]
        Vector2 m_BaseBounds = Vector2.one;

        public override string TraitName { get { return TraitNames.Bounds2D; } }

        public string ConditionName { get { return "Plane Size"; } }
        public Type ConditionType { get { return typeof(PlaneSizeCondition); } }

        public string ValueString
        {
            get
            {
                var data = GetTraitData();
                return string.Format("{0:0.00}m x {1:0.00}m", data.x, data.y);
            }
        }

        public int Order { get; }

        public override bool UpdateWithTransform { get { return true; } }

        public Vector2 baseBounds
        {
            get { return m_BaseBounds; }
            set { m_BaseBounds = value; }
        }

#if !FI_AUTOFILL
        public IProvidesCameraOffset provider { get; set; }
#endif

        public override Vector2 GetTraitData()
        {
            // Should use the "real world" scale of the transform, which is the "virtual world" scale divided by the camera scale.
            var realScale = transform.lossyScale / this.GetCameraScale();
            return new Vector2(baseBounds.x * realScale.x, baseBounds.y * realScale.z);
        }

        /// <summary>
        /// Adds conditions to a gameobject that would help a query roughly specify this data
        /// </summary>
        /// <param name="go"> The gameobject to add the condition to </param>
        public void CreateIdealConditions(GameObject go)
        {
            var condition = go.AddComponent<PlaneSizeCondition>();
            var extents = GetTraitData();
            var plusMinus = extents * k_DefaultConditionFuzziness;
            condition.maximumSize = extents + plusMinus;
            condition.minimumSize = extents - plusMinus;
        }

        public void ConformCondition(ICondition condition)
        {
            var data = GetTraitData();
            var boundsCondition = condition as PlaneSizeCondition;
            if (boundsCondition == null)
                return;

            if (boundsCondition.maxBounded)
            {
                var newMaxSize = boundsCondition.maximumSize;
                if (boundsCondition.maximumSize.x < data.x)
                    newMaxSize.x = data.x + k_ConformPadding;

                if (boundsCondition.maximumSize.y < data.y)
                    newMaxSize.y = data.y + k_ConformPadding;

                boundsCondition.maximumSize = newMaxSize;
            }

            if (boundsCondition.minBounded)
            {
                var newMinSize = boundsCondition.minimumSize;
                if (boundsCondition.minimumSize.x > data.x)
                    newMinSize.x = data.x - k_ConformPadding;

                if (boundsCondition.minimumSize.y > data.y)
                    newMinSize.y = data.y - k_ConformPadding;

                boundsCondition.minimumSize = newMinSize;
            }
        }
    }
}
