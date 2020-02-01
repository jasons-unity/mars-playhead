using Unity.Labs.MARS.Data;

namespace Unity.Labs.MARS.Query
{
    public interface IConditionBase
    {
        /// <summary>
        /// Whether this condition is active or not
        /// </summary>
        bool enabled { get; }
    }

    /// <summary>
    /// Used to filter data in a query
    /// </summary>
    public interface ICondition : IConditionBase, IRequiresTraits
    {
        /// <summary>
        /// What trait this condition is testing against
        /// </summary>
        string traitName { get; }
    }

    /// <summary>
    /// Used to filter data of a specific type in a query
    /// </summary>
    /// <typeparam name="T">The type of data to filter against</typeparam>
    public interface ICondition<T> : ICondition, IRequiresTraits<T>
    {
        /// <summary>
        /// Describe how well a given piece of data matches a condition.
        /// 0 means the match is unacceptable, 1 means a perfect match
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A number from 0 to 1 indicating how well a piece of data watches this condition</returns>
        float RateDataMatch(ref T data);
    }

    /// <summary>
    /// Used to filter data of a specific type in a query
    /// </summary>
    /// <typeparam name="T1">The type of the 1st trait</typeparam>
    /// <typeparam name="T2">The type of the 2nd trait</typeparam>
    public interface ICondition<T1, T2> : IConditionBase
        where T1: struct
        where T2: struct
    {
        string TraitName1 { get; }
        string TraitName2 { get; }

        float RateDataMatch(ref T1 trait1, ref T2 trait2);
    }

    /// <summary>
    /// Used to filter data of a specific type in a query
    /// </summary>
    /// <typeparam name="T1">The type of the 1st trait</typeparam>
    /// <typeparam name="T2">The type of the 2nd trait</typeparam>
    /// <typeparam name="T3">The type of the 3rd trait</typeparam>
    public interface ICondition<T1, T2, T3> : IConditionBase
        where T1: struct
        where T2: struct
        where T3: struct
    {
        string TraitName1 { get; }
        string TraitName2 { get; }
        string TraitName3 { get; }

        float RateDataMatch(ref T1 trait1, ref T2 trait2, ref T3 trait3);
    }

    public static class IConditionGenericMethods
    {
        /// <summary>
        /// Collapses a condition's match rating into a pass/fail.
        /// </summary>
        /// <param name="condition">The condition to evaluate</param>
        /// <param name="value">The data being filtered against</param>
        /// <typeparam name="T">The type of data the condition uses</typeparam>
        /// <returns>True if the given trait data passes the filter function, false otherwise</returns>
        public static bool PassesCondition<T>(this ICondition<T> condition, ref T value)
        {
            return condition.RateDataMatch(ref value) > 0f;
        }
    }
}
