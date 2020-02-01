using System.Collections.Generic;
using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS.Data
{
    /// <summary>
    /// Contains information about which data is used for each child in a set match.
    /// </summary>
    public class SetMatchData
    {
        public Dictionary<IMRObject, int> dataAssignments;
        public Dictionary<IMRObject, Exclusivity> exclusivities;
    }
}
