using System;
using UnityEngine;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Represents a situation that depends on the existence of a specific marker
    /// </summary>
    [DisallowMultipleComponent]
    [ComponentTooltip("Requires the object to be an image marker with a particular Guid.")]
    [MonoBehaviourComponentMenu(typeof(MarkerCondition), "Condition/Image Marker")]
    public class MarkerCondition : Condition<string>
    {
        static readonly TraitRequirement[] k_RequiredTraits = { TraitDefinitions.MarkerId };

#pragma warning disable 649
        [SerializeField]
        string m_MarkerGuid;
#pragma warning restore 649
        
        public string MarkerGuid { get { return m_MarkerGuid; } }

        public override TraitRequirement[] GetRequiredTraits() { return k_RequiredTraits; }

        public override float RateDataMatch(ref string data)
        {
            return m_MarkerGuid.Equals(data) ? 1.0f : 0.0f;
        }

#if UNITY_EDITOR
        public override void OnValidate()
        {
            base.OnValidate();
            var scene = gameObject.scene;
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            var session = EditorOnlyDelegates.GetMARSSession(scene);
            if (session == null)
                return;

            ValidateMarkerGuid(session);
        }

        internal void ValidateMarkerGuid(MARSSession session)
        {
            if (!string.IsNullOrEmpty(m_MarkerGuid))
                return;

            var markerLibrary = session.MarkerLibrary;
            if (markerLibrary == null)
                return;

            if (markerLibrary.Count == 0)
                return;

            m_MarkerGuid = markerLibrary[0].MarkerId.ToString();
        }
#endif
    }
}
