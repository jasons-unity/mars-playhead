namespace Unity.Labs.MARS.Query
{
    /// <summary>
    /// A class that implements IUsesSetQueryResults gains the ability to register with the MARS backend for different events
    /// relating to relations between real world data
    /// </summary>
    public interface IUsesSetQueryResults : IUsesMarsSceneEvaluation { }

    public delegate QueryMatchID RegisterSetQueryDelegate(SetQueryArgs queryArgs);
    public delegate void RegisterOverrideSetQueryDelegate(QueryMatchID queryMatchID, SetQueryArgs queryArgs);
    public delegate bool UnregisterSetQueryDelegate(QueryMatchID eventMatchId, bool allMatches = false);

    public static class ISetQueryResultsMethods
    {
        internal static RegisterSetQueryDelegate RegisterSetQuery;
        internal static RegisterOverrideSetQueryDelegate RegisterSetOverrideQuery;
        internal static UnregisterSetQueryDelegate UnregisterSetQuery;
    }

    public static class IUsesSetQueryResultsExtensionMethods
    {
        /// <summary>
        /// Called to get set event(s) from the MARS backend
        /// </summary>
        /// <param name="caller">The object making the query</param>
        /// <param name="queryArgs">The different specified data requirements we are querying for</param>
        /// <returns>A ID that identifies this series of queries</returns>
        public static QueryMatchID RegisterSetQuery(this IUsesSetQueryResults caller, SetQueryArgs queryArgs)
        {
            return ISetQueryResultsMethods.RegisterSetQuery(queryArgs);
        }

        /// <summary>
        /// Called to get set event(s) from the MARS backend
        /// Allows a user to specify a custom query ID to use - make sure it is unique!
        /// </summary>
        /// <param name="caller">The object making the query</param>
        /// <param name="queryMatchID">The identifier to use for this query</param>
        /// <param name="queryArgs">The different specified data requirements we are querying for</param>
        public static void RegisterSetQuery(this IUsesSetQueryResults caller, QueryMatchID queryMatchID, SetQueryArgs queryArgs)
        {
            ISetQueryResultsMethods.RegisterSetOverrideQuery(queryMatchID, queryArgs);
        }

        /// <summary>
        /// Notifies the MARS backend that a particular set query is no longer needed.
        /// This function is not required if the Registration was oneShot - the query will be unregistered automatically
        /// </summary>
        /// <param name="caller">The object that had made a query</param>
        /// <param name="eventMatchId">The identifier of the query</param>
        /// <param name="allMatches">Whether to unregister all matches referring to the same query as <paramref name="eventMatchId"/></param>
        /// <returns>True if the query was stopped, false if the query was not currently running</returns>
        public static bool UnregisterSetQuery(this IUsesSetQueryResults caller, QueryMatchID eventMatchId, bool allMatches = false)
        {
            return ISetQueryResultsMethods.UnregisterSetQuery(eventMatchId, allMatches);
        }
    }
}
