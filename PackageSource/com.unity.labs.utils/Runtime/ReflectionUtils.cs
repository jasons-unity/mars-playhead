﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Unity.Labs.Utils
{
    /// <summary>
    /// Utility methods for common reflection-based operations
    /// </summary>
    public static class ReflectionUtils
    {
        static Assembly[] s_Assemblies;
        static List<Type[]> s_TypesPerAssembly;
        static List<Dictionary<string, Type>> s_AssemblyTypeMaps;

        static Assembly[] GetCachedAssemblies()
        {
            if (s_Assemblies == null)
                s_Assemblies = AppDomain.CurrentDomain.GetAssemblies();

            return s_Assemblies;
        }

        static List<Type[]> GetCachedTypesPerAssembly()
        {
            if (s_TypesPerAssembly == null)
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                s_TypesPerAssembly = new List<Type[]>(assemblies.Length);
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        s_TypesPerAssembly.Add(assembly.GetTypes());
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip any assemblies that don't load properly -- suppress errors
                    }
                }
            }

            return s_TypesPerAssembly;
        }

        static List<Dictionary<string, Type>> GetCachedAssemblyTypeMaps()
        {
            if (s_AssemblyTypeMaps == null)
            {
                var typesPerAssembly = GetCachedTypesPerAssembly();
                s_AssemblyTypeMaps = new List<Dictionary<string, Type>>(typesPerAssembly.Count);
                foreach (var types in typesPerAssembly)
                {
                    try
                    {
                        var typeMap = new Dictionary<string, Type>();
                        foreach (var type in types)
                        {
                            typeMap[type.FullName] = type;
                        }

                        s_AssemblyTypeMaps.Add(typeMap);
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Skip any assemblies that don't load properly -- suppress errors
                    }
                }
            }

            return s_AssemblyTypeMaps;
        }

        public static void PreWarmTypeCache() { GetCachedAssemblyTypeMaps(); }

        /// <summary>
        /// Iterate through all assemblies and execute a method on each one
        /// Catches ReflectionTypeLoadExceptions in each iteration of the loop
        /// </summary>
        /// <param name="callback">The callback method to execute for each assembly</param>
        public static void ForEachAssembly(Action<Assembly> callback)
        {
            var assemblies = GetCachedAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    callback(assembly);
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip any assemblies that don't load properly -- suppress errors
                }
            }
        }

        /// <summary>
        /// Execute a callback for each type in every assembly
        /// </summary>
        /// <param name="callback">The callback to execute</param>
        public static void ForEachType(Action<Type> callback)
        {
            var typesPerAssembly = GetCachedTypesPerAssembly();
            foreach (var types in typesPerAssembly)
            {
                foreach (var type in types)
                {
                    callback(type);
                }
            }
        }

        /// <summary>
        /// Search all assemblies for a type that matches a given predicate delegate
        /// </summary>
        /// <param name="predicate">The predicate; Returns true for the type that matches the search</param>
        /// <returns>The type found, or null if no matching type exists</returns>
        public static Type FindType(Func<Type, bool> predicate)
        {
            var typesPerAssembly = GetCachedTypesPerAssembly();
            foreach (var types in typesPerAssembly)
            {
                foreach (var type in types)
                {
                    if (predicate(type))
                        return type;
                }
            }

            return null;
        }

        /// <summary>
        /// Search all assemblies for a set of types that matches a set of predicates
        /// </summary>
        /// <param name="predicates">The predicates; Returns true for the type that matches each search</param>
        /// <param name="resultList">The list to which found types will be added</param>
        public static void FindTypesBatch(List<Func<Type, bool>> predicates, List<Type> resultList)
        {
            var typesPerAssembly = GetCachedTypesPerAssembly();
            for (var i = 0; i < predicates.Count; i++)
            {
                var predicate = predicates[i];
                foreach (var assemblyTypes in typesPerAssembly)
                {
                    foreach (var type in assemblyTypes)
                    {
                        if (predicate(type))
                            resultList[i] = type;
                    }
                }
            }
        }

        /// <summary>
        /// Search all assemblies for types that match a set of full names
        /// </summary>
        /// <param name="typeNames">A list containing the names of the types to find</param>
        /// <param name="resultList">An empty list which will be used to collect matching types</param>
        public static void FindTypesByFullNameBatch(List<string> typeNames, List<Type> resultList)
        {
            var assemblyTypeMap = GetCachedAssemblyTypeMaps();
            foreach (var typeName in typeNames)
            {
                var found = false;
                foreach (var typeMap in assemblyTypeMap)
                {
                    Type type;
                    if (typeMap.TryGetValue(typeName, out type))
                    {
                        resultList.Add(type);
                        found = true;
                        break;
                    }
                }

                // If a type can't be found, add a null entry to the list to ensure indexes match
                if (!found)
                    resultList.Add(null);
            }
        }
    }
}
