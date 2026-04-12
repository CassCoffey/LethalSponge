using DunGen;
using DunGen.Graph;
using HarmonyLib;
using Scoops.service;

namespace Scoops.patches
{
    [HarmonyPatch(typeof(RoundManager))]
    public class RoundManagerSpongePatch
    {
        [HarmonyPatch("FinishGeneratingLevel")]
        [HarmonyPostfix]
        private static void RoundManager_FinishGeneratingLevel(ref RoundManager __instance)
        {
            if (Config.verboseLogging.Value)
            {
                SpongeService.DungeonLoaded(__instance.dungeonGenerator.Generator);
            }

            if (Config.changeLightFadeDistance.Value)
            {
                LightService.UpdateAllLights();
            }

            Plugin.ManageDeDuping();
        }
    }
}
