using Unity.Labs.MARS.Data;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ScriptableSettingsPath("MARS/Editor")]
    public class MARSEditorPrefabs : EditorScriptableSettings<MARSEditorPrefabs>
    {
#pragma warning disable 649
        [Header("Primitives")]
        [SerializeField]
        GameObject m_ProxyObjectPrefab;

        [SerializeField]
        GameObject m_ProxyGroupPrefab;

        [SerializeField]
        GameObject m_ReplicatorPrefab;

        [SerializeField]
        GameObject m_SyntheticPrefab;

        [Header("Proxy Templates")]
        [SerializeField]
        GameObject m_HorizontalPlanePrefab;

        [SerializeField]
        GameObject m_VerticalPlanePrefab;

        [SerializeField]
        GameObject m_ImageMarkerPrefab;

        [SerializeField]
        GameObject m_FaceMaskPrefab;

        [Header("Visualizers")]
        [SerializeField]
        GameObject m_PlaneVisualsPrefab;

        [SerializeField]
        GameObject m_PointCloudVisualsPrefab;

        [SerializeField]
        GameObject m_FaceLandmarkVisualsPrefab;

        [SerializeField]
        SynthesizedPlane m_GeneratedSimulatedPlanePrefab;
#pragma warning restore 649

        public GameObject ProxyObjectPrefab => m_ProxyObjectPrefab;
        public GameObject ProxyGroupPrefab => m_ProxyGroupPrefab;
        public GameObject ReplicatorPrefab => m_ReplicatorPrefab;
        public GameObject SyntheticPrefab => m_SyntheticPrefab;

        public GameObject HorizontalPlanePrefab => m_HorizontalPlanePrefab;
        public GameObject VerticalPlanePrefab => m_VerticalPlanePrefab;
        public GameObject ImageMarkerPrefab => m_ImageMarkerPrefab;
        public GameObject FaceMaskPrefab => m_FaceMaskPrefab;

        public GameObject PlaneVisualsPrefab => m_PlaneVisualsPrefab;
        public GameObject PointCloudVisualsPrefab => m_PointCloudVisualsPrefab;
        public GameObject FaceLandmarkVisualsPrefab => m_FaceLandmarkVisualsPrefab;

        public SynthesizedPlane GeneratedSimulatedPlanePrefab => m_GeneratedSimulatedPlanePrefab;
    }
}
