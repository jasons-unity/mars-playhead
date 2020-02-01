using System;
using UnityEngine;

namespace Unity.Labs.MARS.Recording
{
    [Serializable]
    struct SerializedPointCloudData
    {
        [SerializeField]
        MarsTrackableId m_Id;

        [SerializeField]
        ulong[] m_Identifiers;

        [SerializeField]
        Vector3[] m_Positions;

        [SerializeField]
        float[] m_ConfidenceValues;

        public MarsTrackableId Id { get { return m_Id; } set { m_Id = value; } }
        public ulong[] Identifiers { get { return m_Identifiers; } set { m_Identifiers = value; } }
        public Vector3[] Positions { get { return m_Positions; } set { m_Positions = value; } }
        public float[] ConfidenceValues { get { return m_ConfidenceValues; } set { m_ConfidenceValues = value; } }
    }
}
