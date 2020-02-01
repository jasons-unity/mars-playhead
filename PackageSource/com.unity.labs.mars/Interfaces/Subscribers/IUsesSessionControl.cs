using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to point cloud features
    /// </summary>
    public interface IUsesSessionControl : IFunctionalitySubscriber<IProvidesSessionControl>
    {
    }

    public static class IUsesSessionControlMethods
    {
        /// <summary>
        /// Check if the session exists, regardless of whether it is running
        /// </summary>
        /// <returns>True if the session exists, false otherwise</returns>
        public static bool SessionExists(this IUsesSessionControl obj)
        {
#if !FI_AUTOFILL
            return obj.provider.SessionExists();
#else
            return default(bool);
#endif
        }

        /// <summary>
        /// Check if the session is running. If the session does not exist, returns false
        /// </summary>
        /// <returns>True if the session exists and is running, false otherwise</returns>
        public static bool SessionRunning(this IUsesSessionControl obj)
        {
#if !FI_AUTOFILL
            return obj.provider.SessionRunning();
#else
            return default(bool);
#endif
        }

        /// <summary>
        /// Check if the session is ready
        /// </summary>
        /// <returns>True if the session is ready, false otherwise</returns>
        public static bool SessionReady(this IUsesSessionControl obj)
        {
#if !FI_AUTOFILL
            return obj.provider.SessionReady();
#else
            return default(bool);
#endif
        }

        /// <summary>
        /// Create a new MR Session. If the session has been created, this does nothing.
        /// </summary>
        public static void CreateSession(this IUsesSessionControl obj)
        {
#if !FI_AUTOFILL
            obj.provider.CreateSession();
#endif
        }

        /// <summary>
        /// Pauses the MR Session. If a session has been paused, this does nothing.
        /// </summary>
        public static void DestroySession(this IUsesSessionControl obj)
        {
#if !FI_AUTOFILL
            obj.provider.DestroySession();
#endif
        }

        /// <summary>
        /// Resets the MR Session. This will trigger removal events
        /// </summary>
        public static void ResetSession(this IUsesSessionControl obj)
        {
#if !FI_AUTOFILL
            obj.provider.ResetSession();
#endif
        }

        /// <summary>
        /// Resumes the MR Session. If a session has not has been paused, this does nothing.
        /// </summary>
        public static void PauseSession(this IUsesSessionControl obj)
        {
#if !FI_AUTOFILL
            obj.provider.PauseSession();
#endif
        }

        /// <summary>
        /// Create a new MR Session. If the session has been created, this does nothing.
        /// </summary>
        public static void ResumeSession(this IUsesSessionControl obj)
        {
#if !FI_AUTOFILL
            obj.provider.ResumeSession();
#endif
        }
    }
}
