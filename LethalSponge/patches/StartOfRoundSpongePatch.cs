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

            if (Config.removePosterizationShader.Value)
            {
                CameraService.DisablePosterization();
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
            if (chatMessage.ToLower() == "/sponge help")
            {
                __instance.AddChatMessage("'/sponge': run Sponge.\n" +
                    "'/sponge evaluate': run Sponge Evaluation only.\n" +
                    "'/sponge clean': run Sponge Cleanup only.\n" +
                    "'/sponge toggle': toggle Sponge daily auto activate.\n");
                return false;
            }

            if (chatMessage.ToLower() == "/sponge")
            {
                __instance.AddChatMessage("Applying Sponge.");
                SpongeService.ApplySponge();
                return false;
            }

            if (chatMessage.ToLower() == "/sponge evaluate")
            {
                __instance.AddChatMessage("Applying Sponge evaluate.");
                SpongeService.ApplySponge(SpongeMode.Evaluate);
                return false;
            }

            if (chatMessage.ToLower() == "/sponge clean")
            {
                __instance.AddChatMessage("Applying Sponge cleanup.");
                SpongeService.ApplySponge(SpongeMode.Clean);
                return false;
            }

            if (chatMessage.ToLower() == "/sponge toggle")
            {
                Plugin.Log.LogMessage((SpongeService.enabled ? "Disabling" : "Enabling") + " Sponge daily automatic activation.");
                __instance.AddChatMessage((SpongeService.enabled ? "Disabling" : "Enabling") + " Sponge daily automatic activation.");
                SpongeService.enabled = !SpongeService.enabled;
                return false;
            }

            return true;
        }
    }
}