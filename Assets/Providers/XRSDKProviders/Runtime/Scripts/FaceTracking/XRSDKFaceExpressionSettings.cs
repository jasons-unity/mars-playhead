#if INCLUDE_MARS
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS.Providers
{
    class XRSDKFaceExpressionSettings : ScriptableSettings<XRSDKFaceExpressionSettings>
    {
        [SerializeField]
        float[] m_Thresholds;

        [SerializeField]
        float m_EventCooldownInSeconds;

        [SerializeField]
        float m_ExpressionChangeSmoothingFilter;

        public float[] thresholds
        {
            get { return m_Thresholds; }
            set { m_Thresholds = value; }
        }

        public float eventCooldownInSeconds
        {
            get => m_EventCooldownInSeconds;
            set => m_EventCooldownInSeconds = value;
        }

        public float expressionChangeSmoothingFilter
        {
            get => m_ExpressionChangeSmoothingFilter;
            set => m_ExpressionChangeSmoothingFilter = value;
        }

        public XRSDKFaceExpressionSettings()
        {
            var length = EnumValues<MRFaceExpression>.Values.Length;

            if (m_Thresholds == null)
                m_Thresholds = new float[length];
        }
    }
}
#endif
