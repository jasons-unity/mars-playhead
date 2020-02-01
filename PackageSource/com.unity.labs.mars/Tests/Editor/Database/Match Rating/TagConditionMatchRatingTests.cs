#if UNITY_EDITOR
using UnityEngine;
using NUnit.Framework;

namespace Unity.Labs.MARS.Data.Tests.ConditionMatching
{
    public class TagConditionMatchRatingTests
    {
        SemanticTagCondition m_ExclusiveCondition;
        SemanticTagCondition m_InclusiveCondition;

        GameObject m_TagsObject;

        [OneTimeSetUp]
        public void Setup()
        {
            m_TagsObject = new GameObject("inclusive tags");
            m_InclusiveCondition = m_TagsObject.AddComponent<SemanticTagCondition>();
            m_ExclusiveCondition = m_TagsObject.AddComponent<SemanticTagCondition>();
            m_ExclusiveCondition.matchRule = SemanticTagMatchRule.Exclude;
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(m_TagsObject);
        }

        [TestCase(true, 1f)]        // return 1 for match
        [TestCase(false, 0f)]       // return 0 for no match
        public void InclusiveTagsMatching(bool input, float expected)
        {
            Assert.AreEqual(m_InclusiveCondition.RateDataMatch(ref input), expected);
        }

        // return 0 for no match, since exclusive tags actually specify what is to be excluded
        [TestCase(false, 0f)]
        [TestCase(true, 1f)]       // return 1 for match
        public void ExclusiveTagsMatching(bool input, float expected)
        {
            Assert.AreEqual(m_ExclusiveCondition.RateDataMatch(ref input), expected);
        }
    }
}
#endif
