using System;

namespace Unity.Labs.MARS
{
    public interface ICreatesConditionsBase
    {
        /// <summary>
        /// The type of condition that will be added
        /// </summary>
        Type ConditionType { get; }

        /// <summary>
        /// The name of the conditions that will be added
        /// </summary>
        string ConditionName { get; }

        /// <summary>
        /// Returns the value that will be used to create the conditions as a formatted string
        /// </summary>
        string ValueString { get; }

        /// <summary>
        /// Order in which the condition is listed
        /// </summary>
        int Order { get; }
    }
}
