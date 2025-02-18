using BepInEx.Configuration;

namespace Scoops
{
    public class Config
    {
        public static ConfigEntry<bool> verboseLogging;
        public static ConfigEntry<string> assetbundleBlacklist;
        public static ConfigEntry<string> propertyBlacklist;

        public static ConfigEntry<bool> unloadUnused;

        public Config(ConfigFile cfg)
        {
            // Investigation
            verboseLogging = cfg.Bind(
                    "Investigation",
                    "verboseLogging",
                    false,
                    "Whether Sponge should output detailed information about possible leak sources. (COSTLY FOR PERFORMANCE)."
            );

            assetbundleBlacklist = cfg.Bind(
                    "Investigation",
                    "assetbundleBlacklist",
                    "",
                    "Any objects originating from assetbundles in this list will never be reported."
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
        }
    }
}
