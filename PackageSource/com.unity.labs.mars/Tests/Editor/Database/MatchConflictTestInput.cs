using System.Collections.Generic;
using Unity.Labs.MARS.Query;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Labs.MARS.Data.Tests
{
    public class MatchConflictTestInput
    {
        public List<Exclusivity> exclusivities;
        public List<Dictionary<int, float>> ratings;
        public int[] expectedAssignments;
        public List<int> workingIndices;

        public MatchConflictTestInput(List<Exclusivity> exclusivities, List<Dictionary<int, float>> ratings,
            int[] expectedAssignments)
        {
            this.exclusivities = exclusivities;
            this.ratings = ratings;
            this.expectedAssignments = expectedAssignments;
            workingIndices = FakeWorkingIndices(exclusivities.Count);
        }

        static List<int> FakeWorkingIndices(int length, HashSet<int> inactiveIndices = null)
        {
            var indices = new List<int>();
            if (inactiveIndices != null)
            {
                for (var i = 0; i < length; i++)
                {
                    if (inactiveIndices.Contains(i))
                        continue;

                    indices.Add(i);
                }
            }
            else
            {
                for (var i = 0; i < length; i++)
                {
                    indices.Add(i);
                }
            }

            return indices;
        }
    }
}
