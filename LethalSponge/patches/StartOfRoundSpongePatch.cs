using HarmonyLib;
using Scoops.service;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

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

            if (Config.changeLightFadeDistance.Value || Config.volumetricCompensation.Value)
            {
                LightService.UpdateAllLights();
            }

            if (Config.fixInputActions.Value)
            {
                InputActionSpongePatches.Enable();
            }

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

            //if (Config.resizeTextures.Value)
            //{
            //    TextureService.ResizeAllTextures();
            //}
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
                __instance.AddChatMessage("'/sponge': Run Sponge.\n" +
                    "'/sponge evaluate': Run Sponge Evaluation only.\n" +
                    "'/sponge clean': Run Sponge Cleanup only.\n" +
                    "'/sponge toggle': Toggle Sponge daily auto activate.\n" +
                    "'/sponge modelcheck': Ask Sponge for a readout of the meshes currently rendering.\n" +
                    "'/sponge texturecheck': Ask Sponge for a readout of the textures currently rendering.\n" +
                    ((Config.useCustomShader.Value || Config.useLegacyCustomShader.Value) ? "'/sponge shader': Toggle between Sponge custom shader and original LC shader.\n" : ""));
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

            if (chatMessage.ToLower() == "/sponge modelcheck")
            {
                __instance.AddChatMessage("Running Sponge model check.");
                SpongeService.ModelCheck();
                return false;
            }

            if (chatMessage.ToLower() == "/sponge texturecheck")
            {
                __instance.AddChatMessage("Running Sponge texture check.");
                SpongeService.TextureCheck();
                return false;
            }

            if (chatMessage.ToLower() == "/sponge shader")
            {
                if (Config.useCustomShader.Value || Config.useLegacyCustomShader.Value)
                {
                    __instance.AddChatMessage("Toggling Sponge custom shader.");
                    CameraService.TogglePasses();
                    return false;
                }
            }

            if (chatMessage.ToLower() == "/sponge resize")
            {
                if (Config.useCustomShader.Value || Config.useLegacyCustomShader.Value)
                {
                    TextureService.ResizeAllTextures();
                    AudioService.DedupeAllAudio();
                    ShaderService.DedupeAllShaders();
                    MeshService.DedupeAllMeshes();
                    return false;
                }
            }

            return true;
        }
    }
}