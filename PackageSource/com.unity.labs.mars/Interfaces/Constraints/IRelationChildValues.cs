using System;
using System.Collections.Generic;
using Unity.Labs.MARS.Interfaces;
using UnityEditor;
using UnityEngine;

namespace Unity.Labs.MARS.Query
{
    /// <summary>
    /// Used for collecting components with this interface, implement the templated version
    /// </summary>
    public interface IRelationChildValues { }

    public interface IRelationChildValues<T1> : IRelationChildValues
        where T1: struct
    {
        T1 Trait1 { get; set; }
    }

    public interface IRelationChildValues<T1, T2> : IRelationChildValues
        where T1: struct
        where T2: struct
    {
        T1 Trait1 { get; set; }
        T2 Trait2 { get; set; }
    }

    public interface IRelationChildValues<T1, T2, T3> : IRelationChildValues
        where T1: struct
        where T2: struct
        where T3: struct
    {
        T1 Trait1 { get; set; }
        T2 Trait2 { get; set; }
        T3 Trait3 { get; set; }
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    public static class IRelationChildValuesAnalyzer
    {
        static string s_ErrorMessage;

        /// <summary>
        /// Maps types that implement IRelationChildValues to the number of trait names they need to work
        /// </summary>
        public static readonly Dictionary<Type, int> TypeToNumberOfTypeArgs = new Dictionary<Type, int>();

        static IRelationChildValuesAnalyzer()
        {
            TypeToNumberOfTypeArgs.Clear();

            var implementors = new List<Type>();
            typeof(IRelationChildValues).GetImplementationsOfInterface(implementors);

            foreach (var implementor in implementors)
            {
               if(!CheckType(implementor, ref s_ErrorMessage))
                   Debug.LogError(s_ErrorMessage);
            }
        }

        static bool ImplementsTemplatedVersion(Type type)
        {
            foreach (var inter in type.GetInterfaces())
            {
                var args = inter.GetGenericArguments();
                // 1-3 traits are allowed for each relation child
                if (args.Length > 0 && args.Length < 4 && typeof(IRelationChildValues).IsAssignableFrom(inter))
                {
                    TypeToNumberOfTypeArgs[type] = args.Length;
                    return true;
                }
            }

            return false;
        }

        static bool CheckType(Type type, ref string errorMessage)
        {
            if (!type.IsValueType)
            {
                var baseType = type.BaseType;
                errorMessage =
                    $"Types that implement IRelationChildValues must be structs, but {type.Name} is a {baseType?.Name}!";
                return false;
            }

            if (!ImplementsTemplatedVersion(type))
            {
                const string messageStart =
                    "Types that implement IRelationChildValues must also implement one of the typed versions: " +
                    "IRelationChildValues<T>, IRelationChildValues<T1, T2>, or IRelationChildValues<T1, T2, T3>, ";

                errorMessage = $"{messageStart}but {type.Name} does not!";
                return false;
            }

            return true;
        }
    }
#endif
}
