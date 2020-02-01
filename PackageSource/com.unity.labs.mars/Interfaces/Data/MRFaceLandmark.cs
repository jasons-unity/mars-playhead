using System.Collections.Generic;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Enumerates the types of tracked face landmarks
    /// </summary>
    public enum MRFaceLandmark
    {
        LeftEye,
        RightEye,
        LeftEyebrow,
        RightEyebrow,
        NoseBridge,
        NoseTip,
        Mouth,
        UpperLip,
        LowerLip,
        LeftEar,
        RightEar,
        Chin
    }

    public class MRFaceLandmarkComparer : IEqualityComparer<MRFaceLandmark>
    {
        public bool Equals(MRFaceLandmark x, MRFaceLandmark y)
        {
            return x == y;
        }

        public int GetHashCode(MRFaceLandmark obj)
        {
            return (int)obj;
        }
    }
}
