#if INCLUDE_MARS
using System;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    abstract class ULSFaceExpression : IFacialExpression
    {
        const float k_EventCooldownSeconds = 0.15f;
        const float k_Smoothing = 0.9f;

        protected readonly MRFaceExpression m_Name;

        protected float m_Threshold;

        protected float m_LastEngageEvent;
        protected float m_LastDisengageEvent;

        protected float m_PreviousCoefficient;
        protected float m_Coefficient;

        protected bool m_Engaged;

        public event Action<float> Engaged;
        public event Action<float> Disengaged;

        public MRFaceExpression Name { get { return m_Name; } }

        public float maxDistance { get; set; }
        public float minDistance { get; set; }

        public float Coefficient { get { return m_Coefficient; } }

        public float Threshold
        {
            get { return m_Threshold; }
            set { m_Threshold = Mathf.Clamp01(value); }
        }

        protected ULSFaceExpression(MRFaceExpression name)
        {
            m_Name = name;
            var index = (int)name;
            var settings = ULSFacialExpressionSettings.instance;
            m_Threshold = settings.thresholds[index];
            minDistance = settings.expressionDistanceMinimums[index];
            maxDistance = settings.expressionDistanceMaximums[index];
        }

        public abstract float GetCoefficient(Vector3[] landmarks);

        public float Update(Vector3[] landmarks, bool withinRange)
        {
            var time = MarsTime.Time;
            if (!withinRange)
            {
                DisengageIfAppropriate(time);
                return m_PreviousCoefficient;
            }

            m_Coefficient = Mathf.Lerp(m_PreviousCoefficient, GetCoefficient(landmarks), k_Smoothing);

            if (m_Coefficient > m_Threshold && !m_Engaged)
            {
                m_Engaged = true;
                if (Engaged != null && time - m_LastEngageEvent > k_EventCooldownSeconds)
                {
                    m_LastEngageEvent = time;
                    Engaged(m_Coefficient);
                }
            }
            else if (m_Coefficient < m_Threshold && m_Engaged)
            {
                DisengageIfAppropriate(time);
            }

            m_PreviousCoefficient = m_Coefficient;
            return m_Coefficient;
        }

        void DisengageIfAppropriate(float time)
        {
            m_Engaged = false;
            if (Disengaged != null && time - m_LastDisengageEvent > k_EventCooldownSeconds)
            {
                m_LastDisengageEvent = time;
                Disengaged(m_Coefficient);
            }
        }
    }

    class ULSMouthOpenExpression : ULSFaceExpression
    {
        public ULSMouthOpenExpression() : base(MRFaceExpression.MouthOpen) {}

        public override float GetCoefficient(Vector3[] landmarks)
        {
            var upperLip = landmarks[(int)ULSFaceLandmarks.UpperLipMiddle51];
            var lowerLip = landmarks[(int)ULSFaceLandmarks.LowerLipMiddle57];
            return CoefficientUtils.FromDistance(upperLip, lowerLip, minDistance, maxDistance);
        }
    }

    class ULSRightEyeCloseExpression : ULSFaceExpression
    {
        public ULSRightEyeCloseExpression() : base(MRFaceExpression.RightEyeClose) {}

        public override float GetCoefficient(Vector3[] landmarks)
        {
            // ULS labels are the reverse of ARKit
            var upperInner = landmarks[(int)ULSFaceLandmarks.LeftEyeUpper43];
            var lowerInner = landmarks[(int)ULSFaceLandmarks.LeftEyeLower47];
            var upperOuter = landmarks[(int)ULSFaceLandmarks.LeftEyeUpper44];
            var lowerOuter = landmarks[(int)ULSFaceLandmarks.LeftEyeLower46];

            var upper = (upperInner + upperOuter) * 0.5f;
            var lower = (lowerInner + lowerOuter) * 0.5f;

            return CoefficientUtils.FromInverseDistance(upper, lower, minDistance, maxDistance);
        }
    }

    class ULSLeftEyeCloseExpression : ULSFaceExpression
    {
        public ULSLeftEyeCloseExpression() : base(MRFaceExpression.LeftEyeClose) {}

        public override float GetCoefficient(Vector3[] landmarks)
        {
            // ULS labels are the reverse of ARKit
            var upperInner = landmarks[(int)ULSFaceLandmarks.RightEyeUpper38];
            var lowerInner = landmarks[(int)ULSFaceLandmarks.RightEyeLower40];
            var upperOuter = landmarks[(int)ULSFaceLandmarks.RightEyeUpper37];
            var lowerOuter = landmarks[(int)ULSFaceLandmarks.RightEyeLower41];

            var upper = (upperInner + upperOuter) * 0.5f;
            var lower = (lowerInner + lowerOuter) * 0.5f;
            return CoefficientUtils.FromInverseDistance(upper, lower, minDistance, maxDistance);
        }
    }

    class ULSLeftEyebrowRaiseExpression : ULSFaceExpression
    {
        public ULSLeftEyebrowRaiseExpression() : base(MRFaceExpression.LeftEyebrowRaise) {}

        public override float GetCoefficient(Vector3[] landmarks)
        {
            // ULS labels are the reverse of ARKit
            return ULSFaceExpressionUtils.EyebrowRaiseCoefficient(landmarks,
                ULSFaceLandmarks.RightEyebrowOuter17, ULSFaceLandmarks.RightEyebrow20,
                ULSFaceLandmarks.RightEyeOuter36, minDistance, maxDistance);
        }
    }

    class ULSRightEyebrowRaiseExpression : ULSFaceExpression
    {
        public ULSRightEyebrowRaiseExpression() : base(MRFaceExpression.RightEyebrowRaise) {}

        public override float GetCoefficient(Vector3[] landmarks)
        {
            // ULS labels are the reverse of ARKit
            return ULSFaceExpressionUtils.EyebrowRaiseCoefficient(landmarks,
                ULSFaceLandmarks.LeftEyebrowOuter26, ULSFaceLandmarks.LeftEyebrowInner22,
                ULSFaceLandmarks.LeftEyeOuter45, minDistance, maxDistance);
        }
    }

    class ULSBothEyebrowsRaiseExpression : ULSFaceExpression
    {
        ULSLeftEyebrowRaiseExpression m_LeftBrow;
        ULSRightEyebrowRaiseExpression m_RightBrow;

        public ULSBothEyebrowsRaiseExpression(ULSLeftEyebrowRaiseExpression left, ULSRightEyebrowRaiseExpression right)
            : base(MRFaceExpression.BothEyebrowsRaise)
        {
            m_LeftBrow = left;
            m_RightBrow = right;
        }

        public override float GetCoefficient(Vector3[] landmarks)
        {
            const float raiseMinimum = 0.20f;
            var left = m_LeftBrow.Coefficient;
            var right = m_RightBrow.Coefficient;
            if (left < raiseMinimum || right < raiseMinimum)
                return 0f;

            return Mathf.Clamp01((left + right) * 0.5f);
        }
    }

    class ULSLeftLipCornerRaiseExpression : ULSFaceExpression
    {
        public ULSLeftLipCornerRaiseExpression() : base(MRFaceExpression.LeftLipCornerRaise) {}

        public override float GetCoefficient(Vector3[] landmarks)
        {
            // ULS labels are the reverse of ARKit
            return ULSFaceExpressionUtils.LipCornerCoefficient(landmarks,
                ULSFaceLandmarks.RightLipCorner48, minDistance, maxDistance);
        }
    }

    class ULSRightLipCornerRaiseExpression : ULSFaceExpression
    {
        public ULSRightLipCornerRaiseExpression() : base(MRFaceExpression.RightLipCornerRaise) {}

        public override float GetCoefficient(Vector3[] landmarks)
        {
            // ULS labels are the reverse of ARKit
            return ULSFaceExpressionUtils.LipCornerCoefficient(landmarks,
                ULSFaceLandmarks.LeftLipCorner54, minDistance, maxDistance);
        }
    }

    class ULSSmileExpression : ULSFaceExpression
    {
        ULSLeftLipCornerRaiseExpression m_LeftCorner;
        ULSRightLipCornerRaiseExpression m_RightCorner;

        public ULSSmileExpression(ULSLeftLipCornerRaiseExpression left, ULSRightLipCornerRaiseExpression right)
            : base(MRFaceExpression.Smile)
        {
            m_LeftCorner = left;
            m_RightCorner = right;
        }

        public override float GetCoefficient(Vector3[] landmarks)
        {
            const float smileMinimum = 0.15f;
            var left = m_LeftCorner.Coefficient;
            var right = m_RightCorner.Coefficient;

            // if both corners aren't at least slightly raised, no smile.
            if (left < smileMinimum || right < smileMinimum)
                return 0f;

            return (left + right) * 0.5f;
        }
    }
}
#endif
