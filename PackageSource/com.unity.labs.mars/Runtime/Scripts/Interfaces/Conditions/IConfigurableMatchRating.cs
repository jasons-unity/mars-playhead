namespace Unity.Labs.MARS
{
    public interface IConfigurableMatchRating
    {
        RatingConfiguration ratingConfig { get; set; }

        /// <summary>
        /// Callback that runs after the rating configuration is set by code (not in the inspector)
        /// </summary>
        void OnRatingConfigChange();
    }
}
