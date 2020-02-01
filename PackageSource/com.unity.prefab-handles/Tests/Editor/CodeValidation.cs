using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.PrefabHandles;
using UnityEngine.PrefabHandles.Picking;

namespace UnityEditor.PrefabHandles.Tests
{
    internal sealed class CodeValidation
    {
        static readonly Assembly[] s_AssemblyToValidate =
        {
            Assembly.GetAssembly(typeof(RuntimeHandleContext)), // UnityEngine.PrefabHandles
            Assembly.GetAssembly(typeof(EditorHandleContext)),  // UnityEditor.PrefabHandles
            Assembly.GetAssembly(typeof(IPickingTarget)),       // UnityEngine.PrefabHandles.Picking
        }; 

        static IEnumerable<Type> s_BuiltinInteractiveHandles
        {
            get { return GetInheritingTypes(s_AssemblyToValidate, new [] {typeof(InteractiveHandle)}); }
        }

        static IEnumerable<Type> s_BuiltinPickingTargets
        {
            get { return GetInheritingTypes(s_AssemblyToValidate, new[] { typeof(IPickingTarget)}); }
        }

        static IEnumerable<Type> s_BuiltinHandleBehaviours
        {
            get { return GetInheritingTypes(s_AssemblyToValidate, new[] { typeof(HandleBehaviour) }, new[] { typeof(InteractiveHandle) }); }
        }

        static List<Type> GetInheritingTypes(Assembly[] assemblies, Type[] include, Type[] ignore = null)
        {
            List<Type> results = new List<Type>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    foreach (var includeType in include)
                    {
                        if (includeType.IsAssignableFrom(type))
                        {
                            if (ShouldIgnoreType(type, ignore))
                                break;

                            results.Add(type);
                            break;
                        }
                    }
                    
                }
            }

            return results;
        }

        static bool ShouldIgnoreType(Type type, Type[] ignore)
        {
            if (type.IsAbstract || type.IsInterface)
                return true;

            if (ignore == null)
                return false;

            foreach (var ti in ignore) 
            {
                if (ti.IsAssignableFrom(type))
                    return true;
            }

            return false;
        }

        [Test]
        public void BuiltinInteractiveHandles_InProperAddComponentMenu(
            [ValueSource(nameof(s_BuiltinInteractiveHandles))] Type type)
        {
            var attribute = type.GetCustomAttribute<AddComponentMenu>();
            Assert.IsTrue(attribute != null && attribute.componentMenu.StartsWith(UnityEngine.PrefabHandles.AddComponentMenuNames.interactiveHandles),
                $"The interactive handle {type.FullName} should have the attribute AddComponentMenu(\"{UnityEngine.PrefabHandles.AddComponentMenuNames.interactiveHandles}\"");
        }

        [Test]
        public void BuiltinPickingTargets_InProperAddComponentMenu(
            [ValueSource(nameof(s_BuiltinPickingTargets))] Type type)
        {
            var attribute = type.GetCustomAttribute<AddComponentMenu>();
            Assert.IsTrue(attribute != null && attribute.componentMenu.StartsWith(UnityEngine.PrefabHandles.Picking.AddComponentMenuNames.pickingTargets),
                $"The interactive handle {type.FullName} should have the attribute AddComponentMenu(\"{UnityEngine.PrefabHandles.Picking.AddComponentMenuNames.pickingTargets}\"");
        }

        [Test]
        public void BuiltinHandleBehaviours_InProperAddComponentMenu(
            [ValueSource(nameof(s_BuiltinHandleBehaviours))] Type type)
        {
            var attribute = type.GetCustomAttribute<AddComponentMenu>();
            Assert.IsTrue(attribute != null && attribute.componentMenu.StartsWith(UnityEngine.PrefabHandles.AddComponentMenuNames.handleBehaviours),
                $"The interactive handle {type.FullName} should have the attribute AddComponentMenu(\"{UnityEngine.PrefabHandles.AddComponentMenuNames.handleBehaviours}\"");
        }
    }
}