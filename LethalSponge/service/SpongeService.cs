using BepInEx;
using HarmonyLib;
using LethalLevelLoader;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

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
        private static readonly Dictionary<Type, List<FieldInfo>> assignableFieldsByObjectType = new Dictionary<Type, List<FieldInfo>>() { { typeof(UnityEngine.Object), null } };
        private static readonly Dictionary<Type, List<PropertyInfo>> assignablePropertiesByObjectType = new Dictionary<Type, List<PropertyInfo>>() { { typeof(UnityEngine.Object), null } };

        private static UnityEngine.Object[] allObjects;

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

        private static readonly List<string> meshReadProperties = new List<string>() { "vertices", "normals", "tangents", "uv", "uv2", "uv3", "uv4", "uv5", "uv6", "uv7", "uv8", "colors", "colors32", "triangles" };

        public static void Initialize()
        {
            Plugin.Log.LogInfo("---");
            Plugin.Log.LogInfo("Initializing Sponge");

            allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            Plugin.Log.LogInfo("Initial count of " + allObjects.Length + " objects loaded.");
            prevCount = allObjects.Length;
            initialCount = allObjects.Length;
            allObjects = [];

            Plugin.Log.LogInfo("Sponge Initialised.");
            Plugin.Log.LogInfo("---");
        }

        public static void ParseConfig()
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

            referenceTracking.Clear();
            leakTracking.Clear();

            // Initial pass to build references
            for (int i = 0; i < allObjects.Length; i++)
            {
                IncrementReferenceCounts(GetUnityObjectReferences(i));
            }

            // Pass for each object type with no references
            int meshNoRefCount = ExamineType<Mesh>();
            int matNoRefCount = ExamineType<Material>();
            int texNoRefCount = ExamineType<Texture2D>();
            int audioNoRefCount = ExamineType<AudioClip>();
            int navMeshNoRefCount = ExamineType<NavMeshData>();

            int newCount = Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Length;
            if (Config.performRemoval.Value)
            {
                newCount -= meshNoRefCount;
            }
            Plugin.Log.LogInfo("Changed to " + newCount + " objects.");

            if (newCount > allObjects.Length)
            {
                Plugin.Log.LogWarning("More objects after cleanup than before. Property calls possibly instantiated unexpected objects.");
            }

            prevCount = newCount;
            allObjects = [];

            foreach (string bundleName in leakTracking.Keys)
            {
                BundleLeakTracker tracker = leakTracking[bundleName];
                if (bundleName != "unknown")
                {
                    Plugin.Log.LogInfo("For AssetBundle " + bundleName + ": ");
                }
                else
                {
                    Plugin.Log.LogInfo("For Base Game/unknown AssetBundle sources: ");
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

        private static int ExamineType<T>() where T : UnityEngine.Object
        {
            int noRefCount = 0;

            T[] allObjects = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < allObjects.Length; i++)
            {
                int instanceID = allObjects[i].GetInstanceID();
                if (!referenceTracking.TryGetValue(instanceID, out ushort refs) || refs < 1)
                {
                    if (HandleLeakedObject(allObjects[i])) { noRefCount++; }
                }
            }

            allObjects = [];

            if (noRefCount > 0)
            {
                Plugin.Log.LogInfo("Found " + noRefCount + " " + typeof(T).Name + " with no references.");
            }

            return noRefCount;
        }

        public static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            streamedSceneTracking.TryGetValue(scene.name, out string bundle);

            foreach (UnityEngine.GameObject obj in scene.GetRootGameObjects())
            {
                FindSceneDependenciesRecursively(bundle ?? scene.name, obj);
            }
        }

        public static void FindSceneDependenciesRecursively(string bundleName, GameObject obj)
        {
            bundleTracking[obj.name] = bundleName;

            foreach (UnityEngine.Component component in obj.GetComponents(typeof(UnityEngine.Component)))
            {
                if (component != null)
                {
                    BuildBundleDependencies(bundleName, GetUnityObjectReferences(component));
                }
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
            string baseName = leakedObj.name.Replace(" (Instance)", "");
            bundleTracking.TryGetValue(baseName, out string bundle);
            string bundleName = bundle ?? "unknown";
            // if we find a bundle, make sure it isn't on the blacklist
            if (!(bundleName != "unknown" && ignoredAssetbundles.Contains(bundle)))
            {
                string leakedType = leakedObj.GetType().Name;

                if (bundleName == "unknown")
                {
                    Plugin.Log.LogInfo(leakedType + " with no known bundle - " + leakedObj.name + ", ID - " + leakedObj.GetInstanceID());
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

        public static List<UnityEngine.Object> GetUnityObjectReferences(int index)
        {
            var target = allObjects[index];
            if (target == null || target is not UnityEngine.Object) { return new List<UnityEngine.Object>(); }

            return GetUnityObjectReferences(target);
        }

        public static List<UnityEngine.Object> GetUnityObjectReferences(UnityEngine.Object target)
        {
            Type objectType = target.GetType();

            List<UnityEngine.Object> result = new List<UnityEngine.Object>();

            if (!assignableFieldsByObjectType.TryGetValue(objectType, out var assignableFields))
            {
                var type = objectType;
                do
                {
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                    foreach (var field in type.GetFields(flags))
                    {
                        assignableFields ??= new List<FieldInfo>();
                        assignableFields.Add(field);
                    }

                    type = type.BaseType;
                    if (!assignableFieldsByObjectType.TryGetValue(type, out var assignableFieldsFromBaseTypes))
                    {
                        continue;
                    }

                    if (assignableFieldsFromBaseTypes is null)
                    {
                        break;
                    }

                    assignableFields ??= new List<FieldInfo>();
                    assignableFields.AddRange(assignableFieldsFromBaseTypes);

                    break;
                }
                while (true);

                assignableFieldsByObjectType.Add(objectType, assignableFields);
            }

            if (!assignablePropertiesByObjectType.TryGetValue(objectType, out var assignableProperties))
            {
                var type = objectType;
                do
                {
                    const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
                    foreach (var property in type.GetProperties(flags))
                    {
                        assignableProperties ??= new List<PropertyInfo>();
                        assignableProperties.Add(property);
                    }

                    type = type.BaseType;
                    if (!assignablePropertiesByObjectType.TryGetValue(type, out var assignablePropertiesFromBaseTypes))
                    {
                        continue;
                    }

                    if (assignablePropertiesFromBaseTypes is null)
                    {
                        break;
                    }

                    assignableProperties ??= new List<PropertyInfo>();
                    assignableProperties.AddRange(assignablePropertiesFromBaseTypes);

                    break;
                }
                while (true);

                assignablePropertiesByObjectType.Add(objectType, assignableProperties);
            }

            if (assignableFields is not null)
            {
                foreach (var field in assignableFields)
                {
                    if (!(field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) || field.FieldType == typeof(UnityEngine.Object) || 
                        (typeof(IEnumerable).IsAssignableFrom(field.FieldType) && field.FieldType != typeof(string)))) { continue; }

                    IEnumerable enumerable = field.GetValue(target) as IEnumerable;

                    if (enumerable != null && !(enumerable is string))
                    {
                        foreach (var obj in enumerable)
                        {
                            UnityEngine.Object reference = obj as UnityEngine.Object;
                            if (reference != null) result.Add(reference);
                        }
                    }
                    else
                    {
                        UnityEngine.Object reference = field.GetValue(target) as UnityEngine.Object;
                        if (reference != null) result.Add(reference);
                    }
                }
            }

            if (assignableProperties is not null)
            {
                foreach (var property in assignableProperties)
                {
                    string propertyId = property.DeclaringType.Name + "." + property.Name;
                    if (ignoredProperties.Contains(propertyId)) { continue; }

                    if (!property.CanRead || 
                        !(property.PropertyType.IsSubclassOf(typeof(UnityEngine.Object)) || property.PropertyType == typeof(UnityEngine.Object) ||
                        (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string)))) { continue; }

                    // Avoiding not having the correct arguments for dicts
                    if (property.Name == "Item") { continue; }

                    // Need to account for materials with shaders that don't have _MainTex or we'll throw errors.
                    if (target is Material)
                    {
                        Material mat = (Material)target;
                        if (!mat.HasProperty("_MainTex"))
                        {
                            continue;
                        }
                    }

                    // Need to account for meshes with read/write disabled or we'll throw errors.
                    if (target is Mesh)
                    {
                        Mesh mesh = (Mesh)target;
                        if (!mesh.isReadable && meshReadProperties.Contains(property.Name))
                        {
                            continue;
                        }
                    }

                    if (property.Name == "fontSharedMaterials")
                    {
                        Mesh mesh = (Mesh)target;
                        if (!mesh.isReadable && meshReadProperties.Contains(property.Name))
                        {
                            continue;
                        }
                    }

                    List<UnityEngine.Object> references = new List<UnityEngine.Object>();

                    try
                    {
                        IEnumerable enumerable = property.GetValue(target) as IEnumerable;

                        if (enumerable != null && !(enumerable is string))
                        {
                            foreach (var obj in enumerable)
                            {
                                UnityEngine.Object reference = obj as UnityEngine.Object;
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
                        else
                        {
                            UnityEngine.Object reference = property.GetValue(target) as UnityEngine.Object;
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
                    catch (Exception e)
                    {
                        Plugin.Log.LogWarning("Error while calling " + propertyId + ", continuing:");
                        Plugin.Log.LogWarning(e);

                        continue;
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
                referenceTracking[refID] = (ushort)(currentCount + 1);
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