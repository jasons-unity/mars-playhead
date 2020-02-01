using System.Collections.Generic;

namespace Unity.Labs.Utils
{
    /// <summary>
    /// Extension methods for List&lt;T&gt; objects
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Fill the list with default objects of type <typeparamref name="T"/>
        /// </summary>
        /// <param name="list">The list</param>
        /// <param name="count">The number of items to fill the list with</param>
        /// <typeparam name="T">The type of objects in this list</typeparam>
        /// <returns>The list that was filled</returns>
        public static List<T> Fill<T>(this List<T> list, int count)
            where T: new()
        {
            for (var i = 0; i < count; i++)
            {
                list.Add(new T());
            }

            return list;
        }

        public static void EnsureCapacity<T>(this List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
                list.Capacity = capacity;
        }
    }
}
