using HarmonyLib;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Unity.Profiling.Memory;
using UnityEngine;
using UnityEngine.UIElements;

namespace Scoops.service
{
    internal struct ModLeakTracker
    {
        public int meshCount;
        public int gameObjectCount;
    }

    internal static class SpongeService
    {
        private static UnityEngine.Component[] allComponents;
        private static IEnumerable<AssetBundle> allBundles;

        private static Dictionary<string, ModLeakTracker> leakTracking;
        private static Dictionary<int, ushort> referenceTracking;

        private static Stopwatch stopwatch = new Stopwatch();

        private static int prevCount = 0;

        public static void Initialize()
        {
            Plugin.Log.LogInfo("---");
            Plugin.Log.LogInfo("Initialising Sponge");

            leakTracking = new Dictionary<string, ModLeakTracker>();
            referenceTracking = new Dictionary<int, ushort>();
            Mesh[] allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
            Plugin.Log.LogInfo("Initial count of " + allMeshes.Length + " meshes loaded.");
            prevCount = allMeshes.Length;
            allMeshes = [];
            allComponents = [];

            Plugin.Log.LogInfo("Sponge Initialised.");
            Plugin.Log.LogInfo("---");
        }

        public static void PerformCleanup()
        {
            Plugin.Log.LogInfo("---");
            Plugin.Log.LogInfo("Running Sponge");
            stopwatch.Restart();
            
            Mesh[] allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();
            Plugin.Log.LogInfo("Last run there were " + prevCount + " meshes loaded, now there are " + allMeshes.Length);

            allComponents = Resources.FindObjectsOfTypeAll<UnityEngine.Component>();
            referenceTracking.Clear();

            allBundles = AssetBundle.GetAllLoadedAssetBundles();

            // Initial pass to build references
            for (int i = 0; i < allComponents.Length; i++)
            {
                GetObjectReferences(i);
            }

            uint noRefCount = 0;

            // Second pass to find objects with no references
            for (int i = 0; i < allMeshes.Length; i++)
            {
                int instanceID = allMeshes[i].GetInstanceID();
                if (!referenceTracking.TryGetValue(instanceID, out ushort refs) || refs < 1)
                {
                    noRefCount++;
                }
            }

            Plugin.Log.LogInfo("Found " + noRefCount + " meshes with no references.");

            allMeshes = Resources.FindObjectsOfTypeAll<Mesh>();

            Plugin.Log.LogInfo("Reduced to " + allMeshes.Length + " meshes.");
            prevCount = allMeshes.Length;
            allMeshes = [];
            allComponents = [];

            stopwatch.Stop();
            TimeSpan elapsedTime = stopwatch.Elapsed;
            Plugin.Log.LogInfo("Sponge took " + elapsedTime.TotalSeconds + " seconds to execute.");
            Plugin.Log.LogInfo("---");
        }

        private static readonly Dictionary<Type, List<FieldInfo>> assignableFieldsByComponentType = new Dictionary<Type, List<FieldInfo>>() { { typeof(UnityEngine.Component), null } };
        private static readonly Dictionary<Type, List<PropertyInfo>> assignablePropertiesByComponentType = new Dictionary<Type, List<PropertyInfo>>() { { typeof(UnityEngine.Component), null } };

        public static void GetObjectReferences(int index)
        {
            var target = allComponents[index];

            if (target is not UnityEngine.Component) { return; }

            Type componentType = target.GetType();

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
                    if (!(field.FieldType.IsSubclassOf(typeof(UnityEngine.Object)) || field.FieldType == typeof(UnityEngine.Object))) { continue; }

                    UnityEngine.Object reference = field.GetValue(target) as UnityEngine.Object;
                    if (reference != null)
                    {
                        int refID = reference.GetInstanceID();
                        if (referenceTracking.ContainsKey(refID))
                        {
                            referenceTracking[refID]++;
                        }
                        else
                        {
                            referenceTracking.Add(refID, 1);
                        }
                    }
                }
            }

            if (assignableProperties is not null)
            {
                foreach (var property in assignableProperties)
                {
                    if (!property.CanRead || !(property.PropertyType.IsSubclassOf(typeof(UnityEngine.Object)) || property.PropertyType == typeof(UnityEngine.Object))) { continue; }

                    UnityEngine.Object reference = null;

                    try
                    {
                        reference = property.GetValue(target) as UnityEngine.Object;
                    }
                    catch (Exception e)
                    {
                        Plugin.Log.LogWarning("Error while checking properties, ignoring property and continuing:");
                        Plugin.Log.LogWarning(e);

                        continue;
                    }
                    
                    if (reference != null)
                    {
                        int refID = reference.GetInstanceID();
                        if (referenceTracking.ContainsKey(refID))
                        {
                            referenceTracking[refID]++;
                        }
                        else
                        {
                            referenceTracking.Add(refID, 1);
                        }
                    }
                }
            }
        }
    }
}