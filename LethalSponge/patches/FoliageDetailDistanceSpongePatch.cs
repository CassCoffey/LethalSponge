using HarmonyLib;
using Scoops.service;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scoops.patches
{
    [HarmonyPatch(typeof(FoliageDetailDistance))]
    public class FoliageDetailDistanceSpongePatch
    {
        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        private static bool FoliageDetailDistance_Update(ref FoliageDetailDistance __instance)
        {
            if (__instance.localPlayerTransform == null)
            {
                return false;
            }

            if (__instance.updateInterval >= 0f)
            {
                __instance.updateInterval -= Time.deltaTime;
            }
            else if (__instance.bushIndex < __instance.allBushRenderers.Count)
            {
                if (__instance.allBushRenderers[__instance.bushIndex] == null)
                {
                    return false;
                }

                if ((__instance.localPlayerTransform.position - __instance.allBushRenderers[__instance.bushIndex].transform.position).sqrMagnitude > 75f * 75f)
                {
                    if (__instance.allBushRenderers[__instance.bushIndex].sharedMaterial != __instance.lowDetailMaterial)
                    {
                        __instance.allBushRenderers[__instance.bushIndex].sharedMaterial = __instance.lowDetailMaterial;
                    }
                }
                else if (__instance.allBushRenderers[__instance.bushIndex].sharedMaterial != __instance.highDetailMaterial)
                {
                    __instance.allBushRenderers[__instance.bushIndex].sharedMaterial = __instance.highDetailMaterial;
                }

                __instance.bushIndex++;
            }
            else
            {
                __instance.bushIndex = 0;
                __instance.updateInterval = 1f;
            }

            return false;
        }
    }
}
