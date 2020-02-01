using System.Collections.Generic;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Enumerates the types of tracked body landmarks
    /// </summary>
    public enum MRBodyLandmark
    {
        Head,
        Body
    }

    public class MRBodyLandmarkComparer : IEqualityComparer<MRBodyLandmark>
    {
        public bool Equals(MRBodyLandmark x, MRBodyLandmark y)
        {
            return x == y;
        }

        public int GetHashCode(MRBodyLandmark obj)
        {
            return (int)obj;
        }
    }
}
