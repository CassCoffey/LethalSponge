using BepInEx;
using DunGen;
using DunGen.Graph;
using HarmonyLib;
using LethalLevelLoader;
using Mono.Cecil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem.HID;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace Scoops.service
{
    internal class BundleLeakTracker
    {
        public Dictionary<string, int> leakCount;
        public int allGameObjects;
        public int instantiatedGameObjects;

        public BundleLeakTracker() 
        {
            leakCount = new Dictionary<string, int>();
            allGameObjects = 0;
            instantiatedGameObjects = 0;
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
        private static Dictionary<string, BundleLeakTracker> previousLeakTracking = new Dictionary<string, BundleLeakTracker>();
        private static Dictionary<int, ushort> referenceTracking = new Dictionary<int, ushort>();
        private static Dictionary<string, string> bundleTracking = new Dictionary<string, string>();
        private static Dictionary<string, string> streamedSceneTracking = new Dictionary<string, string>();

        private static Stopwatch stopwatch = new Stopwatch();

        private static int prevCount = 0;
        private static int initialCount = 0;

        private static readonly List<string> meshReadProperties = new List<string>() { "vertices", "normals", "tangents", "uv", "uv2", "uv3", "uv4", "uv5", "uv6", "uv7", "uv8", "colors", "colors32", "triangles" };

        public static void PluginLoad()
        {
            ParseConfig();

            SceneManager.sceneLoaded += SceneLoaded;
        }

        public static void Initialize()
        {
            Plugin.Log.LogMessage("---");
            Plugin.Log.LogMessage("Initializing Sponge");

            allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            Plugin.Log.LogMessage("Initial count of " + allObjects.Length + " objects loaded.");
            prevCount = allObjects.Length;
            initialCount = allObjects.Length;
            allObjects = [];

            Plugin.Log.LogMessage("Sponge Initialised.");
            Plugin.Log.LogMessage("---");
        }

        public static void ParseConfig()
        {
            ignoredProperties.AddRange(Config.propertyBlacklist.Value.Split(';'));
            ignoredAssetbundles.AddRange(Config.assetbundleBlacklist.Value.Split(';'));
        }

        public static void ApplySponge()
        {
            Plugin.Log.LogMessage("---");
            Plugin.Log.LogMessage("Applying Sponge");
            stopwatch.Restart();

            if (Config.verboseLogging.Value)
            {
                PerformEvaluation();
            }
            int newCount = Resources.FindObjectsOfTypeAll<UnityEngine.Object>().Length;
            if (Config.verboseLogging.Value && newCount > allObjects.Length)
            {
                Plugin.Log.LogMessage("There are now " + newCount + " objects.");
                Plugin.Log.LogWarning("More objects after checking than before. Property calls possibly instantiated unexpected objects.");
            }

            PerformCleanup();

            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            int initialChange = newCount - initialCount;
            float initialPercentChange = ((float)initialChange / (float)initialCount) * 100f;
            int prevChange = newCount - prevCount;
            float prevPercentChange = ((float)prevChange / (float)prevCount) * 100f;

            string qualifier = "more";
            if (prevChange < 0) qualifier = "less";

            Plugin.Log.LogMessage("Sponge took " + elapsedTime.TotalSeconds + " seconds to execute.");
            Plugin.Log.LogMessage("There were " + initialChange + " more objects than on initialization, a " + initialPercentChange + "% change.");
            Plugin.Log.LogMessage("There were " + prevChange + " " + qualifier + " objects than last check, a " + prevPercentChange + "% change.");
            Plugin.Log.LogMessage("---");

            prevCount = newCount;
        }

        private static void PerformEvaluation()
        {
            allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            referenceTracking.Clear();
            previousLeakTracking = leakTracking;
            leakTracking = new Dictionary<string, BundleLeakTracker>();

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

            int gameObjectCount = ExamineGameObjects();

            allObjects = [];

            foreach (string bundleName in leakTracking.Keys)
            {
                if (bundleName != "unknown")
                {
                    Plugin.Log.LogMessage("For AssetBundle/Scene " + bundleName + ": ");
                }
                else
                {
                    Plugin.Log.LogMessage("For Base Game/unknown AssetBundle sources: ");
                }
                BundleLeakTracker tracker = leakTracking[bundleName];
                bool prev = previousLeakTracking.TryGetValue(bundleName, out BundleLeakTracker previousTracker);
                foreach (string assetType in tracker.leakCount.Keys)
                {
                    int newLeakCount = tracker.leakCount[assetType];
                    Plugin.Log.LogMessage(" - " + newLeakCount + " " + assetType + " with no known native unity references.");
                    if (prev && previousTracker.leakCount.ContainsKey(assetType))
                    {
                        int prevLeakCount = previousTracker.leakCount[assetType];
                        if (newLeakCount > prevLeakCount)
                        {
                            Plugin.Log.LogMessage("   - " + (newLeakCount - prevLeakCount) + " more than last check.");
                        }
                    }
                }
                if (tracker.allGameObjects > 0)
                {
                    Plugin.Log.LogMessage(" - " + tracker.allGameObjects + " GameObjects, " + tracker.instantiatedGameObjects + " of which were Instantiated.");
                }
                if (prev)
                {
                    if (tracker.allGameObjects > previousTracker.allGameObjects)
                    {
                        Plugin.Log.LogMessage("   - " + (tracker.allGameObjects - previousTracker.allGameObjects) + " more than last check.");
                    }
                }
            }
            Plugin.Log.LogMessage("The above counts may be inaccurate, use them as approximations. Objects can be attributed to the wrong bundle/scene in the case of overlapping names.");
            Plugin.Log.LogMessage("Objects with no native unity references will likely be cleaned up with UnloadUnusedAssets. Remember that Meshes, Textures, and Materials should be cleaned up manually.");
            Plugin.Log.LogMessage("Unwanted GameObjects will not. Large amounts of GameObjects can be fine, but if these increase day over day there may be an issue.");
        }

        private static void PerformCleanup()
        {
            if (Config.unloadUnused.Value)
            {
                Plugin.Log.LogMessage("Calling Resources.UnloadUnusedAssets().");
                Resources.UnloadUnusedAssets();
            }
        }

        private static int ExamineType<T>() where T : UnityEngine.Object
        {
            int noRefCount = 0;

            T[] allType = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < allType.Length; i++)
            {
                int instanceID = allType[i].GetInstanceID();
                if (!referenceTracking.TryGetValue(instanceID, out ushort refs) || refs < 1)
                {
                    if (HandleLeakedObject(allType[i])) { noRefCount++; }
                }
            }

            allType = [];

            if (noRefCount > 0)
            {
                Plugin.Log.LogMessage("Found " + noRefCount + " " + typeof(T).Name + " with no known references.");
            }

            return noRefCount;
        }

        private static int ExamineGameObjects()
        {
            int gameObjectCount = 0;

            GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < allGameObjects.Length; i++)
            {
                string name = GetOriginalName(allGameObjects[i].name);
                if (bundleTracking.TryGetValue(name, out string bundleName) && leakTracking.ContainsKey(bundleName))
                {
                    if (!(allGameObjects[i].scene.name == null || allGameObjects[i].scene.name == allGameObjects[i].name))
                    {
                        leakTracking[bundleName].allGameObjects++;
                        if (allGameObjects[i].GetInstanceID() < 0)
                        {
                            leakTracking[bundleName].instantiatedGameObjects++;
                        }
                    }
                }
            }

            allGameObjects = [];

            return gameObjectCount;
        }

        public static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Config.verboseLogging.Value)
            {
                streamedSceneTracking.TryGetValue(scene.name, out string bundle);

                foreach (UnityEngine.GameObject obj in scene.GetRootGameObjects())
                {
                    FindGameObjectDependenciesRecursively(bundle ?? scene.name, obj);
                }
            }
        }

        public static void DungeonLoaded(DungeonGenerator generator)
        {
            bundleTracking.TryGetValue(generator.DungeonFlow.name, out string bundle);

            FindGameObjectDependenciesRecursively(bundle ?? generator.DungeonFlow.name, generator.Root);
        }

        public static void ObjectLoaded(AssetBundle bundle, UnityEngine.Object obj)
        {
            GameObject gameObject = obj as GameObject;
            UnityEngine.Component component = obj as UnityEngine.Component;
            if (gameObject != null)
            {
                FindGameObjectDependenciesRecursively(bundle.name, gameObject);
            }
            else if (component != null)
            {
                BuildBundleDependencies(bundle.name, GetUnityObjectReferences(component));
            }
            else
            {
                bundleTracking[obj.name.ToLower()] = bundle.name;
            }
        }

        public static void FindGameObjectDependenciesRecursively(string bundleName, GameObject obj)
        {
            bundleTracking[obj.name.ToLower()] = bundleName;

            foreach (UnityEngine.Component component in obj.GetComponents(typeof(UnityEngine.Component)))
            {
                if (component != null)
                {
                    BuildBundleDependencies(bundleName, GetUnityObjectReferences(component));
                }
            }

            foreach (Transform child in obj.transform)
            {
                FindGameObjectDependenciesRecursively(bundleName, child.gameObject);
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
                    string assetName = Path.GetFileNameWithoutExtension(assetPath).ToLower();
                    bundleTracking.TryAdd(assetName, bundle.name);
                }
            }
        }

        public static IEnumerator RegisterAssetBundleStale(AssetBundle bundle)
        {
            // Delay to catch objects that are still instantiating
            yield return new WaitForSeconds(1.5f);

            GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();

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
                    string assetName = Path.GetFileNameWithoutExtension(assetPath).ToLower();
                    bundleTracking.TryAdd(assetName, bundle.name);

                    GameObject existingObj = allGameObjects.Where(x => x.name.ToLower() == assetName).FirstOrDefault<GameObject>();

                    if (existingObj != default(GameObject))
                    {
                        FindGameObjectDependenciesRecursively(bundle.name, existingObj);
                    }
                }
            }
        }

        private static bool HandleLeakedObject(UnityEngine.Object leakedObj)
        {
            string baseName = GetOriginalName(leakedObj.name);
            bundleTracking.TryGetValue(baseName, out string bundle);
            string bundleName = bundle ?? "unknown";
            // if we find a bundle, make sure it isn't on the blacklist
            if (!ignoredAssetbundles.Contains(bundle))
            {
                string leakedType = leakedObj.GetType().Name;

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

                return true;
            }

            return false;
        }

        private static string GetOriginalName(string name)
        {
            return name.Replace(" (Instance)", "").ToLower();
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

            if (target.GetType() == typeof(Camera)) { return result; }

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
                        if (!mat.HasProperty("_MainTex") && property.Name == "mainTexture")
                        {
                            continue;
                        }
                    }

                    // Need to account for meshes or we'll throw errors.
                    if (target is Mesh)
                    {
                        Mesh mesh = (Mesh)target;
                        if (meshReadProperties.Contains(property.Name))
                        {
                            continue;
                        }
                    }

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
                                    result.Add(reference);
                                }
                            }
                        } 
                        else
                        {
                            UnityEngine.Object reference = property.GetValue(target) as UnityEngine.Object;
                            if (reference != null)
                            {
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

            // Need to account for materials having many textures.
            if (target is Material)
            {
                Material mat = (Material)target;
                foreach (string texName in mat.GetTexturePropertyNames())
                {
                    Texture tex = mat.GetTexture(texName);
                    if (tex != null) result.Add(tex);
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
                if (!bundleTracking.ContainsKey(reference.name.ToLower()))
                {
                    bundleTracking[reference.name.ToLower()] = bundleName;
                    BuildBundleDependencies(bundleName, GetUnityObjectReferences(reference));
                }
            }
        }
    }
}