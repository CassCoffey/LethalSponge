using HarmonyLib;
using Scoops.service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scoops.patches
{
    [HarmonyPatch]
    public class AssetBundleSpongePatch
    {
        static IEnumerable<MethodBase> TargetMethods() => new[]
        {
            AccessTools.Method(typeof(AssetBundle), "LoadFromFile", new Type[] { typeof(string) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromFile", new Type[] { typeof(string), typeof(uint) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromFile", new Type[] { typeof(string), typeof(uint), typeof(ulong) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromMemory", new Type[] { typeof(byte[]) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromMemory", new Type[] { typeof(byte[]), typeof(uint) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromStream", new Type[] { typeof(System.IO.Stream) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromStream", new Type[] { typeof(System.IO.Stream), typeof(uint) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromStream", new Type[] { typeof(System.IO.Stream), typeof(uint), typeof(uint) }),
        };

        static void Postfix(ref AssetBundle __result)
        {
            SpongeService.RegisterAssetBundle(__result);
        }
    }

    [HarmonyPatch]
    public class AssetBundleAsyncSpongePatch
    {
        static IEnumerable<MethodBase> TargetMethods() => new[]
        {
            AccessTools.Method(typeof(AssetBundle), "LoadFromFileAsync", new Type[] { typeof(string) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromFileAsync", new Type[] { typeof(string), typeof(uint) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromFileAsync", new Type[] { typeof(string), typeof(uint), typeof(ulong) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromMemoryAsync", new Type[] { typeof(byte[]) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromMemoryAsync", new Type[] { typeof(byte[]), typeof(uint) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromStreamAsync", new Type[] { typeof(System.IO.Stream) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromStreamAsync", new Type[] { typeof(System.IO.Stream), typeof(uint) }),
            AccessTools.Method(typeof(AssetBundle), "LoadFromStreamAsync", new Type[] { typeof(System.IO.Stream), typeof(uint), typeof(uint) }),
        };

        static void Postfix(ref AssetBundleCreateRequest __result)
        {
            if (__result.isDone)
            {
                SpongeService.RegisterAssetBundle(__result.assetBundle);
            }
            else
            {
                __result.completed += (asyncOperation) =>
                {
                    SpongeService.RegisterAssetBundle(((AssetBundleCreateRequest)asyncOperation).assetBundle);
                };
            }
        }
    }

    [HarmonyPatch]
    public class AssetBundleLoadSpongePatch
    {
        static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(AssetBundle)).Where(x => x.Name.Equals("LoadAsset")).Where(x => !x.IsGenericMethod);

        static void Postfix(ref AssetBundle __instance, ref UnityEngine.Object __result)
        {
            SpongeService.ObjectLoaded(__instance, __result);
        }
    }

    [HarmonyPatch]
    public class AssetBundleLoadAsyncSpongePatch
    {
        static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(AssetBundle)).Where(x => x.Name.Equals("LoadAssetAsync")).Where(x => !x.IsGenericMethod);

        static void Postfix(ref AssetBundle __instance, ref AssetBundleRequest __result)
        {
            AssetBundle bundle = __instance;

            if (__result.isDone)
            {
                SpongeService.ObjectLoaded(bundle, __result.asset);
            }
            else
            {
                __result.completed += (asyncOperation) =>
                {
                    SpongeService.ObjectLoaded(bundle, ((AssetBundleRequest)asyncOperation).asset);
                };
            }
        }
    }

    [HarmonyPatch]
    public class AssetBundleLoadMultipleSpongePatch
    {
        static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(AssetBundle)).Where(x => x.Name.Equals("LoadAllAssets") || x.Name.Equals("LoadAssetWithSubAssets")).Where(x => !x.IsGenericMethod);

        static void Postfix(ref AssetBundle __instance, ref UnityEngine.Object[] __result)
        {
            if (__result != null)
            {
                foreach (UnityEngine.Object obj in __result)
                {
                    SpongeService.ObjectLoaded(__instance, obj);
                }
            }
        }
    }

    [HarmonyPatch]
    public class AssetBundleLoadMultipleAsyncSpongePatch
    {
        static IEnumerable<MethodBase> TargetMethods() => AccessTools.GetDeclaredMethods(typeof(AssetBundle)).Where(x => x.Name.Equals("LoadAllAssetsAsync") || x.Name.Equals("LoadAssetWithSubAssetsAsync")).Where(x => !x.IsGenericMethod);

        static void Postfix(ref AssetBundle __instance, ref AssetBundleRequest __result)
        {
            AssetBundle bundle = __instance;

            if (__result.isDone)
            {
                foreach (UnityEngine.Object obj in __result.allAssets)
                {
                    SpongeService.ObjectLoaded(bundle, obj);
                }
            }
            else
            {
                __result.completed += (asyncOperation) =>
                {
                    foreach (UnityEngine.Object obj in ((AssetBundleRequest)asyncOperation).allAssets)
                    {
                        SpongeService.ObjectLoaded(bundle, obj);
                    }
                };
            }
        }
    }
}
