using UnityEngine;

namespace Unity.Labs.MARS
{
    public class FaceExpressionEventText : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        TextMesh m_LeftEyeTextMesh;
        [SerializeField]
        TextMesh m_RightEyeTextMesh;
        [SerializeField]
        TextMesh m_LeftBrowTextMesh;
        [SerializeField]
        TextMesh m_RightBrowTextMesh;
        [SerializeField]
        TextMesh m_LeftLipCornerTextMesh;
        [SerializeField]
        TextMesh m_RightLipCornerTextMesh;
        [SerializeField]
        TextMesh m_SmileTextMesh;
        [SerializeField]
        TextMesh m_BothBrowTextMesh;
        [SerializeField]
        TextMesh m_MouthOpenTextMesh;
#pragma warning restore 649

        public void OnLeftEyeClose(float expressionCoefficient)
        {
            m_LeftEyeTextMesh.text = "L Eye CLOSE";
        }

        public void OnLeftEyeOpen(float expressionCoefficient)
        {
            m_LeftEyeTextMesh.text = "L Eye OPEN";
        }

        public void OnRightEyeClose(float expressionCoefficient)
        {
            m_RightEyeTextMesh.text = "R Eye CLOSE";
        }

        public void OnRightEyeOpen(float expressionCoefficient)
        {
            m_RightEyeTextMesh.text = "R Eye OPEN";
        }

        public void OnLeftEyebrowLower(float expressionCoefficient)
        {
            m_LeftBrowTextMesh.text = "L brow LOW";
        }

        public void OnLeftEyebrowRaise(float expressionCoefficient)
        {
            m_LeftBrowTextMesh.text = "L brow RAISED";
        }

        public void OnRightEyebrowLower(float expressionCoefficient)
        {
            m_RightBrowTextMesh.text = "R brow LOW";
        }

        public void OnRightEyebrowRaise(float expressionCoefficient)
        {
            m_RightBrowTextMesh.text = "R brow RAISED";
        }

        public void OnLeftLipCornerRaise(float expressionCoefficient)
        {
            m_LeftLipCornerTextMesh.text = "L lip RAISED";
        }

        public void OnLeftLipCornerLower(float expressionCoefficient)
        {
            m_LeftLipCornerTextMesh.text = "L lip NEUTRAL";
        }

        public void OnRightLipCornerRaise(float expressionCoefficient)
        {
            m_RightLipCornerTextMesh.text = "R lip RAISED";
        }

        public void OnRightLipCornerLower(float expressionCoefficient)
        {
            m_RightLipCornerTextMesh.text = "R lip NEUTRAL";
        }

        public void OnSmileEngaged(float expressionCoefficient)
        {
            m_SmileTextMesh.text = "Smiling";
        }

        public void OnSmileDisengaged(float expressionCoefficient)
        {
            m_SmileTextMesh.text = "Not smiling";
        }

        public void OnBothBrowsRaised(float expressionCoefficient)
        {
            m_BothBrowTextMesh.text = "Both Brows RAISED";
        }

        public void OnBothBrowsLowered(float expressionCoefficient)
        {
            m_BothBrowTextMesh.text = "Both Brows NEUTRAL";
        }

        public void OnMouthOpen(float expressionCoefficient)
        {
            m_MouthOpenTextMesh.text = "Mouth Open";
        }

        public void OnMouthClosed(float expressionCoefficient)
        {
            m_MouthOpenTextMesh.text = "Mouth Closed";
        }
    }
}
