using System;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    /// <summary>
    /// Creates the data for a plane alignment trait
    /// When added to a synthesized object, adds an alignment based on the game object's rotation
    /// </summary>
    public class SynthesizedAlignment : SynthesizedTrait<int>, ICreatesConditions
    {
        const float k_VerticalRange = 0.996f;
        const float k_HorizontalRange = 0.087f;

        public override string TraitName { get { return TraitNames.Alignment; } }
        public override bool UpdateWithTransform { get { return false; } }
        public string ConditionName { get { return "Plane Alignment"; } }
        public Type ConditionType { get { return typeof(AlignmentCondition); } }
        public string ValueString
        {
            get
            {
                return ((MarsPlaneAlignment)GetTraitData()).ToString();
            }
        }

        public int Order { get; }

        public override int GetTraitData()
        {
            var up = transform.up.y;
            var absUp = Mathf.Abs(up);

            if (absUp > k_VerticalRange)
            {
                return up > 0 ? (int) MarsPlaneAlignment.HorizontalUp : (int) MarsPlaneAlignment.HorizontalDown;
            }
            if (absUp < k_HorizontalRange)
            {
                return (int)MarsPlaneAlignment.Vertical;
            }
            return (int)MarsPlaneAlignment.NonAxis;
        }

        /// <summary>
        /// Adds conditions to a gameobject that would help a query specify this alignment
        /// </summary>
        /// <param name="go"> The gameobject to add the condition to </param>
        public void CreateIdealConditions(GameObject go)
        {
            var condition = go.AddComponent<AlignmentCondition>();
            condition.alignment = (MarsPlaneAlignment)GetTraitData();
        }

        public void ConformCondition(ICondition condition)
        {
            var alignmentCondition = condition as AlignmentCondition;
            if (alignmentCondition == null)
                return;

            alignmentCondition.alignment = (MarsPlaneAlignment)((int)alignmentCondition.alignment | GetTraitData());
        }
    }
}
