using DunGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Properties;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using static System.Net.Mime.MediaTypeNames;
using static UnityEngine.Rendering.HighDefinition.CopyFilterAttribute;

namespace Scoops.service
{
    public enum SpongeMode
    {
        Evaluate,
        Clean,
        Full,
    }

    internal class BundleLeakTracker
    {
        public Dictionary<string, int> leakCount;
        public Dictionary<string, int> objectCount;

        public BundleLeakTracker() 
        {
            leakCount = new Dictionary<string, int>();
            objectCount = new Dictionary<string, int>();
        }
    }

    internal static class SpongeService
    {
        public static bool enabled = true;

        private static readonly Dictionary<Type, List<FieldInfo>> assignableFieldsByObjectType = new Dictionary<Type, List<FieldInfo>>() { { typeof(UnityEngine.Object), null } };
        private static readonly Dictionary<Type, List<PropertyInfo>> assignablePropertiesByObjectType = new Dictionary<Type, List<PropertyInfo>>() { { typeof(UnityEngine.Object), null } };

        private static UnityEngine.Object[] allObjects;

        // List of known Getters that will create new objects or problems (Unity whyyyy)
        private static List<String> ignoredProperties = new List<string>();
        private static List<String> ignoredAssetbundles = new List<string>();
        private static List<String> focusedAssetbundles = new List<string>();
        private static List<String> fullReportBundles = new List<string>();

        private static Dictionary<string, BundleLeakTracker> leakTracking = new Dictionary<string, BundleLeakTracker>();
        private static Dictionary<string, BundleLeakTracker> previousLeakTracking = new Dictionary<string, BundleLeakTracker>();
        private static Dictionary<int, ushort> referenceTracking = new Dictionary<int, ushort>();
        private static Dictionary<string, string> bundleTracking = new Dictionary<string, string>();
        private static Dictionary<string, string> streamedSceneTracking = new Dictionary<string, string>();

        private static Stopwatch stopwatch = new Stopwatch();

        private static int prevCount = 0;
        private static int initialCount = 0;
        private static int newCount = 0;

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

            if (!Config.minimalLogging.Value)
            {
                allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                Plugin.Log.LogMessage("Initial count of " + allObjects.Length + " objects loaded.");
                prevCount = allObjects.Length;
                initialCount = allObjects.Length;
                allObjects = [];
            }

            Plugin.Log.LogMessage("Type '/sponge help' in chat for commands.");
            Plugin.Log.LogMessage("Sponge Initialised.");
            Plugin.Log.LogMessage("---");
        }

        public static void ParseConfig()
        {
            if (Config.propertyBlacklist.Value != "")
            {
                ignoredProperties.AddRange(Config.propertyBlacklist.Value.ToLower().Split(';'));
            }
            if (Config.assetbundleBlacklist.Value != "")
            {
                ignoredAssetbundles.AddRange(Config.assetbundleBlacklist.Value.ToLower().Split(';'));
            }
            if (Config.assetbundleWhitelist.Value != "")
            {
                focusedAssetbundles.AddRange(Config.assetbundleWhitelist.Value.ToLower().Split(';'));
            }
            if (Config.fullReportList.Value != "")
            {
                fullReportBundles.AddRange(Config.fullReportList.Value.ToLower().Split(';'));
            }

            enabled = Config.runDaily.Value;
        }

        public static bool AssetBundleValid(string bundleName)
        {
            if (focusedAssetbundles.Count > 0)
            {
                return focusedAssetbundles.Contains(bundleName.ToLower());
            }
            
            return !ignoredAssetbundles.Contains(bundleName.ToLower());
        }

        public static void ApplySponge(SpongeMode mode = SpongeMode.Full)
        {
            Plugin.Log.LogMessage("---");
            Plugin.Log.LogMessage("Applying Sponge");
            stopwatch.Restart();

            if (Config.verboseLogging.Value && (mode == SpongeMode.Evaluate || mode == SpongeMode.Full))
            {
                PerformEvaluation();
            }
            if (!Config.minimalLogging.Value)
            {
                allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                newCount = allObjects.Length;
                Plugin.Log.LogMessage("There are " + newCount + " loaded objects total.");
            }
            if (Config.verboseLogging.Value && (mode == SpongeMode.Evaluate || mode == SpongeMode.Full) && newCount > allObjects.Length)
            {
                Plugin.Log.LogWarning("More objects after evaluating than before. Property calls possibly instantiated unexpected objects.");
            }

            allObjects = [];

            if (mode == SpongeMode.Clean || mode == SpongeMode.Full)
            {
                PerformCleanup();
            }
        }

        private static void FinishSponge()
        {
            if (Config.unloadUnused.Value)
            {
                Plugin.Log.LogMessage("Resources.UnloadUnusedAssets() completed.");
                if (!Config.minimalLogging.Value)
                {
                    allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
                    int beforeCleanCount = newCount;
                    newCount = allObjects.Length;
                    Plugin.Log.LogMessage("After cleaning there are " + newCount + " loaded objects total.");
                    Plugin.Log.LogMessage("Resources.UnloadUnusedAssets() cleaned up " + (beforeCleanCount - newCount) + " objects.");
                }
                allObjects = [];
            }

            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            if (!Config.minimalLogging.Value)
            {
                int initialChange = newCount - initialCount;
                float initialPercentChange = ((float)initialChange / (float)initialCount) * 100f;
                int prevChange = newCount - prevCount;
                float prevPercentChange = ((float)prevChange / (float)prevCount) * 100f;

                string qualifier = "more";
                if (prevChange < 0) qualifier = "less";

                Plugin.Log.LogMessage("Sponge took " + elapsedTime.TotalSeconds + " seconds to execute.");

                Plugin.Log.LogMessage("There were " + initialChange + " more objects than on initialization, a " + initialPercentChange + "% change.");
                Plugin.Log.LogMessage("There were " + prevChange + " " + qualifier + " objects than last check, a " + prevPercentChange + "% change.");

                prevCount = newCount;
            }
            else
            {
                Plugin.Log.LogMessage("Sponge took " + elapsedTime.TotalSeconds + " seconds to execute.");
            }
            Plugin.Log.LogMessage("---");
        }

        private static void PerformEvaluation()
        {
            allObjects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            referenceTracking.Clear();
            previousLeakTracking = leakTracking;
            leakTracking = new Dictionary<string, BundleLeakTracker>();

            Plugin.Log.LogMessage("Evaluating loaded objects, please wait.");

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

            int gameObjectCount = CountType<GameObject>();
            int cameraCount = CountType<Camera>();

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
                if (!prev)
                {
                    Plugin.Log.LogMessage(" - AssetBundle/Scene was not present on previous checks.");
                }
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
                foreach (string assetType in tracker.objectCount.Keys)
                {
                    int newObjectCount = tracker.objectCount[assetType];
                    Plugin.Log.LogMessage(" - " + newObjectCount + " " + (Config.ignoreInactiveObjects.Value ? "Active " : "") + assetType + ".");
                    if (prev && previousTracker.objectCount.ContainsKey(assetType))
                    {
                        int prevObjectCount = previousTracker.objectCount[assetType];
                        if (newObjectCount > prevObjectCount)
                        {
                            Plugin.Log.LogMessage("   - " + (newObjectCount - prevObjectCount) + " more than last check.");
                        }
                    }
                }
            }
            Plugin.Log.LogMessage("The above counts may be inaccurate, use them as approximations. Objects can be attributed to the wrong bundle/scene in the case of overlapping names.");
            Plugin.Log.LogMessage("Remember that Meshes, Textures, and Materials should be cleaned up manually, but if they have no native unity references they will be cleaned up by UnloadUnusedAssets.");
            Plugin.Log.LogMessage("Unwanted GameObjects will never be cleaned up automatically. Large amounts of GameObjects can be fine, but if these increase day over day there may be an issue.");
        }

        private static void PerformCleanup()
        {
            if (Config.unloadUnused.Value)
            {
                Plugin.Log.LogMessage("Calling Resources.UnloadUnusedAssets().");
                Resources.UnloadUnusedAssets().completed += (asyncOperation) =>
                {
                    FinishSponge();
                };
            } 
            else
            {
                FinishSponge();
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

        private static int CountType<T>() where T : UnityEngine.Object
        {
            int typeCount = 0;

            T[] allType = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < allType.Length; i++)
            {
                string bundle = TryGetObjectBundleName(allType[i].name);

                // if we find a bundle, make sure it isn't on the blacklist
                if (!AssetBundleValid(bundle)) { continue; }

                if (typeof(GameObject).IsAssignableFrom(typeof(T)))
                {
                    GameObject gameObject = allType[i] as GameObject;
                    if (gameObject.scene.name == null || gameObject.scene.name == gameObject.name || (Config.ignoreInactiveObjects.Value && !gameObject.activeInHierarchy))
                    {
                        continue;
                    }
                }

                if (typeof(UnityEngine.Component).IsAssignableFrom(typeof(T)))
                {
                    Behaviour behavior = allType[i] as Behaviour;
                    if (Config.ignoreInactiveObjects.Value && (!behavior.enabled || !behavior.gameObject.activeInHierarchy))
                    {
                        continue;
                    }
                }

                string countedType = typeof(T).Name;

                if (fullReportBundles.Contains(bundle.ToLower()))
                {
                    Plugin.Log.LogMessage("Counted " + (Config.ignoreInactiveObjects.Value ? "Active " : "") + countedType + " with name '" + allType[i].name + "' and ID '" + allType[i].GetInstanceID() + "' from bundle/scene " + bundle + ".");
                }

                typeCount++;

                if (leakTracking.ContainsKey(bundle))
                {
                    leakTracking[bundle].objectCount.TryGetValue(countedType, out int currentCount);
                    leakTracking[bundle].objectCount[countedType] = currentCount + 1;
                }
                else
                {
                    leakTracking.Add(bundle, new BundleLeakTracker());
                    leakTracking[bundle].objectCount.Add(countedType, 1);
                }
            }

            allType = [];

            if (typeCount > 0)
            {
                Plugin.Log.LogMessage("Found " + typeCount + " " + (Config.ignoreInactiveObjects.Value ? "Active " : "") + typeof(T).Name + ".");
            }

            return typeCount;
        }

        public static void ModelCheck()
        {
            Plugin.Log.LogMessage("---");
            Plugin.Log.LogMessage("Running Sponge Model Check.");

            int meshCount = 0;
            int vertCount = 0;

            Dictionary<string, Vector2Int> BundleVertDict = new Dictionary<string, Vector2Int>();
            HashSet<Mesh> checkedMeshes = new HashSet<Mesh>();

            List<MeshFilter> bigWinners = new List<MeshFilter>(10);

            MeshFilter[] allMeshFilters = UnityEngine.Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (MeshFilter filter in allMeshFilters)
            {
                if (filter == null || filter.sharedMesh == null || checkedMeshes.Contains(filter.sharedMesh)) { continue; }
                string bundle = TryGetObjectBundleName(filter.sharedMesh.name);

                // if we find a bundle, make sure it isn't on the blacklist
                if (!AssetBundleValid(bundle)) { continue; }

                Renderer meshRenderer = filter.gameObject.GetComponent<Renderer>();

                if (meshRenderer == null || !meshRenderer.isVisible) { continue; }

                int vertexCount = filter.sharedMesh.vertexCount;

                Vector2Int newCount = new Vector2Int(vertexCount, 1);

                vertCount += vertexCount;
                meshCount++;

                BundleVertDict.TryGetValue(bundle, out Vector2Int currentCounts);
                BundleVertDict[bundle] = currentCounts + newCount;

                checkedMeshes.Add(filter.sharedMesh);

                if ((bigWinners.Count < 10 || bigWinners[0].sharedMesh.vertexCount < vertexCount) && !bigWinners.Contains(filter))
                {
                    if (bigWinners.Count == 10)
                    {
                        bigWinners[0] = filter;
                    } else
                    {
                        bigWinners.Add(filter);
                    }

                    bigWinners.Sort((a, b) => { return a.sharedMesh.vertexCount.CompareTo(b.sharedMesh.vertexCount); });
                }

                if (vertexCount > 1000)
                {
                    float cubedMeters = meshRenderer.bounds.size.x * meshRenderer.bounds.size.y * meshRenderer.bounds.size.z;
                    float vertPerMeter = (float)vertexCount / cubedMeters;

                    if (vertPerMeter > 5000f)
                    {
                        Plugin.Log.LogWarning("Mesh " + filter.sharedMesh.name + " for GameObject " + filter.gameObject.name + " has vertex density of " + vertPerMeter);
                        Transform rootParent = filter.gameObject.transform;
                        while (rootParent.parent != null)
                        {
                            rootParent = rootParent.parent;
                        }
                        Plugin.Log.LogWarning(" - " + vertexCount + " vertices over " + cubedMeters + " cubed meters.");
                        Plugin.Log.LogWarning(" - On root GameObject " + rootParent.gameObject.name + ".");
                        if (bundle != "unknown") Plugin.Log.LogWarning(" - From the bundle '" + bundle + "'");
                    }
                }
            }

            Plugin.Log.LogMessage("Found " + meshCount + " Visible Meshes.");
            Plugin.Log.LogMessage("With " + vertCount + " Vertices total.");

            foreach (string key in BundleVertDict.Keys)
            {
                if (key == "unknown") continue;

                Plugin.Log.LogMessage(" - Bundle '" + key + "' contributed " + BundleVertDict[key].y + " Visible Meshes");
                Plugin.Log.LogMessage("   and " + BundleVertDict[key].x + " Vertices total.");
            }

            Plugin.Log.LogMessage("The " + bigWinners.Count + " largest meshes were:");
            foreach (MeshFilter filter in bigWinners)
            {
                string bundle = TryGetObjectBundleName(filter.sharedMesh.name);

                Plugin.Log.LogMessage(" - Mesh '" + filter.sharedMesh.name + "' with " + filter.sharedMesh.vertexCount + " Vertices");
                Plugin.Log.LogMessage("   on the GameObject with the name '" + filter.gameObject.name + "'");
                if (bundle != "unknown") Plugin.Log.LogMessage("   from the bundle '" + bundle + "'");
            }
            Plugin.Log.LogMessage("The above counts may be inaccurate. Meshes can be attributed to the wrong bundle/scene in the case of overlapping names.");
            Plugin.Log.LogMessage("---");
        }

        public static void TextureCheck()
        {
            Plugin.Log.LogMessage("---");
            Plugin.Log.LogMessage("Running Sponge Texture Check.");

            int texCount = 0;
            int pixCount = 0;

            Dictionary<string, Vector2Int> BundlePixelDict = new Dictionary<string, Vector2Int>();
            HashSet<Texture> checkedTextures = new HashSet<Texture>();

            List<Texture> bigWinners = new List<Texture>(10);

            Renderer[] allRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (Renderer renderer in allRenderers)
            {
                if (renderer == null || !renderer.isVisible) { continue; }

                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat == null || mat.shader == null || mat.shader.GetPropertyCount() == 0) { continue; }

                    List<Texture> allTextures = new List<Texture>();
                    for (int i = 0; i < mat.shader.GetPropertyCount(); i++)
                    {
                        if (mat.shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Texture)
                        {
                            Texture propertyTex = mat.GetTexture(mat.shader.GetPropertyName(i));
                            if (propertyTex != null)
                            {
                                allTextures.Add(propertyTex);
                            }
                        }
                    }

                    foreach (Texture tex in allTextures)
                    {
                        if (checkedTextures.Contains(tex)) { continue; }

                        string bundle = TryGetObjectBundleName(tex.name);

                        // if we find a bundle, make sure it isn't on the blacklist
                        if (!AssetBundleValid(bundle)) { continue; }

                        int pixelCount = tex.height * tex.width;

                        Vector2Int newCount = new Vector2Int(pixCount, 1);

                        pixCount += pixelCount;
                        texCount++;

                        BundlePixelDict.TryGetValue(bundle, out Vector2Int currentCounts);
                        BundlePixelDict[bundle] = currentCounts + newCount;

                        checkedTextures.Add(tex);

                        if ((bigWinners.Count < 10 || (bigWinners[0].height * bigWinners[0].width) < pixelCount) && !bigWinners.Contains(tex))
                        {
                            if (bigWinners.Count == 10)
                            {
                                bigWinners[0] = tex;
                            }
                            else
                            {
                                bigWinners.Add(tex);
                            }

                            bigWinners.Sort((a, b) => { return (a.height * a.width).CompareTo(b.height * b.width); });
                        }

                        if (pixelCount > 1048576)
                        {
                            float largestDimension = Mathf.Max(renderer.bounds.size.x, Mathf.Max(renderer.bounds.size.y, renderer.bounds.size.z));
                            float cubedMeters = largestDimension * largestDimension * largestDimension;
                            float pixPerMeter = (float)pixelCount / cubedMeters;

                            if (pixPerMeter > 500000f)
                            {
                                Plugin.Log.LogWarning("Texture " + tex.name + " for GameObject " + renderer.gameObject.name + " has pixel density of " + pixPerMeter);
                                Transform rootParent = renderer.gameObject.transform;
                                while (rootParent.parent != null)
                                {
                                    rootParent = rootParent.parent;
                                }
                                Plugin.Log.LogWarning(" - Dimensions " + tex.width + "x" + tex.height + " for " + cubedMeters + " cubed meters.");
                                Plugin.Log.LogWarning(" - On root GameObject " + rootParent.gameObject.name + ".");
                                if (bundle != "unknown") Plugin.Log.LogWarning(" - From the bundle '" + bundle + "'");
                            }
                        }
                    }
                }
            }

            Plugin.Log.LogMessage("Found " + texCount + " Visible Textures.");
            Plugin.Log.LogMessage("With " + pixCount + " Pixels total.");

            foreach (string key in BundlePixelDict.Keys)
            {
                if (key == "unknown") continue;

                Plugin.Log.LogMessage(" - Bundle '" + key + "' contributed " + BundlePixelDict[key].y + " Visible Textures");
                Plugin.Log.LogMessage("   and " + BundlePixelDict[key].x + " Pixels total.");
            }

            Plugin.Log.LogMessage("The " + bigWinners.Count + " largest textures were:");
            foreach (Texture texture in bigWinners)
            {
                string bundle = TryGetObjectBundleName(texture.name);

                Plugin.Log.LogMessage(" - Texture '" + texture.name + "' with dimensions " + texture.width + "x" + texture.height);
                if (bundle != "unknown") Plugin.Log.LogMessage("   from the bundle '" + bundle + "'");
            }
            Plugin.Log.LogMessage("The above counts may be inaccurate. Textures can be attributed to the wrong bundle/scene in the case of overlapping names.");
            Plugin.Log.LogMessage("---");
        }

        public static void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (Config.verboseLogging.Value)
            {
                streamedSceneTracking.TryGetValue(scene.name, out string bundle);

                foreach (GameObject obj in scene.GetRootGameObjects())
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
            string bundleName = FormatBundleName(bundle.name);

            GameObject gameObject = obj as GameObject;
            UnityEngine.Component component = obj as UnityEngine.Component;
            if (gameObject != null)
            {
                FindGameObjectDependenciesRecursively(bundleName, gameObject);
            }
            else if (component != null)
            {
                BuildBundleDependencies(bundleName, GetUnityObjectReferences(component));
            }
            else
            {
                BuildBundleDependencies(bundleName, obj);
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
            string bundleName = FormatBundleName(bundle.name);

            if (bundle.isStreamedSceneAssetBundle)
            {
                foreach (string scenePath in bundle.GetAllScenePaths())
                {
                    string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                    streamedSceneTracking.TryAdd(sceneName, bundleName);
                }
            }
            else
            {
                foreach (string assetPath in bundle.GetAllAssetNames())
                {
                    string assetName = Path.GetFileNameWithoutExtension(assetPath).ToLower();
                    bundleTracking.TryAdd(assetName, bundleName);
                }
            }
        }

        public static IEnumerator CheckAllGameObjectDependencies(GameObject[] allGameObjects)
        {
            List<string> checkedObjects = new List<string>();

            foreach (GameObject obj in allGameObjects)
            {
                if (!checkedObjects.Contains(obj.name) && obj.transform.parent == null)
                {
                    if (bundleTracking.TryGetValue(obj.name.ToLower(), out string bundleName))
                    {
                        checkedObjects.Add(obj.name);
                        FindGameObjectDependenciesRecursively(bundleName, obj);
                    }
                }
            }

            yield return null;
        }

        private static string FormatBundleName(string bundleName)
        {
            return (bundleName == null || bundleName == "" ? "unknown" : bundleName).ToLower();
        }

        private static bool HandleLeakedObject(UnityEngine.Object leakedObj)
        {
            string bundle = TryGetObjectBundleName(leakedObj.name);

            // if we find a bundle, make sure it isn't on the blacklist
            if (!AssetBundleValid(bundle)) { return false; }

            string leakedType = leakedObj.GetType().Name;

            if (fullReportBundles.Contains(bundle))
            {
                Plugin.Log.LogMessage("Found suspected leaked " + leakedType + " with name '" + leakedObj.name + "' and ID '" + leakedObj.GetInstanceID() + "' from bundle/scene " + bundle + ".");
            }

            if (leakTracking.ContainsKey(bundle))
            {
                leakTracking[bundle].leakCount.TryGetValue(leakedType, out int currentCount);
                leakTracking[bundle].leakCount[leakedType] = currentCount + 1;
            }
            else
            {
                leakTracking.Add(bundle, new BundleLeakTracker());
                leakTracking[bundle].leakCount.Add(leakedType, 1);
            }

            return true;
        }

        private static string TryGetObjectBundleName(string name)
        {
            string objectName = name.ToLower();

            bundleTracking.TryGetValue(objectName, out string bundleName);

            if (bundleName == null)
            {
                bundleTracking.TryGetValue(objectName.Replace(" (instance)", ""), out bundleName);
            }

            if (bundleName == null)
            {
                bundleTracking.TryGetValue(objectName.Replace("(clone)", ""), out bundleName);
            }

            bundleName ??= "unknown";

            return bundleName;
        }

        public static List<UnityEngine.Object> GetUnityObjectReferences(int index)
        {
            var target = allObjects[index];
            if (target == null || target is not UnityEngine.Object) { return new List<UnityEngine.Object>(); }

            return GetUnityObjectReferences(target);
        }

        public static List<UnityEngine.Object> GetUnityObjectReferences(UnityEngine.Object target)
        {
            List<UnityEngine.Object> result = new List<UnityEngine.Object>();
            Type objectType = target.GetType();

            try
            {
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
                        string propertyId = (property.DeclaringType.Name + "." + property.Name).ToLower();
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
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Error while retrieving object references for " + target.name + " of type " + objectType.Name + ", continuing:");
                Plugin.Log.LogWarning(e);

                return result;
            }
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

        public static void BuildBundleDependencies(string bundleName, UnityEngine.Object reference)
        {
            if (!AssetBundleValid(bundleName) || reference == null) { return; }

            if (!bundleTracking.ContainsKey(reference.name.ToLower()))
            {
                bundleTracking[reference.name.ToLower()] = bundleName;
                BuildBundleDependencies(bundleName, GetUnityObjectReferences(reference));
            }
        }

        public static void BuildBundleDependencies(string bundleName, List<UnityEngine.Object> references)
        {
            if (!AssetBundleValid(bundleName)) { return; }

            foreach (UnityEngine.Object reference in references)
            {
                BuildBundleDependencies(bundleName, reference);
            }
        }
    }
}