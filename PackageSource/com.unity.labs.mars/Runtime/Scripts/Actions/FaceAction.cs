using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [RequireComponent(typeof(IsFaceCondition))]
    [RequireComponent(typeof(Proxy))]
    [MonoBehaviourComponentMenu(typeof(FaceAction), "Action/Set Face")]
    public class FaceAction : MonoBehaviour, IMatchAcquireHandler, IMatchUpdateHandler, IMatchLossHandler,
                                                IUsesFaceTracking, IUsesCameraOffset, IUsesMARSTrackableData<IMRFace>
    {
#pragma warning disable 649
        [SerializeField]
        Transform m_FaceAnchorOverride;

        [SerializeField]
        MeshFilter m_FaceMesh;
#pragma warning restore 649

        bool m_RunInEditModeDirty;
        Pose m_StartPose;

        IMRFace m_AssignedFace;

        Transform FaceAnchor { get { return m_FaceAnchorOverride == null ? transform : m_FaceAnchorOverride; } }

        IProvidesFaceTracking IFunctionalitySubscriber<IProvidesFaceTracking>.provider { get; set; }
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }

        public void OnMatchAcquire(QueryResult queryResult)
        {
            m_RunInEditModeDirty = true;
            m_StartPose = FaceAnchor.transform.GetWorldPose();

            m_AssignedFace = queryResult.ResolveValue(this);

            FaceAnchor.transform.SetLocalPose(this.ApplyOffsetToPose(m_AssignedFace.pose));

            if (m_FaceMesh)
                m_FaceMesh.sharedMesh = m_AssignedFace.Mesh;
        }

        public void OnMatchUpdate(QueryResult queryResult)
        {
            FaceAnchor.transform.SetLocalPose(this.ApplyOffsetToPose(m_AssignedFace.pose));
        }

        public void OnMatchLoss(QueryResult queryResult)
        {
            if (!FaceAnchor)
                return;

            if (m_RunInEditModeDirty)
            {
                FaceAnchor.transform.SetWorldPose(m_StartPose);
            }
        }
    }
}
