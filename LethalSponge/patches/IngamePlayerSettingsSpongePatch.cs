using HarmonyLib;
using UnityEngine;

namespace Scoops.patches
{
    [HarmonyPatch(typeof(IngamePlayerSettings))]
    public class IngamePlayerSettingsSpongePatch
    {
        [HarmonyPatch("SetFramerateCap")]
        [HarmonyPostfix]
        public static void IngamePlayerSettings_SetFramerateCap(int value)
        {
            if (value == 0)
            {
                QualitySettings.vSyncCount = Config.vSyncCount.Value;
            }
        }
    }
}
