using System.Collections.Generic;
using UnityEngine;

namespace Unity.Labs.MARS
{
    [ExecuteInEditMode]
    public class SimulatedPlaneVisual : MonoBehaviour
    {
        const float k_InteractionColorFactor = 0.5f;
        static readonly Color k_InteractionColor = Color.cyan;

        static int s_ColorNameID;
        static MaterialPropertyBlock s_MatPropBlock;

        List<Renderer> m_Renderers = new List<Renderer>();
        InteractionTarget m_InteractionTarget;
        InteractionTarget.InteractionState m_InteractionState;

        void Awake()
        {
            s_MatPropBlock = new MaterialPropertyBlock();
            s_ColorNameID = Shader.PropertyToID("_Color");
        }

        void OnEnable()
        {
            m_InteractionTarget = GetComponentInChildren<InteractionTarget>();
            if (m_InteractionTarget != null)
                m_InteractionTarget.interactionStateChanged += UpdateInteractionTargetState;

            SetupRenderers();
        }
        void SetupRenderers()
        {
            gameObject.GetComponentsInChildren(m_Renderers);
            ApplyColors();
        }

        void OnDisable()
        {
            if (m_InteractionTarget != null)
                m_InteractionTarget.interactionStateChanged -= UpdateInteractionTargetState;
        }

        void ApplyColors()
        {
            foreach (var renderComponent in m_Renderers)
            {
                if (renderComponent == null)
                    continue;

                var newColor = renderComponent.sharedMaterial.color;
                if (m_InteractionState != InteractionTarget.InteractionState.None)
                {
                    newColor = Color.Lerp(newColor, k_InteractionColor, k_InteractionColorFactor);
                }

                if (renderComponent.sharedMaterial != null)
                {
                    if (s_MatPropBlock == null)
                        s_MatPropBlock = new MaterialPropertyBlock();

                    s_MatPropBlock.SetColor(s_ColorNameID, newColor);
                    renderComponent.SetPropertyBlock(s_MatPropBlock);
                }
            }
        }

        void UpdateInteractionTargetState(InteractionTarget.InteractionState state)
        {
            m_InteractionState = state;
            ApplyColors();
        }
    }
}
