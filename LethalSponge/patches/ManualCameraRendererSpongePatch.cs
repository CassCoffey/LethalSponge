using GameNetcodeStuff;
using HarmonyLib;
using Scoops.service;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace Scoops.patches
{
    [HarmonyPatch(typeof(ManualCameraRenderer))]
    public class ManualCameraRendererSpongePatch
    {
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
                if (__instance.mesh != null && !MeshVisible(GameNetworkManager.Instance.localPlayerController.gameplayCamera, __instance.mesh))
                {
                    __instance.cam.enabled = false;
                    return;
                }

                // Just gonna redo this for now, might make it a transpiler later
                if (__instance.renderAtLowerFramerate)
                {
                    __instance.cam.enabled = false;
                    __instance.elapsed += Time.deltaTime;
                    if (__instance.elapsed > 1f / __instance.fps)
                    {
                        __instance.elapsed = 0f;
                        __instance.cam.Render();
                    }
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
            // Recheck the mesh visibility but with a working check 
            if (__instance.mesh != null && !MeshVisible(player.gameplayCamera, __instance.mesh))
            {
                __result = false;
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
    }
}
