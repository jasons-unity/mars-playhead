#if INCLUDE_MARS
using Unity.Labs.Utils;
using UnityEngine;

#if ENABLE_ULS_TRACKER && (!UNITY_IOS || UNITY_EDITOR)
using ULSTrackerForUnity;
#endif

namespace Unity.Labs.MARS
{
    public class ULSLandmarksVisualizer : MonoBehaviour, ISimulatable
    {
#if ENABLE_ULS_TRACKER && (!UNITY_IOS || UNITY_EDITOR)
        const string k_ContainerName = "ULSLandmarksVisualizer";
        const string k_FaceNameFormat = "Face {0}";

        class LandmarkSet
        {
            readonly MeshRenderer[] m_Renderers;

            public readonly Transform transform;
            public readonly Transform[] points;
            public bool wasTracked;

            public LandmarkSet(int maxTrackerPoints, float scale, string faceId, Transform parent)
            {
                points = new Transform[maxTrackerPoints];
                m_Renderers = new MeshRenderer[maxTrackerPoints];
                transform = GameObjectUtils.Create(faceId).transform;
                transform.parent = parent;
                for (var i = 0; i < maxTrackerPoints; i++)
                {
                    var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    sphere.name = ((ULSFaceLandmarks)i).ToString();
                    var sphereTransform = sphere.transform;
                    sphereTransform.localScale *= scale;
                    sphereTransform.parent = transform;
                    m_Renderers[i] = sphere.GetComponent<MeshRenderer>();
                    points[i] = sphere.transform;
                }
            }

            public void SetEnabled(bool enabled)
            {
                foreach (var renderer in m_Renderers)
                {
                    renderer.enabled = enabled;
                }
            }
        }

        [SerializeField]
        ULSFaceTrackingProvider m_Provider;

        [SerializeField]
        float m_Scale = 0.05f;

        LandmarkSet[] m_LandmarkSets = new LandmarkSet[Plugins.MAX_TRACKER_FACES];

        void Start()
        {
            if (m_Provider == null)
                m_Provider = GetComponent<ULSFaceTrackingProvider>();

            var container = GameObjectUtils.Create(k_ContainerName).transform;
            for (var i = 0; i < Plugins.MAX_TRACKER_FACES; i++)
            {
                var faceName = string.Format(k_FaceNameFormat, i);
                var landmark = new LandmarkSet(Plugins.MAX_TRACKER_POINTS, m_Scale, faceName, container);
                landmark.SetEnabled(false);
                m_LandmarkSets[i] = landmark;
            }
        }

        void Update()
        {
            var trackingStates = m_Provider.trackingStates;
            var targetPoses = m_Provider.targetPoses;
            var landmarkPositions = m_Provider.rawLandmarks;
            var poseScale = m_Provider.poseScale;
            var cameraScale = m_Provider.cameraScale;
            var sphereScale = m_Scale * cameraScale * Vector3.one;
            for (var i = 0; i < Plugins.MAX_TRACKER_FACES; ++i)
            {
                var landmarkSet = m_LandmarkSets[i];
                var trackingState = trackingStates[i];
                if (trackingState)
                {
                    if (!landmarkSet.wasTracked)
                        landmarkSet.SetEnabled(true);

                    var landmarkSetTransform = landmarkSet.transform;
                    var pose = targetPoses[i];
                    pose.position *= poseScale;
                    landmarkSetTransform.SetWorldPose(m_Provider.ApplyOffsetToPose(pose));

                    var landmarks = landmarkPositions[i];
                    var points = landmarkSet.points;
                    for (var j = 0; j < Plugins.MAX_TRACKER_POINTS; j++)
                    {
                        var point = points[j];
                        point.localPosition = landmarks[j] * cameraScale;
                        point.localScale = sphereScale;
                    }
                }
                else
                {
                    if (landmarkSet.wasTracked)
                        landmarkSet.SetEnabled(false);
                }

                landmarkSet.wasTracked = trackingState;
            }
        }
#else
        [SerializeField]
        ULSFaceTrackingProvider m_Provider;

        [SerializeField]
        float m_Scale;
#endif
    }
}
#endif
