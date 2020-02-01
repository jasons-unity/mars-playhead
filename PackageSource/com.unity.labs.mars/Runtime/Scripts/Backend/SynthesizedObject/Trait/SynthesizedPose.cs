using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    /// <summary>
    /// Create the data for a pose trait
    /// When added to a synthesized object, adds a pose in the form of the GameObject's world position
    /// to its representation in the database
    /// </summary>
    public class SynthesizedPose : SynthesizedTrait<Pose>, IUsesCameraOffset, ICreatesRelations, IRequiresTraits<Pose>
    {
        const float k_MinRange = 0.5f;
        const float k_Fuzziness = 0.25f;

        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Pose };

        public Type ConditionType { get { return typeof(ElevationRelation); } }
        public string ConditionName { get { return "Elevation"; } }
        public string ValueString { get { return string.Format("{0:0.00}m", elevationFromFloor); } }
        public int Order { get; }

        float floorElevation
        {
            get
            {
                var elevation = 0f;
                Dictionary<int, Pose> results;
                if (this.TryGetAllTraitsWithSemanticTag(TraitNames.Pose, TraitNames.Floor, out results))
                    elevation = results.First().Value.position.y;

                return elevation;
            }
        }

        internal float elevationFromFloor
        {
            get
            {
                return GetTraitData().position.y - floorElevation;
            }
        }

        public override string TraitName { get { return TraitNames.Pose; } }
        public override bool UpdateWithTransform { get { return true; } }

#if !FI_AUTOFILL
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }
#endif

        public override Pose GetTraitData()
        {
            return this.ApplyInverseOffsetToPose(new Pose(transform.position, transform.rotation));
        }

        public TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

        /// <summary>
        /// Adds a relation to a set that would help a query specify this data.
        /// This also adds a new child object to which <paramref name="primaryChild"/> is related.
        /// </summary>
        /// <param name="set">The set to which the relation should be added</param>
        /// <param name="primaryChild">The preexisting child object of <paramref name="set"/>.
        /// A new child object is created in relation to this object.</param>
        public void CreateIdealRelation(ProxyGroup set, Proxy primaryChild)
        {
            // If this synthesized object is the floor we don't want to create a set at all
            var tags = GetComponents<SynthesizedSemanticTag>();
            if (tags.Any((synthTag) => synthTag.TraitName.Equals(TraitNames.Floor)))
                return;

            // Create elevation relation
            // If this is not the floor, then we want to create a set with the floor
            var pose = GetTraitData();
            var elevation = pose.position.y - floorElevation;
            var buffer = Mathf.Max(k_MinRange * 0.5f, elevation * k_Fuzziness);
            var relation = set.gameObject.AddComponent<ElevationRelation>();

            // Create floor query
            var floorGO = new GameObject("Floor");
            floorGO.transform.SetParent(set.transform, false);
            var realWorldFloor = floorGO.AddComponent<Proxy>();
            var floorCondition = floorGO.AddComponent<SemanticTagCondition>();

            relation.maximum = elevation + buffer;
            relation.minimum = elevation - buffer;

            realWorldFloor.exclusivity = Exclusivity.ReadOnly;
            floorCondition.SetTraitName(TraitNames.Floor);

            relation.child1Proxy = primaryChild;
            relation.child2Proxy = realWorldFloor;

            var newSetPosition = set.transform.position;
            newSetPosition.y = 0f;
            set.transform.position = newSetPosition;

            var newPrimaryChildPosition = primaryChild.transform.localPosition;
            newPrimaryChildPosition.y = elevation * this.GetCameraScale();
            primaryChild.transform.localPosition = newPrimaryChildPosition;
        }
    }
}
