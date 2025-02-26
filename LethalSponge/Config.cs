using BepInEx.Configuration;
using UnityEngine.Rendering.HighDefinition;

namespace Scoops
{
    public class Config
    {
        public static ConfigEntry<bool> verboseLogging;
        public static ConfigEntry<bool> ignoreInactiveObjects;
        public static ConfigEntry<string> fullReportList;
        public static ConfigEntry<string> assetbundleBlacklist;
        public static ConfigEntry<string> assetbundleWhitelist;
        public static ConfigEntry<string> propertyBlacklist;

        public static ConfigEntry<bool> unloadUnused;
        public static ConfigEntry<bool> fixFoliageLOD;

        public static ConfigEntry<bool> fixCameraSettings;
        public static ConfigEntry<bool> patchCameraScript;
        public static ConfigEntry<int> mapCameraFramerate;
        public static ConfigEntry<int> securityCameraFramerate;

        public static ConfigEntry<bool> removePosterizationShader;
        public static ConfigEntry<bool> useCustomShader;
        public static ConfigEntry<bool> disableDOF;
        public static ConfigEntry<bool> disableMotionBlur;
        public static ConfigEntry<bool> disableBloom;
        public static ConfigEntry<bool> disableShadows;
        public static ConfigEntry<bool> disableReflections;
        public static ConfigEntry<bool> disableMotionVectors;
        public static ConfigEntry<bool> disableRefraction;

        public static ConfigEntry<bool> qualityOverrides;
        public static ConfigEntry<int> decalDrawDist;
        public static ConfigEntry<int> decalAtlasSize;
        public static ConfigEntry<int> maxVolumetricFog;
        public static ConfigEntry<string> reflectionAtlasSize;
        public static ConfigEntry<int> maxCubeReflectionProbes;
        public static ConfigEntry<int> maxPlanarReflectionProbes;
        //public static ConfigEntry<int> maxDirectionalLights;
        //public static ConfigEntry<int> maxPunctualLights;
        //public static ConfigEntry<int> maxAreaLights;
        public static ConfigEntry<int> maxShadows;
        public static ConfigEntry<int> shadowsMaxResolution;
        public static ConfigEntry<int> shadowsAtlasSize;
        public static ConfigEntry<float> fogBudget;
        public static ConfigEntry<bool> deferredOnly;

        public static ConfigEntry<bool> runDaily;

        public Config(ConfigFile cfg)
        {
            // Investigation
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

            // Cameras
            fixCameraSettings = cfg.Bind(
                    "Cameras",
                    "fixCameraSettings",
                    true,
                    "Should Sponge change the settings for the ship cameras and radar cam to improve performance?"
            );
            patchCameraScript = cfg.Bind(
                    "Cameras",
                    "patchCameraScript",
                    true,
                    "Should Sponge replace the base Lethal Company ManualCameraRenderer.MeetsCameraEnabledConditions function with one that more reliably disables ship cameras when they're not in view?"
            );
            securityCameraFramerate = cfg.Bind(
                    "Cameras",
                    "securityCameraFramerate",
                    15,
                    "What framerate should the interior and exterior cams run at? (Requires fixCameraSettings = true)"
            );
            mapCameraFramerate = cfg.Bind(
                    "Cameras",
                    "mapCameraFramerate",
                    15,
                    "What framerate should the radar map camera run at? (Requires fixCameraSettings = true)"
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
                new ConfigDescription("What should the maximum distance be for drawing decals like blood splatters? (LC default is 1000)", new AcceptableValueRange<int>(50, 100))
            );
            decalAtlasSize = cfg.Bind(
                "Graphics Quality",
                "decalAtlasSize",
                2048,
                new ConfigDescription("What should the texture size be for the the Decal Atlas? (squared)", new AcceptableValueList<int>(2048, 4096))
            );
            maxVolumetricFog = cfg.Bind(
                "Graphics Quality",
                "maxVolumetricFog",
                20,
                new ConfigDescription("How many Volumetric Fog volumes should be able to be shown at once? (LC default is 80)", new AcceptableValueRange<int>(5, 80))
            );
            reflectionAtlasSize = cfg.Bind(
                "Graphics Quality",
                "reflectionAtlasSize",
                "Resolution1024x1024",
                new ConfigDescription("What should the texture size be for the the Decal Atlas? (LC default is 2048x1024)", new AcceptableValueList<string>("Resolution512x512", "Resolution1024x512", "Resolution1024x1024", "Resolution2048x1024"))
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
                new ConfigDescription("What should the maximum resolution be for Shadow Maps? (LC default is 1024)", new AcceptableValueList<int>(64, 128, 256, 512, 1024))
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
                new ConfigDescription("What should the budget (0-1) be for the volumetric fog? (LC default is 0.166) (WARNING: Lowering this will make fog less dangerous)", new AcceptableValueRange<float>(0.05f, 0.17f))
            );
            deferredOnly = cfg.Bind(
                "Graphics Quality",
                "deferredOnly",
                true,
                "Should Sponge set the Lit Shader Mode to Deferred Only? (This might free up some memory, and LC only uses Deferred anyway.)"
            );


            // Debug
            runDaily = cfg.Bind(
                    "Debug",
                    "runDaily",
                    true,
                    "If false, Sponge will no longer run automatically each day and will only run when you type '/sponge' in chat. This can be toggled mid-game with '/sponge toggle'."
            );
        }
    }
}
