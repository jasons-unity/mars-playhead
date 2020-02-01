using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ScriptableSettingsPath(MARSCore.UserSettingsFolder)]
    public class MARSDebugSettings : ScriptableSettings<MARSDebugSettings>
    {
#pragma warning disable 649
        [SerializeField]
        bool m_SceneModuleLogging;

        [SerializeField]
        bool m_GeolocationModuleLogging;

        [SerializeField]
        bool m_QuerySimulationModuleLogging;

        [SerializeField]
        bool m_SimObjectsManagerLogging;

        [SerializeField]
        bool m_SimPlaneFindingLogging;

        [SerializeField]
        bool m_AllowInteractionTargetSelection;

        [SerializeField]
        bool m_SimDiscoveryPointCloudDebug;

        [SerializeField]
        bool m_SimDiscoveryPlaneVerticesDebug;

        [SerializeField]
        bool m_SimDiscoveryPlaneExtentsDebug;

        [SerializeField]
        bool m_SimDiscoveryPlaneCenterDebug;

        [SerializeField]
        bool m_SimDiscoveryVoxelsDebug;
#pragma warning restore 649

        [SerializeField]
        float m_SimDiscoveryPointCloudRayGizmoTime = 0.5f;

        public static bool sceneModuleLogging { get { return instance.m_SceneModuleLogging; } }

        public static bool geoLocationModuleLogging { get { return instance.m_GeolocationModuleLogging; } }

        public static bool querySimulationModuleLogging { get { return instance.m_QuerySimulationModuleLogging; } }

        public static bool SimObjectsManagerLogging { get { return instance.m_SimObjectsManagerLogging; } }

        public static bool SimPlaneFindingLogging { get { return instance.m_SimPlaneFindingLogging; } }

        public static bool SimDiscoveryPointCloudDebug { get { return instance.m_SimDiscoveryPointCloudDebug; } }
        public static float SimDiscoveryPointCloudRayGizmoTime { get { return instance.m_SimDiscoveryPointCloudRayGizmoTime; } }
        public static bool simDiscoveryModulePlaneVerticesDebug { get { return instance.m_SimDiscoveryPlaneVerticesDebug; } }
        public static bool simDiscoveryModulePlaneExtentsDebug { get { return instance.m_SimDiscoveryPlaneExtentsDebug; } }
        public static bool simDiscoveryModulePlaneCenterDebug { get { return instance.m_SimDiscoveryPlaneCenterDebug; } }
        public static bool SimDiscoveryVoxelsDebug { get { return instance.m_SimDiscoveryVoxelsDebug; } }

        public static bool allowInteractionTargetSelection { get { return instance.m_AllowInteractionTargetSelection; } }
    }
}
