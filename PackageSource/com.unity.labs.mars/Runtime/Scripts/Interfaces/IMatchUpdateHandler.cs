using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Allows a component on a Real World Object to receive callbacks for when a query match's data is updated
    /// </summary>
    public interface IMatchUpdateHandler : IAction, ISimulatable
    {
        /// <summary>
        /// Called when a query match's data has updated
        /// </summary>
        /// <param name="queryResult">Data associated with this event</param>
        void OnMatchUpdate(QueryResult queryResult);
    }
}
