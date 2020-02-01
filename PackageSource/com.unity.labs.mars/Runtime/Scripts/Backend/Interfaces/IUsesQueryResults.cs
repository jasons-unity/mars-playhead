namespace Unity.Labs.MARS.Query
{
    /// <summary>
    /// A class that implements IUsesQueryResults gains the ability to register with the MARS backend for different events relating to real world data
    /// </summary>
    public interface IUsesQueryResults : IUsesMarsSceneEvaluation { }

    public delegate QueryMatchID RegisterQueryDelegate(QueryArgs queryArgs);
    public delegate bool UnregisterQueryDelegate(QueryMatchID queryMatchID, bool allMatches = false);

    static class IUsesQueryResultsMethods
    {
        public static RegisterQueryDelegate RegisterQuery { get; internal set; }
        public static UnregisterQueryDelegate UnregisterQuery { get; internal set; }
    }

    public static class IUsesQueryResultsExtensionMethods
    {
        /// <summary>
        /// Registers to get event(s) from the MARS backend
        /// </summary>
        /// <param name="caller">The object making the query</param>
        /// <param name="queryArgs">The different specified data requirements we are querying for</param>
        /// <returns>A ID that identifies this series of queries</returns>
        public static QueryMatchID RegisterQuery(this IUsesQueryResults caller, QueryArgs queryArgs)
        {
            return IUsesQueryResultsMethods.RegisterQuery(queryArgs);
        }

        /// <summary>
        /// Notifies the MARS backend that a particular query is no longer needed.
        /// This function is not required if the Registration was oneShot - the query will be unregistered automatically
        /// </summary>
        /// <param name="caller">The object that had made a query</param>
        /// <param name="queryMatchID">The identifier of the query</param>
        /// <param name="allMatches">Whether to unregister all matches referring to the same query as <paramref name="queryMatchID"/></param>
        /// <returns>True if the query was stopped, false if the query was not currently running</returns>
        public static bool UnregisterQuery(this IUsesQueryResults caller, QueryMatchID queryMatchID, bool allMatches = false)
        {
            return IUsesQueryResultsMethods.UnregisterQuery(queryMatchID, allMatches);
        }
    }
}
