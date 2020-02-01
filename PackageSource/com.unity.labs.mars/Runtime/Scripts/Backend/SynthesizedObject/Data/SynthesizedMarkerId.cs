using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    public class SynthesizedMarkerId : SynthesizedTrait<string>
    {
#pragma warning disable 649
        [SerializeField]
        string m_MarkerGuid;
#pragma warning restore 649
        public override string TraitName => TraitNames.MarkerId;
        public override bool UpdateWithTransform => false;
        public override string GetTraitData()
        {
            return m_MarkerGuid;
        }
    }
}
