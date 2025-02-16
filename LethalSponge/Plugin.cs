using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Scoops.patches;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scoops;

public static class PluginInformation
{
    public const string PLUGIN_GUID = "LethalSponge";
    public const string PLUGIN_NAME = "LethalSponge";
    public const string PLUGIN_VERSION = "1.0.0";
}

[BepInPlugin(PluginInformation.PLUGIN_GUID, PluginInformation.PLUGIN_NAME, PluginInformation.PLUGIN_VERSION)]
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

        Log.LogInfo($"Applying patches...");
        ApplyPluginPatch();
        Log.LogInfo($"Patches applied");
    }

    /// <summary>
    /// Applies the patch to the game.
    /// </summary>
    private void ApplyPluginPatch()
    {
        _harmony.PatchAll(typeof(StartOfRoundSpongePatch));
    }
}
