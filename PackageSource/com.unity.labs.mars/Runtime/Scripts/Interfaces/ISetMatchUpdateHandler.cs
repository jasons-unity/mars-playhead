using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Allows a component on a Set to receive callbacks for when a query match's data is updated
    /// </summary>
    public interface ISetMatchUpdateHandler : IAction, ISimulatable
    {
        /// <summary>
        /// Called when a query match's data has updated
        /// </summary>
        /// <param name="queryResult">Data associated with this event</param>
        void OnSetMatchUpdate(SetQueryResult queryResult);
    }
}
