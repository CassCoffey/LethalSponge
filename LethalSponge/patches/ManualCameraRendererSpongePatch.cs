using GameNetcodeStuff;
using HarmonyLib;
using Scoops.service;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scoops.patches
{
    [HarmonyPatch(typeof(ManualCameraRenderer))]
    public class ManualCameraRendererSpongePatch
    {
        [HarmonyPatch("MeetsCameraEnabledConditions")]
        [HarmonyPrefix]
        private static bool ManualCameraRenderer_MeetsCameraEnabledConditions(ref ManualCameraRenderer __instance, ref bool __result, PlayerControllerB player)
        {
            if (__instance.currentCameraDisabled)
            {
                __result = false;
                return false;
            }
            if (__instance.mesh != null && player != null && !MeshVisible(player.gameplayCamera, __instance.mesh))
            {
                __result = false;
                return false;
            }
            if (!StartOfRound.Instance.inShipPhase && (!player.isInHangarShipRoom || (!StartOfRound.Instance.shipDoorsEnabled && (StartOfRound.Instance.currentPlanetPrefab == null || !StartOfRound.Instance.currentPlanetPrefab.activeSelf))))
            {
                __result = false;
                return false;
            }

            __result = true;
            return false;
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
