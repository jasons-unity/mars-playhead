using Unity.Collections;

namespace Unity.Labs.Utils
{
    /// <summary>
    /// Utility methods for NativeArray&lt;T&gt;
    /// </summary>
    public static class NativeArrayUtils
    {
        public static void EnsureCapacity<T>(ref NativeArray<T> array, int capacity, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory) where T : struct
        {
            if (array.Length < capacity)
            {
                array.Dispose();
                array = new NativeArray<T>(capacity, allocator, options);
            }
        }
    }
}
