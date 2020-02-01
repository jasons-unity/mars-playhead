﻿namespace Unity.Labs.ModuleLoader.Example
{
    /// <summary>
    /// Provides access to Example2 functionality
    /// </summary>
    /// <typeparam name="T">The type returned by ExampleGetter</typeparam>
    public interface ISubscriberExample2<T> : IFunctionalitySubscriber<IProviderExample2<T>>
    {
    }

    /// <summary>
    /// Extension methods for users of Example2 functionality
    /// </summary>
    public static class SubscriberExample2Methods
    {
        /// <summary>
        /// An example method
        /// </summary>
        /// <param name="user">The functionality subscriber whose provider will be used</param>
        /// <param name="int1">Example argument</param>
        /// <param name="int2">Example argument</param>
        /// <returns>An int value</returns>
        public static int ExampleMethod<T>(this ISubscriberExample2<T> user, int int1, int int2)
        {
#if FI_AUTOFILL
            return default(int);
#else
            return user.provider.ExampleMethod(int1, int2);
#endif
        }

        /// <summary>
        /// An example method following the TryGet pattern
        /// </summary>
        /// <param name="user">The functionality subscriber whose provider will be used</param>
        /// <param name="input">Example input data</param>
        /// <param name="output">Example out parameter</param>
        /// <returns>True if the operation succeeded</returns>
        public static bool TryGetMethod<T>(this ISubscriberExample2<T> user, int input, out int output)
        {
#if FI_AUTOFILL
            output = default(int);
            return default(bool);
#else
            return user.provider.TryGetMethod(input, out output);
#endif
        }

        /// <summary>
        /// An example getter
        /// </summary>
        /// <param name="user">The functionality subscriber whose provider will be used</param>
        /// <returns>An object of type T</returns>
        public static T ExampleGetter<T>(this ISubscriberExample2<T> user)
        {
#if FI_AUTOFILL
            return default(T);
#else
            return user.provider.ExampleGetter();
#endif
        }

        /// <summary>
        /// Get the value of an example property
        /// </summary>
        /// <param name="obj">The functionality subscriber whose provider will be used</param>
        /// <typeparam name="T">The type argument of the implementing user</typeparam>
        /// <returns>The value of the property</returns>
        public static int GetPropExampleProperty<T>(this ISubscriberExample2<T> obj)
        {
#if FI_AUTOFILL
            return default(int);
#else
            return obj.provider.ExampleProperty;
#endif
        }

        /// <summary>
        /// Set the value of an example property
        /// </summary>
        /// <param name="obj">The functionality subscriber whose provider will be used</param>
        /// <param name="value">The value to set</param>
        /// <typeparam name="T">The type argument of the implementing user</typeparam>
        public static void SetPropExampleProperty<T>(this ISubscriberExample2<T> obj, int value)
        {
#if !FI_AUTOFILL
            obj.provider.ExampleProperty = value;
#endif
        }
    }
}
