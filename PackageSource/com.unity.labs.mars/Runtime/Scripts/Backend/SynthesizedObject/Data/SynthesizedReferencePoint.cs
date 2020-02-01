using System.Collections.Generic;
using Unity.Labs.ModuleLoader;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    /// <summary>
    /// When added to a synthesized object, adds a trackable reference point to its representation in the database
    /// </summary>
    [RequireComponent(typeof(SynthesizedPose))]
    public class SynthesizedReferencePoint : SynthesizedTrackable<MRReferencePoint>, IUsesCameraOffset
    {
        static readonly TraitDefinition[] k_ProvidedTraits = { TraitDefinitions.Point };

        SynthesizedPose m_PoseSource;

        MRReferencePoint m_ReferencePoint = new MRReferencePoint(new Pose(), MARSTrackingState.Tracking);

        public override string TraitName { get { return TraitNames.Point; } }

#if !FI_AUTOFILL
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }
#endif

        public override void Initialize()
        {
            m_PoseSource = GetComponent<SynthesizedPose>();
        }

        public override MRReferencePoint GetData()
        {
            m_ReferencePoint.pose = m_PoseSource.GetTraitData();
            return m_ReferencePoint;
        }
    }
}
