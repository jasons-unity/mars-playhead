using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Holds metadata about an MR session recording
    /// </summary>
    public class SessionRecordingInfo : ScriptableObject
    {
        [SerializeField]
        TimelineAsset m_Timeline;

        [SerializeField]
        List<DataRecording> m_DataRecordings = new List<DataRecording>();

        [SerializeField]
        List<GameObject> m_SyntheticEnvironments = new List<GameObject>();

        public TimelineAsset Timeline
        {
            get { return m_Timeline; }
            set { m_Timeline = value; }
        }

        public void AddDataRecording(DataRecording recording) { m_DataRecordings.Add(recording); }

        public void GetDataRecordings(List<DataRecording> dataRecordings) { dataRecordings.AddRange(m_DataRecordings); }

        public void AddSyntheticEnvironment(GameObject environmentPrefab) { m_SyntheticEnvironments.Add(environmentPrefab); }

        public void GetSyntheticEnvironments(List<GameObject> environments) { environments.AddRange(m_SyntheticEnvironments); }
    }
}
