using System.Collections.Generic;

namespace Unity.Labs.MARS.Data
{
    /// <summary>
    /// Data ID that a set child could potentially use,
    /// as well as the valid data assignments for other children if this data is used
    /// </summary>
    public struct SetChildDataCandidate
    {
        public int dataID;

        // this represent what would be the set of valid ids for other children if we used this data id for this child
        public Dictionary<IMRObject, HashSet<int>> otherChildrenValidIDs;
    }
}
