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
using UnityEngine.SceneManagement;
using System.IO;

namespace Scoops;

public static class PluginInformation
{
    public const string PLUGIN_GUID = "LethalSponge";
    public const string PLUGIN_NAME = "LethalSponge";
    public const string PLUGIN_VERSION = "1.2.0";
}

[BepInPlugin(PluginInformation.PLUGIN_GUID, PluginInformation.PLUGIN_NAME, PluginInformation.PLUGIN_VERSION)]
[BepInDependency("imabatby.lethallevelloader", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("me.swipez.melonloader.morecompany", BepInDependency.DependencyFlags.SoftDependency)]
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

        if (Scoops.Config.fixComplexMeshes.Value || Scoops.Config.generateLODs.Value)
        {
            MeshService.Init();
        }

        if (Scoops.Config.fixComplexMeshes.Value && Scoops.Config.fixComplexCosmetics.Value && MoreCompanyCompat.Enabled)
        {
            MoreCompanyCompat.DecimateAllCosmetics();
        }
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
        _harmony.PatchAll(typeof(RoundManagerSpongePatch));

        if (Scoops.Config.unloadUnused.Value)
        {
            _harmony.PatchAll(typeof(MainMenuSpongePatch));
        }

        if (Scoops.Config.fixFoliageLOD.Value)
        {
            _harmony.PatchAll(typeof(FoliageDetailDistanceSpongePatch));
        }

        if (Scoops.Config.patchCameraScript.Value)
        {
            _harmony.PatchAll(typeof(ManualCameraRendererSpongePatch));
        }

        if (Scoops.Config.disableBloom.Value || Scoops.Config.disableDOF.Value || Scoops.Config.disableMotionBlur.Value || Scoops.Config.disableShadows.Value || Scoops.Config.disableMotionVectors.Value || Scoops.Config.disableRefraction.Value || Scoops.Config.disableReflections.Value || Scoops.Config.useCustomShader.Value || Scoops.Config.useLegacyCustomShader.Value)
        {
            _harmony.PatchAll(typeof(PlayerControllerBSpongePatch));
        }

        if (Scoops.Config.useCustomShader.Value || Scoops.Config.useLegacyCustomShader.Value)
        {
            _harmony.PatchAll(typeof(HDRenderPipeline_RecordRenderGraph_Patch));
            if (Scoops.Config.volumetricCompensation.Value)
            {
                _harmony.PatchAll(typeof(LightService));
                _harmony.PatchAll(typeof(TeleportPatches));
            }
        }

        if (Scoops.Config.fixInputActions.Value)
        {
            InputActionSpongePatches.Init();
            _harmony.PatchAll(typeof(InputActionSpongePatches));
            Plugin.Log.LogInfo("Input Actions Patched");
        }

        if (Scoops.Config.generateLODs.Value || Scoops.Config.fixComplexMeshes.Value)
        {
            _harmony.PatchAll(typeof(GrabbableObjectPatches));
        }

        if (Scoops.Config.fixComplexMeshes.Value && Scoops.Config.fixComplexCosmetics.Value && MoreCompanyCompat.Enabled)
        {
            _harmony.PatchAll(typeof(MoreCompanyCompat));
        }

        if (Scoops.Config.vSyncCount.Value != 1)
        {
            _harmony.PatchAll(typeof(IngamePlayerSettingsSpongePatch));
        }
    }

    private void ApplyVerbosePluginPatch()
    {
        _harmony.PatchAll(typeof(AssetBundleSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleAsyncSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleLoadSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleLoadAsyncSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleLoadMultipleSpongePatch));
        _harmony.PatchAll(typeof(AssetBundleLoadMultipleAsyncSpongePatch));
    }

    public static void ManageDeDuping()
    {
        bool deduped = false;

        if (Scoops.Config.deDupeTextures.Value || Scoops.Config.resizeTextures.Value)
        {
            TextureService.deDupeBlacklist = Scoops.Config.deDupeTextureBlacklist.Value.ToLower().Split(';');
            TextureService.ResizeAllTextures();
            deduped = true;
        }

        // No shader de-duping until I can figure it out
        //if (Scoops.Config.deDupeShaders.Value)
        //{
        //    ShaderService.deDupeBlacklist = Scoops.Config.deDupeShaderBlacklist.Value.ToLower().Split(';');
        //    ShaderService.DedupeAllShaders();
        //    deduped = true;
        //}

        if (Scoops.Config.deDupeMeshes.Value)
        {
            MeshService.deDupeBlacklist = Scoops.Config.deDupeMeshBlacklist.Value.ToLower().Split(';');
            MeshService.DedupeAllMeshes();
            deduped = true;
        }

        if (Scoops.Config.deDupeAudio.Value)
        {
            AudioService.deDupeBlacklist = Scoops.Config.deDupeAudioBlacklist.Value.ToLower().Split(';');
            AudioService.DedupeAllAudio();
            deduped = true;
        }

        // Some final cleanup
        if (deduped)
        {
            Resources.UnloadUnusedAssets();
        }
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

        // if the settings are set to the highest value/default, don't override
        if (Scoops.Config.decalDrawDist.Value != 1000)
        {
            settings.decalSettings.drawDistance = Scoops.Config.decalDrawDist.Value;
        }
        if (Scoops.Config.decalAtlasSize.Value != 4096)
        {
            settings.decalSettings.atlasHeight = Scoops.Config.decalAtlasSize.Value;
            settings.decalSettings.atlasWidth = Scoops.Config.decalAtlasSize.Value;
        }

        if (Scoops.Config.reflectionAtlasSize.Value != "Resolution16384x8192")
        {
            settings.lightLoopSettings.reflectionProbeTexCacheSize = Enum.Parse<ReflectionProbeTextureCacheResolution>(Scoops.Config.reflectionAtlasSize.Value);
        }
        if (Scoops.Config.maxCubeReflectionProbes.Value != 48)
        {
            settings.lightLoopSettings.maxCubeReflectionOnScreen = Scoops.Config.maxCubeReflectionProbes.Value;
        }
        if (Scoops.Config.maxPlanarReflectionProbes.Value != 16)
        {
            settings.lightLoopSettings.maxPlanarReflectionOnScreen = Scoops.Config.maxPlanarReflectionProbes.Value;
        }

        if (Scoops.Config.shadowsMaxResolution.Value != 2048)
        {
            settings.hdShadowInitParams.maxPunctualShadowMapResolution = Scoops.Config.shadowsMaxResolution.Value;
            settings.hdShadowInitParams.maxDirectionalShadowMapResolution = Scoops.Config.shadowsMaxResolution.Value;
            settings.hdShadowInitParams.maxAreaShadowMapResolution = Scoops.Config.shadowsMaxResolution.Value;
        }
        if (Scoops.Config.shadowsAtlasSize.Value != 4096)
        {
            settings.hdShadowInitParams.punctualLightShadowAtlas.shadowAtlasResolution = Scoops.Config.shadowsAtlasSize.Value;
            settings.hdShadowInitParams.cachedPunctualLightShadowAtlas = Scoops.Config.shadowsAtlasSize.Value / 2; // Just make it half size for now
            settings.hdShadowInitParams.areaLightShadowAtlas.shadowAtlasResolution = Scoops.Config.shadowsAtlasSize.Value;
            settings.hdShadowInitParams.cachedAreaLightShadowAtlas = Scoops.Config.shadowsAtlasSize.Value / 2;
        }
        if (Scoops.Config.fogBudget.Value != 0.17f)
        {
            settings.lightingQualitySettings.Fog_Budget[QualitySettings.GetQualityLevel()] = Scoops.Config.fogBudget.Value;
        }

        ((HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).m_RenderPipelineSettings = settings;
        ((HDRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).OnValidate();
    }
}
