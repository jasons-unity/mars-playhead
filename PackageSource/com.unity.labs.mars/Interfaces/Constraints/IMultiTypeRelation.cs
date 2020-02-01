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
    // TODO - remove this once the base relation interface is refactored to implement this
    public interface IMultiTypeRelation : IRelationBase
    {
        // We would enforce that there is the right number of trait names by
        // checking which variation of IRelationChildValues<T1, T2 ...> is implemented.
        // So the <T1, T2, T3> version should have 3 trait names in the right order.
        string[] Child1TraitNames { get; }
        string[] Child2TraitNames { get; }
    }

    /// <summary>
    /// A constraint between two MR objects that is used to filter data in a query
    /// </summary>
    public interface IRelation<TChild1Values, TChild2Values> : IMultiTypeRelation,
        IRelationRatingMethod<TChild1Values, TChild2Values>
        where TChild1Values : struct, IRelationChildValues
        where TChild2Values : struct, IRelationChildValues
    {
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    static class IMultiTypeRelationAnalyzer
    {
        static string s_ErrorMessage;

        static IMultiTypeRelationAnalyzer()
        {
            var implementors = new List<Type>();
            typeof(IMultiTypeRelation).GetImplementationsOfInterface(implementors);
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
                if(!typeof(IMultiTypeRelation).IsAssignableFrom(inter))
                    continue;

                if(inter.GetGenericArguments().Length == 2)
                    return true;
            }

            return false;
        }

        static bool CheckType(Type type, ref string errorMessage)
        {
            if (!ImplementsTemplatedVersion(type))
            {
                errorMessage = "Types that implement IMultiTypeRelation " +
                    $"must also implement the typed version: IRelation<T1, T2>, but {type.Name} does not!";

                return false;
            }

            return true;
        }
    }

    public static class IRelationMethods
    {
        public static bool ValidateTraitNames<T1, T2>(this IMultiTypeRelation relation)
            where T1 : struct, IRelationChildValues
            where T2 : struct, IRelationChildValues
        {
            var typeCounts = IRelationChildValuesAnalyzer.TypeToNumberOfTypeArgs;
            var found1 = typeCounts.TryGetValue(typeof(T1), out var expectedCount1);
            var found2 = typeCounts.TryGetValue(typeof(T2), out var expectedCount2);
            if (!found1 || !found2)
            {
                Debug.LogWarning($"Didn't find either {typeof(T1).Name} or ${typeof(T2).Name} in type map for relation children");
                return false;
            }

            if (expectedCount1 == 1 && expectedCount2 == 1)
            {
                var type1 = typeof(T1);
                if (type1 == typeof(T2))
                {
                    Debug.LogWarning($"Both children of relation type {relation.GetType()} have 1 trait of type {type1} - " +
                        $"This should implement the regular IRelation<T> instead of IRelation<{type1}, {type1}>");

                    return false;
                }
            }

            var child1Names = relation.Child1TraitNames;
            if (child1Names.Length != expectedCount1)
            {
                Debug.LogError($"Expected ${expectedCount1} trait names, but ${child1Names.Length} were provided for " +
                    $"child 1 of Relation type ${typeof(T1).Name}");

                return false;
            }

            var child2Names = relation.Child2TraitNames;
            if (child2Names.Length != expectedCount2)
            {
                Debug.LogError($"Expected ${expectedCount2} trait names, but ${child2Names.Length} were provided for " +
                    $"child 2 of Relation type ${typeof(T1).Name}");

                return false;
            }

            return true;
        }
    }

#endif
}
