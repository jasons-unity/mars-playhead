using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS.UseCaseContent
{
    [RequireComponent(typeof(LineRenderer))]
    public class LineBetweenLandmarkPoints : MonoBehaviour, ISimulatable
    {
        [SerializeField]
        List<LandmarkController> m_Points = new List<LandmarkController>();

        LineRenderer m_Line;

        public void UpdateLine(LandmarkController landmark)
        {
            if (m_Line == null)
                m_Line = GetComponent<LineRenderer>();

            m_Line.positionCount = m_Points.Count;
            for (int i = 0; i < m_Points.Count; i++)
            {
                var output = m_Points[i].output as LandmarkOutputPoint;
                m_Line.SetPosition(i, output.position);
            }
        }

        void Awake()
        {
            m_Line = GetComponent<LineRenderer>();
        }
    }
}
