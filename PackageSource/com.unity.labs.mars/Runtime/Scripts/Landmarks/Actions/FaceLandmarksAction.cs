using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [MonoBehaviourComponentMenu(typeof(FaceLandmarksAction), "Action/Face Landmarks")]
    public class FaceLandmarksAction : MonoBehaviour, IUsesCameraOffset, IUsesMARSTrackableData<IMRFace>, ICalculateLandmarks, ISpawnable
    {
        Dictionary<MRFaceLandmark, Pose> m_AssignedFaceLandmarkPoses;
        Dictionary<MRFaceLandmark, Pose> m_FallbackLandmarkPoses;
        IMRFace m_AssignedFace;

        internal List<LandmarkController> landmarks { get { return GetComponentsInChildren<LandmarkController>().ToList(); } }

#if !FI_AUTOFILL
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }
#endif

        public List<LandmarkDefinition> AvailableLandmarkDefinitions
        {
            get { return s_Definitions; }
        }

        static List<LandmarkDefinition> s_Definitions;

        static FaceLandmarksAction()
        {
            // Initialize the definitions to be all the MRFaceLandmark enum values with output type of pose
            s_Definitions = Enum.GetNames(typeof(MRFaceLandmark)).ToList().ConvertAll(name => new LandmarkDefinition(name, typeof(LandmarkOutputPose)));
        }

        void OnDisable()
        {
            m_AssignedFace = null;
            m_AssignedFaceLandmarkPoses = null;
        }

        public void SetupLandmark(ILandmarkController landmark)
        {
            var faceLandmark = landmark.landmarkDefinition.GetEnumName<MRFaceLandmark>();

            if (m_FallbackLandmarkPoses == null)
                m_FallbackLandmarkPoses = MARSFallbackFaceLandmarks.instance.GetFallbackFaceLandmarkPoses();

            var initialPose = m_FallbackLandmarkPoses[faceLandmark];
            var landmarkTransform = ((Component)(landmark)).transform;
            landmarkTransform.SetLocalPose(initialPose);
            var landmarkPose = landmark.output as LandmarkOutputPose;
            if (landmarkPose != null)
                landmarkPose.currentPose = landmarkTransform.GetWorldPose();
        }

        public Action<ILandmarkController> GetLandmarkCalculation(LandmarkDefinition definition)
        {
            var faceLandmark = definition.GetEnumName<MRFaceLandmark>();
            return (landmark) => UpdateFaceLandmark(landmark, faceLandmark);
        }

        void UpdateFaceLandmark(ILandmarkController landmark, MRFaceLandmark landmarkType)
        {
            // TODO: Investigate why we get calls to OnMatchLoss continuously after tracking loss
            if (m_AssignedFace == null)
                return;

            if (m_FallbackLandmarkPoses == null)
                m_FallbackLandmarkPoses = MARSFallbackFaceLandmarks.instance.GetFallbackFaceLandmarkPoses();

            Pose pose;
            if (m_AssignedFaceLandmarkPoses == null || !m_AssignedFaceLandmarkPoses.TryGetValue(landmarkType, out pose))
                pose = m_AssignedFace.pose.ApplyOffsetTo(m_FallbackLandmarkPoses[landmarkType]);

            var landmarkPose = landmark.output as LandmarkOutputPose;
            if (landmarkPose != null)
                landmarkPose.currentPose = this.ApplyOffsetToPose(pose);
        }

        protected void OnMatchDataChanged(QueryResult queryResult)
        {
            m_AssignedFace = queryResult.ResolveValue(this);

            if (m_AssignedFace == null)
            {
                Debug.LogError("Assigned face is null", gameObject);
            }
            else if (m_AssignedFace.id != MarsTrackableId.InvalidId)
            {
                m_AssignedFaceLandmarkPoses = m_AssignedFace.LandmarkPoses;
            }
        }

        protected void OnMatchDataLost(QueryResult queryResult)
        {
            m_AssignedFace = null;
            m_AssignedFaceLandmarkPoses = null;
        }

        public event Action<ICalculateLandmarks> dataChanged;
        public void OnMatchAcquire(QueryResult queryResult)
        {
            OnMatchDataChanged(queryResult);
            if (dataChanged != null)
                dataChanged(this);
        }

        public void OnMatchUpdate(QueryResult queryResult)
        {
            OnMatchDataChanged(queryResult);
            if (dataChanged != null)
                dataChanged(this);
        }

        public void OnMatchLoss(QueryResult queryResult)
        {
            OnMatchDataLost(queryResult);
            if (dataChanged != null)
                dataChanged(this);
        }
    }
}
