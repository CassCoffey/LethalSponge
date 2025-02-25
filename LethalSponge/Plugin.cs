using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Scoops.compatibility;
using Scoops.patches;
using Scoops.service;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using System;

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

    public static AssetBundle SpongeAssets;

    public static ManualLogSource Log => Instance.Logger;

    private readonly Harmony _harmony = new(PluginInformation.PLUGIN_GUID);

    public Plugin()
    {
        Instance = this;
    }

    private void Awake()
    {
        Log.LogInfo("Loading LethalSponge Version " + PluginInformation.PLUGIN_VERSION);

        var dllFolderPath = System.IO.Path.GetDirectoryName(Info.Location);
        var assetBundleFilePath = System.IO.Path.Combine(dllFolderPath, "spongeassets");
        SpongeAssets = AssetBundle.LoadFromFile(assetBundleFilePath);

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

            StartCoroutine(RegisterAssetBundlesStale());

            if (LLLCompat.Enabled)
            {
                Log.LogInfo($"Lethal Level Loader compat enabled...");
                LLLCompat.AddBundleHook();
            }
        }

        AlterQualitySettings();
    }

    public IEnumerator RegisterAssetBundlesStale()
    {
        // Delay to catch objects that are still instantiating
        yield return new WaitForSeconds(1.5f);

        Plugin.Log.LogMessage("Sponge is acquiring assetbundle references, please wait.");
        GameObject[] allGameObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        IEnumerable<AssetBundle> allBundles = AssetBundle.GetAllLoadedAssetBundles();

        foreach (AssetBundle bundle in allBundles)
        {
            SpongeService.RegisterAssetBundle(bundle);
        }

        StartCoroutine(SpongeService.CheckAllGameObjectDependencies(allGameObjects));

        Plugin.Log.LogMessage("Assetbundle references acquired.");

        allGameObjects = [];
    }

    private void ApplyPluginPatch()
    {
        _harmony.PatchAll(typeof(StartOfRoundSpongePatch));

        if (Scoops.Config.fixFoliageLOD.Value)
        {
            _harmony.PatchAll(typeof(FoliageDetailDistanceSpongePatch));
        }

        if (Scoops.Config.patchCameraScript.Value)
        {
            _harmony.PatchAll(typeof(ManualCameraRendererSpongePatch));
        }

        if (Scoops.Config.disableBloom.Value || Scoops.Config.disableDOF.Value || Scoops.Config.disableMotionBlur.Value || Scoops.Config.disableShadows.Value || Scoops.Config.disableMotionVectors.Value || Scoops.Config.disableRefraction.Value || Scoops.Config.disableReflections.Value || Scoops.Config.useCustomShader.Value)
        {
            _harmony.PatchAll(typeof(PlayerControllerBSpongePatch));
        }

        if (Scoops.Config.useCustomShader.Value)
        {
            _harmony.PatchAll(typeof(HDRenderPipeline_RecordRenderGraph_Patch));
        }
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

    private void AlterQualitySettings()
    {
        if (!Scoops.Config.qualityOverrides.Value) return;

        RenderPipelineSettings settings = ((HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).currentPlatformRenderPipelineSettings;

        if (Scoops.Config.deferredOnly.Value)
        {
            // It might be too late for this to help -_-
            settings.supportedLitShaderMode = RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly;
        }

        settings.decalSettings.drawDistance = Scoops.Config.decalDrawDist.Value;
        settings.decalSettings.atlasHeight = Scoops.Config.decalAtlasSize.Value;
        settings.decalSettings.atlasWidth = Scoops.Config.decalAtlasSize.Value;

        settings.lightLoopSettings.maxLocalVolumetricFogOnScreen = Scoops.Config.maxVolumetricFog.Value;
        settings.lightLoopSettings.reflectionProbeTexCacheSize = Enum.Parse<ReflectionProbeTextureCacheResolution>(Scoops.Config.reflectionAtlasSize.Value);
        settings.lightLoopSettings.maxCubeReflectionOnScreen = Scoops.Config.maxCubeReflectionProbes.Value;
        settings.lightLoopSettings.maxPlanarReflectionOnScreen = Scoops.Config.maxPlanarReflectionProbes.Value;
        // disabled until I have a distance based light culling system
        //settings.lightLoopSettings.maxDirectionalLightsOnScreen = Scoops.Config.maxDirectionalLights.Value;
        //settings.lightLoopSettings.maxPunctualLightsOnScreen = Scoops.Config.maxPunctualLights.Value;
        //settings.lightLoopSettings.maxAreaLightsOnScreen = Scoops.Config.maxAreaLights.Value;

        settings.hdShadowInitParams.maxShadowRequests = Scoops.Config.maxShadows.Value;
        settings.hdShadowInitParams.maxPunctualShadowMapResolution = Scoops.Config.shadowsMaxResolution.Value;
        settings.hdShadowInitParams.maxDirectionalShadowMapResolution = Scoops.Config.shadowsMaxResolution.Value;
        settings.hdShadowInitParams.maxAreaShadowMapResolution = Scoops.Config.shadowsMaxResolution.Value;
        settings.hdShadowInitParams.punctualLightShadowAtlas.shadowAtlasResolution = Scoops.Config.shadowsAtlasSize.Value;
        settings.hdShadowInitParams.cachedPunctualLightShadowAtlas = Scoops.Config.shadowsAtlasSize.Value / 2; // Just make it half size for now
        settings.hdShadowInitParams.areaLightShadowAtlas.shadowAtlasResolution = Scoops.Config.shadowsAtlasSize.Value;
        settings.hdShadowInitParams.cachedAreaLightShadowAtlas = Scoops.Config.shadowsAtlasSize.Value / 2;

        settings.lightingQualitySettings.Fog_Budget[QualitySettings.GetQualityLevel()] = Scoops.Config.fogBudget.Value;

        ((HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).m_RenderPipelineSettings = settings;
        ((HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).OnValidate();
    }
}
