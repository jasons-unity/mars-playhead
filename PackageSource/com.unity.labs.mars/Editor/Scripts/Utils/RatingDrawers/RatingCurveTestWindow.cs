using Unity.Labs.MARS.Providers;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public class RatingCurveTestWindow : EditorWindow
    {
        const int k_DefaultWidth = 280;
        const int k_HeightPerCondition = 240;

        CameraOffsetProvider m_CameraOffsetProvider;

        const string k_InfoBoxLabel = "This shows how each type of condition's match rating changes over " +
                                              "the width of its distribution as you change the dead zone and center.\n" +
                                              "X is a point in the distribution of possible values.\n" +
                                              "Y is match rating: bottom is 0, top is 1.";

        const string k_LegendLabel = "The dotted line on the bottom indicates the global minimum passing " +
                                     "match rating, and there is a small amount on either side of the range " +
                                     "indicating data that does not match.";

        ElevationRatingCurveDrawer m_Elevation;
        DistanceRatingCurveDrawer m_Distance;
        PlaneSizeRatingCurveDrawer m_PlaneSize;

        GameObject m_CameraProviderObject;

        public void OnEnable()
        {
            m_CameraProviderObject = new GameObject("camera offset provider");
            m_CameraProviderObject.hideFlags = HideFlags.HideAndDontSave;
            m_CameraOffsetProvider = m_CameraProviderObject.AddComponent<CameraOffsetProvider>();

            minSize = new Vector2(k_DefaultWidth, k_HeightPerCondition);
            maxSize = new Vector2(k_DefaultWidth, k_HeightPerCondition * 4f);
            titleContent = new GUIContent("Rating Explorer");

            var elevationRelation = m_CameraProviderObject.AddComponent<ElevationRelation>();
            m_CameraOffsetProvider.ConnectSubscriber(elevationRelation);
            m_Elevation = new ElevationRatingCurveDrawer(elevationRelation);

            var distanceRelation = m_CameraProviderObject.AddComponent<DistanceRelation>();
            m_CameraOffsetProvider.ConnectSubscriber(distanceRelation);
            m_Distance = new DistanceRatingCurveDrawer(distanceRelation);

            var planeSizeCondition = m_CameraProviderObject.AddComponent<PlaneSizeCondition>();
            planeSizeCondition.minimumSize = new Vector2(1f, 1f);
            planeSizeCondition.maximumSize = new Vector2(2f, 2f);
            m_PlaneSize = new PlaneSizeRatingCurveDrawer(planeSizeCondition);
        }

        public void OnDisable()
        {
            if (EditorApplication.isPlaying)
                return;

            DestroyImmediate(m_CameraProviderObject);
        }

        [MenuItem(MenuConstants.DevMenuPrefix + "Match Rating Explorer", priority = MenuConstants.MatchRatingExplorerPriority)]
        public static void MenuItem()
        {
            var window = GetWindow<RatingCurveTestWindow>();
            window.titleContent = new GUIContent("Rating Explorer");
            window.Focus();
        }

        public void OnGUI()
        {
            EditorGUILayout.HelpBox(k_InfoBoxLabel, MessageType.Info);

            m_Elevation?.OnGUI();

            EditorGUILayout.HelpBox(k_LegendLabel, MessageType.Info);
            EditorGUIUtils.DrawSplitter();

            m_Distance?.OnGUI();
            EditorGUIUtils.DrawBoxSplitter();
            m_PlaneSize?.OnGUI();
        }
    }
}
