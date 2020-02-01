#if UNITY_EDITOR
using UnityEngine;

namespace Unity.Labs.MARS.Data.Tests
{
    public class TagConditionRatingPerformance : TimedConditionPerformanceTest<SemanticTagCondition, bool>
    {
        public virtual SemanticTagMatchRule matchRule { get; set; }
        
        public void Start()
        {
            m_Condition.matchRule = matchRule;
            for (int i = 0; i < s_DataCount; i++)
            {
                m_DataToCompare[i] = Random.Range(0f, 1f) > 0.5f;
            }
        }
    }
    
    public class TagConditionInclusiveMatchRatingPerformance : TagConditionRatingPerformance
    {
        public override SemanticTagMatchRule matchRule { get { return SemanticTagMatchRule.Match; } }
    }
    
    public class TagConditionExclusiveMatchRatingPerformance : TagConditionRatingPerformance
    {
        public override SemanticTagMatchRule matchRule { get { return SemanticTagMatchRule.Exclude; } }
    }
}
#endif
