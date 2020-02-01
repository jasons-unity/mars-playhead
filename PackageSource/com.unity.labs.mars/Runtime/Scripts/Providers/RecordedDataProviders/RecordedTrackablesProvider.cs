using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS
{
    public abstract class RecordedTrackablesProvider : MonoBehaviour
    {
        readonly Dictionary<MarsTrackableId, MarsTrackableId> m_PlaybackIDs = new Dictionary<MarsTrackableId, MarsTrackableId>();

        protected virtual void OnDisable()
        {
            m_PlaybackIDs.Clear();
        }

        /// <summary>
        /// Gets the session-unique trackable ID mapped to the given recording-unique ID.
        /// This creates a new mapping if one does not already exist for the given ID.
        /// </summary>
        /// <param name="recordedID">Recording-unique trackable ID</param>
        /// <returns>The corresponding session-unique trackable ID</returns>
        protected MarsTrackableId GetPlaybackID(MarsTrackableId recordedID)
        {
            if (m_PlaybackIDs.TryGetValue(recordedID, out var playbackID))
                return playbackID;

            playbackID = MarsTrackableId.Create();
            m_PlaybackIDs[recordedID] = playbackID;
            return playbackID;
        }
    }
}
