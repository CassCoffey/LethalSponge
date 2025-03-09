using GameNetcodeStuff;
using HarmonyLib;
using Scoops.service;
using Sponge.Utilities.IL;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Unity.Netcode;
using UnityEngine;

namespace Scoops.patches
{
    [HarmonyPatch(typeof(ManualCameraRenderer))]
    public class ManualCameraRendererSpongePatch
    {
        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> ManualCameraRenderer_Update_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // Looking for 'if (this.renderAtLowerFramerate)' 
            var injector = new ILInjector(instructions)
            .Find([
                ILMatcher.Ldarg(0),
                ILMatcher.Ldfld(AccessTools.Field(typeof(ManualCameraRenderer), nameof(ManualCameraRenderer.renderAtLowerFramerate))),
                ILMatcher.Opcode(OpCodes.Brfalse)
            ]);
            
            if (!injector.IsValid)
            {
                Plugin.Log.LogError("Failed to find renderAtLowerFramerate branch in ManualCameraRenderer.Update");
                return instructions;
            }
            
            // Remove the block inside the if statement and replace with a static call
            var label = (Label)injector.LastMatchedInstruction.operand;
            return injector
                .GoToLastMatchedInstruction()
                .FindLabel(label)
                .RemoveLastMatch()
                .InsertInPlace([
                    new CodeInstruction(OpCodes.Ldarg_0),
                    new CodeInstruction(OpCodes.Call, typeof(ManualCameraRendererSpongePatch).GetMethod(nameof(ManualCameraRendererSpongePatch.ApplyFramerateCap))),
                    new CodeInstruction(OpCodes.Ret)
                    ])
                .ReleaseInstructions();
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        private static void ManualCameraRenderer_Update(ref ManualCameraRenderer __instance)
        {
            if (GameNetworkManager.Instance.localPlayerController == null || NetworkManager.Singleton == null)
            {
                return;
            }
            // While the camera is overridden it runs at full framerate, we need to stop that
            if (__instance.overrideCameraForOtherUse)
            {
                PlayerControllerB player = GameNetworkManager.Instance.localPlayerController;
                Camera currentCamera = player.isPlayerDead ? StartOfRound.Instance.spectateCamera : player.gameplayCamera;

                if (__instance.mesh != null && !MeshVisible(currentCamera, __instance.mesh))
                {
                    __instance.cam.enabled = false;
                    return;
                }

                // Just gonna redo this for now, might make it a transpiler later
                if (__instance.renderAtLowerFramerate)
                {
                    ApplyFramerateCap(__instance);
                }
                else
                {
                    __instance.cam.enabled = true;
                }
            }
        }

        [HarmonyPatch("MeetsCameraEnabledConditions")]
        [HarmonyAfter(["Zaggy1024.OpenBodyCams"])]
        [HarmonyPostfix]
        private static void ManualCameraRenderer_MeetsCameraEnabledConditions(ref ManualCameraRenderer __instance, ref bool __result, PlayerControllerB player)
        {
            Camera currentCamera = player.isPlayerDead ? StartOfRound.Instance.spectateCamera : player.gameplayCamera;

            // Recheck the mesh visibility but with a working check 
            if (__instance.mesh != null && !MeshVisible(currentCamera, __instance.mesh))
            {
                __result = false;

                if (__instance.cam == CameraService.SecurityCamera && CameraService.DoorMonitor != null && MeshVisible(currentCamera, CameraService.DoorMonitor))
                {
                    __result = true;
                }
            }

            if (__instance == StartOfRound.Instance.mapScreen)
            {
                if (__result || CameraService.MainTerminal == null) return;

                if (CameraService.MainTerminal.displayingPersistentImage == __instance.mapCamera.activeTexture && CameraService.MainTerminal.terminalUIScreen.isActiveAndEnabled)
                {
                    __result = true;
                }
            }
        }

        private static bool MeshVisible(Camera camera, MeshRenderer mesh)
        {
            Plane[] frustum = GeometryUtility.CalculateFrustumPlanes(camera);

            if (mesh.GetComponent<Collider>())
            {
                return GeometryUtility.TestPlanesAABB(frustum, mesh.GetComponent<Collider>().bounds);
            }
            else if (mesh.GetComponent<Renderer>())
            {
                return GeometryUtility.TestPlanesAABB(frustum, mesh.GetComponent<Renderer>().bounds);
            }
            else
            {
                return !frustum.Any(plane => plane.GetDistanceToPoint(mesh.transform.position) < 0);
            }
        }

        // Thanks again Zaggy1024 for letting me know about camera.enabled being better than camera.render for HDRP
        public static void ApplyFramerateCap(ManualCameraRenderer manualCameraRenderer)
        {
            manualCameraRenderer.cam.enabled = false;
            manualCameraRenderer.elapsed += Time.deltaTime;
            if (manualCameraRenderer.elapsed > 1f / manualCameraRenderer.fps)
            {
                manualCameraRenderer.elapsed = 0f;
                manualCameraRenderer.cam.enabled = true;
            }
        }
    }
}
