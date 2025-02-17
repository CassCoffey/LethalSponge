using BepInEx;
using HarmonyLib;
using LethalLevelLoader;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Profiling.Memory;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Scoops.service
{
    internal class BundleLeakTracker
    {
        public Dictionary<string, int> leakCount;

        public BundleLeakTracker() 
        {
            leakCount = new Dictionary<string, int>();
        }
    }

    internal static class SpongeService
    {
        private static UnityEngine.Object[] allObjects;
        private static UnityEngine.Component[] allComponents;

        // List of known Getters that will create new objects or problems (Unity whyyyy)
        private static List<String> ignoredProperties = new List<string>();

        private static List<String> ignoredAssetbundles = new List<string>();

        private static Dictionary<string, BundleLeakTracker> leakTracking = new Dictionary<string, BundleLeakTracker>();
        private static Dictionary<int, ushort> referenceTracking = new Dictionary<int, ushort>();
        private static Dictionary<string, string> bundleTracking = new Dictionary<string, string>();
        private static Dictionary<string, string> streamedSceneTracking = new Dictionary<string, string>();

        private static Stopwatch stopwatch = new Stopwatch();

        private static int prevCount = 0;
        private static int initialCount = 0;

        public static void Initialize()
        {
            Plugin.Log.LogInfo("---");
            Plugin.Log.LogInfo("Initializing Sponge");

            ParseConfig();

            allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            Plugin.Log.LogInfo("Initial count of " + allObjects.Length + " objects loaded.");
            prevCount = allObjects.Length;
            initialCount = allObjects.Length;
            allObjects = [];
            allComponents = [];

            Plugin.Log.LogInfo("Sponge Initialised.");
            Plugin.Log.LogInfo("---");
        }

        private static void ParseConfig()
        {
            ignoredProperties.AddRange(Config.propertyBlacklist.Value.Split(';'));
            ignoredAssetbundles.AddRange(Config.assetbundleBlacklist.Value.Split(';'));
        }

        public static void PerformCleanup()
        {
            Plugin.Log.LogInfo("---");
            Plugin.Log.LogInfo("Applying Sponge");
            stopwatch.Restart();

            allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            Plugin.Log.LogInfo("Last check there were " + prevCount + " objects loaded, now there are " + allObjects.Length);

            allComponents = Resources.FindObjectsOfTypeAll<UnityEngine.Component>();
            referenceTracking.Clear();

            // Initial pass to build references
            for (int i = 0; i < allComponents.Length; i++)
            {
                IncrementReferenceCounts(GetUnityObjectReferences(i));
            }

            // Additional pass for each object type with no references
            int meshNoRefCount = ExamineMeshes();
            Plugin.Log.LogInfo("Found " + meshNoRefCount + " meshes with no references.");

            int newCount = Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Length;
            if (Config.performRemoval.Value)
            {
                newCount -= meshNoRefCount;
            }
            Plugin.Log.LogInfo("Changed to " + newCount + " objects.");

            if (newCount > allObjects.Length)
            {
                Plugin.Log.LogWarning("More objects after cleanup than before. Property calls likely instantiated unexpected objects.");
            }

            prevCount = newCount;
            allObjects = [];
            allComponents = [];

            foreach (string bundleName in leakTracking.Keys)
            {
                BundleLeakTracker tracker = leakTracking[bundleName];
                if (bundleName != "unknown")
                {
                    Plugin.Log.LogInfo("For AssetBundle " + bundleName + ": ");
                }
                else
                {
                    Plugin.Log.LogInfo("For unknown AssetBundle sources: ");
                }
                foreach (string assetType in tracker.leakCount.Keys)
                {
                    Plugin.Log.LogInfo(" - " + tracker.leakCount[assetType] + " leaked " + assetType + ".");
                }
            }

            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            int change = newCount - initialCount;
            float percentChange = ((float)change / (float)initialCount) * 100f;
            Plugin.Log.LogInfo("Sponge took " + elapsedTime.TotalSeconds + " seconds to execute.");
            Plugin.Log.LogInfo("There are " + change + " more objects than on initialization, a " + percentChange + "% change.");
            Plugin.Log.LogInfo("---");
        }

        private static int ExamineMeshes()
        {
            int noRefCount = 0;

            Mesh[] allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
            for (int i = 0; i < allMeshes.Length; i++)
            {
                int instanceID = allMeshes[i].GetInstanceID();
                if (!referenceTracking.TryGetValue(instanceID, out ushort refs) || refs < 1)
                {
                    if (HandleLeakedObject(allMeshes[i])) { noRefCount++; }
                }
            }

            allMeshes = [];
            return noRefCount;
        }

        public static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (streamedSceneTracking.TryGetValue(scene.name, out string bundle))
            {
                foreach (UnityEngine.GameObject obj in scene.GetRootGameObjects())
                {
                    FindSceneDependenciesRecursively(bundle, obj);
                }
            }
        }

        public static void FindSceneDependenciesRecursively(string bundleName, GameObject obj)
        {
            bundleTracking[obj.name] = bundleName;

            foreach (UnityEngine.Component component in obj.GetComponents(typeof(UnityEngine.Component)))
            {
                BuildBundleDependencies(bundleName, GetUnityObjectReferences(component));
            }

            foreach (Transform child in obj.transform)
            {
                FindSceneDependenciesRecursively(bundleName, child.gameObject);
            }
        }

        public static void RegisterAssetBundle(AssetBundle bundle)
        {
            if (bundle.isStreamedSceneAssetBundle)
            {
                foreach (string scenePath in bundle.GetAllScenePaths())
                {
                    string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    streamedSceneTracking.TryAdd(sceneName, bundle.name);
                }
            }
            else
            {
                foreach (string assetPath in bundle.GetAllAssetNames())
                {
                    string assetName = Path.GetFileNameWithoutExtension(assetPath);
                    bundleTracking.TryAdd(assetName, bundle.name);
                }
            }
        }

        private static bool HandleLeakedObject(UnityEngine.Object leakedObj)
        {
            bundleTracking.TryGetValue(leakedObj.name, out string bundle);
            string bundleName = bundle ?? "unknown";
            // if we find a bundle, make sure it isn't on the blacklist
            if (!(bundleName != "unknown" && ignoredAssetbundles.Contains(bundle)))
            {
                string leakedType = leakedObj.GetType().Name;

                if (bundleName == "unknown")
                {
                    Plugin.Log.LogInfo(leakedType + " with no known bundle - " + leakedObj.name);
                }

                if (leakTracking.ContainsKey(bundleName))
                {
                    leakTracking[bundleName].leakCount.TryGetValue(leakedType, out int currentCount);
                    leakTracking[bundleName].leakCount[leakedType] = currentCount + 1;
                }
                else
                {
                    leakTracking.Add(bundleName, new BundleLeakTracker());
                    leakTracking[bundleName].leakCount.Add(leakedType, 1);
                }

                if (Config.performRemoval.Value) { UnityEngine.Object.Destroy(leakedObj); }
                return true;
            }

            return false;
        }

        private static readonly Dictionary<Type, List<FieldInfo>> assignableFieldsByComponentType = new Dictionary<Type, List<FieldInfo>>() { { typeof(UnityEngine.Component), null } };
        private static readonly Dictionary<Type, List<PropertyInfo>> assignablePropertiesByComponentType = new Dictionary<Type, List<PropertyInfo>>() { { typeof(UnityEngine.Component), null } };

        public static List<UnityEngine.Object> GetUnityObjectReferences(int index)
        {
            var target = allComponents[index];
            if (target is not UnityEngine.Component) { return new List<UnityEngine.Object>(); }

            return GetUnityObjectReferences(target);
        }

        public static List<UnityEngine.Object> GetUnityObjectReferences(UnityEngine.Component target)
        {
            Type componentType = target.GetType();

            List<UnityEngine.Object> result = new List<UnityEngine.Object>();

            if (!assignableFieldsByComponentType.TryGetValue(componentType, out var assignableFields))
            {
                var type = componentType;
                do
                {
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                    foreach (var field in type.GetFields(flags))
                    {
                        assignableFields ??= new List<FieldInfo>();
                        assignableFields.Add(field);
                    }

                    type = type.BaseType;
                    if (!assignableFieldsByComponentType.TryGetValue(type, out var assignableFieldsFromBaseTypes))
                    {
                        continue;
                    }

                    if (assignableFieldsFromBaseTypes is null)
                    {
                        break;
                    }

                    assignableFields.AddRange(assignableFieldsFromBaseTypes);

                    break;
                }
                while (true);

                assignableFieldsByComponentType.Add(componentType, assignableFields);
            }

            if (!assignablePropertiesByComponentType.TryGetValue(componentType, out var assignableProperties))
            {
                var type = componentType;
                do
                {
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
                    foreach (var property in type.GetProperties(flags))
                    {
                        assignableProperties ??= new List<PropertyInfo>();
                        assignableProperties.Add(property);
                    }

                    type = type.BaseType;
                    if (!assignablePropertiesByComponentType.TryGetValue(type, out var assignablePropertiesFromBaseTypes))
                    {
                        continue;
                    }

                    if (assignablePropertiesFromBaseTypes is null)
                    {
                        break;
                    }

                    assignableProperties.AddRange(assignablePropertiesFromBaseTypes);

                    break;
                }
                while (true);

                assignablePropertiesByComponentType.Add(componentType, assignableProperties);
            }

            if (assignableFields is not null)
            {
                foreach (var field in assignableFields)
                {
                    if (field.FieldType is)

                    if (!(field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) || field.FieldType == typeof(UnityEngine.Object))) { continue; }

                    UnityEngine.Object reference = field.GetValue(target) as UnityEngine.Object;
                    if (reference != null)
                    {
                        result.Add(reference);
                    }
                }
            }

            if (assignableProperties is not null)
            {
                foreach (var property in assignableProperties)
                {
                    string propertyId = property.DeclaringType.Name + "." + property.Name;
                    if (ignoredProperties.Contains(propertyId)) { continue; }

                    if (!property.CanRead || !(property.PropertyType.IsSubclassOf(typeof(UnityEngine.Object)) || property.PropertyType == typeof(UnityEngine.Object))) { continue; }

                    List<UnityEngine.Object> references = new List<UnityEngine.Object>();

                    try
                    {
                        IEnumerable enumerable = property.GetValue(target) as IEnumerable;

                        if (enumerable != null && !(enumerable is string))
                        {
                            foreach (var obj in enumerable)
                            {
                                UnityEngine.Object reference = obj as UnityEngine.Object;
                                if (reference != null) references.Add(reference);
                            }
                        } 
                        else
                        {
                            UnityEngine.Object reference = property.GetValue(target) as UnityEngine.Object;
                            if (reference != null) references.Add(reference);
                        }
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogWarning("Error while calling " + propertyId + ", ignoring property and continuing:");
                        Plugin.Log.LogWarning(e);

                        ignoredProperties.AddItem(propertyId);

                        continue;
                    }
                    
                    if (reference != null)
                    {
                        if (Config.verboseLogging.Value && !allObjects.Contains(reference))
                        {
                            Plugin.Log.LogWarning("Calling " + propertyId + " created a new object: " + reference.name);

                            ignoredProperties.AddItem(propertyId);
                        }

                        result.Add(reference);
                    }
                }
            }

            return result;
        }

        public static void IncrementReferenceCounts(List<UnityEngine.Object> references)
        {
            foreach (UnityEngine.Object reference in references)
            {
                int refID = reference.GetInstanceID();
                referenceTracking.TryGetValue(refID, out ushort currentCount);
                referenceTracking[refID] = currentCount++;
            }
        }

        public static void BuildBundleDependencies(string bundleName, List<UnityEngine.Object> references)
        {
            foreach (UnityEngine.Object reference in references)
            {
                bundleTracking[reference.name] = bundleName;
            }
        }
    }
}