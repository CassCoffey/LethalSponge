using BepInEx.Configuration;

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
        public static ConfigEntry<bool> disableBloom;
        public static ConfigEntry<bool> disableShadows;
        public static ConfigEntry<bool> disableReflections;
        public static ConfigEntry<bool> disableMotionVectors;
        public static ConfigEntry<bool> disableRefraction;

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
