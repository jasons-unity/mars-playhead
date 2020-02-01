using Unity.Labs.MARS.Query;

namespace Unity.Labs.MARS
{
    /// <summary>
    /// Allows a component on a Real World Object to receive callbacks for when a query match has not been found in time
    /// </summary>
    public interface IMatchTimeoutHandler : IAction, ISimulatable
    {
        /// <summary>
        /// Called when no query match has been found in time
        /// </summary>
        /// <param name="queryArgs">The original query associated with this object</param>
        void OnMatchTimeout(QueryArgs queryArgs);
    }
}
