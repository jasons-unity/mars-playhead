using System;
using Unity.Labs.Utils;
using UnityEngine;

namespace Unity.Labs.MARS
{
    static class MarkerConstants
    {
        public static readonly string[] MarkerSizeOptions = {"Custom", "Postcard", "A4 Paper", "Poster"};
        public static readonly Vector2[] MarkerSizeOptionsValuesInMeters = { new Vector2(1, 1),
            new Vector2(k_PostcardWidthInMeters, k_PostcardHeightInMeters),
            new Vector2(k_A4PaperWidthInMeters,k_A4PaperHeightInMeters),
            new Vector2(k_PosterWidthInMeters, k_PosterHeightInMeters) };

        const float k_PostcardWidthInMeters = 0.148f;
        const float k_PostcardHeightInMeters = 0.105f;
        public static float PostcardWidthInMeters { get => k_PostcardWidthInMeters; }
        public static float PostcardHeightInMeters { get => k_PostcardHeightInMeters; }


        const float k_A4PaperWidthInMeters = 0.210f;
        const float k_A4PaperHeightInMeters = 0.297f;
        public static float A4PaperWidthInMeters { get => k_A4PaperWidthInMeters; }
        public static float A4PaperHeightInMeters { get => k_A4PaperHeightInMeters; }


        const float k_PosterWidthInMeters = 0.841f;
        const float k_PosterHeightInMeters = 1.189f;
        public static float PosterWidthInMeters { get => k_PosterWidthInMeters; }
        public static float PosterHeightInMeters { get => k_PosterHeightInMeters; }

        const float k_MinimumPhysicalMarkerWidthInMeters = 0.01f;
        const float k_MinimumPhysicalMarkerHeightInMeters = 0.01f;
        public static float MinimumPhysicalMarkerSizeWidthInMeters { get => k_MinimumPhysicalMarkerWidthInMeters; }
        public static float MinimumPhysicalMarkerSizeHeightInMeters { get => k_MinimumPhysicalMarkerHeightInMeters; }

        public static Vector2 MinimumPhysicalMarkerSizeInCentimeters => new Vector2(k_MinimumPhysicalMarkerWidthInMeters*100, k_MinimumPhysicalMarkerHeightInMeters*100);
    }

    [Serializable]
    public struct MarsMarkerDefinition : IEquatable<MarsMarkerDefinition>
    {
#pragma warning disable 649
        [SerializeField]
        bool m_SpecifySize;

        [SerializeField]
        Vector2 m_Size;

        [SerializeField]
        string m_Label;

        [SerializeField]
        Texture2D m_Texture;

        [SerializeField]
        SerializableGuid m_MarkerDefinitionId;
#pragma warning restore 649

        internal SerializableGuid MarkerDefinitionId { set => m_MarkerDefinitionId = value; }

        /// <summary>
        /// The <c>Guid</c> associated with this marker. The guid is generated for each new marker definition created.
        /// </summary>
        public Guid MarkerId { get => m_MarkerDefinitionId.guid; }

        /// <summary>
        /// The size of the marker image, in meters. This can improve marker detection,
        /// and may be required by some platforms.
        /// </summary>
        public Vector2 Size
        {
            get => m_Size;
            set => m_Size = value;
        }

        /// <summary>
        /// The source texture whose image this marker represents.
        /// </summary>
        public Texture2D Texture
        {
            get => m_Texture;
            set => m_Texture = value;
        }

        /// <summary>
        /// An optional label associated with this marker, for a user to identify a particular marker from script in
        /// the case of a condition that matches multiple images.
        /// </summary>
        public string Label
        {
            get => m_Label;
            set => m_Label = value;
        }

        /// <summary>
        /// Must be set to true for <see cref="Size"/> to be used.
        /// </summary>
        public bool SpecifySize
        {
            get => m_SpecifySize;
            internal set => m_SpecifySize = value;
        }

        public override bool Equals(object obj)
        {
            return obj is MarsMarkerDefinition other && Equals(other);
        }

        public bool Equals(MarsMarkerDefinition other)
        {
            return MarkerId == other.MarkerId;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_Size.GetHashCode();
                hashCode = (hashCode * 397) ^ (m_Label != null ? m_Label.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (m_Texture != null ? m_Texture.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}
