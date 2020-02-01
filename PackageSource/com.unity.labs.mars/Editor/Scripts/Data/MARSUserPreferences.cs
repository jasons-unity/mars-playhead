using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ScriptableSettingsPath(MARSCore.UserSettingsFolder)]
    public class MARSUserPreferences : ScriptableSettings<MARSUserPreferences>
    {
        const string k_ResetHintsEvent = "Reset Hints";
        const string k_ResetColorsEvent = "Reset Colors";

        static readonly Color k_DefaultDefaultHandleColor = Color.magenta;
        static readonly Color k_DefaultValidDependencyColor = Color.green;
        static readonly Color k_DefaultInvalidDependencyColor = Color.red;
        static readonly Color k_DefaultRelationColor = Color.cyan;
        static readonly Color k_DefaultEditingRelationColor = new Color(0, 1, 1, 0.2f);
        static readonly Color k_DefaultPlaneSizeConditionColor = new Color(0.1172f, 0.7383f, 0.8086f);
        static readonly Color k_DefaultElevationRelationColor = new Color(0.1133f, 0.8672f, 0.5742f);
        static readonly Color k_DefaultDistanceConditionColor = new Color(0.8125f, 0.3516f, 0.2109f);
        static readonly Color k_DefaultAngleAxisConditionColor = new Color(0.6484f, 0.2656f, 0.668f);
        static readonly Color k_DefaultUnmatchedConditionColor = Color.red;
        static readonly Color k_DefaultHighlightedSimulatedObjectColor = Color.yellow;

        [Header("Hints")]

        [SerializeField]
        bool m_ShowMARSHints = true;

        [SerializeField]
        bool m_ShowWorldScaleHint = true;

        [SerializeField]
        bool m_ShowEntitySetupHints = true;

        [SerializeField]
        bool m_ShowDataVisualizersHints = true;

        [SerializeField]
        bool m_ShowMemoryOptionsHint = true;

        [SerializeField]
        bool m_ShowQuerySearchHint = true;

        [SerializeField]
        bool m_ShowDisabledSimulationComponentsHint = true;

        [SerializeField]
        bool m_ShowSimulatedGeoLocationHint = true;

        [Header("Colors")]

        [SerializeField]
        Color m_DefaultHandleColor = k_DefaultDefaultHandleColor;

        [SerializeField]
        Color m_ValidDependencyColor = k_DefaultValidDependencyColor;

        [SerializeField]
        Color m_InvalidDependencyColor = k_DefaultInvalidDependencyColor;

        [SerializeField]
        Color m_RelationColor = k_DefaultRelationColor;

        [SerializeField]
        Color m_EditingRelationColor = k_DefaultEditingRelationColor;

        [SerializeField]
        Color m_PlaneSizeConditionColor = k_DefaultPlaneSizeConditionColor;

        [SerializeField]
        Color m_ElevationRelationColor = k_DefaultElevationRelationColor;

        [SerializeField]
        Color m_DistanceConditionColor = k_DefaultDistanceConditionColor;

        [SerializeField]
        Color m_AngleAxisConditionColor = k_DefaultAngleAxisConditionColor;

        [SerializeField]
        Color m_UnmatchedConditionColor = k_DefaultUnmatchedConditionColor;

        [SerializeField]
        Color m_HighlightedSimulatedObjectColor = k_DefaultHighlightedSimulatedObjectColor;

        [Header("Planes Extraction")]
#pragma warning disable 649
        [SerializeField]
        bool m_ExtractPlanesOnSave;
#pragma warning restore 649

        [Header("World Scale")]

        [SerializeField]
        bool m_ScaleEntityPositions = true;

#pragma warning disable 649
        [SerializeField]
        bool m_ScaleEntityChildren;

        [SerializeField]
        bool m_ScaleSceneAudio;

        [SerializeField]
        bool m_ScaleSceneLighting;
#pragma warning restore 649

        [SerializeField]
        bool m_ScaleClippingPlanes = true;

        [Header("MARS Views")]

        [SerializeField]
        bool m_RestrictCameraToEnvironmentBounds = true;

        [Header("MARS Image Markers")]
        [SerializeField]
        bool m_TintImageMarkers = false;

        public bool TintImageMarkers { get => m_TintImageMarkers; }
        
        public bool showWorldScaleHint
        {
            get { return m_ShowMARSHints && m_ShowWorldScaleHint; }
            set { m_ShowWorldScaleHint = value; }
        }

        public bool showEntitySetupHints
        {
            get { return m_ShowMARSHints && m_ShowEntitySetupHints; }
            set { m_ShowEntitySetupHints = value; }
        }

        public bool showDataVisualizersHints
        {
            get { return m_ShowMARSHints && m_ShowDataVisualizersHints; }
            set { m_ShowDataVisualizersHints = value; }
        }

        public bool showMemoryOptionsHint
        {
            get { return m_ShowMARSHints && m_ShowMemoryOptionsHint; }
            set { m_ShowMemoryOptionsHint = value; }
        }

        public bool ShowQuerySearchHint
        {
            get { return m_ShowMARSHints && m_ShowQuerySearchHint; }
            set { m_ShowMemoryOptionsHint = value; }
        }

        public bool ShowDisabledSimulationComponentsHint
        {
            get { return m_ShowMARSHints && m_ShowDisabledSimulationComponentsHint; }
            set { m_ShowDisabledSimulationComponentsHint = value; }
        }

        public bool showSimulatedGeoLocationHint
        {
            get { return m_ShowMARSHints && m_ShowSimulatedGeoLocationHint; }
            set { m_ShowSimulatedGeoLocationHint = value; }
        }

        public Color defaultHandleColor { get { return m_DefaultHandleColor; } }
        public Color validDependencyColor { get { return m_ValidDependencyColor; } }
        public Color invalidDependencyColor { get { return m_InvalidDependencyColor; } }
        public Color relationColor { get { return m_RelationColor; } }
        public Color editingRelationColor { get { return m_EditingRelationColor; } }
        public Color planeSizeConditionColor { get { return m_PlaneSizeConditionColor; } }
        public Color elevationRelationColor { get { return m_ElevationRelationColor; } }
        public Color distanceConditionColor { get { return m_DistanceConditionColor; } }
        public Color angleAxisConditionColor { get { return m_AngleAxisConditionColor; } }
        public Color unmatchedConditionColor { get { return m_UnmatchedConditionColor; } }
        public Color highlightedSimulatedObjectColor { get { return m_HighlightedSimulatedObjectColor; } }

        public bool extractPlanesOnSave { get { return m_ExtractPlanesOnSave; } }

        public bool scaleEntityPositions { get { return m_ScaleEntityPositions; } }
        public bool scaleEntityChildren { get { return m_ScaleEntityChildren; } }
        public bool scaleSceneAudio { get { return m_ScaleSceneAudio; } }
        public bool scaleSceneLighting { get { return m_ScaleSceneLighting; } }
        public bool scaleClippingPlanes { get { return m_ScaleClippingPlanes; } }

        public bool RestrictCameraToEnvironmentBounds { get { return m_RestrictCameraToEnvironmentBounds; } }

        public void ResetHints()
        {
            m_ShowMARSHints = true;
            m_ShowWorldScaleHint = true;
            m_ShowEntitySetupHints = true;
            m_ShowDataVisualizersHints = true;
            m_ShowSimulatedGeoLocationHint = true;

            EditorEvents.UiComponentUsed.Send(new UiComponentArgs { label = k_ResetHintsEvent, active = true });
        }

        public void ResetColors()
        {
            m_DefaultHandleColor = k_DefaultDefaultHandleColor;
            m_ValidDependencyColor = k_DefaultValidDependencyColor;
            m_InvalidDependencyColor = k_DefaultInvalidDependencyColor;
            m_RelationColor = k_DefaultRelationColor;
            m_EditingRelationColor = k_DefaultEditingRelationColor;
            m_PlaneSizeConditionColor = k_DefaultPlaneSizeConditionColor;
            m_ElevationRelationColor = k_DefaultElevationRelationColor;
            m_DistanceConditionColor = k_DefaultDistanceConditionColor;
            m_AngleAxisConditionColor = k_DefaultAngleAxisConditionColor;
            m_UnmatchedConditionColor = k_DefaultUnmatchedConditionColor;
            m_HighlightedSimulatedObjectColor = k_DefaultHighlightedSimulatedObjectColor;

            EditorEvents.UiComponentUsed.Send(new UiComponentArgs { label = k_ResetColorsEvent, active = true });

            SceneView.RepaintAll();
        }

    }
}
