using Unity.Labs.MARS.Data;
using Unity.Labs.MARS.Query;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [RequireComponent(typeof(Proxy))]
    [ComponentTooltip("Sets the position of this GameObject to the position of the found real-world object.")]
    [MonoBehaviourComponentMenu(typeof(SetPoseAction), "Action/Set Pose")]
    public class SetPoseAction : TransformAction, IMatchAcquireHandler, IMatchUpdateHandler, IRequiresTraits
    {
        [SerializeField]
        [Tooltip("When enabled, movement of the matched data, such as surface-resizing, will be followed")]
        public bool FollowMatchUpdates = true;

        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.Pose };

        public void OnMatchAcquire(QueryResult queryResult)
        {
            UpdatePosition(queryResult);
        }

        public void OnMatchUpdate(QueryResult queryResult)
        {
            if (FollowMatchUpdates)
            {
                UpdatePosition(queryResult);
            }
        }

        void UpdatePosition(QueryResult queryResult)
        {
            if (queryResult.TryGetTrait(TraitNames.Pose, out Pose newPose))
                transform.SetWorldPose(newPose);
        }

        public TraitRequirement[] GetRequiredTraits()
        {
            return k_RequiredTraits;
        }
    }
}
