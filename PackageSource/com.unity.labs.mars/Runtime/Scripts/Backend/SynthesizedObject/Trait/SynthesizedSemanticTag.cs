using System;
using Unity.Labs.MARS.Query;
using UnityEngine;

namespace Unity.Labs.MARS.Data
{
    /// <summary>
    /// Creates data for a semantic tag trait
    /// When added to a synthesized object, adds a semantic tag to its representation in the database
    /// </summary>
    public class SynthesizedSemanticTag : SynthesizedTrait<bool>, ICreatesConditions
    {
        [SerializeField]
        [Tooltip("The semantic tag to apply to the Synthesized Object")]
        string m_SemanticTag;

        void OnValidate()
        {
            if (m_SemanticTag != null)
            {
                m_SemanticTag = m_SemanticTag.ToLower();
            }
        }

        public override string TraitName { get { return m_SemanticTag; } }

        public string ConditionName { get { return "Semantic Tag"; } }
        public Type ConditionType { get { return typeof(SemanticTagCondition); } }

        public string ValueString { get { return string.Format("\"{0}\"", m_SemanticTag); } }
        public int Order { get { return int.MaxValue / 2; } }

        public override bool UpdateWithTransform { get { return false; } }

        /// <summary>
        /// Adds conditions to a gameobject to specify this semantic tag
        /// </summary>
        /// <param name="go"> The gameobject to add the condition to </param>
        public void CreateIdealConditions(GameObject go)
        {
            var condition = go.AddComponent<SemanticTagCondition>();
            condition.SetTraitName(m_SemanticTag);
        }

        public void ConformCondition(ICondition condition)
        {
            var tagCondition = condition as SemanticTagCondition;
            if (tagCondition == null)
                return;

            tagCondition.matchRule = SemanticTagMatchRule.Match;
        }

        public override bool GetTraitData() { return true; }
    }
}
