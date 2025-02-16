using BepInEx.Configuration;

namespace Scoops
{
    public class Config
    {
        public static ConfigEntry<bool> verboseLogging;

        public Config(ConfigFile cfg)
        {
            // General
            verboseLogging = cfg.Bind(
                    "General",
                    "verboseLogging",
                    false,
                    "Whether Sponge should output more detailed information about the leaks it's cleaning up (COSTLY FOR PERFORMANCE)."
            );
        }
    }
}
