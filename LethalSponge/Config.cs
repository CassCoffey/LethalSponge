using BepInEx.Configuration;

namespace Scoops
{
    public class Config
    {
        public static ConfigEntry<bool> verboseLogging;
        public static ConfigEntry<bool> performRemoval;
        public static ConfigEntry<bool> simpleMode;
        public static ConfigEntry<string> assetbundleBlacklist;
        public static ConfigEntry<string> propertyBlacklist;

        public Config(ConfigFile cfg)
        {
            // General
            verboseLogging = cfg.Bind(
                    "General",
                    "verboseLogging",
                    false,
                    "Whether Sponge should output more detailed information about the leaks it's cleaning up (COSTLY FOR PERFORMANCE)."
            );

            performRemoval = cfg.Bind(
                    "General",
                    "performRemoval",
                    false,
                    "Whether Sponge should destroy leaked objects that it finds."
            );

            simpleMode = cfg.Bind(
                    "General",
                    "simpleMode",
                    false,
                    "Only runs UnloadUnusedAssets each day and does not output any additional info on leaks."
            );

            assetbundleBlacklist = cfg.Bind(
                    "General",
                    "assetbundleBlacklist",
                    "",
                    "Any objects originating from assetbundles in this list will never be reported or removed."
            );

            propertyBlacklist = cfg.Bind(
                    "General",
                    "propertyBlacklist",
                    "Renderer.material;Renderer.materials;LightProbes.GetInstantiatedLightProbesForScene;MeshFilter.mesh;Collider.material;TMP_Text.fontMaterial;TMP_Text.fontMaterials;TMP_Text.spriteAnimator;Graphic.materialForRendering;Volume.profile;Volume.profileRef;TMP_InputField.mesh;InputSystemUIInputModule.trackedDeviceSelect;BasePanel.dungeon;BasePanel.selectedExtendedDungeonFlow;BasePanel.selectedDungeonFlow;TMP_Text.fontSharedMaterials;RenderSettings.customReflection",
                    "While crawling through objects/classes to find references, Sponge will ignore these properties. Useful if there are Getters that run unwanted code."
            );
        }
    }
}
