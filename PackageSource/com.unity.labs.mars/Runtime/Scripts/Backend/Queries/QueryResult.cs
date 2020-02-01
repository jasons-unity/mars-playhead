using System;
using Unity.Labs.MARS.Data;

namespace Unity.Labs.MARS.Query
{
    /// <summary>
    /// Data returned from a particular query
    /// </summary>
    public partial class QueryResult : IEquatable<QueryResult>
    {
        public QueryMatchID queryMatchId;

        // keep this private, only allow data lookup via IUses/ProvidesMARSData functionality
        int m_DataId;

        public QueryResult() { }

        public QueryResult(QueryMatchID id)
        {
            queryMatchId = id;
        }

        public void Reset()
        {
            m_DataId = -1;
            queryMatchId = QueryMatchID.NullQuery;
            Clear(this);
        }

        internal void SetDataId(int id)
        {
            m_DataId = id;
        }

        /// <summary>
        /// Gets the value for a particular type of data for a given data id
        /// </summary>
        /// <typeparam name="T">The type of data to return</typeparam>
        /// <param name="dataUser">The functionality subscriber that will actually do the data lookup</param>
        /// <returns>The typed value for the given data id</returns>
        public T ResolveValue<T>(IUsesMARSData<T> dataUser)
        {
            return dataUser.GetIdValue(m_DataId);
        }

        /// <summary>
        /// Gets the value for a particular type of data for a given data id
        /// </summary>
        /// <typeparam name="T">The type of data to return</typeparam>
        /// <param name="dataUser">The functionality subscriber that will actually do the data lookup</param>
        /// <returns>The typed value for the given data id</returns>
        public T ResolveValue<T>(IUsesMARSTrackableData<T> dataUser)
            where T: IMRTrackable
        {
            return dataUser.GetIdValue(m_DataId);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (queryMatchId.GetHashCode() * 397) ^ m_DataId;
            }
        }

        public bool Equals(QueryResult other)
        {
            if (ReferenceEquals(null, other))
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return queryMatchId.Equals(other.queryMatchId) && m_DataId == other.m_DataId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;

            return obj.GetType() == typeof(QueryResult) && Equals((QueryResult) obj);
        }

        public static bool operator ==(QueryResult left, QueryResult right) { return Equals(left, right); }
        public static bool operator !=(QueryResult left, QueryResult right) { return !Equals(left, right); }

        // These methods should be unused once code generation runs
        // ReSharper disable UnusedMember.Global
        [Obsolete("This method exists in order for MARS to compile before type-specific code is generated. Use the type-specific version of this method")]
        public bool TryGetTrait(string traitName, out object value)
        {
            value = default;
            return false;
        }

        public bool TryGetTrait<T>(string traitName, out T value)
            where T: struct
        {
            value = default;
            return false;
        }

        public void SetTrait(string traitName, object value) { }
        // ReSharper restore UnusedMember.Global

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        static void Clear(object result) { }
    }
}
