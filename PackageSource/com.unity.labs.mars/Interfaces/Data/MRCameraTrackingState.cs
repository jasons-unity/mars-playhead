namespace Unity.Labs.MARS
{
    /// <summary>
    /// Enumerates the types of MR CameraTracking
    /// </summary>
    public enum MRCameraTrackingState
    {
        /// <summary>
        /// NotAvailable is needed to differentiate app start from limited tracking
        /// </summary>
        NotAvailable,

        /// <summary>
        /// 3dof-only fallback when AR tracking is not available
        /// </summary>
        Limited,

        /// <summary>
        /// 6dof tracking provided by AR SDKs
        /// </summary>
        Normal
    }
}
