using BepInEx.Configuration;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Rendering.HighDefinition;

namespace Scoops
{
    public class Config
    {
        public static ConfigEntry<bool> minimalLogging;
        public static ConfigEntry<bool> verboseLogging;
        public static ConfigEntry<bool> ignoreInactiveObjects;
        public static ConfigEntry<string> fullReportList;
        public static ConfigEntry<string> assetbundleBlacklist;
        public static ConfigEntry<string> assetbundleWhitelist;
        public static ConfigEntry<string> propertyBlacklist;

        public static ConfigEntry<bool> unloadUnused;
        public static ConfigEntry<bool> fixFoliageLOD;
        public static ConfigEntry<bool> fixInputActions;

        public static ConfigEntry<bool> generateLODs;
        public static ConfigEntry<string> generateLODsBlacklist;
        public static ConfigEntry<bool> generateLODMeshes;
        public static ConfigEntry<float> LOD1Start;
        public static ConfigEntry<float> LOD1Quality;
        public static ConfigEntry<float> cullStart;
        public static ConfigEntry<bool> fixComplexMeshes;
        public static ConfigEntry<bool> fixComplexGrabbable;
        public static ConfigEntry<bool> fixComplexCosmetics;
        public static ConfigEntry<float> complexMeshVertCutoff;
        public static ConfigEntry<string> fixComplexMeshesBlacklist;
        public static ConfigEntry<string> fixComplexMeshesGameObjectBlacklist;
        public static ConfigEntry<bool> preserveSurfaceCurvature;

        public static ConfigEntry<bool> resizeTextures;
        public static ConfigEntry<int> maxResizeTextureSize;

        public static ConfigEntry<bool> deDupeMeshes;
        public static ConfigEntry<string> deDupeMeshBlacklist;
        public static ConfigEntry<bool> deDupeTextures;
        public static ConfigEntry<string> deDupeTextureBlacklist;
        public static ConfigEntry<bool> deDupeAudio;
        public static ConfigEntry<string> deDupeAudioBlacklist;
        //public static ConfigEntry<bool> deDupeShaders;
        //public static ConfigEntry<string> deDupeShaderBlacklist;

        public static ConfigEntry<bool> fixCameraSettings;
        public static ConfigEntry<bool> applyShipCameraQualityOverrides;
        public static ConfigEntry<bool> applySecurityCameraQualityOverrides;
        public static ConfigEntry<bool> applyMapCameraQualityOverrides;
        public static ConfigEntry<bool> cameraRenderTransparent;
        public static ConfigEntry<bool> patchCameraScript;
        public static ConfigEntry<float> securityCameraCullDistance;
        public static ConfigEntry<int> mapCameraFramerate;
        public static ConfigEntry<int> securityCameraFramerate;
        public static ConfigEntry<int> shipCameraFramerate;

        public static ConfigEntry<bool> removePosterizationShader;
        public static ConfigEntry<bool> useCustomShader;
        public static ConfigEntry<bool> useLegacyCustomShader;
        public static ConfigEntry<bool> volumetricCompensation;
        public static ConfigEntry<string> compensationMoonBlacklist;
        public static ConfigEntry<bool> disableDOF;
        public static ConfigEntry<bool> disableMotionBlur;
        public static ConfigEntry<bool> disableBloom;
        public static ConfigEntry<bool> disableShadows;
        public static ConfigEntry<bool> disableReflections;
        public static ConfigEntry<bool> disableMotionVectors;
        public static ConfigEntry<bool> disableRefraction;
        public static ConfigEntry<int> vSyncCount;

        public static ConfigEntry<bool> qualityOverrides;
        public static ConfigEntry<int> decalDrawDist;
        public static ConfigEntry<int> decalAtlasSize;
        //public static ConfigEntry<int> maxVolumetricFog;
        public static ConfigEntry<string> reflectionAtlasSize;
        public static ConfigEntry<int> maxCubeReflectionProbes;
        public static ConfigEntry<int> maxPlanarReflectionProbes;
        //public static ConfigEntry<int> maxDirectionalLights;
        //public static ConfigEntry<int> maxPunctualLights;
        //public static ConfigEntry<int> maxAreaLights;
        public static ConfigEntry<int> shadowsMaxResolution;
        public static ConfigEntry<int> shadowsAtlasSize;
        public static ConfigEntry<float> fogBudget;
        public static ConfigEntry<bool> deferredOnly;
        public static ConfigEntry<bool> changeLightFadeDistance;
        public static ConfigEntry<float> lightVolumetricDistMult;
        public static ConfigEntry<float> lightVolumetricDistCap;

        public static ConfigEntry<bool> runDaily;

        public Config(ConfigFile cfg)
        {
            cfg.SaveOnConfigSet = false;

            // Investigation
            minimalLogging = cfg.Bind(
                    "Investigation",
                    "minimalLogging",
                    true,
                    "If enabled, Sponge will stop logging how many objects were cleaned up. This will reduce the cleanup stutter on day rollover."
            );
            verboseLogging = cfg.Bind(
                    "Investigation",
                    "verboseLogging",
                    false,
                    "Whether Sponge should output detailed information about possible leak sources. (COSTLY FOR PERFORMANCE, ONLY ENABLE WHILE DEBUGGING)."
            );
            ignoreInactiveObjects = cfg.Bind(
                    "Investigation",
                    "ignoreInactiveObjects",
                    true,
                    "Whether Sponge should exclude gameobjects/behaviors that are inactive from totals."
            );
            fullReportList = cfg.Bind(
                    "Investigation",
                    "fullReportList",
                    "",
                    "Bundles/Scenes in this semicolon-separated list will have all objects' Name and ID printed each check. Use 'unknown' for basegame/unknown sources."
            );
            assetbundleBlacklist = cfg.Bind(
                    "Investigation",
                    "assetbundleBlacklist",
                    "",
                    "Any objects originating from assetbundles/scenes in this list will never be reported. Use 'unknown' for basegame/unknown sources."
            );
            assetbundleWhitelist = cfg.Bind(
                    "Investigation",
                    "assetbundleWhitelist",
                    "",
                    "ONLY objects originating from assetbundles/scenes in this list will be reported. This takes precedence over the Blacklist. Use 'unknown' for basegame/unknown sources."
            );
            propertyBlacklist = cfg.Bind(
                    "Investigation",
                    "propertyBlacklist",
                    "Renderer.material;Renderer.materials;LightProbes.GetInstantiatedLightProbesForScene;MeshFilter.mesh;Collider.material;TMP_Text.fontMaterial;TMP_Text.fontMaterials;TMP_Text.spriteAnimator;Graphic.materialForRendering;Volume.profile;Volume.profileRef;TMP_InputField.mesh;InputSystemUIInputModule.trackedDeviceSelect;BasePanel.dungeon;BasePanel.selectedExtendedDungeonFlow;BasePanel.selectedDungeonFlow;TMP_Text.fontSharedMaterials;RenderSettings.customReflection;NetworkManager.ConnectedClients;NetworkManager.ConnectedClientsList;NetworkManager.ConnectedClientsIds",
                    "While crawling through objects/classes to find references, Sponge will ignore these properties. Useful if there are Getters that run unwanted code."
            );

            // Cleanup
            unloadUnused = cfg.Bind(
                    "Cleanup",
                    "unloadUnused",
                    true,
                    "Should Sponge call UnloadUnusedAssets each day?"
            );
            fixFoliageLOD = cfg.Bind(
                    "Cleanup",
                    "fixFoliageLOD",
                    true,
                    "Should Sponge replace the base Lethal Company FoliageDetailDistance script with one that doesn't leak materials? (LethalPerformance has a similar fix as well)"
            );
            fixInputActions = cfg.Bind(
                    "Cleanup",
                    "fixInputActions",
                    true,
                    "Should Sponge fix the repeated instantiation of PlayerActions that would cause additional input lag every time a game is loaded?"
            );

            // Meshes
            generateLODs = cfg.Bind(
                    "Meshes",
                    "generateLODs",
                    true,
                    "Should Sponge automatically generate LODs for scrap/items?"
            );
            generateLODsBlacklist = cfg.Bind(
                    "Meshes",
                    "generateLODsBlacklist",
                    "ExtensionLadderItem;LockPickerItem;JetpackItem",
                    "GameObject names in this semicolon-separated list will not generate LODs."
            );
            generateLODMeshes = cfg.Bind(
                    "Meshes",
                    "generateLODMeshes",
                    false,
                    "Should LOD levels use simplified meshes? (This will increase load times/memory usage for a slight fps boost in some cases, not generally recommended)"
            );
            LOD1Start = cfg.Bind(
                    "Meshes",
                    "LOD1Start",
                    0.2f,
                    new ConfigDescription("Where should the first LOD start? (Measured in mesh size on screen) (requires generateLODs = true)", new AcceptableValueRange<float>(0f, 1f))
            );
            LOD1Quality = cfg.Bind(
                    "Meshes",
                    "LOD1Quality",
                    0.65f,
                    new ConfigDescription("What quality level the first LOD be? (requires generateLODs = true and generateLODMeshes = true)", new AcceptableValueRange<float>(0f, 1f))
            );
            cullStart = cfg.Bind(
                    "Meshes",
                    "cullStart",
                    0.01f,
                    new ConfigDescription("Where should the last/cull LOD start? (Measured in mesh size on screen) (requires generateLODs = true)", new AcceptableValueRange<float>(0f, 1f))
            );
            fixComplexMeshes = cfg.Bind(
                    "Meshes",
                    "fixComplexMeshes",
                    true,
                    "Should Sponge reduce vertex counts of overly complex meshes? (Will increase load times and memory usage slightly)"
            );
            fixComplexGrabbable = cfg.Bind(
                    "Meshes",
                    "fixComplexGrabbable",
                    true,
                    "Should fixComplexMeshes apply to scrap and tools?"
            );
            fixComplexCosmetics = cfg.Bind(
                    "Meshes",
                    "fixComplexCosmetics",
                    true,
                    "Should fixComplexMeshes apply to MoreCompany cosmetics?"
            );
            complexMeshVertCutoff = cfg.Bind(
                    "Meshes",
                    "complexMeshVertCutoff",
                    5000f,
                    "What is the minimum Vertex Density (Vertices over Meters Cubed) that meshes should be considered 'too complex'. This also determines the minimum Vertices for the cutoff."
            );
            fixComplexMeshesBlacklist = cfg.Bind(
                    "Meshes",
                    "fixComplexMeshesBlacklist",
                    "",
                    "Mesh names in this semicolon-separated list will be ignored while fixing complex meshes."
            );
            fixComplexMeshesGameObjectBlacklist = cfg.Bind(
                    "Meshes",
                    "fixComplexMeshesGameObjectBlacklist",
                    "",
                    "GameObject names in this semicolon-separated list will be ignored while fixing complex meshes."
            );
            preserveSurfaceCurvature = cfg.Bind(
                    "Meshes",
                    "preserveSurfaceCurvature",
                    false,
                    "More accurate mesh simplification, but is more CPU intensive to run. (Requires generateLODs or fixComplexMeshes)"
            );

            // Textures
            resizeTextures = cfg.Bind(
                    "Textures",
                    "resizeTextures",
                    true,
                    "Should Sponge automatically resize textures to fit the maxTextureSize? (Will slightly increase load times and decrease VRAM usage)"
            );
            maxResizeTextureSize = cfg.Bind(
                    "Textures",
                    "maxResizeTextureSize",
                    2048,
                    new ConfigDescription("All textures with height over this number will be resized down to this number.", new AcceptableValueList<int>(64, 128, 256, 512, 1024, 2048))
            );

            // Dedupe
            deDupeMeshes = cfg.Bind(
                    "Dedupe",
                    "deDupeMeshes",
                    false,
                    "Should Sponge automatically remove duplicate meshes? (Will increase load times and decrease RAM/VRAM usage)"
            );
            deDupeMeshBlacklist = cfg.Bind(
                    "Dedupe",
                    "deDupeMeshBlacklist",
                    // sigh
                    "cube;sphere;circle;cylinder",
                    "Mesh names in this semicolon-separated list will be exempt from de-duping."
            );
            deDupeTextures = cfg.Bind(
                    "Dedupe",
                    "deDupeTextures",
                    false,
                    "Should Sponge automatically remove duplicate textures? (Will increase load times and decrease VRAM usage)"
            );
            deDupeTextureBlacklist = cfg.Bind(
                    "Dedupe",
                    "deDupeTextureBlacklist",
                    "playersuittex2b;scavengerplayermodel;LightningBallSpriteSheet2",
                    "Texture names in this semicolon-separated list will be exempt from de-duping."
            );
            deDupeAudio = cfg.Bind(
                    "Dedupe",
                    "deDupeAudio",
                    false,
                    "Should Sponge automatically remove duplicate audio clips? (Will increase load times and decrease RAM usage)"
            );
            deDupeAudioBlacklist = cfg.Bind(
                    "Dedupe",
                    "deDupeAudioBlacklist",
                    "",
                    "Audio clip names in this semicolon-separated list will be exempt from de-duping."
            );
            //deDupeShaders = cfg.Bind(
            //        "Dedupe",
            //        "deDupeShaders",
            //        true,
            //        "Should Sponge automatically remove duplicate shaders? (Will increase load times and decrease RAM usage)"
            //);
            //deDupeShaderBlacklist = cfg.Bind(
            //        "Dedupe",
            //        "deDupeShaderBlacklist",
            //        "",
            //        "Shader names in this semicolon-separated list will be exempt from de-duping."
            //);

            // Cameras
            fixCameraSettings = cfg.Bind(
                    "Cameras",
                    "fixCameraSettings",
                    true,
                    "Should Sponge change the settings for the ship cameras and radar cam to improve performance?"
            );
            applyShipCameraQualityOverrides = cfg.Bind(
                    "Cameras",
                    "applyShipCameraQualityOverrides",
                    true,
                    "Should Sponge disable extra HDRP rendering features on the Ship camera? (Requires fixCameraSettings = true)"
            );
            applySecurityCameraQualityOverrides = cfg.Bind(
                    "Cameras",
                    "applySecurityCameraQualityOverrides",
                    true,
                    "Should Sponge disable extra HDRP rendering features on the Security camera? (Requires fixCameraSettings = true)"
            );
            applyMapCameraQualityOverrides = cfg.Bind(
                    "Cameras",
                    "applyMapCameraQualityOverrides",
                    true,
                    "Should Sponge disable extra HDRP rendering features on the Map camera? (Requires fixCameraSettings = true)"
            );
            cameraRenderTransparent = cfg.Bind(
                    "Cameras",
                    "cameraRenderTransparent",
                    true,
                    "Should the Ship and Security camera render transparent objects? (Requires applyShipCameraQualityOverrides or applySecurityCameraQualityOverrides)"
            );
            patchCameraScript = cfg.Bind(
                    "Cameras",
                    "patchCameraScript",
                    true,
                    "Should Sponge replace the base Lethal Company ManualCameraRenderer.MeetsCameraEnabledConditions function with one that more reliably disables ship cameras when they're not in view?"
            );
            securityCameraCullDistance = cfg.Bind(
                    "Cameras",
                    "securityCameraCullDistance",
                    20f,
                    new ConfigDescription("What should the culling distance be for the ship security camera? You might want to increase this if you're using a mod to re-add planets in orbit. (LC default is 150)", new AcceptableValueRange<float>(15, 150))
            );
            securityCameraFramerate = cfg.Bind(
                    "Cameras",
                    "securityCameraFramerate",
                    15,
                    "What framerate should the exterior cam run at? 0 = not limited. (Requires fixCameraSettings = true)"
            );
            shipCameraFramerate = cfg.Bind(
                    "Cameras",
                    "shipCameraFramerate",
                    15,
                    "What framerate should the interior cam run at? 0 = not limited. (Requires fixCameraSettings = true)"
            );
            mapCameraFramerate = cfg.Bind(
                    "Cameras",
                    "mapCameraFramerate",
                    20,
                    "What framerate should the radar map camera run at? 0 = not limited. (Requires fixCameraSettings = true)"
            );

            // Rendering
            removePosterizationShader = cfg.Bind(
                "Rendering",
                "removePosterizationShader",
                true,
                "Should Sponge remove the expensive posterization + outline custom pass to save processing power?"
            );
            useCustomShader = cfg.Bind(
                "Rendering",
                "useCustomShader",
                true,
                "Should Sponge replace the removed shader with a faster one that looks similar? (Requires removePosterizationShader = true)"
            );
            useLegacyCustomShader = cfg.Bind(
                "Rendering",
                "useLegacyCustomShader",
                false,
                "Should Sponge replace the removed shader with the shader from the original sponge release? (Takes precedence over useCustomShader) (Requires removePosterizationShader = true)"
            );
            volumetricCompensation = cfg.Bind(
                "Rendering",
                "volumetricCompensation",
                false,
                "Should Sponge adjust all of the lights/fog to be more intense to make up for the changes in the custom shader? (Requires useCustomShader or useWIPCustomShader = true)"
            );
            compensationMoonBlacklist = cfg.Bind(
                "Rendering",
                "compensationMoonBlacklist",
                "",
                "Moons in this semicolon-separated list will have Volmetric Compensation disabled on the exterior of the moon. Use this if a modded moon is too foggy. (Requires volumetricCompensation = true)"
            );
            disableDOF = cfg.Bind(
                "Rendering",
                "disableDOF",
                false,
                "Should Sponge disable Depth of Field on the player camera?"
            );
            disableMotionBlur = cfg.Bind(
                "Rendering",
                "disableMotionBlur",
                false,
                "Should Sponge disable Motion Blur on the player camera?"
            );
            disableBloom = cfg.Bind(
                "Rendering",
                "disableBloom",
                false,
                "Should Sponge disable Bloom on the player camera?"
            );
            disableShadows = cfg.Bind(
                "Rendering",
                "disableShadows",
                false,
                "Should Sponge disable Shadows on the player camera?"
            );
            disableReflections = cfg.Bind(
                "Rendering",
                "disableReflections",
                false,
                "Should Sponge disable Reflections on the player camera?"
            );
            disableMotionVectors = cfg.Bind(
                "Rendering",
                "disableReflections",
                false,
                "Should Sponge disable MotionVectors on the player camera?"
            );
            disableRefraction = cfg.Bind(
                "Rendering",
                "disableReflections",
                false,
                "Should Sponge disable Refraction on the player camera?"
            );
            vSyncCount = cfg.Bind(
                "Rendering",
                "vSyncCount",
                1,
                "When the option \"Use monitor (V-Sync)\" is selected in Options, what VSyncCount should be used? (LC default is 1)"
            );

            // Graphics Quality
            qualityOverrides = cfg.Bind(
                "Graphics Quality",
                "qualityOverrides",
                true,
                "Should Sponge change the default quality settings? This must be on for any of the other Graphics Quality settings to take effect."
            );
            decalDrawDist = cfg.Bind(
                "Graphics Quality",
                "decalDrawDist",
                100,
                new ConfigDescription("What should the maximum distance be for drawing decals like blood splatters? (LC default is 1000)", new AcceptableValueRange<int>(50, 1000))
            );
            decalAtlasSize = cfg.Bind(
                "Graphics Quality",
                "decalAtlasSize",
                2048,
                new ConfigDescription("What should the texture size be for the the Decal Atlas? (squared) (LC default is 4096)", new AcceptableValueList<int>(2048, 4096))
            );
            //maxVolumetricFog = cfg.Bind(
            //    "Graphics Quality",
            //    "maxVolumetricFog",
            //    20,
            //    new ConfigDescription("How many Volumetric Fog volumes should be able to be shown at once? (LC default is 80)", new AcceptableValueRange<int>(5, 80))
            //);
            reflectionAtlasSize = cfg.Bind(
                "Graphics Quality",
                "reflectionAtlasSize",
                "Resolution1024x1024",
                new ConfigDescription("What should the texture size be for the the Reflection Atlas? (LC default is 16384x8192)", new AcceptableValueList<string>("Resolution512x512", "Resolution1024x512", "Resolution1024x1024", "Resolution2048x1024", "Resolution2048x2048", "Resolution4096x2048", "Resolution16384x8192"))
            );
            maxCubeReflectionProbes = cfg.Bind(
                "Graphics Quality",
                "maxCubeReflectionProbes",
                12,
                new ConfigDescription("How many Cube Reflection Probes should be able to be on screen at once? (LC default is 48)", new AcceptableValueRange<int>(6, 48))
            );
            maxPlanarReflectionProbes = cfg.Bind(
                "Graphics Quality",
                "maxPlanarReflectionProbes",
                8,
                new ConfigDescription("How many Cube Reflection Probes should be able to be on screen at once? (LC default is 16)", new AcceptableValueRange<int>(4, 16))
            );
            //maxDirectionalLights = cfg.Bind(
            //    "Graphics Quality",
            //    "maxDirectionalLights",
            //    8,
            //    new ConfigDescription("How many Directional Lights should be able to be on screen at once? (LC default is 16)", new AcceptableValueRange<int>(4, 16))
            //);
            //maxPunctualLights = cfg.Bind(
            //    "Graphics Quality",
            //    "maxPunctualLights",
            //    64,
            //    new ConfigDescription("How many Punctual(Point/Spot) Lights should be able to be on screen at once? (LC default is 512)", new AcceptableValueRange<int>(16, 512))
            //);
            //maxAreaLights = cfg.Bind(
            //    "Graphics Quality",
            //    "maxAreaLights",
            //    16,
            //    new ConfigDescription("How many Area Lights should be able to be on screen at once? (LC default is 64)", new AcceptableValueRange<int>(4, 64))
            //);
            shadowsMaxResolution = cfg.Bind(
                "Graphics Quality",
                "shadowsMaxResolution",
                256,
                new ConfigDescription("What should the maximum resolution be for Shadow Maps? (LC default is 2048)", new AcceptableValueList<int>(64, 128, 256, 512, 1024, 2048))
            );
            shadowsAtlasSize = cfg.Bind(
                "Graphics Quality",
                "shadowsAtlasSize",
                2048,
                new ConfigDescription("What should the resolution be for the Shadow Map Atlas? (LC default is 4096)", new AcceptableValueList<int>(1024, 2048, 4096))
            );
            fogBudget = cfg.Bind(
                "Graphics Quality",
                "fogBudget",
                0.15f,
                new ConfigDescription("What should the budget (0-1) be for the volumetric fog? (LC default is 0.166) (Lowering this may make fog less dangerous)", new AcceptableValueRange<float>(0.05f, 0.17f))
            );
            deferredOnly = cfg.Bind(
                "Graphics Quality",
                "deferredOnly",
                true,
                "Should Sponge set the Lit Shader Mode to Deferred Only? (This might free up some memory, and LC only uses Deferred anyway.)"
            );
            changeLightFadeDistance = cfg.Bind(
                "Graphics Quality",
                "changeLightFadeDistance",
                true,
                "Should Sponge change the Fade Distance for all lights so they're not visible from too far away?"
            );
            lightVolumetricDistMult = cfg.Bind(
                "Graphics Quality",
                "lightVolumetricDistMult",
                10f,
                new ConfigDescription("What should the light's Range be multiplied by to find the Fade Distance?", new AcceptableValueRange<float>(2f, 50f))
            );
            lightVolumetricDistCap = cfg.Bind(
                "Graphics Quality",
                "lightVolumetricDistCap",
                250f,
                new ConfigDescription("What should the maximum Fade Distance be for lights? (LC Defaults tend to use 10000 for Fade Distance)", new AcceptableValueRange<float>(25f, 1000f))
            );

            // Debug
            runDaily = cfg.Bind(
                    "Debug",
                    "runDaily",
                    true,
                    "If false, Sponge will no longer run automatically each day and will only run when you type '/sponge' in chat. This can be toggled mid-game with '/sponge toggle'."
            );

            ClearOrphanedEntries(cfg);
            cfg.Save();
            cfg.SaveOnConfigSet = true;
        }

        // Thanks modding wiki
        static void ClearOrphanedEntries(ConfigFile cfg)
        {
            // Find the private property `OrphanedEntries` from the type `ConfigFile` //
            PropertyInfo orphanedEntriesProp = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries");
            // And get the value of that property from our ConfigFile instance //
            var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp.GetValue(cfg);
            // And finally, clear the `OrphanedEntries` dictionary //
            orphanedEntries.Clear();
        }
    }
}
