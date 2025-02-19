using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Scoops.compatibility;
using Scoops.patches;
using Scoops.service;
using System.Collections.Generic;
using UnityEngine;

namespace Scoops;

public static class PluginInformation
{
    public const string PLUGIN_GUID = "LethalSponge";
    public const string PLUGIN_NAME = "LethalSponge";
    public const string PLUGIN_VERSION = "1.0.0";
}

[BepInPlugin(PluginInformation.PLUGIN_GUID, PluginInformation.PLUGIN_NAME, PluginInformation.PLUGIN_VERSION)]
[BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; set; }

    public static Config SpongeConfig { get; internal set; }

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new(PluginInformation.PLUGIN_GUID);

    public Plugin()
    {
        Instance = this;
    }

    private void Awake()
    {
        Log.LogInfo("Loading LethalSponge Version " + PluginInformation.PLUGIN_VERSION);

        SpongeConfig = new(base.Config);

        SpongeService.PluginLoad();

        Log.LogInfo($"Applying base patches...");
        ApplyPluginPatch();
        Log.LogInfo($"Base patches applied");
        
        if (Scoops.Config.verboseLogging.Value)
        {
            Log.LogInfo($"Applying verbose patches...");
            ApplyVerbosePluginPatch();
            Log.LogInfo($"Verbose patches applied");

            IEnumerable<AssetBundle> allBundles = AssetBundle.GetAllLoadedAssetBundles();
            foreach (AssetBundle bundle in allBundles)
            {
                StartCoroutine(SpongeService.RegisterAssetBundleStale(bundle));
            }

            if (LLLCompat.Enabled)
            {
                Log.LogInfo($"Lethal Level Loader compat enabled...");
                LLLCompat.AddBundleHook();
            }
        }
    }

    private void ApplyPluginPatch()
    {
        _harmony.PatchAll(typeof(StartOfRoundSpongePatch));
    }

    private void ApplyVerbosePluginPatch()
    {
        _harmony.PatchAll(typeof(RoundManagerSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleAsyncSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleLoadSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleLoadAsyncSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleLoadMultipleSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleLoadMultipleAsyncSpongePatch));
    }
}
