using HarmonyLib;
using Scoops.service;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace Scoops.patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    public class StartOfRoundSpongePatch
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void StartOfRound_Start(ref StartOfRound __instance)
        {
            SpongeService.Initialize();
        }

        [HarmonyPatch("PassTimeToNextDay")]
        [HarmonyPostfix]
        private static void StartOfRound_PassTimeToNextDay(ref StartOfRound __instance)
        {
            if (!Config.debugMode.Value)
            {
                SpongeService.ApplySponge();
            }
        }

        [HarmonyPatch(typeof(HUDManager))]
        [HarmonyPatch("AddTextToChatOnServer")]
        [HarmonyPostfix]
        private static void HUDManager_AddTextToChatOnServer(ref HUDManager __instance, string chatMessage, int playerId)
        {
            if (Config.debugMode.Value && chatMessage.ToLower() == "/sponge")
            {
                SpongeService.ApplySponge();
            }
        }
    }
}