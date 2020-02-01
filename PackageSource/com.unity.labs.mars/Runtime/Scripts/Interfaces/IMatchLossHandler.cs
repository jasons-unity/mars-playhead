using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Allows a component on a Real World Object to receive callbacks for when a query match is lost
    /// </summary>
    public interface IMatchLossHandler : IAction, ISimulatable
    {
        /// <summary>
        /// Called when a query match has been lost
        /// </summary>
        /// <param name="queryResult">Data associated with this event</param>
        void OnMatchLoss(QueryResult queryResult);
    }
}
