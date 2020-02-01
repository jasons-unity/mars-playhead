using System;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to frame-rate independent time information for the MARS lifecycle
    /// </summary>
    public static class MarsTime
    {
        /// <summary>
        /// The time the latest <see cref="MarsUpdate"/> has started. This is the time in seconds since the start of the
        /// MARS lifecycle. This scales with <see cref="UnityEngine.Time.timeScale"/>.
        /// </summary>
        public static float Time { get; internal set; }

        /// <summary>
        /// The fixed interval in seconds at which <see cref="MarsUpdate"/>s are performed. This is not affected by
        /// <see cref="UnityEngine.Time.timeScale"/>.
        /// </summary>
        public static float TimeStep { get; internal set; }

        /// <summary>
        /// The total number of <see cref="MarsUpdate"/>s that have occurred
        /// </summary>
        public static int FrameCount { get; internal set; }

        /// <summary>
        /// Called every <see cref="TimeStep"/> seconds while the MARS lifecycle is running. The frequency of MARS updates
        /// per player loop update scales with <see cref="UnityEngine.Time.timeScale"/>.
        /// </summary>
        public static event Action MarsUpdate;

        internal static void InvokeMarsUpdate() { MarsUpdate?.Invoke(); }
    }
}
