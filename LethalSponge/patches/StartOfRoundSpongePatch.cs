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

            if (Config.fixCameraSettings.Value)
            {
                if (CameraService.Init())
                {
                    CameraService.ApplyCameraFixes();
                }
            }
        }

        [HarmonyPatch("PassTimeToNextDay")]
        [HarmonyPostfix]
        private static void StartOfRound_PassTimeToNextDay(ref StartOfRound __instance)
        {
            if (SpongeService.enabled)
            {
                SpongeService.ApplySponge();
            }
        }

        [HarmonyPatch(typeof(HUDManager))]
        [HarmonyPatch("AddTextToChatOnServer")]
        [HarmonyPrefix]
        private static bool HUDManager_AddTextToChatOnServer(ref HUDManager __instance, string chatMessage, int playerId)
        {
            if (chatMessage.ToLower() == "/sponge")
            {
                SpongeService.ApplySponge();
                return false;
            }

            if (chatMessage.ToLower() == "/sponge evaluate")
            {
                SpongeService.ApplySponge(SpongeMode.Evaluate);
                return false;
            }

            if (chatMessage.ToLower() == "/sponge clean")
            {
                SpongeService.ApplySponge(SpongeMode.Clean);
                return false;
            }

            if (chatMessage.ToLower() == "/sponge toggle")
            {
                Plugin.Log.LogMessage((SpongeService.enabled ? "Disabling" : "Enabling") + " Sponge daily automatic activation.");
                SpongeService.enabled = !SpongeService.enabled;
                return false;
            }

            return true;
        }
    }
}