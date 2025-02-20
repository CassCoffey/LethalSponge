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

        public static ConfigEntry<bool> debugMode;

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

            // Debug
            debugMode = cfg.Bind(
                    "Debug",
                    "debugMode",
                    false,
                    "If true, Sponge will no longer run each day and will only run when you type '/sponge' in chat."
            );
        }
    }
}
