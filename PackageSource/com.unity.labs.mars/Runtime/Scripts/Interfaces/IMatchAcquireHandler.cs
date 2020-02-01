using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Allows a component on a Real World Object to receive callbacks for when a query match is found
    /// </summary>
    public interface IMatchAcquireHandler : IAction, ISimulatable
    {
        /// <summary>
        /// Called when a query match has been found
        /// </summary>
        /// <param name="queryResult">Data associated with this event</param>
        void OnMatchAcquire(QueryResult queryResult);
    }
}
