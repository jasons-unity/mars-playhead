#if UNITY_EDITOR
using System.Collections;
using Unity.Labs.MARS.Tests;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Labs.MARS.Data.Tests
{
    public class RuntimeQueryTestList
    {
        [UnityTest]
        public IEnumerator NonRequiredChildrenLoss()
        {
            yield return new MonoBehaviourTest<NonRequiredChildrenTest>();
        }
    }
}
#endif
