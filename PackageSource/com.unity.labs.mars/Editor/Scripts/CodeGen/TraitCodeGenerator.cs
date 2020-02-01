using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Labs.MARS.Query;
using Unity.Labs.Utils;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Unity.Labs.MARS.CodeGen
{
    [InitializeOnLoad]
    public static class TraitCodeGenerator
    {
        static readonly List<Type> k_GeneratorTypes = new List<Type>();
        static readonly List<Type> k_TempTypes = new List<Type>();
        static readonly List<Type> k_GenericInterfaceTypes = new List<Type>();
        static readonly List<IGeneratesCode> k_GeneratorInstances = new List<IGeneratesCode>();
        static readonly Dictionary<Type, string> k_TraitTypeToPrefix = new Dictionary<Type, string>();
        static readonly Dictionary<Type, string> k_RelationTraitTypeToPrefix = new Dictionary<Type, string>();
        static readonly HashSet<Type> k_ConditionTraitTypes = new HashSet<Type>();
        static readonly HashSet<Type> k_RelationTraitTypes = new HashSet<Type>();
        static CodeGenerationTypeData[] s_AllCodeGenerationTypeData;
        static CodeGenerationTypeData[] s_RelationGenerationData;
        const string k_CodeGenVersionFilename = "CodeGenVersion.Generated.txt";
        const int k_CodeGenVersionWanted = 3003; // change this value to cause a code-gen rebuild

        internal static bool HasGenerated { get; private set; }

        static TraitCodeGenerator()
        {
#if UNITY_CLOUD_BUILD
            TryRunGeneration();
#else
            EditorApplication.delayCall += TryRunGeneration;
#endif
        }

        static bool AnyTypeChanges(CodeGenerationTypeData[] types, HashSet<Type> previouslyGeneratedTypes)
        {
            foreach (var typeData in types)
            {
                if (!previouslyGeneratedTypes.Contains(typeData.Type))
                    return true;
            }

            foreach (var type in previouslyGeneratedTypes)
            {
                var found = false;
                foreach (var td in types)
                {
                    if (td.Type != type)
                        continue;

                    found = true;
                    break;
                }

                if (!found)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Run every code generator we can find with every type we want to generate
        /// </summary>
        /// <returns>True if all generators succeeded, false if any error occured</returns>
        internal static void TryRunGeneration()
        {
            var previousConditionTypes = GetPreviouslyGeneratedTypes();
            var conditionTypes = GatherConditionTypes();
            var relationTypes = GatherRelationTypes();
            var previousRelationTypes = GetPreviouslyGeneratedRelationTypes();

            var anyConditionTypeChanges = AnyTypeChanges(conditionTypes, previousConditionTypes);
            var anyRelationTypeChanges = AnyTypeChanges(relationTypes, previousRelationTypes);
            if (!anyConditionTypeChanges && !anyRelationTypeChanges && CodeGenVersionIsUpToDate())
            {
                HasGenerated = true;
                return;
            }

            RunGenerators(conditionTypes, relationTypes, GatherGenerators());
            CodeGenVersionUpdate();
            AssetDatabase.Refresh();
        }

        static string CodeGenVersionFilePath()
        {
            return CodeGenerationShared.OutputFolder + k_CodeGenVersionFilename;
        }

        static void CodeGenVersionUpdate()
        {
            var fullPath = CodeGenVersionFilePath();
            try
            {
                File.WriteAllText(fullPath, k_CodeGenVersionWanted.ToString());
            }
            catch (IOException ex)
            {
                Debug.LogException(ex);
            }
        }

        static bool CodeGenVersionIsUpToDate()
        {
            var fullPath = CodeGenVersionFilePath();
            try
            {
                if (File.Exists(fullPath))
                {
                    var curText = File.ReadAllText(fullPath);
                    if (int.TryParse(curText, out int fileVersion))
                    {
                        if (fileVersion == k_CodeGenVersionWanted)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (IOException ex)
            {
                Debug.LogException(ex);
            }
            return false;
        }

        static HashSet<Type> GetConditionTraitSubscribers()
        {
            k_ConditionTraitTypes.Clear();
            k_TempTypes.Clear();
            typeof(ICondition).GetImplementationsOfInterface(k_TempTypes);
            foreach (var iConditionType in k_TempTypes.Where(t => !t.IsGenericTypeDefinition))
            {
                k_GenericInterfaceTypes.Clear();
                iConditionType.GetGenericInterfaces(typeof(ICondition<>), k_GenericInterfaceTypes);
                if (k_GenericInterfaceTypes.Count == 0)
                    continue;

                foreach (var gi in k_GenericInterfaceTypes)
                {
                    var args = gi.GetGenericArguments();
                    if (args.Length == 0)
                        continue;

                    k_ConditionTraitTypes.Add(args[0]);
                }
            }

            return k_ConditionTraitTypes;
        }

        static HashSet<Type> GetRelationTraitSubscribers()
        {
            k_RelationTraitTypes.Clear();
            k_TempTypes.Clear();
            typeof(IRelation).GetImplementationsOfInterface(k_TempTypes);
            foreach (var iRelationType in k_TempTypes.Where(t => !t.IsGenericTypeDefinition))
            {
                k_GenericInterfaceTypes.Clear();
                iRelationType.GetGenericInterfaces(typeof(IRelation<>), k_GenericInterfaceTypes);
                if (k_GenericInterfaceTypes.Count == 0)
                    continue;

                foreach (var gi in k_GenericInterfaceTypes)
                {
                    var args = gi.GetGenericArguments();
                    if (args.Length == 0)
                        continue;

                    var type = args[0];
                    if (type == typeof(bool))
                        continue;

                    k_RelationTraitTypes.Add(type);
                }
            }

            return k_RelationTraitTypes;
        }

        internal static string OverrideSpecialNames(string typeName)
        {
            switch (typeName)
            {
                case "Int32":
                    return "Int";
                case "Int64":
                    return "Long";
                case "Single":
                    return "Float";
                case "Boolean":
                    return "SemanticTag";
                default:
                    return typeName;
            }
        }

        static CodeGenerationTypeData[] GatherConditionTypes()
        {
            k_TraitTypeToPrefix.Clear();
            // get all the trait types used by conditions and relations
            var conditionTraitSet = GetConditionTraitSubscribers();
            foreach (var conditionTraitType in conditionTraitSet)
            {
                if (!TypeIsValid(conditionTraitType, out var typeError))
                {
                    Debug.LogError(typeError);
                    continue;
                }

                k_TraitTypeToPrefix.Add(conditionTraitType,  OverrideSpecialNames(conditionTraitType.Name));
            }

            return k_TraitTypeToPrefix.Select(kvp => new CodeGenerationTypeData(kvp.Key, kvp.Value))
                                      .OrderBy(t => t.Type.FullName).ToArray();
        }


        static CodeGenerationTypeData[] GatherRelationTypes()
        {
            k_RelationTraitTypeToPrefix.Clear();
            var relationTraitSet = GetRelationTraitSubscribers();
            foreach (var relationTraitType in relationTraitSet)
            {
                if (!TypeIsValid(relationTraitType, out var typeError))
                {
                    Debug.LogError(typeError);
                    continue;
                }

                // semantic tag relations are not supported
                if (relationTraitType == typeof(bool))
                    continue;

                k_RelationTraitTypeToPrefix.Add(relationTraitType, OverrideSpecialNames(relationTraitType.Name));
            }

            return k_RelationTraitTypeToPrefix.Select(kvp => new CodeGenerationTypeData(kvp.Key, kvp.Value))
                                              .OrderBy(t => t.Type.FullName).ToArray();
        }

        static bool RunGenerators(CodeGenerationTypeData[] conditionTypes, CodeGenerationTypeData[] relationTypes,
            IEnumerable<IGeneratesCode> generators)
        {
            CodeGenerationShared.EnsureOutputFolder();

            // some generators may need the type from both conditions and relations.
            // though both sets are already sorted, documentation for .Union does not specify if it will always be in the same order.
            var typesUnion = conditionTypes.Union(relationTypes).OrderBy(t => t.Type.FullName).ToArray();

            var success = true;
            foreach (var generator in generators)
            {
                var typeSet = conditionTypes;
                switch (generator.TypeSet)
                {
                    case GeneratedTypeSet.Relations:
                        typeSet = relationTypes;
                        break;
                    case GeneratedTypeSet.All:
                        typeSet = typesUnion;
                        break;
                }

                if (!generator.TryGenerateCode(typeSet))
                {
                    Debug.LogWarning($"Error (likely with file access) occured with generator {generator.GetType().Name}");
                    success = false;
                }
            }

            return success;
        }

        internal static bool TypeIsValid(Type type, out string errorMessage)
        {
            if (type == null)
            {
                errorMessage = "Type cannot be null";
                return false;
            }

            if (type.IsClass)
            {
                if (type != typeof(string))
                {
                    errorMessage = $"{type.FullName} is a class, which is not allowed as a trait type!";
                    return false;
                }

                errorMessage = string.Empty;
                return true;
            }

            if (!type.IsValueType)
            {
                errorMessage = $"Trait types must be a value type or string, but {type.FullName} is not!";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        static List<IGeneratesCode> GatherGenerators()
        {
            k_GeneratorInstances.Clear();
            typeof(IGeneratesCode).GetImplementationsOfInterface(k_GeneratorTypes);
            foreach (var type in k_GeneratorTypes)
            {
                try
                {
                    k_GeneratorInstances.Add((IGeneratesCode) Activator.CreateInstance(type));
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            return k_GeneratorInstances;
        }

        static HashSet<Type> GetPreviouslyGeneratedTypes()
        {
            var hashSet = new HashSet<Type>();
            var resultTypeInfo = typeof(QueryResult).GetTypeInfo();
            // to determine which types we've previously generated, we just pick a method
            // we expect to be generated for each type, and see which types it exists for.
            var setTraitMethods = resultTypeInfo.GetDeclaredMethods(nameof(QueryResult.SetTrait));
            foreach (var method in setTraitMethods)
            {
                var parameters = method.GetParameters();
                foreach (var param in parameters)
                {
                    // we only care about the second input parameter, which is type-specific
                    if (param.Position != 1)
                        continue;

                    var paramType = param.ParameterType;
                    // ignore our pre-generation stub methods
                    if (paramType == typeof(object))
                        continue;

                    hashSet.Add(paramType);
                }
            }

            return hashSet;
        }

        static HashSet<Type> GetPreviouslyGeneratedRelationTypes()
        {
            var hashSet = new HashSet<Type>();
            var relationsTypeInfo = typeof(Relations).GetTypeInfo();
            // to determine which types we've previously generated, we just pick a method
            // we expect to be generated for each type, and see which types it exists for.
            var tryGetTypeMethods = relationsTypeInfo.GetDeclaredMethods(nameof(Relations.TryGetType));

            foreach (var method in tryGetTypeMethods)
            {
                foreach (var param in method.GetParameters())
                {
                    if (param.Position != 0 || param.ParameterType == typeof(object[]))
                        continue;

                    var pType = param.ParameterType;
                    var arrayType = pType.GetElementType();
                    if (arrayType == null || !arrayType.IsArray)
                        continue;

                    var innerElementType = arrayType.GetElementType();
                    if(innerElementType == null || innerElementType == typeof(object))
                        continue;

                    var generics = innerElementType.GenericTypeArguments;
                    if (generics.Length == 0 || generics[0].Name == "T")
                        continue;

                    hashSet.Add(generics[0]);
                }
            }

            return hashSet;
        }
    }
}
