using System;
using System.Collections.Generic;
using System.Text;
using Unity.Labs.MARS.Data;
using Unity.Labs.ModuleLoader;
using Unity.Labs.Utils;
using UnityEngine;
using UnityEngine.Profiling;

namespace Unity.Labs.MARS
{
    public static class FunctionalityIslandExtensions
    {
        static readonly Dictionary<Type, TraitDefinition[]> k_ProvidedTraits = new Dictionary<Type, TraitDefinition[]>();
        static readonly Dictionary<TraitDefinition, List<Type>> k_TraitProviderMap = new Dictionary<TraitDefinition, List<Type>>();

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly Dictionary<GameObject, GameObject> k_NewProviderPrefabInstances = new Dictionary<GameObject, GameObject>();
        static readonly HashSet<TraitDefinition> k_RequiredTraits = new HashSet<TraitDefinition>();

        static FunctionalityIslandExtensions()
        {
            var allProviders = new List<Type>();
            typeof(IFunctionalityProvider).GetImplementationsOfInterface(allProviders);

            foreach (var providerType in allProviders)
            {
                if (!typeof(IProvidesTraits).IsAssignableFrom(providerType))
                    continue;

                var staticProvidedTraits = providerType.GetField(
                    IProvidesTraitsMethods.StaticProvidedTraitsFieldName,
                    IProvidesTraitsMethods.StaticProvidedTraitsBindingFlags);

                if (staticProvidedTraits == null)
                {
                    Debug.LogErrorFormat("{0} is missing {1} field. This should have been caught  by the static analyzer",
                        providerType.Name,
                        IProvidesTraitsMethods.StaticProvidedTraitsFieldName);

                    continue;
                }

                var providedTraits = (TraitDefinition[])staticProvidedTraits.GetValue(null);
                if (providedTraits.Length > 0)
                    k_ProvidedTraits.Add(providerType, providedTraits);

                foreach (var trait in providedTraits)
                {
                    List<Type> providerTypes;
                    if (!k_TraitProviderMap.TryGetValue(trait, out providerTypes))
                    {
                        providerTypes = new List<Type>();
                        k_TraitProviderMap.Add(trait, providerTypes);
                    }

                    providerTypes.Add(providerType);
                }
            }
        }

        /// <summary>
        /// Set up functionality providers from the list of default providers
        /// This allows custom serialized data to be set up on prefabs for providers
        /// </summary>
        /// <param name="island">The functionality island on which to set up default providers</param>
        /// <param name="requiredTraits">The required traits that must be satisfied by providers</param>
        /// <param name="newProviders">(Optional) A list to which new providers will be added</param>
        public static void SetupDefaultProviders(this FunctionalityIsland island, HashSet<TraitDefinition> requiredTraits, List<IFunctionalityProvider> newProviders = null)
        {
            if (requiredTraits.Count == 0)
                return;

            Profiler.BeginSample(FunctionalityIsland.SetupDefaultProvidersProfilerLabel);

            island.CheckSetup();

            var functionalityInjectionModuleLogging = ModuleLoaderDebugSettings.instance.functionalityInjectionModuleLogging;
            if (functionalityInjectionModuleLogging)
            {
                var list = new StringBuilder();

                if (requiredTraits.Count > 0)
                {
                    foreach (var trait in requiredTraits)
                    {
                        list.Append(trait);
                        list.Append(", ");
                    }

                    list.Length -= 2;
                }

                Debug.LogFormat("Requiring default providers on: {0}", string.Join(", ", list));
            }

            // Copy the collection so that we don't modify the original
            requiredTraits = new HashSet<TraitDefinition>(requiredTraits);

            // Clear out traits that are already being provided
            foreach (var provider in island.uniqueProviders)
            {
                var traitsProvider = provider as IProvidesTraits;
                if (traitsProvider != null)
                {
                    var providedTraits = traitsProvider.GetProvidedTraits();
                    foreach (var trait in providedTraits)
                    {
                        requiredTraits.Remove(trait);
                    }
                }
            }

            // Determine which provider interfaces are needed for the given subscribers
            k_NewProviderPrefabInstances.Clear();
            while (CheckMissingTraits(island, requiredTraits) > 0)
            {
                var providerAdded = false;
                foreach (var row in island.defaultProviders)
                {
                    var providerTypeName = row.providerTypeName;
                    var providerType = row.providerType;
                    if (providerType == null)
                    {
                        Debug.LogWarningFormat("Could not find type for {0} while setting up default providers", providerTypeName);
                        continue;
                    }

                    // Silently skip provider types that have already been overridden
                    if (island.providers.ContainsKey(providerType))
                        continue;

                    var prefab = row.defaultProviderPrefab;
                    var providesRequiredTrait = false;
                    if (prefab != null)
                    {
                        var prefabComponents = prefab.gameObject.GetComponentsInChildren<IProvidesTraits>();
                        foreach (var prefabComponent in prefabComponents)
                        {
                            var providedTraits = prefabComponent.GetProvidedTraits();
                            foreach (var trait in providedTraits)
                            {
                                providesRequiredTrait |= requiredTraits.Remove(trait);
                            }
                        }

                        if (!providesRequiredTrait)
                            continue;

                        GameObject instance;
                        if (!k_NewProviderPrefabInstances.TryGetValue(prefab, out instance))
                        {
                            if (functionalityInjectionModuleLogging)
                                Debug.LogFormat("Functionality Injection Module creating default provider: {0}", prefab);

                            instance = GameObjectUtils.Instantiate(prefab);
                            k_NewProviderPrefabInstances[prefab] = instance;
                        }

                        var providersInPrefab = instance.GetComponentsInChildren<IFunctionalityProvider>();
                        foreach (var provider in providersInPrefab)
                        {
                            AddRequirementsAndRemoveProvidedTraits(requiredTraits, provider);
                            island.AddProvider(provider.GetType(), provider);
                        }

                        continue;
                    }

                    var defaultProviderTypeName = row.defaultProviderTypeName;
                    var defaultProviderType = row.defaultProviderType;
                    if (defaultProviderType == null)
                    {
                        Debug.LogWarningFormat("Cannot set up default provider for {0} in {1}. Type {2} cannot be found.", providerTypeName, island.name, defaultProviderTypeName);
                        continue;
                    }

                    TraitDefinition[] staticProvided;
                    if (!k_ProvidedTraits.TryGetValue(defaultProviderType, out staticProvided))
                        continue;

                    foreach (var trait in staticProvided)
                    {
                        providesRequiredTrait |= requiredTraits.Remove(trait);
                    }

                    if (!providesRequiredTrait)
                        continue;

                    var vanillaProvider = FunctionalityIsland.GetOrCreateProviderInstance(defaultProviderType, providerType);
                    if (vanillaProvider == null)
                    {
                        Debug.LogWarningFormat("Cannot instantiate {0} as an IFunctionalityProvider.", defaultProviderTypeName);
                        continue;
                    }

                    if (newProviders != null)
                        newProviders.Add(vanillaProvider);

                    providerAdded = true;
                    AddRequirementsAndRemoveProvidedTraits(requiredTraits, vanillaProvider);
                    island.AddProvider(defaultProviderType, vanillaProvider);
                }

                if (!providerAdded)
                    break;
            }

            island.InjectFunctionalityInDefaultProviders(k_NewProviderPrefabInstances, newProviders);
            island.ActivateProviderGameObjects();

            Profiler.EndSample();
        }

        public static void RequireProviders(this FunctionalityIsland island, HashSet<TraitDefinition> requiredTraits, List<IFunctionalityProvider> newProviders = null)
        {
            if (requiredTraits.Count == 0)
                return;

            Profiler.BeginSample(FunctionalityIsland.RequireProvidersProfilerLabel);

            island.CheckSetup();

            var functionalityInjectionModuleLogging = ModuleLoaderDebugSettings.instance.functionalityInjectionModuleLogging;
            if (functionalityInjectionModuleLogging)
            {
                var list = new StringBuilder();

                if (requiredTraits.Count > 0)
                {
                    foreach (var trait in requiredTraits)
                    {
                        list.Append(trait);
                        list.Append(", ");
                    }

                    list.Length -= 2;
                }

                Debug.LogFormat("Requiring providers on: {0}", string.Join(", ", list));
            }

            // Copy the collection so that we don't modify the original
            requiredTraits = new HashSet<TraitDefinition>(requiredTraits);

            // Clear out traits that are already being provided
            foreach (var provider in island.uniqueProviders)
            {
                var traitsProvider = provider as IProvidesTraits;
                if (traitsProvider != null)
                {
                    var providedTraits = traitsProvider.GetProvidedTraits();
                    foreach (var trait in providedTraits)
                    {
                        requiredTraits.Remove(trait);
                    }
                }
            }

            while (CheckMissingTraits(island, requiredTraits) > 0)
            {
                var providerAdded = false;
                k_RequiredTraits.Clear();
                k_RequiredTraits.UnionWith(requiredTraits); // Copy the collection in order to modify requiredTraits
                foreach (var trait in k_RequiredTraits)
                {
                    List<Type> providerTypes;
                    if (!k_TraitProviderMap.TryGetValue(trait, out providerTypes))
                        continue;

                    var firstProvider = providerTypes[0];
                    if (providerTypes.Count > 1)
                        Debug.LogWarning(string.Format("More than one provider found for {0}. Using {1}", trait, firstProvider));

                    // Spawn or gain access to the class needed to support the functionality
                    var provider = FunctionalityIsland.GetOrCreateProviderInstance(firstProvider, firstProvider);
                    if (provider == null)
                    {
                        Debug.LogWarningFormat("Cannot instantiate {0} as an IFunctionalityProvider.", firstProvider.Name);
                        continue;
                    }

                    if (newProviders != null)
                        newProviders.Add(provider);

                    providerAdded = true;
                    AddRequirementsAndRemoveProvidedTraits(requiredTraits, provider);
                    island.AddProvider(firstProvider, provider);

                    break;
                }

                if (!providerAdded)
                    break;
            }

            island.InjectFunctionalityInDefaultProviders(k_NewProviderPrefabInstances, newProviders);
            island.ActivateProviderGameObjects();

            Profiler.EndSample();
        }

        static void AddRequirementsAndRemoveProvidedTraits(HashSet<TraitDefinition> requiredTraits, IFunctionalityProvider provider)
        {
            var traitsRequirer = provider as IRequiresTraits;
            if (traitsRequirer != null)
            {
                var newRequirements = traitsRequirer.GetRequiredTraits();
                if (newRequirements != null)
                {
                    foreach (var requirement in newRequirements)
                    {
                        requiredTraits.Add(requirement);
                    }
                }
            }

            var traitsProvider = provider as IProvidesTraits;
            if (traitsProvider != null)
            {
                var providedTraits = traitsProvider.GetProvidedTraits();
                if (providedTraits != null)
                    requiredTraits.ExceptWith(providedTraits);
            }
        }

        static int CheckMissingTraits<T>(FunctionalityIsland island, HashSet<T> requiredTraits)
            where T: TraitDefinition
        {
            var compareSet = new HashSet<TraitDefinition>(requiredTraits);
            var existingTraits = new HashSet<TraitDefinition>();
            foreach (var kvp in island.providers)
            {
                var traitsProvider = kvp.Value as IProvidesTraits;
                if (traitsProvider == null)
                    continue;

                var providedTraits = traitsProvider.GetProvidedTraits();
                existingTraits.UnionWith(providedTraits);
            }

            foreach (var definition in existingTraits)
            {
                compareSet.Remove(definition);
            }

            return compareSet.Count;
        }

        public static void GetProvidedTraits(this FunctionalityIsland island, HashSet<TraitDefinition> traits)
        {
            if (traits.Count == 0)
                return;

            foreach (var kvp in island.providers)
            {
                var traitsProvider = kvp.Value as IProvidesTraits;
                if (traitsProvider == null)
                    continue;

                var providedTraits = traitsProvider.GetProvidedTraits();
                traits.UnionWith(providedTraits);
            }
        }
    }
}
