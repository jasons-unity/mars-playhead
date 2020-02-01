#if INCLUDE_MARS
using System;
using UnityEngine;

namespace Unity.Labs.MARS.Providers
{
    public abstract class XRSDKFaceExpression : IFacialExpression
    {
        protected readonly MRFaceExpression m_Name;

        protected float m_Threshold;
        protected float m_Coefficient;

        protected float m_LastEngageEvent;
        protected float m_LastDisengageEvent;

        protected float m_PreviousCoefficient;
        protected bool m_Engaged;

        protected float m_SmoothingFilter;
        protected float m_EventCooldownInSeconds;

        public event Action<float> Engaged;
        public event Action<float> Disengaged;

        public MRFaceExpression Name { get { return m_Name; } }
        public float Coefficient { get { return m_Coefficient; } }
        public float Threshold
        {
            get { return m_Threshold; }
            set { m_Threshold = Mathf.Clamp01(value); }
        }

        protected XRSDKFaceExpression(MRFaceExpression name)
        {
            m_Name = name;
            var index = (int)name;
            var settings = XRSDKFaceExpressionSettings.instance;
            m_Threshold = settings.thresholds[index];
            m_SmoothingFilter = settings.expressionChangeSmoothingFilter;
            m_EventCooldownInSeconds = settings.eventCooldownInSeconds;
        }

        internal abstract float GetRawCoefficient(XRSDKFace xrsdkFace);

        internal float Update(XRSDKFace xrSdkFace)
        {
            var time = Time.time;

            m_Coefficient = Mathf.Lerp(m_PreviousCoefficient, GetRawCoefficient(xrSdkFace), m_SmoothingFilter);

            if (m_Coefficient > m_Threshold && !m_Engaged)
            {
                m_Engaged = true;
                if (Engaged != null && time - m_LastEngageEvent > m_EventCooldownInSeconds)
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
            if (Disengaged != null && time - m_LastDisengageEvent > m_EventCooldownInSeconds)
            {
                m_LastDisengageEvent = time;
                Disengaged(m_Coefficient);
            }
        }
    }

    public class XRSDKRightEyeCloseExpression : XRSDKFaceExpression
    {
        public XRSDKRightEyeCloseExpression() : base(MRFaceExpression.RightEyeClose) {}

        internal override float GetRawCoefficient(XRSDKFace xrsdkFace)
        {
            return xrsdkFace.Expressions[MRFaceExpression.RightEyeClose];
        }
    }

    public class XRSDKLeftEyeCloseExpression : XRSDKFaceExpression
    {
        public XRSDKLeftEyeCloseExpression() : base(MRFaceExpression.LeftEyeClose) {}

        internal override float GetRawCoefficient(XRSDKFace xrsdkFace)
        {
            return xrsdkFace.Expressions[MRFaceExpression.LeftEyeClose];
        }
    }

    public class XRSDKRightEyebrowRaiseExpression : XRSDKFaceExpression
    {
        public XRSDKRightEyebrowRaiseExpression() : base(MRFaceExpression.RightEyebrowRaise) {}

        internal override float GetRawCoefficient(XRSDKFace xrsdkFace)
        {
            return xrsdkFace.Expressions[MRFaceExpression.RightEyebrowRaise];
        }
    }

    public class XRSDKLeftEyebrowRaiseExpression : XRSDKFaceExpression
    {
        public XRSDKLeftEyebrowRaiseExpression() : base(MRFaceExpression.LeftEyebrowRaise) {}

        internal override float GetRawCoefficient(XRSDKFace xrsdkFace)
        {
            return xrsdkFace.Expressions[MRFaceExpression.LeftEyebrowRaise];
        }
    }

    public class XRSDKBothEyebrowsRaiseExpression : XRSDKFaceExpression
    {
        public XRSDKBothEyebrowsRaiseExpression() : base(MRFaceExpression.BothEyebrowsRaise) {}

        internal override float GetRawCoefficient(XRSDKFace xrsdkFace)
        {
            const float raiseMinimum = 0.20f;
            var left = xrsdkFace.Expressions[MRFaceExpression.LeftEyebrowRaise];
            var right = xrsdkFace.Expressions[MRFaceExpression.RightEyebrowRaise];
            if (left < raiseMinimum || right < raiseMinimum)
                return 0f;

            return Mathf.Clamp01((left + right) * 0.5f) * 1.2f - raiseMinimum;
        }
    }

    public class XRSDKSmileRightExpression : XRSDKFaceExpression
    {
        public XRSDKSmileRightExpression() : base(MRFaceExpression.RightLipCornerRaise) {}

        internal override float GetRawCoefficient(XRSDKFace xrsdkFace)
        {
            return xrsdkFace.Expressions[MRFaceExpression.RightLipCornerRaise];
        }
    }

    public class XRSDKSmileLeftExpression : XRSDKFaceExpression
    {
        public XRSDKSmileLeftExpression() : base(MRFaceExpression.LeftLipCornerRaise) {}

        internal override float GetRawCoefficient(XRSDKFace xrsdkFace)
        {
            return xrsdkFace.Expressions[MRFaceExpression.LeftLipCornerRaise];
        }
    }

    public class XRSDKSmileExpression : XRSDKFaceExpression
    {
        public XRSDKSmileExpression() : base(MRFaceExpression.Smile) { }

        internal override float GetRawCoefficient(XRSDKFace xrsdkFace)
        {
            const float smileMinimum = 0.15f;
            var left = xrsdkFace.Expressions[MRFaceExpression.LeftLipCornerRaise];
            var right = xrsdkFace.Expressions[MRFaceExpression.RightLipCornerRaise];

            // if both corners aren't at least slightly raised, no smile.
            if (left < smileMinimum || right < smileMinimum)
                return 0f;

            return Mathf.Clamp01((left + right) * 0.5f) * 1.2f - smileMinimum;
        }
    }

    public class XRSDKMouthOpenExpression : XRSDKFaceExpression
    {
        public XRSDKMouthOpenExpression() : base(MRFaceExpression.MouthOpen) {}

        internal override float GetRawCoefficient(XRSDKFace xrsdkFace)
        {
            return xrsdkFace.Expressions[MRFaceExpression.MouthOpen];
        }
    }
}
#endif
