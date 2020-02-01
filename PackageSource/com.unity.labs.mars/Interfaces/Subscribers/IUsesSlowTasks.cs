using System;
using Unity.Labs.ModuleLoader;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Provides access to slow task management
    /// </summary>
    public interface IUsesSlowTasks : IFunctionalitySubscriber<IProvidesSlowTasks>
    {
    }

    public static class IUsesSlowTasksMethods
    {
        /// <summary>
        /// Registers the given task and starts it running at regular intervals based on game time
        /// </summary>
        /// <param name="task">The delegate to execute at each interval</param>
        /// <param name="sleepTime">The amount of time to wait between executions</param>
        /// <param name="replace">(Optional) Whether this should replace existing parameters
        /// for <paramref name="task"/> if it has already been registered</param>
        /// <returns>True if the task has not already been added or if <paramref name="replace"/> is true, false otherwise</returns>
        public static bool AddSlowTask(this IUsesSlowTasks obj, Action task, float sleepTime, bool replace = false)
        {
#if !FI_AUTOFILL
            return obj.provider.AddSlowTask(task, sleepTime, replace);
#else
            return default(bool);
#endif
        }

        /// <summary>
        /// Unregisters the given task and stops running it
        /// </summary>
        /// <param name="task">The task to remove</param>
        /// <returns>True if the task was successfully found and removed, false otherwise</returns>
        public static bool RemoveSlowTask(this IUsesSlowTasks obj, Action task)
        {
#if !FI_AUTOFILL
            return obj.provider.RemoveSlowTask(task);
#else
            return default(bool);
#endif
        }

        /// <summary>
        /// Registers the given task and starts it running at regular intervals based on MARS time
        /// </summary>
        /// <param name="task">The delegate to execute at each interval</param>
        /// <param name="sleepTime">The amount of time to wait between executions</param>
        /// <param name="replace">(Optional) Whether this should replace existing parameters
        /// for <paramref name="task"/> if it has already been registered</param>
        /// <returns>True if the task has not already been added or if <paramref name="replace"/> is true, false otherwise</returns>
        public static bool AddMarsTimeSlowTask(this IUsesSlowTasks obj, Action task, float sleepTime, bool replace = false)
        {
#if !FI_AUTOFILL
            return obj.provider.AddMarsTimeSlowTask(task, sleepTime, replace);
#else
            return default;
#endif
        }

        /// <summary>
        /// Unregisters the given MARS-time task and stops running it
        /// </summary>
        /// <param name="task">The task to remove</param>
        /// <returns>True if the task was successfully found and removed, false otherwise</returns>
        public static bool RemoveMarsTimeSlowTask(this IUsesSlowTasks obj, Action task)
        {
#if !FI_AUTOFILL
            return obj.provider.RemoveMarsTimeSlowTask(task);
#else
            return default;
#endif
        }
    }
}
