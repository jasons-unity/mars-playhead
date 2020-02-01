using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.Labs.MARS.UseCaseContent
{
    public class ConditionsForTraitsTestBehavior : MonoBehaviour
    {
#pragma warning disable 649
        [SerializeField]
        InputField m_TraitInput;

        [SerializeField]
        Button m_ConditionsButton;

        [SerializeField]
        Button m_RelationsChild1Button;

        [SerializeField]
        Button m_RelationsChild2Button;

        [SerializeField]
        Text m_ResultsText;
#pragma warning restore 649

        readonly List<Type> m_Types = new List<Type>();

        void OnEnable()
        {
            m_ConditionsButton.onClick.AddListener(FindConditions);
            m_RelationsChild1Button.onClick.AddListener(FindRelationsChild1);
            m_RelationsChild2Button.onClick.AddListener(FindRelationsChild2);
        }

        void OnDisable()
        {
            m_ConditionsButton.onClick.RemoveListener(FindConditions);
            m_RelationsChild1Button.onClick.RemoveListener(FindRelationsChild1);
            m_RelationsChild2Button.onClick.RemoveListener(FindRelationsChild2);
        }

        void FindConditions()
        {
            var traitName = m_TraitInput.text;
            m_Types.Clear();
            if (!ConditionsAnalyzer.GetConditionTypesForTrait(traitName, m_Types))
            {
                m_ResultsText.text = "None";
                return;
            }

            UpdateResultsText();
        }

        void FindRelationsChild1()
        {
            var traitName = m_TraitInput.text;
            m_Types.Clear();
            if (!ConditionsAnalyzer.GetRelationTypesForChild1Trait(traitName, m_Types))
            {
                m_ResultsText.text = "None";
                return;
            }

            UpdateResultsText();
        }

        void FindRelationsChild2()
        {
            var traitName = m_TraitInput.text;
            m_Types.Clear();
            if (!ConditionsAnalyzer.GetRelationTypesForChild2Trait(traitName, m_Types))
            {
                m_ResultsText.text = "None";
                return;
            }

            UpdateResultsText();
        }

        void UpdateResultsText()
        {
            m_ResultsText.text = "";
            foreach (var type in m_Types)
            {
                m_ResultsText.text += type.Name + "\n";
            }
        }
    }
}
