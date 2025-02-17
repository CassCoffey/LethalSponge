using LethalLevelLoader.AssetBundles;
using Scoops.service;
using System.Runtime.CompilerServices;

namespace Scoops.compatibility
{
    internal static class LLLCompat
    {
        public static bool Enabled =>
            BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("imabatby.lethallevelloader");

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddBundleHook()
        {
            LethalLevelLoader.AssetBundles.AssetBundleLoader.OnBundleLoaded.AddListener(OnAssetBundleLoad);
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void OnAssetBundleLoad(AssetBundleInfo info)
        {
            if (info.IsAssetBundleLoaded)
            {
                SpongeService.RegisterAssetBundle(info.assetBundle);
            }
        }
    }
}
