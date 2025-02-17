using HarmonyLib;
using Scoops.service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
}
