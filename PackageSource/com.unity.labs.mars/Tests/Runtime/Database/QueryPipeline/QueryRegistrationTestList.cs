#if UNITY_EDITOR
using System.Collections;
using UnityEngine.TestTools;

namespace Unity.Labs.MARS.Data.Tests
{
    public class QueryRegistrationTestList
    {
        [UnityTest]
        public IEnumerator QueryRegistration()
        {
            yield return new MonoBehaviourTest<QueryRegistrationTest>();
        }
    }
}
#endif
