using System.Collections.Generic;

namespace Unity.Labs.MARS
{
    public enum MRFaceExpression
    {
        LeftEyeClose,
        RightEyeClose,
        LeftEyebrowRaise,
        RightEyebrowRaise,
        BothEyebrowsRaise,
        LeftLipCornerRaise,
        RightLipCornerRaise,
        Smile,
        MouthOpen
    }

    public class MRFaceExpressionComparer : IEqualityComparer<MRFaceExpression>
    {
        public bool Equals(MRFaceExpression x, MRFaceExpression y)
        {
            return x == y;
        }

        public int GetHashCode(MRFaceExpression obj)
        {
            return (int)obj;
        }
    }
}
