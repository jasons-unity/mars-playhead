using System;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(SynthesizedPose))]
    [RequireComponent(typeof(SynthesizedBounds2D))]
    [RequireComponent(typeof(SynthesizedMarkerId))]
    public class SynthesizedMarker : SynthesizedTrackable<MRMarker>
    {
        static readonly TraitDefinition[] k_ProvidedTraits = { TraitDefinitions.Marker };

        MRMarker m_Marker;
        SynthesizedPose m_PoseSource;
        SynthesizedBounds2D m_ExtentsSource;
        SynthesizedMarkerId m_IdSource;

        internal int dataID { get; set; }

        public override string TraitName => TraitNames.Marker;

        public override void Initialize()
        {
            base.Initialize();
            if (MarsTrackableId.InvalidId == m_Marker.id)
                m_Marker.id = MarsTrackableId.Create();

            m_IdSource = GetComponent<SynthesizedMarkerId>();
            m_PoseSource = GetComponent<SynthesizedPose>();
            m_ExtentsSource = GetComponent<SynthesizedBounds2D>();

            m_Marker.markerId = new Guid(m_IdSource.GetTraitData());
            m_Marker.pose = m_PoseSource.GetTraitData();
            m_Marker.extents = m_ExtentsSource.GetTraitData();
        }

        public override MRMarker GetData() { return m_Marker; }
    }
}
