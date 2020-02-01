namespace Unity.Labs.MARS
{
    public interface ICreatesRelations : ICreatesConditionsBase
    {
        /// <summary>
        /// Creates a new relation and adds it to the set's game object.
        /// This also adds a new child object to which <paramref name="primaryChild"/> is related.
        /// </summary>
        /// <param name="set">The set to which the relation should be added</param>
        /// <param name="primaryChild">The preexisting child object of <paramref name="set"/>.
        /// A new child object is created in relation to this object.</param>
        void CreateIdealRelation(ProxyGroup set, Proxy primaryChild);
    }
}
