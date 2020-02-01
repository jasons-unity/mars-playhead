using UnityEngine;

namespace Unity.Labs.MARS
{
    public class PlaneExtractionSettings : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        PlaneVoxelGenerationParams m_VoxelGenerationParams;

        [SerializeField]
        VoxelPlaneFindingParams m_PlaneFindingParams;
#pragma warning restore 649

        public PlaneVoxelGenerationParams VoxelGenerationParams { get { return m_VoxelGenerationParams; } }
        public VoxelPlaneFindingParams PlaneFindingParams { get { return m_PlaneFindingParams; } }
    }
}
